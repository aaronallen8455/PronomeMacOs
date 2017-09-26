using System;
namespace Pronome.Mac.Editor.Action
{
    public class RemoveReference : AbstractBeatCodeAction
    {
        Cell Cell;

        public RemoveReference(Cell cell) : base(cell.Row, "Remove Reference")
        {
            Cell = cell;
        }

        protected override void Transformation()
        {
            Cell.Reference = string.Empty;
        }

        public override bool CanPerform()
        {
            return true;
        }
    }
}
