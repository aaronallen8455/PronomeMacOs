using System;
using Foundation;
using CoreAnimation;
using CoreGraphics;
using System.Collections.Generic;
using AppKit;

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

        public double BpmAccumulator;

        const double TWOPI = 2 * Math.PI;

        public GraphLayerDelegate()
        {
        }

        [Export("drawLayer:inContext:")]
        public void DrawLayer(CALayer layer, CGContext context)
        {
            if (Metronome.Instance.PlayState == Metronome.PlayStates.Stopped) return;

            context.SaveState();
            context.SetLineWidth(2);
            context.SetStrokeColor(NSColor.Green.CGColor);
            // draw the current frame
            //BpmAccumulator += ElapsedBpm;
            //BpmAccumulator %= BeatLength;
            // draw the needle
            nfloat angle = (nfloat)(BpmAccumulator / BeatLength * TWOPI);

			var mid = (int)layer.Frame.Width / 2;
            context.TranslateCTM(mid,mid);
            context.RotateCTM(-angle);
            context.MoveTo(0, 0);
            context.AddLineToPoint(0,mid);

            context.StrokePath();

			context.RestoreState();
        }
    }
}
