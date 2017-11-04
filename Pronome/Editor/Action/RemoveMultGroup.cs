using System;
using System.Linq;
using Pronome.Mac.Editor.Groups;

namespace Pronome.Mac.Editor.Action
{
	public class RemoveMultGroup : AbstractBeatCodeAction
	{
        protected CellTree Cells;

		protected Multiply Group;

        public RemoveMultGroup(CellTree cells) : base(cells.Root.Cell.Row, "Remove Multiply Group")
		{
            Cells = cells;
		}

		protected override void Transformation()
		{
			foreach (Cell c in Group.ExclusiveCells)
			{
				c.MultGroups.Remove(Group);
			}

            Row.MultGroups.Remove(Group);

            Group.ExclusiveCells.First.Value.GroupActions.Remove((true, Group));
            Group.ExclusiveCells.Last.Value.GroupActions.Remove((false, Group));

            ChangesViewWidth = true;

			Group = null;
            Cells = null;
		}

        /// <summary>
        /// Assigns the group if a candidate exists
        /// </summary>
        /// <returns><c>true</c>, if perform was caned, <c>false</c> otherwise.</returns>
        public override bool CanPerform()
        {
            var firstGroups = Cells.Min.Cell.MultGroups.Where(x => x.ExclusiveCells.First() == Cells.Min.Cell);

            var lastGroups = Cells.Max.Cell.MultGroups.Where(x => x.ExclusiveCells.Last() == Cells.Max.Cell);

            Group = firstGroups.Intersect(lastGroups).FirstOrDefault();

            return Group != null;
        }
	}
}
