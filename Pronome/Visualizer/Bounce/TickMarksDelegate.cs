using System;
using Foundation;
using CoreAnimation;

namespace Pronome.Mac.Visualizer.Bounce
{
    public class TickMarksDelegate : NSObject, ICALayerDelegate
    {
        public Lane[] Lanes;

        public double BpmToProgress;

        protected AnimationTimer Timer = new AnimationTimer();

        public TickMarksDelegate(Lane[] lanes)
        {
            Lanes = lanes;
        }

        [Export("drawLayer:inContext:")]
        public void DrawLayer(CALayer layer, CoreGraphics.CGContext context)
        {
            double elapsedBpm = BpmToProgress > 0 ? BpmToProgress : Timer.GetElapsedBpm();
            BpmToProgress = 0;

            CATransaction.DisableActions = true;
            context.SaveState();

            foreach (Lane lane in Lanes)
            {
                lane.DrawFrame(context, elapsedBpm);

                context.TranslateCTM((nfloat)BounceHelper.BottomLaneSpacing,0);
            }

            context.RestoreState();
        }

        public void Reset()
        {
            Timer.Reset();

            foreach (Lane lane in Lanes)
            {
                lane.Reset();
            }
        }
    }
}
