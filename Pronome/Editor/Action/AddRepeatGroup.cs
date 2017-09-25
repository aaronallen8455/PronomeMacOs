using System;
using System.Linq;
using Pronome.Mac.Editor.Groups;

namespace Pronome.Mac.Editor.Action
{
	public class AddRepeatGroup : AbstractBeatCodeAction
	{
		protected Repeat Group;

        protected CellTree Cells;

        public AddRepeatGroup(Repeat repeat, CellTree cells) : base(cells.Root.Cell.Row, "Add Repeat Group")
		{
            Group = repeat;
            Cells = cells;

			//if (!Row.BeatCodeIsCurrent)
			//{
			//	Row.UpdateBeatCode();
			//}
			//BeforeBeatCode = Row.BeatCode;
		}

		protected override void Transformation()
		{
            // add cells to the group
            //Cells[0].RepeatGroups.AddLast(Group);
            //Group.Cells.AddFirst(Cells[0]);
            //if (Cells.Length > 1)
            //{
            //	Cells[Cells.Length - 1].RepeatGroups.AddLast(Group);
            //	Group.Cells.AddLast(Cells[Cells.Length - 1]);
            //}

            Cell first = Cells.Min.Cell;
            Cell last = Cells.Max.Cell;

            // make sure we add the group action in the correct order

            if (first.GroupActions.Any() && last.GroupActions.Any())
            {
                // check if first cell opens a group that the last cell is either a member of or a closer of
                var node = first.GroupActions.First;

				while (node != null)
				{
                    var group = node.Value.Item2;

					var lastNode = last.GroupActions.Find((false, group));
                    if (lastNode != null)
                    {
                        first.GroupActions.AddAfter(node, (true, Group));
                        last.GroupActions.AddBefore(lastNode, (false, Group));
                        break;
					}

                    if (last.RepeatGroups.Contains(node.Value.Item2) || last.MultGroups.Contains(node.Value.Item2))
                    {
                        first.GroupActions.AddAfter(node, (true, Group));
                        last.GroupActions.AddLast((false, Group));
                    }

                    node = node.Next;
				}
            }


			Cells = null;
            Group = null;
		}
	}
}
