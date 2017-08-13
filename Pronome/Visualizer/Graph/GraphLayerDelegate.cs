using System;
using Foundation;
using CoreAnimation;
using CoreGraphics;
using System.Collections.Generic;

namespace Pronome.Mac.Visualizer.Graph
{
    /// <summary>
    /// Responsible for drawing the needle animation and interfacing with the ring elements.
    /// </summary>
    public class GraphLayerDelegate : AbstractLayerDelegate
    {
        /// <summary>
        /// The ring display elements.
        /// </summary>

        /// <summary>
        /// The length of the beat in bpm.
        /// </summary>
        public double BeatLength;

        const double TWOPI = 2 * Math.PI;

        public GraphLayerDelegate()
        {
        }

        [Export("drawLayer:inContext:")]
        public void DrawLayer(CALayer layer, CGContext context)
        {
            context.SaveState();
            // draw the current frame

            context.RestoreState();
            // draw the needle
            nfloat angle = (nfloat)(ElapsedBpm / BeatLength * TWOPI);

            context.RotateCTM(angle);
            var mid = (int)layer.Frame.Width / 2;
            context.MoveTo(mid, 0);
            context.AddLineToPoint(mid,mid);
        }
    }
}
