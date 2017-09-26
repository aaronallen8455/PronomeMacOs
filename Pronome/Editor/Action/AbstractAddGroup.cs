using System;
using System.Linq;
using Pronome.Mac.Editor.Groups;

namespace Pronome.Mac.Editor.Action
{
    abstract public class AbstractAddGroup<T> : AbstractBeatCodeAction where T : AbstractGroup
    {
		protected T Group;

		protected CellTree Cells;

        public AbstractAddGroup(T group, CellTree cells, string header) : base(cells.Root.Cell.Row, header)
        {
			Group = group;
			Cells = cells;
		}

		protected override void Transformation()
		{
			bool finished = false;

			Cell first = Cells.Min.Cell;
			Cell last = Cells.Max.Cell;

			// make sure we add the group action in the correct order

			if (first.GroupActions.Any())
			{

				// check if first cell opens a group that the last cell is either a member or a closer of

				var node = first.GroupActions.First;

				while (node != null)
				{
					var group = node.Value.Item2;

					var lastNode = last.GroupActions.Find((false, group));
					if (lastNode != null)
					{
						first.GroupActions.AddAfter(node, (true, Group));
						last.GroupActions.AddBefore(lastNode, (false, Group));
						finished = true;
						break; ;
					}

					if (last.RepeatGroups.Contains(node.Value.Item2) || last.MultGroups.Contains(node.Value.Item2))
					{
						first.GroupActions.AddAfter(node, (true, Group));

						// if last cell is a closer of groups for which first is not a member, add after those
						if (last.GroupActions.Any())
						{
							var lnode = last.GroupActions.First;
							while (lnode != null)
							{
								if ((lnode.Value.Item2.GetType() == typeof(T) && first.RepeatGroups.Contains(lnode.Value.Item2))
								   || first.MultGroups.Contains(lnode.Value.Item2))
								{
									last.GroupActions.AddBefore(lnode, (false, Group));
									finished = true;
									break;
								}

								lnode = lnode.Next;
							}
						}

						if (!finished)
						{
							// if order not important (not a group end), add it as first
							last.GroupActions.AddFirst((false, Group));
							finished = true;
						}
						break;
					}

					node = node.Next;
				}
			}

			// check if order is important for last but not first
			if (!finished && last.GroupActions.Any())
			{
				first.GroupActions.AddLast((true, Group));

				var node = last.GroupActions.First;

				while (node != null)
				{
					if ((node.Value.Item2.GetType() == typeof(T) && first.RepeatGroups.Contains(node.Value.Item2))
						|| first.MultGroups.Contains(node.Value.Item2))
					{
						last.GroupActions.AddBefore(node, (false, Group));
						finished = true;
						break;
					}

					node = node.Next;
				}
			}

			// order not important
			if (!finished)
			{
				first.GroupActions.AddLast((true, Group));
				last.GroupActions.AddFirst((false, Group));
			}

			//Row.RepeatGroups.AddLast(Group);

			ChangesViewWidth = true;

			Cells = null;
			Group = null;
		}

		public override bool CanPerform()
		{
			// checking is done in menu validation
			return true;
		}
    }
}
