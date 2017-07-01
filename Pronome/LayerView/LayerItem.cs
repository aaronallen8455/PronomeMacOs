using System;
using System.Collections.Generic;
using System.Linq;
using Foundation;
using AppKit;

namespace Pronome
{
    public partial class LayerItem : AppKit.NSView
    {
        #region Constructors

        // Called when created from unmanaged code
        public LayerItem(IntPtr handle) : base(handle)
        {
            Initialize();
        }

        // Called when created directly from a XIB file
        [Export("initWithCoder:")]
        public LayerItem(NSCoder coder) : base(coder)
        {
            Initialize();
        }

        // Shared initialization code
        void Initialize()
        {
        }

        #endregion
    }
}
