using AppKit;
using Foundation;

namespace Pronome.Mac
{
    [Register("AppDelegate")]
    public partial class AppDelegate : NSApplicationDelegate
    {
        public AppDelegate()
        {
        }

        public override void DidFinishLaunching(NSNotification notification)
        {
            // Insert code here to initialize your application
            //UserSettings.GetSettings().GetSettingsFromStorage();
        }

        public override void WillTerminate(NSNotification notification)
        {
            // Insert code here to tear down your application
            UserSettings.GetSettings().SaveToStorage();

            Metronome.Instance.Cleanup();
        }

        /// <summary>
        /// Open a beat file.
        /// </summary>
        /// <param name="sender">Sender.</param>
        partial void OpenFileAction(NSObject sender)
        {
            var dlg = NSOpenPanel.OpenPanel;
            dlg.CanChooseFiles = true;
            dlg.CanChooseDirectories = false;
            dlg.AllowedFileTypes = new string[] { "beat" };

            if (dlg.RunModal() == 1)
            {
                // get the first file
                NSUrl url = dlg.Urls[0];

                if (url != null)
                {
                    string path = url.Path;
                    // load the beat
                    SavedFileManager.Load(path);
                }
            }
        }

        partial void SaveFileAction(NSObject sender)
        {
            if (string.IsNullOrEmpty(SavedFileManager.CurrentlyOpenFile))
            {
                SaveFileAsAction(null);
                return;
            }

            SavedFileManager.Save(SavedFileManager.CurrentlyOpenFile);
        }

        partial void SaveFileAsAction(NSObject sender)
        {
            var dlg = new NSSavePanel();
            dlg.Title = "Save Beat File";
            dlg.AllowedFileTypes = new string[] { "beat" };

            if (dlg.RunModal() == 1)
            {
                NSUrl url = dlg.Url;

                if (url != null)
                {
                    string path = url.Path;
                    SavedFileManager.Save(path);
                }
            }
        }

        partial void RevertToSavedAction(NSObject sender)
        {
            if (string.IsNullOrEmpty(SavedFileManager.CurrentlyOpenFile)) return;

            SavedFileManager.Load(SavedFileManager.CurrentlyOpenFile);
        }
    }
}
