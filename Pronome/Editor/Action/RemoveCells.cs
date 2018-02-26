using System;
using System.Collections.Generic;
using Pronome.Mac.Editor.Groups;
using System.Text;
using System.Linq;

namespace Pronome.Mac.Editor.Action
{
	public class RemoveCells : AbstractBeatCodeAction
	{
        protected CellTree Cells;

		//protected string PreviousCellValue;

        protected HashSet<Repeat> RepGroups = new HashSet<Repeat>();

        protected HashSet<Multiply> MultGroups = new HashSet<Multiply>();

        protected LinkedList<AbstractGroup> OpenedGroups = new LinkedList<AbstractGroup>();

        protected LinkedList<AbstractGroup> ClosedGroups = new LinkedList<AbstractGroup>();

		/// <summary>
		/// Total duration in BPM of the group
		/// </summary>
		protected double Duration = 0;

		protected string BeatCodeDuration;

		protected bool ChangeOffset = false;

		/// <summary>
		/// Position of the first cell in the selection
		/// </summary>
        protected CellTreeNode StartNode;

        protected CellTreeNode EndNode;

        public RemoveCells(CellTree cells) : base(cells.Root.Cell.Row, cells.Count > 1 ? "Remove Cells" : "Remove Cell")
		{
			Cells = cells;
            //Row = cells.Root.Cell.Row;
            //PreviousCellValue = previousCellValue;
            //Index = cells[0].Row.Cells.IndexOf(cells[0]);
            StartNode = Row.Cells.LookupIndex(cells.Min.Cell.Index);
            EndNode = Row.Cells.LookupIndex(cells.Max.Cell.Index);

			StringBuilder duration = new StringBuilder();
			// find all groups that are encompassed by the selection
            HashSet<AbstractGroup> touchedGroups = new HashSet<AbstractGroup>();
            Repeat groupBeingAppendedTo = null; // a group who's LTM is actively being augmented
            Queue<Repeat> rgToAppendTo = new Queue<Repeat>(); // RGs that may need to have their LTM added to
            //LinkedList<AbstractGroup> openedGroups = new LinkedList<AbstractGroup>();
            //LinkedList<AbstractGroup> closedGroups = new LinkedList<AbstractGroup>();

			foreach (Cell c in Cells)
			{
                //if (!string.IsNullOrEmpty(c.Reference)) continue;
                foreach ((bool begun, AbstractGroup group) in c.GroupActions)
                {
                    if (begun) OpenedGroups.AddFirst(group);
                    else ClosedGroups.AddFirst(group);
                }

				// add to the LTM of groups with a previous cell in the selection but not this cell
				if (rgToAppendTo.Any() && !c.RepeatGroups.Contains(rgToAppendTo.Peek()))
				{
					groupBeingAppendedTo = rgToAppendTo.Dequeue();
				}
				if (groupBeingAppendedTo != null)
				{
					groupBeingAppendedTo.LastTermModifier = BeatCell.Add(groupBeingAppendedTo.LastTermModifier, c.Value);
				}

				int times = 1; // times this cell gets repeated
							   // track the times that each RG's LTM gets repeated
				Dictionary<Repeat, int> lcmTimes = new Dictionary<Repeat, int>();

				foreach (Repeat rg in c.RepeatGroups.Reverse())
				{
					// remove cell from group
					rg.ExclusiveCells.Remove(c);
                    rg.Cells.Remove(c);

                    // remove break cells
                    if (rg.BreakCell == c) rg.BreakCell = null;

					if (touchedGroups.Contains(rg)) continue;

					rgToAppendTo.Enqueue(rg);
					touchedGroups.Add(rg);

					if (
                        (StartNode.Cell == rg.ExclusiveCells.First?.Value || rg.Position > StartNode.Cell.Position)
                        && (EndNode.Cell == rg.ExclusiveCells.Last?.Value || rg.Position + rg.Length < EndNode.Cell.Position))
					{
						RepGroups.Add(rg);

                        bool cellAboveBreak = rg.BreakCell != null && c.Position > rg.BreakCell.Position;

                        times *= rg.Times - (cellAboveBreak ? 1 : 0);
						// multiply all nested rgs' LTMs by this groups repeat times.
						foreach (KeyValuePair<Repeat, int> kv in lcmTimes)
						{
                            bool aboveBreak = rg.BreakCell != null && kv.Key.Position > rg.BreakCell.Position;

                            lcmTimes[kv.Key] *= rg.Times - (aboveBreak ? 1 : 0);
						}
						lcmTimes.Add(rg, 1);
					}
				}

                foreach (Multiply mg in c.MultGroups)
				{
					// remove cell from group
					mg.ExclusiveCells.Remove(c);
					if (touchedGroups.Contains(mg)) continue;
					touchedGroups.Add(mg);
					if (
                        (StartNode.Cell == mg.ExclusiveCells.First.Value || mg.Position > StartNode.Cell.Position)
                        && (EndNode.Cell == mg.ExclusiveCells.Last.Value || mg.Position + mg.Length < EndNode.Cell.Position + EndNode.Cell.Duration))
					{
						MultGroups.Add(mg);
					}
				}

				// get the double version of duration
				Duration += c.Duration * times;

				// get the string version of duration
				// add cell's repeat durations if this cell is in the same scope as the first cell.
                if ((!c.RepeatGroups.Any() && !StartNode.Cell.RepeatGroups.Any()) ||
                    c.RepeatGroups.Last?.Value == StartNode.Cell.RepeatGroups.Last?.Value)
				{
					duration.Append("+0").Append(BeatCell.MultiplyTerms(c.Value, times));
				}
				// add any LTM's from repeat groups
				foreach (KeyValuePair<Repeat, int> kv in lcmTimes)
				{
					duration.Append("+0").Append(BeatCell.MultiplyTerms(kv.Key.LastTermModifier, kv.Value));
					Duration += BeatCell.Parse(kv.Key.LastTermModifier) * kv.Value;
				}
			}

            // Transfer group actions from deleted cells to the 2 cells outside the deleted group
            //CellTreeNode after = EndNode.Next();
            //CellTreeNode before = StartNode.Prev();
			//
            //if (after != null)
            //{
			//	foreach (AbstractGroup group in openedGroups)
			//	{
			//		if (group.ExclusiveCells.Count > 0)
			//		{
            //            after.Cell.GroupActions.AddFirst((true, group));
			//		}
			//	}
            //}
			//
            //if (before != null)
            //{
			//	foreach (AbstractGroup group in closedGroups)
			//	{
			//		if (group.ExclusiveCells.Count > 0)
			//		{
            //            before.Cell.GroupActions.AddFirst((false, group));
			//		}
			//	}
            //}

			BeatCodeDuration = BeatCell.SimplifyValue(duration.ToString());
		}

		public override void Undo()
		{
			base.Undo();

			if (ChangeOffset)
			{
				Row.Offset -= Duration;
				Row.OffsetValue = BeatCell.Subtract(Row.OffsetValue, BeatCodeDuration);
			}
		}

		protected override void Transformation()
		{
            Cell firstCell = StartNode.Cell;
            // remove cells


			// remove groups
			foreach (Repeat rg in RepGroups)
			{
				Row.RepeatGroups.Remove(rg);
			}
            foreach (Multiply mg in MultGroups)
			{
				Row.MultGroups.Remove(mg);
			}

            // check if first cell of selection is not row's first cell
            if (firstCell.Index == 0)
			{
				// will be increasing the row offset, but only if
				// selection is not part of a rep group that is not
				// encompassed by the selection
				if (!firstCell.RepeatGroups.Any() || !RepGroups.Contains(firstCell.RepeatGroups.First.Value))
				{
					// augment the row's offset
					ChangeOffset = true;
				}
			}
			else
			{
                Cell prevCell = StartNode.Prev().Cell;
                if (prevCell != null)
                {
					// if previous cell is the last cell of a rep group, increase rep groups offset
										
					Repeat groupToAddTo = null;
					foreach (Repeat rg in prevCell.RepeatGroups.Reverse())
					{
						if (!firstCell.RepeatGroups.Contains(rg))
						{
							groupToAddTo = rg;
						}
						else break;
					}
					
					if (groupToAddTo != null)
					{
						groupToAddTo.LastTermModifier = BeatCell.Add(groupToAddTo.LastTermModifier, BeatCodeDuration);
					}
					else if (!firstCell.RepeatGroups.Any() || prevCell.RepeatGroups.Contains(firstCell.RepeatGroups.Last.Value))
					{
						// otherwise, increase the prev cell's duration
						// but only if it is not the cell prior to a repgroup for which first cell of select is first cell of the rep group.
						prevCell.Value = BeatCell.Add(prevCell.Value, BeatCodeDuration);
					}

                    // transfer the closing group actions to prev cell
                    foreach (AbstractGroup group in ClosedGroups)
                    {
                        if (RepGroups.Contains(group) || MultGroups.Contains(group)) continue;

                        if (prevCell.RepeatGroups.Contains(group) || prevCell.MultGroups.Contains(group))
                        {
                            prevCell.GroupActions.AddLast((false, group));
                        }
                    }
                }
			}

            // do the transition of group actions
            if (EndNode.Cell != Row.Cells.Max.Cell)
            {
                Cell nextCell = EndNode.Next().Cell;
                // need to transfer the repeat group creation point to the next cell (if not a 1 cell group)
                foreach (AbstractGroup group in OpenedGroups)
                {
                    if (RepGroups.Contains(group) || MultGroups.Contains(group)) continue;

                    if (nextCell.RepeatGroups.Contains(group) || nextCell.MultGroups.Contains(group))
                    {
                        nextCell.GroupActions.AddFirst((true, group));
                    }
                }
            }

            // remove cells from tree
            while (StartNode != null)
            {
                var next = StartNode.Next();
                Row.Cells.Remove(StartNode);
                if (StartNode == EndNode) break;
                StartNode = next;
            }
			//foreach (CellTreeNode c in Row.Cells.GetRange(StartNode.Cell.Position, EndNode.Cell.Position))
			//{
			//	Row.Cells.Remove(c);
			//}

            DrawingView.Instance.DeselectCells();

			// no longer need these
			RepGroups = null;
			MultGroups = null;
			Cells = null;
            StartNode = null;
            EndNode = null;
		}

		public override void Redo()
		{
			base.Redo();

			if (ChangeOffset)
			{
				Row.Offset += Duration;
				Row.OffsetValue = BeatCell.Add(Row.OffsetValue, BeatCodeDuration);
			}
		}

        public override bool CanPerform()
        {
            return true;
        }
	}
}
