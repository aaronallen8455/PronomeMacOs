using System;
using Foundation;
using CoreAnimation;

namespace Pronome.Mac.Visualizer.Bounce
{
    public class TickMarksDelegate : NSObject, ICALayerDelegate
    {
        protected Lane[] Lanes;

        public TickMarksDelegate(Lane[] lanes)
        {
            Lanes = lanes;
        }

        [Export("drawLayer:inContext:")]
        public void DrawLayer(CALayer layer, CoreGraphics.CGContext context)
        {
            CATransaction.DisableActions = true;
            context.SaveState();

            foreach (Lane lane in Lanes)
            {
                lane.DrawFrame(context);

                context.TranslateCTM((nfloat)BounceHelper.BottomLaneSpacing,0);
            }

            context.RestoreState();
        }
    }
}
