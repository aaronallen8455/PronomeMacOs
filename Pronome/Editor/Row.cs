using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;
using Pronome.Mac.Editor.Groups;
using System.Text;

namespace Pronome.Mac.Editor
{
	public class Row
	{
		/// <summary>
		/// Layer that this row is based on
		/// </summary>
		public Layer Layer;

		/// <summary>
		/// True if the BeatCode field represents the current state of the row.
		/// </summary>
		public bool BeatCodeIsCurrent = true;

		protected string _beatCode;
		/// <summary>
		/// A beat code representation of the row. Must be manually updated.
		/// </summary>
		public string BeatCode
		{
			get => _beatCode;
			set
			{
				BeatCodeIsCurrent = true;
				_beatCode = value;
			}
		}

		/// <summary>
		/// All the cells in this row, including referenced cells
		/// </summary>
        public CellTree Cells; //= new CellList();

		protected double _offset;
		/// <summary>
		/// Amount of offset in BPM.
		/// </summary>
		public double Offset
		{
			get => _offset;
			set
			{
				if (value >= 0)
				{
					//double off = value * EditorWindow.Scale * EditorWindow.BaseFactor;
					//Canvas.Margin = new System.Windows.Thickness(off, 0, 0, 0);
					// reposition background
					_offset = value;
					//if (Background != null)
					//{
					//	// repostion the background
					//	SetBackground(Duration);
					//}
				}
			}
		}

		/// <summary>
		/// The UI friendly string version of the offset value
		/// </summary>
		public string OffsetValue;

		/// <summary>
		/// Total BPM length of the row
		/// </summary>
		public double Duration;

		/// <summary>
		/// All the mult groups in this row
		/// </summary>
        public LinkedList<Multiply> MultGroups = new LinkedList<Multiply>();

		/// <summary>
		/// All Repeat groups in this layer
		/// </summary>
		public LinkedList<Repeat> RepeatGroups = new LinkedList<Repeat>();

		/// <summary>
		/// Layers indexes that are referenced in this row. 0 based.
		/// </summary>
		public HashSet<int> ReferencedLayers = new HashSet<int>();

		/// <summary>
		/// Maps the index of a row to the indexes of the rows that reference it.
		/// </summary>
        public static Dictionary<int, HashSet<int>> ReferenceMap = new Dictionary<int, HashSet<int>>();

        /// <summary>
        /// The positions and durations of all references. Used for hit detection
        /// </summary>
        public List<(double position, double duration)> ReferencePositionAndDurations = new List<(double, double)>();

		/// <summary>
		/// The index of this row
		/// </summary>
		public int Index;

		public Row(Layer layer)
		{
			Layer = layer;
            Index = Metronome.Instance.Layers.IndexOf(Layer);
			touchedRefs.Add(Index); // current layer ref should recurse only once

            Offset = layer.OffsetBpm;
            OffsetValue = layer.ParsedOffset;

			RepeatGroups.Clear();
			MultGroups.Clear();
			ReferencedLayers.Clear();

			FillFromBeatCode(layer.ParsedString);
		}

		/// <summary>
		/// Generate the UI from a beat code string. Sets the BeatCode to the input string.
		/// </summary>
		/// <param name="beatCode"></param>
		public void FillFromBeatCode(string beatCode)
		{
            // track current multiply factor through entire process (including references)
            OpenMultFactor = new Stack<double>();
            OpenMultFactor.Push(1);

            CellIndex = 0;
            //double pos;
			(Cells, Duration) = ParseBeat(beatCode);
			//Cells = result.Cells;
			// set the new beatcode string
			BeatCode = beatCode;
			BeatCodeIsCurrent = true;
		}

        /// <summary>
        /// Reparses to represent current state of cell objects.
        /// </summary>
        public void Reparse()
        {
            string newCode = Stringify();

            FillFromBeatCode(newCode);
        }

		/// <summary>
		/// Used in the ParseBeat method to track the currently open, nested repeat groups
		/// </summary>
        Stack<Repeat> OpenRepeatGroups = new Stack<Repeat>();

        /// <summary>
        /// Used to tack the aggregate multiplication factor.
        /// </summary>
        Stack<double> OpenMultFactor;

        int CellIndex;

		/// <summary>
		/// Build the cell and group objects based on layer.
		/// </summary>
		/// <param name="beat"></param>
		/// <returns></returns>
        protected (CellTree cells, double position) ParseBeat(string beat)
		{
            // cells are stored in red black tree
            CellTree cells = new CellTree();

			//string[] chunks = beat.Split(new char[] { ',', '|' }, StringSplitOptions.RemoveEmptyEntries);
            Stack<Multiply> OpenMultGroups = new Stack<Multiply>();
            // holds the current mult factor
            //Stack<double> OpenMultFactor = new Stack<double>();
            //OpenMultFactor.Push(1);

            // build list of mult group factors ahead of time
            // this is so we already know the factor when the mult group gets created (factors are at end of group).
            List<string> MultFactors = new List<string>();
            Stack<int> mIndexStack = new Stack<int>();
            int mIndex = 0;
            foreach (Match m in Regex.Matches(beat, @"([\{\}])([\d+\-*/.]*)"))
            {
                if (m.Groups[1].Value == "{")
                {
                    MultFactors.Add("");
                    mIndexStack.Push(mIndex);
                    mIndex++;
                }
                else
                {
                    MultFactors[mIndexStack.Pop()] = m.Groups[2].Value;
                }
            }
            mIndex = 0;

			// BPM value
			double position = 0;// Offset;

			// remove comments
			beat = Regex.Replace(beat, @"!.*?!", "");
			// remove whitespace
			beat = Regex.Replace(beat, @"\s", "");
			// switch single cell repeats to bracket notation
            beat = Regex.Replace(beat, @"(?<=^|\[|\{|,|\|)([\[\{]*)([^\]\}\,\|]*?)(?=\(\d+\))", "$1[$2]");

            // split the string into cells
            //foreach (string chunk in chunks)
			foreach (Match match in Regex.Matches(beat, @".+?([,|]|$)"))
			{
                Cell cell = new Cell(this) { Position = position };

				string chunk = match.Value;

                // add all rep and mult groups in order
                int repIndex = chunk.IndexOf('[');
                int multIndex = chunk.IndexOf('{');

                if (repIndex == -1 && OpenRepeatGroups.Any())
                {
					OpenRepeatGroups.Peek().Cells.AddLast(cell);
                }
                if (multIndex == -1 && OpenMultGroups.Any())
                {
					OpenMultGroups.Peek().Cells.AddLast(cell);
                }

                while (multIndex != -1 || repIndex != -1)
                {
					// check for opening repeat group
                    if (repIndex != -1 && (multIndex == -1 || repIndex < multIndex))
					{
						OpenRepeatGroups.Push(new Repeat() { Row = this });
						OpenRepeatGroups.Peek().Cells.AddLast(cell);
						// need to subtract repeat groups offset because contents is in new CGLayer
                        OpenRepeatGroups.Peek().Position = position - OpenRepeatGroups.Select(x => x.Position).Sum();

                        chunk = chunk.Remove(repIndex, 1);

                        cell.GroupActions.AddLast((true, OpenRepeatGroups.Peek()));
					}
					else if (multIndex != -1)
					{
						OpenMultGroups.Push(new Multiply() 
                        { 
                            Row = this, 
                            FactorValue = MultFactors[mIndex],
                            Factor = BeatCell.Parse(MultFactors[mIndex++])
                        });//, FactorValue = factor, Factor = BeatCell.Parse(factor) });
						OpenMultGroups.Peek().Cells.AddLast(cell);
						// need to subtract repeat groups offset because contents is in new CGLayer starting at 0
						OpenMultGroups.Peek().Position = position - OpenRepeatGroups.Select(x => x.Position).Sum();

                        OpenMultFactor.Push(OpenMultFactor.Peek() * OpenMultGroups.Peek().Factor);

						chunk = chunk.Remove(multIndex, 1);

                        cell.GroupActions.AddLast((true, OpenMultGroups.Peek()));
					}
					
					repIndex = chunk.IndexOf('[');
					multIndex = chunk.IndexOf('{');
                }
				
                cell.RepeatGroups = new LinkedList<Repeat>(OpenRepeatGroups);
                cell.MultGroups = new LinkedList<Multiply>(OpenMultGroups);

				// parse the BPM value or get reference
				if (chunk.IndexOf('$') > -1)
				{
					// get reference
					string r = Regex.Match(chunk, @"((?<=\$)\d+|s)").Value;
					// validate the ref index
					if (char.IsNumber(r, 0))
					{
                        if (Metronome.Instance.Layers.Count < int.Parse(r))
						{
							r = "1";
						}
					}
					cell.Reference = r;
					// need to parse the reference
					int refIndex;
					if (cell.Reference == "s")
					{
						// self reference
                        refIndex = Metronome.Instance.Layers.IndexOf(Layer);
					}
					else
					{
						refIndex = int.Parse(cell.Reference) - 1;
					}


                    // todo: factor in mult groups here

                    (CellTree pbCells, double duration) = ResolveReference(refIndex, position);
                    // remember the position and duration of the reference
                    ReferencePositionAndDurations.Add((position, duration));

					// progress position
                    position += duration * OpenMultFactor.Peek();
					//cell.SetDurationDirectly(duration);
                    bool first = true;
					foreach (Cell c in pbCells)
					{
                        if (first)
                        {
                            c.SetDurationDirectly(duration);
                            c.Reference = cell.Reference;
                            c.IsReference = false;
                            first = false;
                        }
                        cells.Insert(c);
					}
				}
				else
				{
                    cell.Index = CellIndex++;

                    cells.Insert(cell); // can't put referencers in b/c of overlap
					// get bpm value
					string bpm = Regex.Match(chunk, @"[\d./+*\-]+").Value;
					if (!string.IsNullOrEmpty(bpm))
					{
						cell.Value = bpm;
                        cell.SetDurationDirectly(BeatCell.Parse(bpm) * OpenMultFactor.Peek());
						// progress position
                        position += cell.Duration;
					}
				}

				// check for source modifier
				if (chunk.IndexOf('@') > -1)
				{
					string sourceCode = Regex.Match(chunk, @"(?<=@)([pP]\d*\.?\d*|u?\d+|[a-gA-G][b#]?\d+)").Value;

                    StreamInfoProvider source = StreamInfoProvider.GetFromModifier(sourceCode);

					cell.Source = source;
				}

				//bool addedToRepCanvas = false;

                // close groups
				multIndex = chunk.IndexOf('}');
				repIndex = chunk.IndexOf(']');
                while (multIndex != -1 || repIndex != -1)
                {
                    // create the mult and rep groups in the correct order

					if (multIndex > -1 && (multIndex < repIndex || repIndex == -1))
					{
                        // close mult group
                        OpenMultFactor.Pop();
                        Multiply mg = OpenMultGroups.Pop();
						//mg.FactorValue = Regex.Match(chunk, @"(?<=})[\d.+\-/*]+").Value;
						//mg.Factor = BeatCell.Parse(mg.FactorValue);
						// set duration
                        mg.Length = position - mg.Position - OpenRepeatGroups.Select(x => x.Position).Sum();
                        mg.Length *= OpenMultFactor.Peek();

						var m = Regex.Match(chunk, @"\}[\d.+\-/*]+");

						chunk = chunk.Remove(m.Index, m.Length);

						MultGroups.AddLast(mg);

                        // log group end
                        cell.GroupActions.AddLast((false, mg));
					}
					else if (repIndex > -1)
					{
						// close rep group

                        Repeat rg = OpenRepeatGroups.Pop();
                        rg.Length = position - rg.Position - OpenRepeatGroups.Select(x => x.Position).Sum();
						Match mtch = Regex.Match(chunk, @"](\d+)");
						if (mtch.Length == 0)
						{
							mtch = Regex.Match(chunk, @"]\((\d+)\)([\d+\-/*.]*)");
							rg.Times = int.Parse(mtch.Groups[1].Value);
							if (mtch.Groups[2].Length != 0)
							{
								rg.LastTermModifier = mtch.Groups[2].Value;
							}
						}
						else
						{
							rg.Times = int.Parse(mtch.Groups[1].Value);
						}

                        RepeatGroups.AddLast(rg);

                        cell.GroupActions.AddLast((false, rg));

						// build the group
                        position = BuildRepeatGroup(rg, OpenMultFactor.Peek(), position);

						//addedToRepCanvas = true;
						// move to outer group if exists
						chunk = chunk.Substring(chunk.IndexOf(']') + 1);
					}

					multIndex = chunk.IndexOf('}');
					repIndex = chunk.IndexOf(']');
				}

				// check if its a break, |
				if (chunk.Last() == '|')
				{
					cell.IsBreak = true;
				}
			}

            return (cells, position);
		}

		protected struct ParsedBeatResult
		{
			public CellTree Cells;
			public double Duration;
			public ParsedBeatResult(CellTree cells, double duration)
			{
				Cells = cells;
				Duration = duration;
			}
		}

		private HashSet<int> touchedRefs = new HashSet<int>();

        protected (CellTree cells, double position) ResolveReference(int refIndex, double position)
		{
			// get beat code from the layer, or from the row if available
			string beat;
            if (DrawingView.Instance.Rows.Length > refIndex)
			{
                beat = DrawingView.Instance.Rows[refIndex].BeatCode;
			}
			else
			{
                beat = Metronome.Instance.Layers[refIndex].ParsedString;
			}
			// remove comments
			beat = Regex.Replace(beat, @"!.*?!", "");
			// remove whitespace
			beat = Regex.Replace(beat, @"\s", "");
			// convert self references
			beat = Regex.Replace(beat, @"(?<=\$)[sS]", (refIndex + 1).ToString());
			var matches = Regex.Matches(beat, @"(?<=\$)\d+");
			foreach (Match match in matches)
			{
				int ind;
				
				int.TryParse(match.Value, out ind);
				if (touchedRefs.Contains(refIndex))
				{
					// remove refs that have been touched
					// remove closest nest
					if (Regex.IsMatch(beat, @"[[{][^[{\]}]*\$" + ind.ToString() + @"[^[{\]}]*[\]}][^\]},]*"))
					{
						beat = Regex.Replace(beat, @"[[{][^[{\]}]*\$" + ind.ToString() + @"[^[{\]}]*[\]}][^\]},]*", "");
					}
					else
					{
						// no nest
						beat = Regex.Replace(beat, $@"\${ind},?", "");
					}

					// get rid of empty single cell repeats.
					beat = Regex.Replace(beat, @"(?<!\]|\d)\(\d+\)[\d.+\-/*]*", "");
					// clean out empty cells
					beat = Regex.Replace(beat, @",,", ",");

					beat = beat.Trim(',');
				}
			}

			touchedRefs.Add(refIndex);

			// recurse
            (CellTree cells, double pbPosition) = ParseBeat(beat);

            HashSet<AbstractGroup> touchedGroups = new HashSet<AbstractGroup>();
			// mark the cells as refs
			foreach (Cell c in cells)
			{
				c.IsReference = true;
				c.Position += position;
				
				// reposition groups
                foreach (Repeat rg in c.RepeatGroups)
				{
					// only reposition groups that were created within the reference
					if (OpenRepeatGroups.Count > 0 && OpenRepeatGroups.Peek() == rg) break;
					// only reposition each group once
					if (touchedGroups.Contains(rg)) continue;
					touchedGroups.Add(rg);
					rg.Position += position;
				}
                foreach (Multiply mg in c.MultGroups)
				{
					if (touchedGroups.Contains(mg)) continue;
					touchedGroups.Add(mg);
					mg.Position += position;
				}
			}

			// no longer block this refIndex
			if (refIndex != Index)
			{
				touchedRefs.Remove(refIndex);
			}

			ReferencedLayers.Add(refIndex);
			// map referenced layer to this one
			if (ReferenceMap.ContainsKey(refIndex))
			{
				ReferenceMap[refIndex].Add(Index);
			}
			else
			{
				ReferenceMap.Add(refIndex, new HashSet<int>(new int[] { Index }));
			}

            return (cells, pbPosition);
		}

		/// <summary>
		/// Redraw the editor to reflect the internal state
		/// </summary>
		public void Redraw()
		{
			string code = Stringify();
			FillFromBeatCode(code);
			Offset = BeatCell.Parse(OffsetValue);
		}

		/// <summary>
		/// Update the beat code for this row
		/// </summary>
		public string UpdateBeatCode()
		{
			BeatCode = Stringify();
			BeatCodeIsCurrent = true;
			return BeatCode;
		}

		/// <summary>
		/// Outputs the string representation of the beat layer from the editor.
		/// </summary>
		/// <returns></returns>
		public string Stringify()
		{
			StringBuilder result = new StringBuilder();

			foreach (Cell cell in Cells)
			{
                if (cell.IsReference) continue;

                bool innersAdded = false;

                foreach ((bool begun, AbstractGroup group) in cell.GroupActions)
                {
                    if (!begun && !innersAdded)
                    {
                        // add inner components
                        StringifyInnerComponents(result, cell);

                        innersAdded = true;
                    }

                    if (begun)
                    {
                        if (group.GetType() == typeof(Repeat))
                        {
                            // open repeat group
                            // is single cell?
                            if (((Repeat)group).Cells.Count != 1)
                            {
                                result.Append('[');
                            }
                        }
                        else
                        {
                            // open mult group
                            result.Append('{');
                        }
                    }
                    else
                    {
                        if (group.GetType() == typeof(Repeat))
                        {
                            var rg = group as Repeat;
                            // close repeat group
                            if (rg.Cells.Count != 1)
                            {
                                result.Append(']');
                                // multi cell
                                if (!string.IsNullOrEmpty(rg.LastTermModifier))
                                {
                                    result.Append($"({rg.Times.ToString()}){rg.LastTermModifier})");
                                }
                                else
                                {
                                    result.Append($"{rg.Times.ToString()}");
                                }
                            }
                            else
                            {
                                // single cell
                                result.Append($"({rg.Times.ToString()})");
                            }
                        }
                        else
                        {
                            // close mult group
                            result.Append('}');
                            result.Append((group as Multiply).FactorValue);
                        }
                    }
                }

                if (!innersAdded)
                {
                    StringifyInnerComponents(result, cell);
                }

				//// check for open mult group
                //foreach (Multiply mg in cell.MultGroups)
				//{
				//	if (mg.Cells.First.Value == cell)
				//	{
				//		//OpenMultGroups.Push(cell.MultGroup);
				//		result.Append('{');
				//	}
				//}
				//// check for open repeat group
                //foreach (Repeat rg in cell.RepeatGroups)
				//{
				//	if (rg.Cells.First.Value == cell && rg.Cells.Where(x => !x.IsReference).Count() > 1)
				//	{
				//		//OpenRepeatGroups.Push(cell.RepeatGroup);
				//		result.Append('[');
				//	}
				//}
				// get duration or reference ID
				//if (string.IsNullOrEmpty(cell.Reference))
				//{
				//	result.Append(cell.Value);
				//}
				//else
				//{
				//	result.Append($"${cell.Reference}");
				//}
				//// check for source modifier
				//if (cell.Source != null && cell.Source.Uri != Layer.BaseSourceName)
				//{
				//	string source;
				//	// is pitch or wav?
				//	if (cell.Source.IsPitch)
				//	{
				//		source = cell.Source.Uri;
				//	}
				//	else
				//	{
                //        if (!cell.Source.IsInternal)
				//		{
				//			source = cell.Source.Index.ToString();
				//		}
				//		else
				//		{
				//			source = cell.Source.Index.ToString();
				//		}
				//	}
				//	result.Append($"@{source}");
				//}
				//// check for close repeat group
                //foreach (Repeat rg in cell.RepeatGroups)
				//{
				//	Cell[] cells = rg.Cells.Where(x => !x.IsReference).ToArray();
				//	if (cells.Last() == cell)
				//	{
				//		// is single cell rep?
				//		if (cells.Length == 1)
				//		{
				//			result.Append($"({rg.Times})");
				//			if (!string.IsNullOrEmpty(rg.LastTermModifier))
				//			{
				//				result.Append(rg.LastTermModifier);
				//			}
				//		}
				//		else
				//		{
				//			// multi cell
				//			if (!string.IsNullOrEmpty(rg.LastTermModifier))
				//			{
				//				result.Append($"]({rg.Times}){rg.LastTermModifier}");
				//			}
				//			else
				//			{
				//				result.Append($"]{rg.Times}");
				//			}
				//		}
				//	}
				//}
				//// check for close mult group
                //foreach (Multiply mg in cell.MultGroups)
				//{
				//	if (mg.Cells.Last.Value == cell)
				//	{
				//		result.Append($"}}{mg.FactorValue}");
				//	}
				//}
				// check if is break point |
				if (cell.IsBreak)
				{
					result.Append('|');
				}
				else
				{
					result.Append(',');
				}
			}

			return result.ToString().TrimEnd(',');
		}

        /// <summary>
        /// Stringifies the inner components - value, source mod, reference.
        /// </summary>
        /// <param name="result">Result.</param>
        /// <param name="cell">Cell.</param>
        private void StringifyInnerComponents(StringBuilder result, Cell cell)
        {
            if (string.IsNullOrEmpty(cell.Reference))
            {
                result.Append(cell.Value);
            }
            else
            {
                result.Append($"${cell.Reference}");
            }
            // check for source modifier
            if (cell.Source != null && cell.Source.Uri != Layer.BaseSourceName)
            {
                string source;
                // is pitch or wav?
                if (cell.Source.IsPitch)
                {
                    source = cell.Source.Uri;
                }
                else
                {
                    if (!cell.Source.IsInternal)
                    {
                        source = cell.Source.Index.ToString();
                    }
                    else
                    {
                        source = cell.Source.Index.ToString();
                    }
                }
                result.Append($"@{source}");
            }
        }

        /// <summary>
        /// Perform all graphical tasks with initializing a repeat group. Group must have and Times, LastTermMod, Postion, Duration already set.
        /// </summary>
        /// <param name="rg"></param>
        /// <returns></returns>
        protected double BuildRepeatGroup(Repeat rg, double multGroupsFactor, double position)
		{
			//double position = 0;
			RepeatGroups.AddLast(rg);

			for (int i = 0; i < rg.Times - 1; i++)
			{
				// move position forward
                position += rg.Length;
			}

            position += BeatCell.Parse(rg.LastTermModifier) * multGroupsFactor; //* EditorWindow.Scale * EditorWindow.BaseFactor;

			return position;
		}

		/// <summary>
		/// Apply current state to the background element and position the sizer.
		/// </summary>
		/// <param name="widthBpm"></param>
		protected void SetBackground(double widthBpm)
		{
			Duration = widthBpm;
			
		}
	}
}
