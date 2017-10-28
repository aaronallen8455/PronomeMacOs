using System.Collections.Generic;
using Pronome.Mac.Editor.Groups;
using System.Linq;
using System.Text;

namespace Pronome.Mac.Editor
{
	public class Cell
	{
		public Row Row;

		protected double _duration;
		/// <summary>
		/// The cell's value in BPM
		/// </summary>
		public double Duration
		{
			get => _duration;
			set
			{
				//HashSet<RepeatGroup> touchedRepGroups = new HashSet<RepeatGroup>();
				//HashSet<MultGroup> touchedMultGroups = new HashSet<MultGroup>();
                HashSet<AbstractGroup> touchedGroups = new HashSet<AbstractGroup>();
				double diff = value - _duration;
				_duration = value;
				// resize groups of which this cell is a part
                foreach (Repeat rg in RepeatGroups)
				{
					touchedGroups.Add(rg);
                    rg.Length += diff;
				}
                foreach (Multiply mg in MultGroups)
				{
					touchedGroups.Add(mg);
                    mg.Length += diff;
				}
			}
		}

		protected double _actualDuration = -1;
		/// <summary>
		/// Get the duration of the cell with multiplication groups applied
		/// </summary>
		public double ActualDuration
		{
			get => Duration;
		}

		protected string _value;
		/// <summary>
		/// The string representation of the duration. ie 1+2/3
		/// </summary>
		public string Value
		{
			get => _value;
			set
			{
				_value = value;
			}
		}

		protected double _position;
		/// <summary>
		/// The horizontal position of the cell in BPM. Changes actual position when set.
		/// </summary>
		public double Position
		{
			get => _position;
			set
			{
				_position = value;
				//Canvas.SetLeft(Rectangle, value * EditorWindow.Scale * EditorWindow.BaseFactor);

			}
		}

        /// <summary>
        /// Index of this cell within it's row.
        /// </summary>
        public int Index;

		public bool IsSelected = false;

		/// <summary>
		/// Whether cell is a break point for a loop - |
		/// </summary>
		public bool IsBreak = false;

		/// <summary>
		/// The index of the layer that this cell is a reference for. Null if it's a regular cell.
		/// </summary>
		public string Reference;
		
		/// <summary>
		/// The audio source for this cell.
		/// </summary>
        public StreamInfoProvider Source = null;

		/// <summary>
		/// Multiplication groups that this cell is a part of
		/// </summary>
        public LinkedList<Multiply> MultGroups = new LinkedList<Multiply>();

		/// <summary>
		/// Repeat groups that this cell is part of
		/// </summary>
		public LinkedList<Repeat> RepeatGroups = new LinkedList<Repeat>();

        /// <summary>
        /// The group actions that occur at this cell. First part of tuple is true if group was begun, false if ended.
        /// </summary>
        public LinkedList<(bool, AbstractGroup)> GroupActions = new LinkedList<(bool, AbstractGroup)>();

		/// <summary>
		/// Is this cell part of a reference. Should not be manipulable if so
		/// </summary>
		public bool IsReference = false;

        /// <summary>
        /// Aggregation of the mult factors for this cell from mult groups it's a member of
        /// </summary>
        public string MultFactor = "1";

        protected string MultipliedValue;

		public Cell(Row row)
		{
			Row = row;
		}

		/// <summary>
		/// Assign a new duration with altering the UI
		/// </summary>
		/// <param name="duration"></param>
		public void SetDurationDirectly(double duration)
		{
			_duration = duration;
			//_actualDuration = -1; // reevaluate actual duration
		}

        /// <summary>
        /// Gets the string value with mult factors applied.
        /// </summary>
        /// <returns>The value with mult factors.</returns>
        public string GetValueWithMultFactors()
        {
            // don't operate if scaling is disabled
            if (!UserSettings.GetSettings().DrawMultToScale) return Value;

            if (string.IsNullOrEmpty(MultipliedValue))
            {
                MultipliedValue = BeatCell.MultiplyTerms(Value, MultFactor);
            }

            return MultipliedValue;

            //string val = Value;
			//
            //foreach (Multiply mg in MultGroups)
            //{
            //    val = BeatCell.MultiplyTerms(val, mg.FactorValue);
            //}
			//
            //return val;
        }

        /// <summary>
        /// Divides the given string value by the factors of all nested mult groups.
        /// This is used to convert a "to scale" value to the actual value.
        /// </summary>
        /// <returns>The value divided by mult factors.</returns>
        /// <param name="value">Value.</param>
        public string GetValueDividedByMultFactors(string value)
        {
            // don't operate if scaling is disabled
            if (!UserSettings.GetSettings().DrawMultToScale) return value;

            return BeatCell.DivideTerms(value, MultFactor);

            //foreach (Multiply mg in MultGroups)
            //{
            //    value = BeatCell.DivideTerms(value, mg.FactorValue);
            //}
			//
            //return value;
        }

        public void Delete()
        {
            Row.Cells.Remove(this);

            foreach (AbstractGroup grp in RepeatGroups.Concat<AbstractGroup>(MultGroups))
            {
                grp.Cells.Remove(this);
            }
        }

        static public LinkedList<Cell> DeepCopyCells(IEnumerable<Cell> cells, AbstractGroup noCopyGroup = null)
        {
            var copiedCells = new LinkedList<Cell>();
            var oldToNew = new Dictionary<AbstractGroup, AbstractGroup>();

            foreach (Cell ce in cells)
            {
                Cell copy = new Cell(null)
                {
                    Duration = ce.Duration,
                    Source = ce.Source,
                    IsBreak = ce.IsBreak,
                    Value = ce.Value
                };

                foreach (var kp in oldToNew)
                {
                    // add cell to each containing group
                    kp.Value.Cells.AddLast(copy);

                    // add the group to the cell
                    if (kp.Key is Repeat)
                    {
                        copy.RepeatGroups.AddLast((Repeat)kp.Value);
                    }
                    else if (kp.Key is Multiply)
                    {
                        copy.MultGroups.AddLast((Multiply)kp.Value);
                    }
                }

                // copy the groups
                foreach ((bool isStart, AbstractGroup oldGroup) in ce.GroupActions)
                {
                    if (oldGroup == noCopyGroup) continue;

                    if (isStart)
                    {
                        // start of group
						AbstractGroup newGroup = null;
						
                        if (oldGroup is Repeat)
						{
							newGroup = new Repeat()
							{
                                Times = ((Repeat)oldGroup).Times,
                                Length = oldGroup.Length,
                                LastTermModifier = ((Repeat)oldGroup).LastTermModifier,
                                Position = oldGroup.Position,
                                MultFactor = ((Repeat)oldGroup).MultFactor
							};
							
                            copy.RepeatGroups.AddLast((Repeat)newGroup);
						}
                        else if (oldGroup is Multiply)
						{
							newGroup = new Multiply()
							{
                                Length = oldGroup.Length,
                                Factor = (oldGroup as Multiply).Factor,
                                FactorValue = ((Multiply)oldGroup).FactorValue,
                                Position = oldGroup.Position
							};

                            copy.MultGroups.AddLast((Multiply)newGroup);
						}
						newGroup.Cells.AddLast(copy);
						
						copy.GroupActions.AddLast((true, newGroup));
						
                        oldToNew.Add(oldGroup, newGroup);
                    }
                    else
                    {
                        // end of group
                        copy.GroupActions.AddLast((false, oldToNew[oldGroup]));

                        if (oldGroup is Repeat)
                        {
                            copy.RepeatGroups.AddLast((Repeat)oldToNew[oldGroup]);
                        }
                        else if (oldGroup is Multiply)
                        {
                            copy.MultGroups.AddLast((Multiply)oldToNew[oldGroup]);
                        }

						oldToNew.Remove(oldGroup);
                    }
                }

                copiedCells.AddLast(copy);
            }

            return copiedCells;
        }
	}
}
