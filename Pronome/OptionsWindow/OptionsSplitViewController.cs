using System;
using System.Collections.Generic;
using System.Linq;
using Foundation;
using AppKit;
using Pronome.Mac.OptionsWindow;

namespace Pronome.Mac
{
    public partial class OptionsSplitViewController : NSSplitViewController
    {
        #region Constructors

        // Called when created from unmanaged code
        public OptionsSplitViewController(IntPtr handle) : base(handle)
        {
        }

        #endregion

        #region Overriden Methods
        public override void ViewDidLoad()
        {
			base.ViewDidLoad();
            var left = SplitViewItems[0].ViewController as OptionsLeftViewController;
            var right = SplitViewItems[1].ViewController as OptionsRightViewController;

            // wireup the event for when the option section is changed
            left.ViewTypeChanged += right.ShowView;
        }
        #endregion
    }
}
