using System;
using System.Collections.Generic;
using System.Linq;
using Foundation;
using AppKit;

namespace Pronome
{
    public partial class MySplitView : NSSplitView
    {
        #region Constructors

        // Called when created from unmanaged code
        public MySplitView(IntPtr handle) : base(handle)
        {
            Initialize();
        }

        // Called when created directly from a XIB file
        [Export("initWithCoder:")]
        public MySplitView(NSCoder coder) : base(coder)
        {
            Initialize();
        }

        // Shared initialization code
        void Initialize()
        {
        }

        #endregion

        #region Overrides

        public override nfloat DividerThickness
        {
            get => 0;
        }

        #endregion
    }
}