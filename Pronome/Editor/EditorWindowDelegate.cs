using System;
using AppKit;

namespace Pronome.Mac.Editor
{
    public class EditorWindowDelegate : NSWindowDelegate
    {
        public NSWindow Window { get; set; }

        public EditorWindowDelegate(NSWindow window)
        {
            Window = window;
        }

        public override bool WindowShouldClose(Foundation.NSObject sender)
        {
            var controller = Window.ContentViewController as EditorViewController;

            if (!controller.DView.ChangesApplied)
            {
                //controller.PerformSegue("ConfirmCloseSegue", controller);
                var alert = new NSAlert()
                {
                    AlertStyle = NSAlertStyle.Informational,
                    InformativeText = "Changes were made in the editor that hve not been applied to the beat. Do you want to apply the changes or discard them before closing the editor?",
                    MessageText = "Apply Changes?"
                };
                alert.AddButton("Apply Changes");
                alert.AddButton("Discard Changes");
                alert.AddButton("Cancel");
                var result = alert.RunSheetModal(Window);
                // take action based on result
                switch (result)
                {
                    case 1000:
                        controller.ApplyChangesAction();
                        return true;
                    case 1001:
                        // discard
                        return true;
                    default:
                        // cancel
                        return false;
                }
            }

            return true;
        }
    }
}
