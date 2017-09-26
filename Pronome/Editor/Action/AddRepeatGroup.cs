using System;
using System.Linq;
using Pronome.Mac.Editor.Groups;

namespace Pronome.Mac.Editor.Action
{
    public class AddRepeatGroup : AbstractAddGroup<Repeat>
	{
        public AddRepeatGroup(Repeat repeat, CellTree cells) : base(repeat, cells, "Add Repeat Group")
		{
		}
	}
}
