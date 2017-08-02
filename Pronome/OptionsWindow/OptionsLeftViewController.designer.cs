// WARNING
//
// This file has been generated automatically by Visual Studio to store outlets and
// actions made in the UI designer. If it is removed, they will be lost.
// Manual changes to this file may not be handled correctly.
//
using Foundation;
using System.CodeDom.Compiler;

namespace Pronome.Mac
{
	[Register ("OptionsLeftViewController")]
	partial class OptionsLeftViewController
	{
		[Outlet]
		Pronome.Mac.OptionListView SourceList { get; set; }
		
		void ReleaseDesignerOutlets ()
		{
			if (SourceList != null) {
				SourceList.Dispose ();
				SourceList = null;
			}
		}
	}
}
