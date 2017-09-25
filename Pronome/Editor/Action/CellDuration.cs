using System;
using System.Linq;

namespace Pronome.Mac.Editor.Action
{
	public class CellDuration : AbstractBeatCodeAction
	{
        protected CellTree Cells;

		protected string NewValue;

		/// <summary>
		/// Should be done before changing the cell properties.
		/// </summary>
		/// <param name="cells"></param>
		/// <param name="newValue">What will be the new value</param>
        public CellDuration(CellTree cells, string newValue) : base(cells.Root.Cell.Row, cells.Count > 1 ? "Change Duration of Cells" : "Change Cell Duration")
		{
			Cells = cells;
			NewValue = newValue;
		}

		protected override void Transformation()
		{
            foreach (Cell c in Cells)
            {
                c.Value = NewValue;
            }

			Cells = null;
		}

        public override bool CanPerform()
        {
            return true;
        }
	}
}
