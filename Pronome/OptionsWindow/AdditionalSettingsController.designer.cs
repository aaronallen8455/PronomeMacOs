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
	[Register ("AdditionalSettingsController")]
	partial class AdditionalSettingsController
	{
		[Outlet]
		AppKit.NSTableView CustomSourceTable { get; set; }

		[Action ("ExportWavFileAction:")]
		partial void ExportWavFileAction (Foundation.NSObject sender);

		[Action ("RecordWavFileAction:")]
		partial void RecordWavFileAction (Foundation.NSObject sender);
		
		void ReleaseDesignerOutlets ()
		{
			if (CustomSourceTable != null) {
				CustomSourceTable.Dispose ();
				CustomSourceTable = null;
			}
		}
	}
}
