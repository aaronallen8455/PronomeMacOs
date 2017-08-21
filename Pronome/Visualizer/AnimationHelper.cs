using System;
using System.Collections.Generic;
using System.Threading;
using CoreAnimation;
using Pronome.Mac.Visualizer;
using CoreVideo;
using AppKit;
using Pronome.Mac;

namespace Pronome.Mac
{
    /// <summary>
    /// This class helps to coordinate various animations
    /// </summary>
    public class AnimationHelper
    {
        #region Static Public Fields
        public static List<AbstractVisualizerView> AnimationViews = new List<AbstractVisualizerView>();

        //public static double BpmAccumulator;
        #endregion

        #region Protected Static Fields
        static protected double LastCycle;

        static protected int _inUseCount;
        #endregion

        #region Static Public Methods
        public static CVReturn RequestDraw(CVDisplayLink displayLink, ref CVTimeStamp inNow, ref CVTimeStamp inOutputTime, CVOptionFlags flagsIn, ref CVOptionFlags flagsOut)
        {
            NSApplication.SharedApplication.BeginInvokeOnMainThread( () => RequestDraw());

            return CVReturn.Success;
        }

        /// <summary>
        /// Starts the frame drawing process for all animation views.
        /// Finds the elapsed bpm and passes it to each view
        /// The view then orchestrates the drawing of it's independent layers
        /// </summary>
        /// <param name="currentCycle">Current cycle.</param>
        public static void RequestDraw()//double currentCycle)
        {
            // only let one thread in at a time
            if (Interlocked.Increment(ref _inUseCount) == 1 && AnimationViews.Count > 0)
            {
				//double elapsed = currentCycle - LastCycle;
				//LastCycle = currentCycle;
				
				//double numFrames = elapsed * Mixer.BufferSize;
				// convert frames to quarter-notes
                //double bpm = Metronome.Instance.ConvertSamplesToBpm(numFrames);

                //BpmAccumulator += bpm;

                // send the info to each animation layer to draw the new frame
                foreach (AbstractVisualizerView view in AnimationViews)
                {
                    view.DrawFrame();//bpm);
                }
            }

            Interlocked.Decrement(ref _inUseCount);
        }
        #endregion

        static AnimationHelper()
        {
            Metronome.Instance.Started += Instance_Started;
        }

        static void Instance_Started(object sender, Metronome.StartedEventArgs e)
        {
            // reset to starting position
            if (e.PreviousState == Metronome.PlayStates.Stopped)
            {
				LastCycle = 0;
            }
            //BpmAccumulator = 0;
        }

        private AnimationHelper()
        {
        }
    }
}
