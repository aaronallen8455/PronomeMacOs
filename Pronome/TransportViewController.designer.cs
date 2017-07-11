// WARNING
//
// This file has been generated automatically by Visual Studio to store outlets and
// actions made in the UI designer. If it is removed, they will be lost.
// Manual changes to this file may not be handled correctly.
//
using Foundation;
using System.CodeDom.Compiler;

namespace Pronome
{
	[Register ("TransportViewController")]
	partial class TransportViewController
	{
		[Outlet]
		AppKit.NSButton PauseButton { get; set; }

		[Outlet]
		AppKit.NSButton PlayButton { get; set; }

		[Outlet]
		AppKit.NSButton StopButton { get; set; }

		[Outlet]
		AppKit.NSTextField TempoField { get; set; }

		[Action ("NewLayerAction:")]
		partial void NewLayerAction (Foundation.NSObject sender);

		[Action ("PauseButtonAction:")]
		partial void PauseButtonAction (Foundation.NSObject sender);

		[Action ("PlayButtonAction:")]
		partial void PlayButtonAction (Foundation.NSObject sender);

		[Action ("StopButtonAction:")]
		partial void StopButtonAction (Foundation.NSObject sender);
		
		void ReleaseDesignerOutlets ()
		{
			if (PauseButton != null) {
				PauseButton.Dispose ();
				PauseButton = null;
			}

			if (PlayButton != null) {
				PlayButton.Dispose ();
				PlayButton = null;
			}

			if (StopButton != null) {
				StopButton.Dispose ();
				StopButton = null;
			}

			if (TempoField != null) {
				TempoField.Dispose ();
				TempoField = null;
			}
		}
	}
}
