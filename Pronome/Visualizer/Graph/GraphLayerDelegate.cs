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
        /// The length of the beat in bpm.
        /// </summary>
        public double BeatLength;

        const double TWOPI = 2 * Math.PI;

        [Export("drawLayer:inContext:")]
        public void DrawLayer(CALayer layer, CGContext context)
        {
            if (Metronome.Instance.PlayState == Metronome.PlayStates.Stopped) return;

            CATransaction.DisableActions = true;

            context.SaveState();
            context.SetLineWidth(2);
            context.SetStrokeColor(NSColor.Green.CGColor);

            var nn = Metronome.Instance.ElapsedBpm;
            // draw the needle
            nfloat angle = (nfloat)((Metronome.Instance.ElapsedBpm % BeatLength) / BeatLength * TWOPI);

			var mid = (int)layer.Frame.Width / 2;
            context.TranslateCTM(mid,mid);
            context.RotateCTM(-angle);
            context.MoveTo(0, 0);
            context.AddLineToPoint(0, mid);

            context.StrokePath();

			context.RestoreState();
        }
    }
}
