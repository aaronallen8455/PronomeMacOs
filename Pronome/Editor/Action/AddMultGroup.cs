using System;
using Pronome.Mac.Editor.Groups;

namespace Pronome.Mac.Editor.Action
{
    public class AddMultGroup : AbstractAddGroup<Multiply>
    {
        public AddMultGroup(Multiply group, CellTree cells) : base(group, cells, "Add Multiply Group")
        {
        }
    }
}
