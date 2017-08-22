using System;
using AppKit;
using CoreAnimation;
using CoreGraphics;
using Foundation;

namespace Pronome.Mac.Visualizer.Bounce
{
    public class BounceLayerDelegate : AbstractLayerDelegate
    {
        public BounceLayerDelegate()
        {
        }

        [Export("drawLayer:inContext:")]
        public void DrawLayer(CALayer layer, CGContext context)
        {
            CATransaction.DisableActions = true;
            CATransaction.AnimationDuration = 0;
            //nfloat width = layer.Frame.Width;
            //nfloat spread = UserSettings.GetSettings().BounceWidthPad;
            //nfloat division = UserSettings.GetSettings().BounceDivision;

            nfloat pad = (nfloat)BounceHelper.LanePadding; //(layer.Frame.Width - layer.Frame.Height) / 2;
            nfloat horiz = (nfloat)BounceHelper.LaneAreaHeight; //layer.Frame.Height * division;

            context.SetLineWidth(2);
            context.SetStrokeColor(NSColor.White.CGColor);
            // draw the left lane edge
            context.MoveTo(0, 0);
            context.AddLineToPoint(pad, horiz);

            // draw each lane divider
            nfloat horizSpacing = (nfloat)BounceHelper.TopLaneSpacing; //(width - 2 * pad) / Metronome.Instance.Layers.Count;
            nfloat baseSpacing = (nfloat)BounceHelper.BottomLaneSpacing; //width / Metronome.Instance.Layers.Count;
            for (int i = 1; i <= Metronome.Instance.Layers.Count; i++)
            {
                context.MoveTo(baseSpacing * i, 0);
                context.AddLineToPoint(pad + horizSpacing * i, horiz);
            }

            context.StrokePath();
        }
    }
}
