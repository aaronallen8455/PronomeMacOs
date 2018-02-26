using System;
using System.Collections.Generic;
using System.Linq;

namespace Pronome.Mac.Editor.Groups
{
    public abstract class AbstractGroup
    {
        #region Public Fields
        public double Position;

        /// <summary>
        /// The bpm length of a single cycle, not including the LTM
        /// </summary>
        public double Length;

        public Row Row;

        /// <summary>
        /// The cells contained by this group that exclusive to this group, i.e. not also in a nested group
        /// </summary>
        public LinkedList<Cell> ExclusiveCells;

        public LinkedList<Cell> Cells;
        #endregion

        public AbstractGroup()
        {
            ExclusiveCells = new LinkedList<Cell>();
            Cells = new LinkedList<Cell>();
        }

        #region public methods
        /// <summary>
        /// Produce a deep copy of this group and it's components.
        /// </summary>
        /// <returns>The copy.</returns>
        public AbstractGroup DeepCopy()
        {
            return DeepCopy(this);
        }
        #endregion

        #region Public Static Methods
        /// <summary>
        /// Produce a deep copy of a group and it's components
        /// </summary>
        /// <returns>The copy.</returns>
        /// <param name="group">Group.</param>
        public static AbstractGroup DeepCopy(AbstractGroup group)
        {
            AbstractGroup copy = null;

            if (group is Repeat)
            {
                var r = group as Repeat;

				copy = new Repeat()
				{
					LastTermModifier = r.LastTermModifier,
					Length = r.Length,
					MultFactor = r.MultFactor,
					Position = r.Position,
					Row = r.Row,
					Times = r.Times,
                    BreakCell = r.BreakCell
				};
            }
            else if (group is Multiply)
            {
                var m = group as Multiply;

                copy = new Multiply()
                {
                    Factor = m.Factor,
                    FactorValue = m.FactorValue,
                    Length = m.Length,
                    Position = m.Position,
                    Row = m.Row
                };
            }

            Dictionary<AbstractGroup, AbstractGroup> copiedGroups = new Dictionary<AbstractGroup, AbstractGroup>();

            bool first = true;
            foreach (Cell c in group.Cells)
            {
                IEnumerable<(bool, AbstractGroup)> nested = c.GroupActions;

                // the first cell, we skip any opening groups up to and including the target
                if (first)
                {
                    nested = nested.SkipWhile(x => x.Item2 != group).Skip(1);

					first = false;
                }

                // only clone a group that is opening
                nested = nested.SkipWhile(x => !x.Item1);
                if (nested.Any())
                {
                    (bool _, AbstractGroup g) = nested.First();
                    AbstractGroup nestedCopy = DeepCopy(g);

                    copy.Cells = new LinkedList<Cell>(copy.Cells.Concat(nestedCopy.Cells));
                }
                else
                {
                    Cell copyCell = new Cell(group.Row)
                    {
                        Duration = c.Duration,
                        MultFactor = c.MultFactor,
                        Position = c.Position,
                        Source = c.Source,
                        Value = c.Value,
                        Reference = c.Reference,
                        IsBreak = c.IsBreak,
                    };

                    // add the group to the cell's collection
                    if (copy is Repeat)
                    {
						copyCell.RepeatGroups.AddLast(copy as Repeat);
                    }
                    else if (copy is Multiply)
                    {
                        copyCell.MultGroups.AddLast(copy as Multiply);
                    }

                    copy.ExclusiveCells.AddLast(copyCell);
                    copy.Cells.AddLast(copyCell);
                }
            }

            copy.ExclusiveCells.First.Value.GroupActions.AddFirst((true, copy));
            copy.ExclusiveCells.Last.Value.GroupActions.AddLast((false, copy));

            return copy;
        }
        #endregion
    }
}
