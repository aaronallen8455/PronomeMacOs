using System;
using System.Linq;
using Pronome.Mac.Editor.Groups;

namespace Pronome.Mac.Editor.Action
{
	public class MoveCells : AbstractBeatCodeAction
	{
		protected Cell[] Cells;

		/// <summary>
		/// True if cells are being shifted to the right, otherwise shift left.
		/// </summary>
		protected bool ShiftingRight;

		protected string Increment;

		protected int Times;

		public MoveCells(Cell[] cells, string increment, int times) : base(cells[0].Row, cells.Length > 1 ? "Move Cells" : "Move Cell")
		{
			Cells = cells;
			ShiftingRight = times > 0;
			Increment = increment;
			Times = Math.Abs(times);
		}

		protected override void Transformation()
		{
			string value = BeatCell.MultiplyTerms(Increment, Math.Abs(Times));

			Cell last = Cells[Cells.Length - 1];
            Cell first = Cells[0];

            if (Row.Cells.Min.Cell == Cells[0])
			{
				// selection is at start of row, offset will be changed
				if (ShiftingRight)
				{
					// add to offset
					if (string.IsNullOrEmpty(Row.OffsetValue))
					{
						Row.OffsetValue = "0";
					}
                    Row.OffsetValue = BeatCell.Add(Row.OffsetValue, first.GetValueDividedByMultFactors(value));
					// subtract from last cell's value if not last cell of row
                    if (last != Row.Cells.Max.Cell)
					{
                        last.Value = BeatCell.Subtract(last.Value, last.GetValueDividedByMultFactors(value));
					}
				}
				else
				{
					// subtract from offset
                    Row.OffsetValue = BeatCell.Subtract(Row.OffsetValue, first.GetValueDividedByMultFactors(value));
					// zero becomes an empty string, make it zero.
					if (string.IsNullOrEmpty(Row.OffsetValue))
					{
						Row.OffsetValue = "0";
					}
					// add to last cell's value if not last cell of row
                    if (last != Row.Cells.Max.Cell)
					{
                        last.Value = BeatCell.Add(last.Value, last.GetValueDividedByMultFactors(value));
					}
				}
			}
			else
			{
                Cell below = Row.Cells.LookupIndex(Cells[0].Index - 1).Cell;
				// if below is last cell of a repeat group, we instead operate on that group's LTM
				Repeat leftGroup = below.RepeatGroups.Where(x => x.Cells.Last.Value == below).FirstOrDefault();
				bool useLeftGroup = leftGroup != default(Repeat);
				// if last cell in selection is last of a repeat group, operate on it's LTM
				Repeat rightGroup = last.RepeatGroups.Where(x => x.Cells.Last.Value == last).FirstOrDefault();
				bool useRightGroup = rightGroup != default(Repeat);

				if (ShiftingRight)
				{
					if (useLeftGroup)
					{
						// add to LTM
                        leftGroup.LastTermModifier = BeatCell.Add(leftGroup.LastTermModifier, leftGroup.GetValueDividedByMultFactor(value));
					}
					else
					{
						// add to below cell's value
                        below.Value = BeatCell.Add(below.Value, below.GetValueDividedByMultFactors(value));
					}
					// subtract from last cell's value if not last of row
                    if (last != Row.Cells.Max.Cell)
					{
						if (useRightGroup)
						{
							// subtract from LTM
                            rightGroup.LastTermModifier = BeatCell.Subtract(rightGroup.LastTermModifier, rightGroup.GetValueDividedByMultFactor(value));
						}
						else
						{
                            last.Value = BeatCell.Subtract(last.Value, last.GetValueDividedByMultFactors(value));
						}
					}
				}
				else
				{
					if (useLeftGroup)
					{
						// subtract from LTM
                        leftGroup.LastTermModifier = BeatCell.Subtract(leftGroup.LastTermModifier, leftGroup.GetValueDividedByMultFactor(value));
					}
					else
					{
						// subtract from below cell's value
                        below.Value = BeatCell.Subtract(below.Value, below.GetValueDividedByMultFactors(value));
					}
					// add to last cell's value if not last in row
                    if (last != Row.Cells.Max.Cell)
					{
						if (useRightGroup)
						{
                            rightGroup.LastTermModifier = BeatCell.Add(rightGroup.LastTermModifier, rightGroup.GetValueDividedByMultFactor(value));
						}
						else
						{
                            last.Value = BeatCell.Add(last.Value, last.GetValueDividedByMultFactors(value));
						}
					}
				}
			}

			Cells = null;
		}

        public override bool CanPerform()
        {
			double increment = BeatCell.Parse(Increment) * Times;
			//double pad = ShiftingRight ? .01 : -.01;

            if (ShiftingRight)
            {
                if (Cells.Last().Index == Row.Cells.Count - 1) return true;

                var next = Row.Cells.LookupIndex(Cells.Last().Index).Next();

                return next.Cell.Position > Cells.Last().Position + increment;
            }

            if (Cells.First().Index == 0) 
            {
                return Row.Offset >= increment;
            }

            var prev = Row.Cells.LookupIndex(Cells.First().Index).Prev();

            return prev.Cell.Position < Cells[0].Position - increment;
        }
	}
}
