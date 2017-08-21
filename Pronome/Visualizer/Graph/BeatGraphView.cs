// This file has been autogenerated from a class added in the UI designer.

using System;
using Pronome.Mac.Visualizer.Graph;
using System.Collections.Generic;
using CoreAnimation;
using Pronome.Mac.Visualizer;
using CoreGraphics;

namespace Pronome.Mac
{
    public partial class BeatGraphView : AbstractVisualizerView
	{
        protected LinkedList<Ring> Rings;

        object _ringLock = new object();

		/// <summary>
		/// The length of the beat in bpm.
		/// </summary>
		protected double BeatLength;

        protected AnimationTimer Timer = new AnimationTimer();

        public BeatGraphView (IntPtr handle) : base (handle) 
        {
            // TODO: check that beat is graphable
            BeatLength = Metronome.Instance.GetQuartersForCompleteCycle();

			// build the graph, get rings
			Rings = GraphingHelper.BuildGraph(Layer, BeatLength);

            // attach the layer delegate
            AnimationLayer.Delegate = new GraphLayerDelegate()
            {
                BeatLength = BeatLength
            };

            Metronome.Instance.BeatChanged += Instance_BeatChanged;
        }

        public override void DrawFrame()//double bpm)
        {
            // determines duration of the blink effect
			CATransaction.AnimationDuration = .1;

			AnimationLayer.SetNeedsDisplay();

            lock (_ringLock)
            {
				// progress the rings. Animate any blinking if needed.
				foreach (Ring ring in Rings)
				{
                    ring.Progress(Timer.GetElapsedBpm());
				}
            }
        }

        protected override CGRect GetRect(nfloat winWidth, nfloat winHeight)
        {
            double size = Math.Min(winWidth, winHeight);
            int posX = (int)(winWidth / 2 - (size / 2));
            int posY = (int)(winHeight / 2 - (size / 2));
			return new CGRect(posX, posY, size, size);
		}

        void Instance_BeatChanged(object sender, EventArgs e)
        {
            // update the beat length
            BeatLength = Metronome.Instance.GetQuartersForCompleteCycle();

            ((GraphLayerDelegate)AnimationLayer.Delegate).BeatLength = BeatLength;

            var newRings = GraphingHelper.BuildGraph(Layer, BeatLength);

            // need to fast forward the new rings
            if (Metronome.Instance.PlayState != Metronome.PlayStates.Stopped)
            {
				foreach (Ring ring in newRings)
				{
                    ring.Progress(Metronome.Instance.ElapsedBpm);
				}
            }

            lock (_ringLock)
            {
				// replace old rings
				foreach (Ring ring in Rings)
				{
					ring.Dispose();
				}
				
				Rings = newRings;
            }
        }

        protected override void Window_DidResize(object sender, EventArgs e)
        {
            base.Window_DidResize(sender, e);

            // resize the rings
            foreach (Ring ring in Rings)
            {
                ring.InnerRadiusLocation = (nfloat)(ring.StartPoint * Layer.Frame.Width);
                ring.OuterRadiusLocation = (nfloat)(ring.EndPoint * Layer.Frame.Width);
            }
        }
    }
}
