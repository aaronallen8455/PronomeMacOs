using AppKit;

namespace Pronome.Mac.OptionsWindow
{
    public class OptionsWindowDelegate : NSWindowDelegate
    {
        NSWindow Window;

        public OptionsWindowDelegate(NSWindow window)
        {
            Window = window;
        }

        /// <summary>
        /// Window closes or loses focus, We clear first responder so that field being edited gets applied
        /// </summary>
        /// <param name="notification">Notification.</param>
        public override void DidResignKey(Foundation.NSNotification notification)
        {
            Window.MakeFirstResponder(null);
        }
    }
}
