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
	[Register ("LayerItemController")]
	partial class LayerItemController
	{
		[Outlet]
		AppKit.NSBox BackgroundBox { get; set; }

		[Outlet]
		AppKit.NSComboBox SoundSourceSelector { get; set; }

		[Action ("CloseLayerAction:")]
		partial void CloseLayerAction (Foundation.NSObject sender);
		
		void ReleaseDesignerOutlets ()
		{
			if (BackgroundBox != null) {
				BackgroundBox.Dispose ();
				BackgroundBox = null;
			}

			if (SoundSourceSelector != null) {
				SoundSourceSelector.Dispose ();
				SoundSourceSelector = null;
			}
		}
	}
}
