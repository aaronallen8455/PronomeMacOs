﻿using System.Text;
using System.Collections.Generic;
using Pronome.Mac.Editor.Groups;
using System.Linq;

namespace Pronome.Mac.Editor.Action
{
	public class AddCell : AbstractBeatCodeAction
	{
        /// <summary>
        /// Position of the new cell in BPM
        /// </summary>
        protected double Position;

        protected CellTreeNode FirstSelected;

        protected CellTreeNode LastSelected;

        protected int NumIntervals;

        protected bool AboveSelection;

		/// <summary>
		/// Determines how close a mouse click needs to be to a grid line to count as that line. It's a factor of the increment size.
		/// </summary>
		public const float GridProx = .15f;

		public AddCell(int div, bool aboveSelection, double position, Row row, CellTreeNode firstSelect, CellTreeNode lastSelect) : base(row, "Add Cell")
		{
            FirstSelected = firstSelect;
            LastSelected = lastSelect;
            NumIntervals = div;
            AboveSelection = aboveSelection;
            Position = position;
		}

		protected override void Transformation()
		{
            if (AboveSelection)
            {
                if (Position > Row.Cells.Max.Cell.Position + Row.Cells.Max.Cell.Duration)
                {
                    // add above row
                    AddCellAboveRow();
                }
                else
                {
                    // above selection, inside row
                    AddCellToRowAboveSelection();
                }
            }
            else
            {
                if (Position < 0)
                {
                    // in offset region
                    AddCellBelowRow();
                }
                else
                {
                    // below selection, inside row
                    AddCellToRowBelowSelection();
                }
            }

            FirstSelected = null;
            LastSelected = null;
		}

		/**
         * In order to have mult group resizing:
         * 1) Adding to a mult group from flat within that group,
         *    - the interval will need to be multiplied by the group factor
         *    - use the cell values that have said group's factor applied
         * 
         * 2) adding from inside a mult group to outside that gorup
         * 
         * 3) adding from before a mult group to after that group
         * 
         * 4) adding from outside the group, into the group
         *    - like normal, use the actualValue inside of mult group. Multiply result by group factor to get
         *    base value.
         */

		/**
         * Add above row works like this:
         *    - if placed above all other cells, and above all rep group's LTMs,
         *    increase the previous last cell's or rep group LTM's duration
         *    * get the BPM value of the increment multiplied by how many ticks from last selected cell
         *    * to the new cell
         *    * Then subtract the accumulated value of all cells including rep groups from the total value.
         *    make new cell duration the increment value
         * 
         * Add above selection, within row works like this:
         *    - Get the value of increment times # of ticks between last selected cell and new cell position
         *    - Subtract the accumulated values of all cells including rep groups to get the new value
         *    of the preceding cell OR a rep group's LTM if we are placing the cell inside of the LTM
         *    - The cells value is then the preceding cell's previous value minus it's new value.
         * 
         * Add below row works like this:
         *    - Get the value of increment times # of ticks between first selected cell and new cell position
         *    - subtract the accumulated values of all cells and groups between selected cell and new cell
         *    to get the value of the new cell.
         *    - Subtract new cell value from row's offset to get the new offset
         * 
         * add below section, within row works like this:
         *    - Get the increment * # of ticks value between the first selected cell and new cell postion
         *    - subtract the accumulated values of all cells and groups between selected cell and new cell
         *    to get the value of the new cell.
         *    - subtract new cell value from preceding cell / group LTM's old value to get value
         * 
         */

		/**
         * Test Cases:
         * 
         * 1) Above row
         * 2) Above row where last cell is in a repeat
         * 3) Above row and within the duration of the last cell
         * 4) Above selection and within row
         * 5) ^ Where selection is in a repeat and new cell is not
         * 6) ^ Where selection and new cell are in the same repeat
         * 7) ^ Where new cell is in a repeat group
         * 8) ^ Selection is in a repeat group that is nested
         * 9) ^ new cell is in a repeat group that is nested
         * 10) Below selection and within row
         * 11) ^ Where selection is in a repeat and new cell is not
         * 12) ^ Where selection and new cell are in the same repeat
         * 13) ^ Where new cell is in a repeat group
         * 14) ^ Selection is in a repeat group that is nested
         * 15) ^ new cell is in a repeat group that is nested
         * 16) Below the row, in offset area
         * 17) ^ selection is in a repeat group
         * 18) ^ there is a repeat group between the selection and the start
         */

		protected void AddCellAboveRow()
		{
            Cell cell = new Cell(Row);

			if (cell != null)
			{
                cell.Value = BeatCell.SimplifyValue(DrawingView.Instance.GridSpacingString);
                cell.Position = LastSelected.Cell.Position + DrawingView.Instance.GridSpacing * NumIntervals;
				// set new duration of previous cell

                Cell below = Row.Cells.Max.Cell;

				// if add above a reference, just drop it in and exit.
				if (below.IsReference)
				{
					Row.Cells.Insert(cell);

					return;
				}

				// find the value string
				StringBuilder val = new StringBuilder();
                val.Append(BeatCell.MultiplyTerms(DrawingView.Instance.GridSpacingString, NumIntervals));

				HashSet<Repeat> repGroups = new HashSet<Repeat>();

                CellTreeNode c = LastSelected;

                while (c != null)
				{
                    //if (!string.IsNullOrEmpty(c.Cell.Reference)) // needed in WPF, not Mac
                    //{
                    //    c = c.Next();
                    //    continue;
                    //}

					val.Append("+0").Append(BeatCell.Invert(c.Cell.Value));
					// account for rep groups and their LTMs
					Dictionary<Repeat, int> ltmTimes = new Dictionary<Repeat, int>();
					foreach (Repeat rg in c.RepeatGroups.Reverse())
					{
						if (repGroups.Contains(rg)) continue;

						foreach (Cell ce in rg.Cells.Where(x => string.IsNullOrEmpty(x.Reference)))
						{
							val.Append("+0").Append(BeatCell.MultiplyTerms(BeatCell.Invert(ce.Value), rg.Times - 1));
						}
						foreach (KeyValuePair<Repeat, int> kv in ltmTimes)
						{
							ltmTimes[kv.Key] = kv.Value * rg.Times;
						}

						repGroups.Add(rg);
						ltmTimes.Add(rg, 1);
					}
					foreach (KeyValuePair<Repeat, int> kv in ltmTimes)
					{
						if (!string.IsNullOrEmpty(kv.Key.LastTermModifier))
						{
							val.Append("+0").Append(BeatCell.MultiplyTerms(BeatCell.Invert(kv.Key.LastTermModifier), kv.Value));
						}
					}

                    c = c.Next();
				}

				string oldPrevCellValue = below.Value;
				// if last cell is in a rep group, we need to increase the LTM for that group
				if (below.RepeatGroups.Any())
				{
					oldPrevCellValue = below.RepeatGroups.First.Value.LastTermModifier;
					// add to the bottom repeat group's LTM
					below.RepeatGroups.First.Value.LastTermModifier = BeatCell.SimplifyValue(val.ToString());
				}
				else
				{
					// add to last cell's duration
					//below.Duration = increment * div - (Row.Cells.Last().Position - Cell.SelectedCells.LastCell.Position);
					val.Append("+0").Append(below.Value);
					below.Value = BeatCell.SimplifyValue(val.ToString());
				}

				Row.Cells.Insert(cell);
			}
		}

		protected void AddCellToRowAboveSelection()
		{
            Cell cell = new Cell(Row);

            CellTreeNode node = new CellTreeNode(cell);

            cell.Position = LastSelected.Cell.Position + NumIntervals * DrawingView.Instance.GridSpacing;

			//int index = Row.Cells.InsertSorted(cell);
			if (Row.Cells.Insert(node))
			{
                //RightIndexBoundOfTransform = index - 1;

                CellTreeNode belowNode = node.Prev();
                Cell below = belowNode.Cell;

                // add to applicable groups
                foreach (Repeat rg in below.RepeatGroups.Where(x => x.Position + x.Length > cell.Position))
                {
                    cell.RepeatGroups.AddLast(rg);
                }
                foreach (Multiply mg in below.MultGroups.Where(x => x.Position + x.Length > cell.Position))
                {
                    cell.MultGroups.AddLast(mg);
                }

				// is new cell placed in the LTM zone of a rep group?
				Repeat repWithLtmToMod = null;
				foreach (Repeat rg in below.RepeatGroups.Where(
                    x => x.Cells.Last.Value == below && Position > below.Position + below.ActualDuration))
				{
					repWithLtmToMod = rg;
				}

				// determine new value for the below cell
				StringBuilder val = new StringBuilder();
				// take and the distance from the end of the selection
                val.Append(BeatCell.MultiplyTerms(DrawingView.Instance.GridSpacingString, NumIntervals));
				// subtract the values up to the previous cell
				HashSet<Repeat> repGroups = new HashSet<Repeat>();

                CellTreeNode c = LastSelected;

                while (c != belowNode)
                {
                    //if (!string.IsNullOrEmpty(c.Cell.Reference))
                    //{
                    //    c = c.Next();
                    //    continue;
                    //}

					// subtract each value from the total
					val.Append("+0").Append(BeatCell.Invert(c.Cell.Value));
					// account for rep group repititions.
					Dictionary<Repeat, int> ltmTimes = new Dictionary<Repeat, int>();
					foreach (Repeat rg in c.RepeatGroups.Reverse())
					{
						if (repGroups.Contains(rg)) continue;
						// don't include a rep group if the end point is included in it.

						repGroups.Add(rg);

						if (cell.RepeatGroups.Contains(rg))
						{
							repGroups.Add(rg);
							continue;
						}

						foreach (Cell ce in rg.Cells.Where(x => string.IsNullOrEmpty(x.Reference)))
						{
							val.Append("+0").Append(BeatCell.MultiplyTerms(BeatCell.Invert(ce.Value), rg.Times - 1));
						}
						// get times to count LTMs for each rg
						foreach (KeyValuePair<Repeat, int> kv in ltmTimes)
						{
							ltmTimes[kv.Key] = kv.Value * rg.Times;
						}

						ltmTimes.Add(rg, 1);
					}
					// subtract the LTMs
					foreach (KeyValuePair<Repeat, int> kv in ltmTimes)
					{
						val.Append("+0").Append(
							BeatCell.MultiplyTerms(
								BeatCell.Invert(kv.Key.LastTermModifier), kv.Value));
					}

                    c = c.Next();
                }

				// if the below cell was a single cell repeat, and we are placing the cell into it's LTM
				if (repWithLtmToMod != null)
				{
					string belowLtm = "";
					string belowRepValue = "0";
					foreach (Repeat rg in below.RepeatGroups.Where(x => !repGroups.Contains(x)))
					{
						if (!string.IsNullOrEmpty(belowLtm))
						{
							val.Append("+0").Append(
								BeatCell.MultiplyTerms(
									BeatCell.Invert(belowLtm), rg.Times));
						}

						belowRepValue = BeatCell.MultiplyTerms(BeatCell.Add(belowRepValue, below.Value), rg.Times);
					}

					val.Append("+0").Append(BeatCell.Invert(belowRepValue));
				}

				// get new cells value by subtracting old value of below cell by new value.
				string newVal = BeatCell.SimplifyValue(val.ToString());
				// placing a new cell on the beginning of a LTM is not illegal
				if (repWithLtmToMod != null && newVal == string.Empty)
				{
					newVal = "0";
				}

				cell.Value = BeatCell.Subtract(repWithLtmToMod == null ? below.Value : repWithLtmToMod.LastTermModifier, newVal);

				// if placing cell on top of another cell, it's not valid.
				if (cell.Value == string.Empty || newVal == string.Empty)
				{
					// remove the cell
					Row.Cells.Remove(cell);
					foreach (Repeat rg in cell.RepeatGroups)
					{
						rg.Cells.Remove(cell);
					}
                    foreach (Multiply mg in cell.MultGroups)
					{
						mg.Cells.Remove(cell);
					}
					cell = null;

                    throw new System.Exception();

					//return;
				}

				if (repWithLtmToMod == null)
				{
					// changing a cell value
					below.Value = newVal;
				}
				else
				{
					// changing a LTM value
					//repWithLtmToMod.LastTermModifier = BeatCell.Subtract(repWithLtmToMod.LastTermModifier, newVal);
					repWithLtmToMod.LastTermModifier = newVal.TrimStart('0');
				}
			}
		}

		protected void AddCellBelowRow(double position, double increment)
		{
			// in the offset area
			// how many increments back from first cell selected
			double diff = (Cell.SelectedCells.FirstCell.Position + Row.Offset) - (position + Row.Offset);
			int div = (int)(diff / increment);
			// is it closer to lower of upper grid line?
			Cell cell = null;
			if (diff % increment <= increment * GridProx)
			{
				// upper
				cell = new Cell(Row);
			}
			else if (diff % increment >= increment * GridProx)
			{
				// lower
				cell = new Cell(Row);
				div++;
			}
			if (cell != null)
			{
				// get the value string
				StringBuilder val = new StringBuilder();
				// value of grid lines, the 
				val.Append(BeatCell.MultiplyTerms(EditorWindow.CurrentIncrement, div));

				HashSet<RepeatGroup> repGroups = new HashSet<RepeatGroup>();
				foreach (Cell c in Row.Cells.TakeWhile(x => x != Cell.SelectedCells.FirstCell).Where(x => string.IsNullOrEmpty(x.Reference)))
				{
					val.Append("+0").Append(BeatCell.Invert(c.Value));
					// deal with repeat groups
					Dictionary<RepeatGroup, int> lcmTimes = new Dictionary<RepeatGroup, int>();
					foreach (RepeatGroup rg in c.RepeatGroups.Reverse())
					{
						if (repGroups.Contains(rg)) continue;
						// if the selected cell is in this rep group, we don't want to include repetitions
						if (Cell.SelectedCells.FirstCell.RepeatGroups.Contains(rg))
						{
							repGroups.Add(rg);
							continue;
						}
						foreach (Cell ce in rg.Cells.Where(x => string.IsNullOrEmpty(x.Reference)))
						{
							val.Append("+0").Append(BeatCell.MultiplyTerms(BeatCell.Invert(ce.Value), rg.Times - 1));
						}

						foreach (KeyValuePair<RepeatGroup, int> kv in lcmTimes)
						{
							lcmTimes[kv.Key] = kv.Value * rg.Times;
						}
						repGroups.Add(rg);
						lcmTimes.Add(rg, 1);
					}
					// subtract the LCMs
					foreach (KeyValuePair<RepeatGroup, int> kv in lcmTimes)
					{
						val.Append("+0").Append(BeatCell.MultiplyTerms(BeatCell.Invert(kv.Key.LastTermModifier), kv.Value));
					}
				}
				cell.Value = BeatCell.SimplifyValue(val.ToString());

				Row.Cells.Insert(0, cell);
				RightIndexBoundOfTransform = -1;
				IsValid = true;
				cell.Position = 0;
				Row.OffsetValue = BeatCell.Subtract(Row.OffsetValue, cell.Value);
			}
		}

		protected void AddCellToRowBelowSelection(double position, double increment)
		{
			double diff = Cell.SelectedCells.FirstCell.Position - position;
			int div = (int)(diff / increment);
			Cell cell = null;
			// is it in range of the left or right grid line?
			if (diff % increment <= increment * GridProx)
			{
				// right
				cell = new Cell(Row);
			}
			else if (diff % increment >= increment * (1 - GridProx))
			{
				// left
				cell = new Cell(Row);
				div++;
			}

			if (cell != null)
			{
				cell.Position = Cell.SelectedCells.FirstCell.Position - div * increment;
				int index = Row.Cells.InsertSorted(cell);
				if (index > -1)
				{
					RightIndexBoundOfTransform = index - 1;
					IsValid = true;

					Cell below = Row.Cells[index - 1];

					Group.AddToGroups(cell, below);

					// see if the cell is being added to a rep group's LTM zone
					RepeatGroup repWithLtmToMod = null;
					foreach (RepeatGroup rg in below.RepeatGroups.Where(
						x => x.Cells.Last.Value == below && position + increment * GridProx > below.Position + below.ActualDuration))
					{
						repWithLtmToMod = rg;
					}

					// get new value string for below
					StringBuilder val = new StringBuilder();

					bool repWithLtmToModFound = false;
					HashSet<RepeatGroup> repGroups = new HashSet<RepeatGroup>();
					foreach (Cell c in Row.Cells.SkipWhile(x => x != below).TakeWhile(x => x != Cell.SelectedCells.FirstCell))
					{
						if (c == cell || !string.IsNullOrEmpty(c.Reference)) continue; // don't include the new cell
																					   // add the cells value. If modding an LTM, we need to have passed that group first.
						if (repWithLtmToMod == null || repWithLtmToModFound)
						{
							val.Append(c.Value).Append('+');
						}
						// we need to track how many times to multiply each rep group's LTM
						Dictionary<RepeatGroup, int> ltmFactors = new Dictionary<RepeatGroup, int>();
						// if there's a rep group, add the repeated sections
						// what order are rg's in? reverse
						foreach (RepeatGroup rg in c.RepeatGroups.Reverse())
						{
							// don't add ghost reps more than once
							if (repGroups.Contains(rg)) continue;
							repGroups.Add(rg);

							// don't count reps for groups that contain the selection
							if (Cell.SelectedCells.FirstCell.RepeatGroups.Contains(rg))
							{
								continue;
							}

							// don't add anything until after the group with LTM to mod has been found (if were modding a LTM)
							if (repWithLtmToMod == null || repWithLtmToModFound)
							{
								foreach (Cell ce in rg.Cells.Where(x => string.IsNullOrEmpty(x.Reference)))
								{
									val.Append('0').Append(
										BeatCell.MultiplyTerms(ce.Value, rg.Times - 1))
										.Append('+');
								}

							}
							// found group with LTM tod mod, add it's LTM but not cell values.
							if (rg == repWithLtmToMod) repWithLtmToModFound = true;

							// increase multiplier of LTMs
							foreach (KeyValuePair<RepeatGroup, int> kv in ltmFactors)
							{
								ltmFactors[kv.Key] = kv.Value * rg.Times;
							}
							ltmFactors.Add(rg, 1);
						}
						// add in all the LTMs from rep groups
						foreach (KeyValuePair<RepeatGroup, int> kv in ltmFactors)
						{
							val.Append('0')
								.Append(BeatCell.MultiplyTerms(kv.Key.LastTermModifier, kv.Value))
								.Append('+');
						}
					}

					val.Append('0');
					val.Append("+0").Append(BeatCell.MultiplyTerms(BeatCell.Invert(EditorWindow.CurrentIncrement), div));
					cell.Value = BeatCell.Subtract(repWithLtmToMod == null ? below.Value : repWithLtmToMod.LastTermModifier, val.ToString());
					//cell.Value = BeatCell.SimplifyValue(below.Value + '-' + val.ToString());
					string newValue = BeatCell.SimplifyValue(val.ToString());

					// check for cell being doubled
					if (cell.Value == string.Empty || newValue == string.Empty)
					{
						IsValid = false;
						Row.Cells.Remove(cell);
						foreach (RepeatGroup rg in cell.RepeatGroups)
						{
							rg.Cells.Remove(cell);
						}
						foreach (MultGroup mg in cell.MultGroups)
						{
							mg.Cells.Remove(cell);
						}
						cell = null;
						return;
					}

					if (repWithLtmToMod == null)
					{
						//oldValue = below.Value;
						below.Value = newValue;
					}
					else
					{
						//oldValue = repWithLtmToMod.LastTermModifier;
						repWithLtmToMod.LastTermModifier = BeatCell.Subtract(
							repWithLtmToMod.LastTermModifier,
							newValue);
					}
				}
			}
		}
	}
}
