using System;
using System.Linq;
using Pronome.Mac.Editor.Groups;

namespace Pronome.Mac.Editor.Action
{
	public class RemoveRepeatGroup : AbstractBeatCodeAction
	{
		protected Repeat Group;

        protected CellTree Cells;

        public RemoveRepeatGroup(CellTree cells) : base(cells.Root.Cell.Row, "Remove Repeat Group")
		{
            Cells = cells;
		}

		protected override void Transformation()
		{
			foreach (Cell c in Group.ExclusiveCells)
			{
				c.RepeatGroups.Remove(Group);
			}

            Cells.Min.Cell.GroupActions.Remove((true, Group));
            Cells.Max.Cell.GroupActions.Remove((false, Group));

			Group = null;
            Cells = null;
		}

        public override bool CanPerform()
        {
            var firstGroups = Cells.Min.Cell.RepeatGroups.Where(x => x.ExclusiveCells.First() == Cells.Min.Cell);
            var lastGroups = Cells.Max.Cell.RepeatGroups.Where(x => x.ExclusiveCells.Last() == Cells.Max.Cell);

            Group = firstGroups.Intersect(lastGroups).FirstOrDefault();

            return Group != null;
        }
	}
}
