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
	[Register ("QuantizedTapController")]
	partial class QuantizedTapController
	{
		[Outlet]
		AppKit.NSButton CountOffCheckBox { get; set; }

		[Action ("BeginAction:")]
		partial void BeginAction (Foundation.NSObject sender);

		[Action ("DoneAction:")]
		partial void DoneAction (Foundation.NSObject sender);

		[Action ("TapAction:")]
		partial void TapAction (Foundation.NSObject sender);
		
		void ReleaseDesignerOutlets ()
		{
			if (CountOffCheckBox != null) {
				CountOffCheckBox.Dispose ();
				CountOffCheckBox = null;
			}
		}
	}
}
