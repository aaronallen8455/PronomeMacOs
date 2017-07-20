using System;
using AppKit;
using Foundation;

namespace Pronome
{
    [Register("MyScroller")]
    public class MyScroller : NSScroller
    {
        public MyScroller(IntPtr pt) : base(pt)
        {
        }

        public MyScroller() : base()
        {
        }

        public override CoreGraphics.CGRect Frame
        {
            get
            {
                return new CoreGraphics.CGRect(0, 0, 567, 3);
            }
            set
            {
                base.Frame = value;
            }
        }
    }
}
