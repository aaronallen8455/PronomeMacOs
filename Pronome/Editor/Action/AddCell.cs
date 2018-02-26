using System.Text;
using System.Collections.Generic;
using Pronome.Mac.Editor.Groups;
using System.Linq;
using System;

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

        /// <summary>
        /// The number of Grid Spacings from the selection bound to the new cell location.
        /// </summary>
        protected int NumIntervals;

        /// <summary>
        /// Whether we are adding a cell above the selection.
        /// </summary>
        protected bool AboveSelection;

		/// <summary>
		/// Determines how close a mouse click needs to be to a grid line to count as that line. It's a factor of the increment size.
		/// </summary>
		public const float GridProx = .15f;

		public AddCell(double position, Row row, CellTreeNode firstSelect, CellTreeNode lastSelect) : base(row, "Add Cell")
		{
            FirstSelected = firstSelect;
            LastSelected = lastSelect;
            //NumIntervals = div;
            //AboveSelection = aboveSelection;
            Position = position;
		}

		protected override void Transformation()
		{
            if (AboveSelection)
            {
                if (Position + 1e-9 >= Row.Cells.Max.Cell.Position + Row.Cells.Max.Cell.Duration)
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

        /// <summary>
        /// Checks if action can be performed. Changes 'Position' from click location to new cell location. Sets AboveSelection, and NumIntervals.
        /// </summary>
        /// <returns><c>true</c>, if perform was caned, <c>false</c> otherwise.</returns>
        public override bool CanPerform()
        {
            if (Row != DrawingView.Instance.SelectedCells.Root.Cell.Row) return false;
            // try to create new cell
            // see if clicked on a grid line (within a pad amount)
            double gridSpacing = DrawingView.Instance.GridSpacing;

            double pad = Math.Max(DrawingView.CellWidth / DrawingView.ScalingFactor / 2, gridSpacing * .125);
			double x = -1;
			double mod = -1;
			//bool aboveSelection = false;

            if (LastSelected.Cell.Position < Position)
			{
                x = Position - LastSelected.Cell.Position;
                mod = x % gridSpacing;
				AboveSelection = true;
			}
            else if (FirstSelected.Cell.Position > Position)
			{
                x = FirstSelected.Cell.Position - Position;
                mod = x % gridSpacing;
			}

			// see if it registers as a hit
            if (x >= 0 && (mod <= pad || mod >= gridSpacing - pad))
			{
				// check if it's inside the ghost zone of a rep group
                NumIntervals = (int)Math.Round(x / gridSpacing);
				// bpm position within row
                Position = AboveSelection 
                    ? LastSelected.Cell.Position + NumIntervals * gridSpacing 
                    : FirstSelected.Cell.Position - NumIntervals * gridSpacing;
				//Repeat rg = row.RepeatGroups.Where(x => x.P)
				bool inGroup = false;
				foreach (Repeat rg in Row.RepeatGroups)
				{
                    if (Position < rg.Position + rg.Length) break;

					double range = rg.Position + rg.Length * rg.Times - pad;
                    if (rg.Position + rg.Length <= Position && Position < range)
					{
						inGroup = true;
						break;
					}
				}

				// check if inside a reference
				if (!inGroup) //&& row.ReferencePositionAndDurations.Any(p => p.position <= xPos && xPos < p.position + p.duration))
				{
                    return true;
				}
			}
            return false;
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

        /// <summary>
        /// Adds the cell above row.
        /// </summary>
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

                int ltmFactor = LastSelected.Cell.RepeatGroups.Any() ?
                                            LastSelected.Cell.RepeatGroups
                                            .Reverse()
                                            .Select(x => x.Times)
                                            .Aggregate((x, y) => x * y) : 1;

                CellTreeNode c = LastSelected;

                while (c != null)
				{
                    if (!string.IsNullOrEmpty(c.Cell.Reference))
                    {
                        c = c.Next();
                        continue;
                    }

                    AddCellValueToAccumulator(c.Cell, LastSelected.Cell, cell, val, ref ltmFactor);

                    c = c.Next();
				}

                val.Append('0').Append(BeatCell.MultiplyTerms(BeatCell.Invert(DrawingView.Instance.GridSpacingString), NumIntervals));

                string valToAdd = BeatCell.Invert(BeatCell.SimplifyValue(val.ToString()));

				// if last cell is in a rep group, we need to increase the LTM for that group
				if (below.RepeatGroups.Any())
				{
                    var rg = below.RepeatGroups.First.Value;

					// add to the bottom repeat group's LTM
                    rg.LastTermModifier = BeatCell.SimplifyValue(rg.GetValueDividedByMultFactor(valToAdd));
				}
				else
				{
                    // add to last cell's duration
                    below.Value = BeatCell.Add(below.GetValueDividedByMultFactors(valToAdd), below.Value);
				}

				Row.Cells.Insert(cell);

                ChangesViewWidth = true;
			}
		}

        /// <summary>
        /// Adds the cell to row above selection.
        /// </summary>
		protected void AddCellToRowAboveSelection()
		{
            Cell cell = new Cell(Row);

            CellTreeNode node = new CellTreeNode(cell);

            cell.Position = LastSelected.Cell.Position + NumIntervals * DrawingView.Instance.GridSpacing;

			if (Row.Cells.Insert(node))
			{
                CellTreeNode belowNode = node.Prev();
                Cell below = belowNode.Cell;
				RightIndexBoundOfTransform = below.Index + 1;

                // add to applicable groups
                AddToGroups(cell, below);

				// is new cell placed in the LTM zone of a rep group?
				Repeat repWithLtmToMod = null;
				foreach (Repeat rg in below.RepeatGroups.Where(
                    x => x.ExclusiveCells.Last.Value == below && Position > below.Position + below.ActualDuration))
				{
					repWithLtmToMod = rg;
				}

				// determine new value for the below cell
				StringBuilder val = new StringBuilder();

                var sequence = LastSelected.Cell.RepeatGroups
                   .Where(x => !cell.RepeatGroups.Contains(x) && x.Cells.First.Value != LastSelected.Cell)
                   .Reverse()
                   .Select(x => x.Times);
                int ltmFactor = sequence.Any() ? sequence.Aggregate((x, y) => x * y) : 1;

                CellTreeNode c = LastSelected;

                // we subtract all values up to the "whitespace" below the new cell
                while (c != node)
                {
                    if (!string.IsNullOrEmpty(c.Cell.Reference))
                    {
                        c = c.Next();
                        continue;
                    }

                    AddCellValueToAccumulator(c.Cell, LastSelected.Cell, node.Cell, val, ref ltmFactor);

                    c = c.Next();
                }

                val.Append('0').Append(BeatCell.MultiplyTerms(BeatCell.Invert(DrawingView.Instance.GridSpacingString), NumIntervals));

				// get new cells value by subtracting old value of below cell by new value.
				string newVal = BeatCell.SimplifyValue(val.ToString());
				// placing a new cell on the beginning of a LTM is not illegal
				if (repWithLtmToMod != null && newVal == string.Empty)
				{
					newVal = "0";
				}

                // assign the new cell's value
                cell.Value = BeatCell.SimplifyValue(cell.GetValueDividedByMultFactors(newVal));

				if (repWithLtmToMod == null)
				{
					// change below cell's value
					below.Value = below.GetValueDividedByMultFactors(
                        BeatCell.Subtract(below.Value, newVal));
					below.Value = BeatCell.SimplifyValue(below.Value);

                    if (below.IsBreak)
                    {
                        below.IsBreak = false;
                        cell.IsBreak = true;
                    }
				}
				else
				{
					// changing a LTM value
                    repWithLtmToMod.LastTermModifier = BeatCell.SimplifyValue(
                        repWithLtmToMod.GetValueDividedByMultFactor(
                            BeatCell.Subtract(repWithLtmToMod.LastTermModifier, newVal)));
				}
			}
		}

        /// <summary>
        /// Adds the cell below row.
        /// </summary>
		protected void AddCellBelowRow()
		{
            Cell cell = new Cell(Row);
			
			// get the value string
			StringBuilder val = new StringBuilder();

            int ltmFactor = 1;

            CellTreeNode c = Row.Cells.Min;

            while (c != FirstSelected)
            {
                if (!string.IsNullOrEmpty(c.Cell.Reference)) 
                {
                    c = c.Next();
                    continue;
                }

                AddCellValueToAccumulator(c.Cell, Row.Cells.Min.Cell, FirstSelected.Cell, val, ref ltmFactor);
                c = c.Next();
            }

            val.Append('0').Append(BeatCell.MultiplyTerms(BeatCell.Invert(DrawingView.Instance.GridSpacingString), NumIntervals));

            cell.Value = BeatCell.Invert(BeatCell.SimplifyValue(val.ToString()));

            // does having negative positions like this cause problems?
            cell.Position = FirstSelected.Cell.Position - DrawingView.Instance.GridSpacing * NumIntervals;
            Row.Cells.Insert(cell);
            RightIndexBoundOfTransform = -1;
			Row.OffsetValue = BeatCell.Subtract(Row.OffsetValue, cell.Value); // don't mult group factor this
		}

        /// <summary>
        /// Adds the cell to row below selection.
        /// </summary>
		protected void AddCellToRowBelowSelection()
		{
            Cell cell = new Cell(Row);
            CellTreeNode cellNode = new CellTreeNode(cell);

            cell.Position = FirstSelected.Cell.Position - DrawingView.Instance.GridSpacing * NumIntervals;

            if (Row.Cells.Insert(cellNode))
            {
                Cell below = cellNode.Prev().Cell;
                RightIndexBoundOfTransform = below.Index;

                // add to applicable groups
                AddToGroups(cell, below);

                // see if the cell is being added to a rep group's LTM zone
                Repeat repWithLtmToMod = null;
                foreach (Repeat rg in below.RepeatGroups.Where(
                    x => x.ExclusiveCells.Last.Value == below && Position > below.Position + below.Duration))
                {
                    repWithLtmToMod = rg;
                }

                // get new value string for below
                StringBuilder val = new StringBuilder();

                var sequence = cell.RepeatGroups
                   .Where(x => !FirstSelected.Cell.RepeatGroups.Contains(x) && x.Cells.First.Value != cell)
                   .Reverse()
                   .Select(x => x.Times);
                int ltmFactor = sequence.Any() ? sequence.Aggregate((x, y) => x * y) : 1;

                CellTreeNode c = cellNode.Next();

                while (c != FirstSelected)
                {
                    if (!string.IsNullOrEmpty(c.Cell.Reference))
                    {
                        c = c.Next();
                        continue;
                    }

                    AddCellValueToAccumulator(c.Cell, cell, FirstSelected.Cell, val, ref ltmFactor);

                    c = c.Next();
                }

                val.Append('0').Append(BeatCell.MultiplyTerms(BeatCell.Invert(DrawingView.Instance.GridSpacingString), NumIntervals));

                cell.Value = BeatCell.SimplifyValue(cell.GetValueDividedByMultFactors(BeatCell.Invert(val.ToString())));

                string newValue = BeatCell.SimplifyValue(val.ToString());

                if (repWithLtmToMod == null)
                {
                    below.Value = below.GetValueDividedByMultFactors(BeatCell.Add(below.Value, newValue));//newValue);
                    below.Value = BeatCell.SimplifyValue(below.Value);
                }
                else
                {
                    repWithLtmToMod.LastTermModifier =
                                       repWithLtmToMod.GetValueDividedByMultFactor(
                                           BeatCell.Subtract(repWithLtmToMod.LastTermModifier, newValue));
                    repWithLtmToMod.LastTermModifier = BeatCell.SimplifyValue(repWithLtmToMod.LastTermModifier);
                }
            }
        }

        /// <summary>
        /// Adds the cell to the other cell's groups when applicable
        /// </summary>
        /// <param name="cell">Cell.</param>
        /// <param name="below">Below.</param>
        private static void AddToGroups(Cell cell, Cell below)
        {
            foreach (Repeat rg in below.RepeatGroups.Where(x => x.Position + x.Length > cell.Position))
            {
                //rg.Cells.Last.Value.GroupActions.Remove();
                cell.RepeatGroups.AddLast(rg);

                // transfer actions if below was last cell of group
                if (rg.ExclusiveCells.Last.Value == below)
                {
                    below.GroupActions.Remove((false, rg));
                    cell.GroupActions.AddLast((false, rg));
                }
            }
            foreach (Multiply mg in below.MultGroups.Where(x => x.Position + x.Length > cell.Position))
            {
                cell.MultGroups.AddLast(mg);
                if (UserSettings.GetSettings().DrawMultToScale)
                {
                    cell.MultFactor = BeatCell.MultiplyTerms(cell.MultFactor, mg.FactorValue);
                }

                if (mg.ExclusiveCells.Last.Value == below)
                {
                    below.GroupActions.Remove((false, mg));
                    cell.GroupActions.AddLast((false, mg));
                }
            }
        }

        /// <summary>
        /// Add a cell's value to the accumulator the correct number of times
        /// </summary>
        /// <param name="target"></param>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <param name="accumulator"></param>
        /// <param name="ltmFactor"></param>
        /// <param name="recursing"></param>
        protected void AddCellValueToAccumulator(Cell target, Cell start, Cell end, StringBuilder accumulator, ref int ltmFactor, bool recursing = false)
        {
            // subtract each value from the total
            if (!target.RepeatGroups.Any())
            {
                accumulator.Append(target.GetValueWithMultFactors()).Append('+');
                return;
            }

            int timesDiff = 1;
            int ltmTimesDiff = 1;
            bool isBehind = target.RepeatGroups.First.Value.Position + target.RepeatGroups.First.Value.FullDuration < start.Position;
            bool contains = !isBehind;
            int times = 1;
            foreach (Repeat rg in target.RepeatGroups.TakeWhile(x => !end.RepeatGroups.Contains(x))) // iterate from innermost group
            {
                if (recursing)
                {
                    if (contains && rg.ExclusiveCells.Contains(start))
                    {
                        // this is the times to subtract because they occur before the starting point.
                        timesDiff = times;
                    }
                    else if (isBehind && rg.Cells.Contains(start))
                    {
                        // subtract a full cycle if this rep group exists all behind the target
                        ltmTimesDiff = timesDiff = times;
                        ltmTimesDiff /= target.RepeatGroups.First().Times;
                        isBehind = false;
                    }
                }

                // break cell(s) may decrease the factor
                if (rg.BreakCell != null)
                {
                    times *= rg.Times - (target == rg.BreakCell || target.Position < rg.BreakCell.Position ? 0 : 1);
                }
                else
                {
                    times *= rg.Times;
                }

                if (contains && recursing && rg.ExclusiveCells.Contains(start))
                {
                    ltmTimesDiff = times;
                    ltmTimesDiff /= target.RepeatGroups.First().Times;
                }
            }

            // handle LTMs
            foreach ((bool opens, AbstractGroup rg) in target.GroupActions.Where(x => x.Item2 is Repeat && !end.RepeatGroups.Contains(x.Item2)))
            {
                if (!opens)
                {
                    ltmFactor /= ((Repeat)rg).Times;

                    // subtract out the LTM (if group doesn't contain the end point)
                    if (!string.IsNullOrEmpty((rg as Repeat).LastTermModifier))
                    {
                        accumulator.Append(
                            BeatCell.MultiplyTerms(
                                ((Repeat)rg).GetLtmWithMultFactor(), ltmFactor - (recursing ? ltmTimesDiff : 0))).Append('+');
                    }
                }
                else if (!end.RepeatGroups.Contains(rg))
                {
                    ltmFactor *= ((Repeat)rg).Times;
                }
            }

            // account for preceding cells if we are starting mid-way through a rep group
            if (recursing) times -= timesDiff;
            else if (target == start)
            {
                // find outermost rep group that doesn't contain the new cell
                Repeat rg = target.RepeatGroups.Reverse().SkipWhile(x => end.RepeatGroups.Contains(x)).FirstOrDefault();

                if (rg != null)
                {
                    int ltmFactorR = 1;//rg.Cells.First.Value == target ? 1 : rg.Times;
                    foreach (Cell c in rg.Cells.TakeWhile(x => x.Position < target.Position))
                    {
                        AddCellValueToAccumulator(c, start, end, accumulator, ref ltmFactorR, true);
                    }
                }
            }

            if (!string.IsNullOrEmpty(target.Value))
            {
                accumulator.Append(BeatCell.MultiplyTerms(target.GetValueWithMultFactors(), times)).Append('+');
            }
        }
    }
}
