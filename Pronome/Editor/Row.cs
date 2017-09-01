﻿using System;
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
		public static Dictionary<int, HashSet<int>> ReferenceMap;

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

			ReferenceMap = new Dictionary<int, HashSet<int>>();

			FillFromBeatCode(layer.ParsedString);
		}

		/// <summary>
		/// Generate the UI from a beat code string. Sets the BeatCode to the input string.
		/// </summary>
		/// <param name="beatCode"></param>
		public void FillFromBeatCode(string beatCode)
		{
			ParsedBeatResult result = ParseBeat(beatCode);
			Cells = result.Cells;
			// set the new beatcode string
			BeatCode = beatCode;
			BeatCodeIsCurrent = true;
		}

		/// <summary>
		/// Used in the ParseBeat method to track the currently open, nested repeat groups
		/// </summary>
        Stack<Repeat> OpenRepeatGroups = new Stack<Repeat>();

		/// <summary>
		/// Build the cell and group objects based on layer.
		/// </summary>
		/// <param name="beat"></param>
		/// <returns></returns>
		protected ParsedBeatResult ParseBeat(string beat)
		{
            // cells are stored in red black tree
            CellTree cells = new CellTree();

			string[] chunks = beat.Split(new char[] { ',', '|' }, StringSplitOptions.RemoveEmptyEntries);
            Stack<Multiply> OpenMultGroups = new Stack<Multiply>();

			// BPM value
			double position = 0;// Offset;

			// remove comments
			beat = Regex.Replace(beat, @"!.*?!", "");
			// remove whitespace
			beat = Regex.Replace(beat, @"\s", "");
			// switch single cell repeats to bracket notation
			beat = Regex.Replace(beat, @"(?<=^|\[|\{|,|\|)([^\]\}\,\|]*?)(?=\(\d+\))", "[$1]");

			// split the string into cells
			foreach (Match match in Regex.Matches(beat, @".+?([,|]|$)"))
			{
				Cell cell = new Cell(this) { Position = position };
                cells.Insert(cell);

				string chunk = match.Value;

				// check for opening mult group
				int multInd = chunk.IndexOf('{');
				if (multInd > -1)
				{
					while (chunk.Contains('{'))
					{
                        OpenMultGroups.Push(new Multiply() { Row = this });//, FactorValue = factor, Factor = BeatCell.Parse(factor) });
                        cell.MultGroups = new LinkedList<Multiply>(OpenMultGroups);
						OpenMultGroups.Peek().Cells.AddLast(cell);
						OpenMultGroups.Peek().Position = cell.Position;

						chunk = chunk.Remove(multInd, 1);
					}
				}
				else if (OpenMultGroups.Any())
				{
                    cell.MultGroups = new LinkedList<Multiply>(OpenMultGroups);
					OpenMultGroups.Peek().Cells.AddLast(cell);
				}

				// check for opening repeat group
				if (chunk.IndexOf('[') > -1)
				{
					while (chunk.Contains('['))
					{
                        OpenRepeatGroups.Push(new Repeat() { Row = this });
                        cell.RepeatGroups = new LinkedList<Repeat>(OpenRepeatGroups);
						OpenRepeatGroups.Peek().Cells.AddLast(cell);
						OpenRepeatGroups.Peek().Position = cell.Position;

						chunk = chunk.Remove(chunk.IndexOf('['), 1);
					}
				}
				else if (OpenRepeatGroups.Any())
				{
                    cell.RepeatGroups = new LinkedList<Repeat>(OpenRepeatGroups);
					OpenRepeatGroups.Peek().Cells.AddLast(cell);
				}

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

					ParsedBeatResult pbr = ResolveReference(refIndex, position);
					// add the ref cells in
					//cells = new LinkedList<Cell>(cells.Concat(pbr.Cells));
					// progress position
					position += pbr.Duration;
					cell.SetDurationDirectly(pbr.Duration);

					foreach (Cell c in pbr.Cells)
					{
                        cells.Insert(c);
					}
				}
				else
				{
					// get bpm value
					string bpm = Regex.Match(chunk, @"[\d./+*\-]+").Value;
					if (!string.IsNullOrEmpty(bpm))
					{
						cell.Value = bpm;
						cell.SetDurationDirectly(BeatCell.Parse(bpm));
						// progress position
						position += cell.ActualDuration;
					}
				}

				// check for source modifier
				if (chunk.IndexOf('@') > -1)
				{
					string sourceCode = Regex.Match(chunk, @"(?<=@)([pP]\d*\.?\d*|u?\d+|[a-gA-G][b#]?\d+)").Value;

                    StreamInfoProvider source = StreamInfoProvider.GetFromModifier(sourceCode);

					cell.Source = source;
				}

				bool addedToRepCanvas = false;
				while (chunk.Contains('}') || chunk.Contains(']'))
				{
					// create the mult and rep groups in the correct order
					int multIndex = chunk.IndexOf('}');
					int repIndex = chunk.IndexOf(']');

					if (multIndex > -1 && (multIndex < repIndex || repIndex == -1))
					{
						// add mult group

                        Multiply mg = OpenMultGroups.Pop();
						mg.FactorValue = Regex.Match(chunk, @"(?<=})[\d.+\-/*]+").Value;
						mg.Factor = BeatCell.Parse(mg.FactorValue);
						// set duration
                        mg.Length = cell.Position + cell.ActualDuration - mg.Position;
						
						var m = Regex.Match(chunk, @"\}[\d.+\-/*]+");

						chunk = chunk.Remove(m.Index, m.Length);

						MultGroups.AddLast(mg);
					}
					else if (repIndex > -1)
					{
						// add rep group

                        Repeat rg = OpenRepeatGroups.Pop();
                        rg.Length = cell.Position + cell.ActualDuration - rg.Position;
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

						// build the group
						position = BuildRepeatGroup(cell, rg, OpenRepeatGroups, position, !addedToRepCanvas);

						addedToRepCanvas = true;
						// move to outer group if exists
						chunk = chunk.Substring(chunk.IndexOf(']') + 1);
					}
				}

				// check if its a break, |
				if (chunk.Last() == '|')
				{
					cell.IsBreak = true;
				}
			}

			return new ParsedBeatResult(cells, position);
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

		protected ParsedBeatResult ResolveReference(int refIndex, double position)
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
			var pbr = ParseBeat(beat);

            HashSet<AbstractGroup> touchedGroups = new HashSet<AbstractGroup>();
			// mark the cells as refs
			foreach (Cell c in pbr.Cells)
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

			return pbr;
		}

		/// <summary>
		/// Clear out all the data, prepare row to be rebuilt using a code string.
		/// </summary>
		public void Reset()
		{
			RepeatGroups.Clear();
			MultGroups.Clear();
			ReferencedLayers.Clear();
			Cells.Clear();
		}

		/// <summary>
		/// Redraw the editor to reflect the internal state
		/// </summary>
		public void Redraw()
		{
			string code = Stringify();
			Reset();
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

				// check for open mult group
                foreach (Multiply mg in cell.MultGroups)
				{
					if (mg.Cells.First.Value == cell)
					{
						//OpenMultGroups.Push(cell.MultGroup);
						result.Append('{');
					}
				}
				// check for open repeat group
                foreach (Repeat rg in cell.RepeatGroups)
				{
					if (rg.Cells.First.Value == cell && rg.Cells.Where(x => !x.IsReference).Count() > 1)
					{
						//OpenRepeatGroups.Push(cell.RepeatGroup);
						result.Append('[');
					}
				}
				// get duration or reference ID
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
				// check for close repeat group
                foreach (Repeat rg in cell.RepeatGroups)
				{
					Cell[] cells = rg.Cells.Where(x => !x.IsReference).ToArray();
					if (cells.Last() == cell)
					{
						// is single cell rep?
						if (cells.Length == 1)
						{
							result.Append($"({rg.Times})");
							if (!string.IsNullOrEmpty(rg.LastTermModifier))
							{
								result.Append(rg.LastTermModifier);
							}
						}
						else
						{
							// multi cell
							if (!string.IsNullOrEmpty(rg.LastTermModifier))
							{
								result.Append($"]({rg.Times}){rg.LastTermModifier}");
							}
							else
							{
								result.Append($"]{rg.Times}");
							}
						}
					}
				}
				// check for close mult group
                foreach (Multiply mg in cell.MultGroups)
				{
					if (mg.Cells.Last.Value == cell)
					{
						result.Append($"}}{mg.FactorValue}");
					}
				}
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
		/// Perform all graphical tasks with initializing a repeat group. Group must have and Times, LastTermMod, Postion, Duration already set.
		/// </summary>
		/// <param name="cell"></param>
		/// <param name="rg"></param>
		/// <param name="openRepeatGroups"></param>
		/// <returns></returns>
        protected double BuildRepeatGroup(Cell cell, Repeat rg, Stack<Repeat> openRepeatGroups, double position, bool addToCanvas = true)
		{
			//double position = 0;
			RepeatGroups.AddLast(rg);

			for (int i = 0; i < rg.Times - 1; i++)
			{
				// move position forward
                position += rg.Length;
			}

			position += BeatCell.Parse(rg.LastTermModifier); //* EditorWindow.Scale * EditorWindow.BaseFactor;

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

		/// <summary>
		/// Draw the grid lines for selected cells in this row. Also sets the FirstCell and LastCell of selection object.
		/// </summary>
		/// <param name="intervalCode"></param>
		public void DrawGridLines(string intervalCode)
		{
			double gridCellSize;
			if (BeatCell.TryParse(intervalCode, out gridCellSize))
			{
				gridCellSize *= EditorWindow.BaseFactor * EditorWindow.Scale;
				// get duration of selection and leftmost position
				double duration = 0; // BPM
				double positionBpm = double.MaxValue;
				double maxPostion = -1;
				foreach (Cell cell in Cell.SelectedCells.Cells.Where(x => !x.IsReference))
				{
					//else
					//{
					duration += cell.ActualDuration;
					//}
					// find first cell
					if (cell.Position < positionBpm)
					{
						positionBpm = cell.Position;
						Cell.SelectedCells.FirstCell = cell;
					}
					// find last cell
					if (cell.Position > maxPostion)
					{
						maxPostion = cell.Position;
						Cell.SelectedCells.LastCell = cell;
					}
				}
				if (string.IsNullOrEmpty(Cell.SelectedCells.LastCell.Reference))
				{
					// leave the duration in for references, otherwise it's zero width
					duration -= Cell.SelectedCells.LastCell.ActualDuration;
				}

			}
		}

		/// <summary>
		/// Create a new cell at the position on grid if within a certain range of a grid line
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void BaseElement_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
		{
			if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
			{
				// pass to the select box handler
				Grid_MouseDownSelectBox(sender, e);
			}
			else
			{
				if (Cell.SelectedCells.Cells.Any())
				{
					AddCell action = new AddCell(e.GetPosition((Grid)sender).X, this);

					action.Redo();

					if (action.IsValid)
					{
						EditorWindow.Instance.AddUndoAction(action);
						return;
					}
				}
			}
		}

		/// <summary>
		/// Move mouse handler while selection box is being drawn
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void Grid_MouseMoveSelectBox(object sender, System.Windows.Input.MouseEventArgs e)
		{
			if (SelectionCanvas.IsMouseCaptured)
			{
				double x = e.GetPosition(BaseElement).X;
				double y = Math.Max(
					Math.Min(e.GetPosition(BaseElement).Y, (double)EditorWindow.Instance.Resources["rowHeight"]),
					0);

				Rectangle selector = EditorWindow.Instance.Resources["boxSelect"] as Rectangle;

				// change the size and/or position of the selector based on new mouse position
				if (x < selectorOrigin.X)
				{
					Canvas.SetLeft(selector, x);
					selector.Width = selectorOrigin.X - x;
				}
				else if (x >= selectorOrigin.X)
				{
					Canvas.SetLeft(selector, selectorOrigin.X);
					selector.Width = x - selectorOrigin.X;
				}

				if (y < selectorOrigin.Y)
				{
					Canvas.SetTop(selector, y);
					selector.Height = selectorOrigin.Y - y;
				}
				else if (y >= selectorOrigin.Y)
				{
					Canvas.SetTop(selector, selectorOrigin.Y);
					selector.Height = y - selectorOrigin.Y;
				}

				// scroll the window if necessary

				double windowWidth = EditorWindow.Instance.Width - 20;
				double scrollAmount = EditorWindow.Instance.layerPanelScrollViewer.HorizontalOffset;
				// scroll right
				if (windowWidth < x - scrollAmount)
				{
					EditorWindow.Instance.layerPanelScrollViewer.ScrollToHorizontalOffset(scrollAmount + .1);
				}
				else if (x - scrollAmount < -20) // scroll left
				{
					EditorWindow.Instance.layerPanelScrollViewer.ScrollToHorizontalOffset(scrollAmount - .1);
				}
			}
		}

		/// <summary>
		/// Remove the selection box and select cells within it's range. Deselect all if no cells selected
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void Grid_MouseUpSelectBox(object sender, System.Windows.Input.MouseButtonEventArgs e)
		{
			if (SelectionCanvas.IsMouseCaptured)
			{
				SelectionCanvas.ReleaseMouseCapture();
				Rectangle selector = EditorWindow.Instance.Resources["boxSelect"] as Rectangle;

				// select all cells within the range
				double start = Math.Min(selectorOrigin.X, Canvas.GetLeft(selector)) / EditorWindow.Scale / EditorWindow.BaseFactor - Offset;
				double end = start + selector.Width / EditorWindow.Scale / EditorWindow.BaseFactor;
				IEnumerable<Cell> cells = Cells.Where(x => !x.IsReference).SkipWhile(x => x.Position < start).TakeWhile(x => x.Position < end);

				SelectionCanvas.Children.Remove(selector);

				Cell.SelectedCells.DeselectAll(false);

				if (cells.Any())
				{
					foreach (Cell cell in cells)
					{
						cell.ToggleSelect(false);
					}
				}

				EditorWindow.Instance.UpdateUiForSelectedCell();
			}
		}

		/// <summary>
		/// Start drawing the selection box
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		private void Grid_MouseDownSelectBox(object sender, System.Windows.Input.MouseButtonEventArgs e)
		{
			// unfocus any ui elements (prevents a value holding over to a group selection)
			//Keyboard.ClearFocus();
			//Keyboard.Focus(EditorWindow.Instance);
			var focused = Keyboard.FocusedElement;
			if (focused.GetType() == typeof(TextBox))
			{
				focused.RaiseEvent(new RoutedEventArgs(TextBox.LostFocusEvent));
			}

			Rectangle selector = EditorWindow.Instance.Resources["boxSelect"] as Rectangle;

			if (selector.Parent == null)
			{
				// get selection origin
				double x = e.GetPosition(BaseElement).X;
				double y = e.GetPosition(BaseElement).Y;
				selectorOrigin.X = x;
				selectorOrigin.Y = y;

				// attach the selection box to the canvas
				SelectionCanvas.CaptureMouse();
				selector.Width = 0;
				selector.Height = 0;
				Canvas.SetTop(selector, y);
				Canvas.SetLeft(selector, x);
				Canvas.SetZIndex(selector, 500);
				SelectionCanvas.Children.Add(selector);
			}
		}
	}
}