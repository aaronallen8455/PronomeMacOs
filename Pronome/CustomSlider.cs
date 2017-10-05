// This file has been autogenerated from a class added in the UI designer.

using System;

using Foundation;
using AppKit;
using CoreGraphics;

namespace Pronome.Mac
{
	public partial class CustomSlider : NSSlider
	{
		public CustomSlider (IntPtr handle) : base (handle)
		{
		}

        public override void DrawRect(CGRect dirtyRect)
        {
            // draw our custom interface
            // doing anything other than adding a background created issues

            //double ratio = (DoubleValue - MinValue) / (MaxValue - MinValue);
            CGRect baseRect = new CGRect(dirtyRect.X + 4, dirtyRect.Y, dirtyRect.Width - 8, dirtyRect.Height);

            //dirtyRect.Height -= 10;
            //dirtyRect.Width -= 4;
            //dirtyRect.X += 2;
            //dirtyRect.Y += 5;
            //double finalWidth = ratio * (baseRect.Width);
            var bg = NSBezierPath.FromRoundedRect(dirtyRect, 2.5f, 2.5f);
            NSColor.ScrollBar.SetFill();
            bg.Fill();
            //CGRect active = new CGRect(baseRect.X, baseRect.Y, (nfloat)(finalWidth), dirtyRect.Height);

            //var fg = NSBezierPath.FromRoundedRect(active, 2.5f, 2.5f);
            //NSColor.Magenta.SetFill();
            //fg.Fill();

            base.DrawRect(dirtyRect);
        }
	}
}
