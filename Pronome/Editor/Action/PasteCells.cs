using System;
using System.Collections.Generic;
using Pronome.Mac.Editor.Groups;
using System.Linq;

namespace Pronome.Mac.Editor.Action
{
	public class PasteCells : AbstractBeatCodeAction
	{
		protected LinkedList<Cell> Cells;

        protected CellTree SelectedCells;

		//private int RepCount = 0;
		//private int MultCount = 0;

        public PasteCells(LinkedList<Cell> cells, CellTree selectedCells) : base(selectedCells.Root.Cell.Row, "Paste")
		{
			Cells = cells;
			SelectedCells = selectedCells;
            Row = selectedCells.Root.Cell.Row;
		}

		protected override void Transformation()
		{
			Row row = SelectedCells.Root.Cell.Row;
			// replace selected cells with cells from clipboard

			// get bpm duration of clipboard
			double duration = Cells.Select(x => x.Duration).Sum();
			double pos = Cells.First.Value.Position;
			Cell firstCell = SelectedCells.Min.Cell;
			Cell lastCell = SelectedCells.Max.Cell;
			// bpm position of selection
            double selPos = firstCell.Position;
			double selDuration = SelectedCells.ToArray().Select(x => x.Duration).Sum();

			// if replacement is longer, we need to reposition cells in destination
			if (selDuration > duration)
			{
				CellTreeNode n = row.Cells.Lookup(SelectedCells.Max.Cell.Position);
				n = n.Next();
				while (n != null)
				{
					n.Cell.Position += selDuration - duration;
                    n = n.Next();
				}

                ChangesViewWidth = true;
			}

			// add any applicable groups to the new cells
			foreach (Repeat rg in firstCell.RepeatGroups)
			{
                // if the first cell in the group is behind the selection, make it's last cell before selection
                if (rg.ExclusiveCells.First.Value.Position < firstCell.Position && rg.ExclusiveCells.Last.Value.Position < lastCell.Position)
                {
                    // make last cell of group be before selection
                    rg.ExclusiveCells.Last.Value.GroupActions.Remove((false, rg));

                    CellTreeNode newLast = Row.Cells.Lookup(firstCell.Position).Prev();
                    newLast.Cell.GroupActions.AddLast((false, rg));
                    rg.ExclusiveCells.AddLast(newLast.Cell);
                }
                else if (rg.ExclusiveCells.First.Value != firstCell || rg.ExclusiveCells.Last.Value != lastCell)
                {
                    // reassign the first or last cell of the group
					if (rg.ExclusiveCells.First.Value == firstCell)
					{
						Cells.First.Value.RepeatGroups.AddLast(rg);
                        Cells.First.Value.GroupActions.AddLast((true, rg));
						rg.ExclusiveCells.AddFirst(Cells.First.Value);

					}
					if (Cells.First != Cells.Last && rg.ExclusiveCells.Last.Value == lastCell)
					{
						Cells.Last.Value.RepeatGroups.AddLast(rg);
                        Cells.Last.Value.GroupActions.AddLast((false, rg));
						rg.ExclusiveCells.AddLast(Cells.Last.Value);
					}
                }
			}

            foreach (Repeat rg in lastCell.RepeatGroups)
            {
                // if group is split by end of selection, reassign the group's first cell
                if (rg.ExclusiveCells.First.Value.Position > firstCell.Position && rg.ExclusiveCells.Last.Value.Position > lastCell.Position)
                {
                    rg.ExclusiveCells.First.Value.GroupActions.Remove((true, rg));
                    CellTreeNode newFirst = Row.Cells.Lookup(lastCell.Position).Next();
                    newFirst.Cell.GroupActions.AddFirst((true, rg));
                    rg.ExclusiveCells.AddFirst(newFirst.Cell);
                }
            }

			foreach (Multiply mg in firstCell.MultGroups)
			{
				if (mg.ExclusiveCells.First.Value.Position < firstCell.Position && mg.ExclusiveCells.Last.Value.Position < lastCell.Position)
				{
                    // make last cell of group be before selection
                    mg.ExclusiveCells.Last.Value.GroupActions.Remove((false, mg));
					CellTreeNode newLast = Row.Cells.Lookup(firstCell.Position).Prev();
                    newLast.Cell.GroupActions.AddLast((false, mg));
					mg.ExclusiveCells.AddLast(newLast.Cell);
				}
                else if (mg.ExclusiveCells.First.Value != firstCell || mg.ExclusiveCells.Last.Value != lastCell)
                {
                    // make new first or last cell
					if (mg.ExclusiveCells.First.Value == firstCell)
					{
						Cells.First.Value.MultGroups.AddLast(mg);
						mg.ExclusiveCells.AddFirst(Cells.First.Value);
                        Cells.First.Value.GroupActions.AddFirst((true, mg));
					}
					if (Cells.First != Cells.Last && mg.ExclusiveCells.Last.Value == lastCell)
					{
						Cells.Last.Value.MultGroups.AddLast(mg);
						mg.ExclusiveCells.AddLast(Cells.Last.Value);
                        Cells.Last.Value.GroupActions.AddLast((false, mg));
					}
                }
			}

            foreach (Multiply mg in lastCell.MultGroups)
			{
				// if group is split by end of selection, reassign the group's first cell
				if (mg.ExclusiveCells.First.Value.Position > firstCell.Position && mg.ExclusiveCells.Last.Value.Position > lastCell.Position)
				{
					CellTreeNode newFirst = Row.Cells.Lookup(lastCell.Position).Next();
					mg.ExclusiveCells.AddFirst(newFirst.Cell);
                    newFirst.Cell.GroupActions.AddFirst((true, mg));
				}
			}

            EditorViewController.Instance.DView.DeleteSelectedCells();

			// insert replacement cells
			foreach (Cell cell in Cells)
			{
				cell.Row = row;
				cell.Position += selPos - pos;
				row.Cells.Insert(cell);
			}

			SelectedCells = null;
		}

		public override void Redo()
		{
			base.Redo();

			Cells = null;
		}

        public override bool CanPerform()
        {
            return true;
        }
	}
}
