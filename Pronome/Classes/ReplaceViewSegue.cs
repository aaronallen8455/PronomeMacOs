using System;
using Foundation;
using AppKit;
using CoreGraphics;

namespace Pronome.Mac
{
    /// <summary>
    /// A custom segue for swapping out one view with another
    /// </summary>
    [Register("ReplaceViewSegue")]
    public class ReplaceViewSegue : NSStoryboardSegue
    {
        #region Constructors
        public ReplaceViewSegue()
        {
        }

        public ReplaceViewSegue(string identifier, NSObject sourceController, NSObject destinationController) : base(identifier, sourceController, destinationController)
        {

        }

        public ReplaceViewSegue(IntPtr handle) : base(handle)
        {
        }

        public ReplaceViewSegue(NSObjectFlag x) : base(x)
        {
        }

        #endregion

        #region Override Methods
        public override void Perform()
        {
			// Cast the source and destination controllers
			var source = SourceController as NSViewController;
			var destination = DestinationController as NSViewController;

			// Is there a source?
			if (source == null)
			{
				// No, get the current key window
				var window = NSApplication.SharedApplication.KeyWindow;

                // resize and reposition the window to keep it from jumping around.
                var oldContentFrame = window.ContentViewController.View.Frame;
                var oldFrame = window.Frame;
                var location = oldFrame.Location;
                location.Y -= destination.View.Frame.Height - oldContentFrame.Height;
                oldFrame.Location = location;
                oldFrame.Height += destination.View.Frame.Height - oldContentFrame.Height;

				// Swap the controllers
				window.ContentViewController = destination;
                window.SetFrame(oldFrame, true);

				// Release memory
				window.ContentViewController?.RemoveFromParentViewController();
			}
			else
			{
                // Swap the controllers
                var m = source.View.Window.IsMovable = false;
				source.View.Window.ContentViewController = destination;
                var t = source.View.Window.ContentLayoutRect;

				// Release memory
				source.RemoveFromParentViewController();
			}
        }
        #endregion
    }
}
