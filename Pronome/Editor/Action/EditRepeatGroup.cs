using Pronome.Mac.Editor.Groups;

namespace Pronome.Mac.Editor.Action
{
    public class EditRepeatGroup : AbstractBeatCodeAction
    {
        Repeat Group;
        string Ltm;
        int Times;

        public EditRepeatGroup(Repeat group, string ltm, int times) : base(group.ExclusiveCells.First.Value.Row, "Edit Repeat Group")
        {
            Group = group;
            Ltm = ltm;
            Times = times;
        }

        protected override void Transformation()
        {
            Group.Times = Times;
            Group.LastTermModifier = Ltm;
            ChangesViewWidth = true;
        }

        public override bool CanPerform()
        {
            // checked by controller
            return true;
        }
    }
}
