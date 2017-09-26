using System;
namespace Pronome.Mac.Editor.Action
{
    public class AddEditReference : AbstractBeatCodeAction
    {
        Cell Cell;

        string Index;

        public AddEditReference(Cell cell, string index) : base(cell.Row, string.IsNullOrEmpty(cell.Reference) ? "Add Reference" : "Edit Reference")
        {
            Cell = cell;
            Index = index;
        }

        protected override void Transformation()
        {
            Cell.Reference = Index;
        }

        public override bool CanPerform()
        {
            return true;
        }
    }
}
