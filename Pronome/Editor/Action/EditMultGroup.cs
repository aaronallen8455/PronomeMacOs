using System;
using Pronome.Mac.Editor.Groups;

namespace Pronome.Mac.Editor.Action
{
    public class EditMultGroup : AbstractBeatCodeAction
    {
        Multiply Group;

        string Factor;

        public EditMultGroup(Multiply group, string factor) : base(group.Row, "Edit Multiply Group")
        {
            Group = group;
            Factor = factor;
        }

        protected override void Transformation()
        {
            Group.FactorValue = Factor;
            Group.Factor = BeatCell.Parse(Factor);
            ChangesViewWidth = true;
        }

        public override bool CanPerform()
        {
            return true;
        }
    }
}
