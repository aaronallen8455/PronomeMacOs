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
	[Register ("EditorViewController")]
	partial class EditorViewController
	{
		[Outlet]
		Pronome.Mac.DrawingView DrawingView { get; set; }

		[Outlet]
		AppKit.NSComboBox SourceSelector { get; set; }

		[Action ("ApplyChangesAction:")]
		partial void ApplyChangesAction (Foundation.NSObject sender);

		[Action ("EighthPresetAction:")]
		partial void EighthPresetAction (Foundation.NSObject sender);

		[Action ("EighthTripletPresetAction:")]
		partial void EighthTripletPresetAction (Foundation.NSObject sender);

		[Action ("QuarterPresetAction:")]
		partial void QuarterPresetAction (Foundation.NSObject sender);

		[Action ("QuarterTripletPresetAction:")]
		partial void QuarterTripletPresetAction (Foundation.NSObject sender);

		[Action ("SixteenthPresetAction:")]
		partial void SixteenthPresetAction (Foundation.NSObject sender);

		[Action ("SixteenthTripletPresetAction:")]
		partial void SixteenthTripletPresetAction (Foundation.NSObject sender);
		
		void ReleaseDesignerOutlets ()
		{
			if (DrawingView != null) {
				DrawingView.Dispose ();
				DrawingView = null;
			}

			if (SourceSelector != null) {
				SourceSelector.Dispose ();
				SourceSelector = null;
			}
		}
	}
}
