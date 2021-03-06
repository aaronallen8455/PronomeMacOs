﻿using System;
namespace Pronome.Mac.Visualizer.Bounce
{
    public class BounceHelper
    {
        #region Static Public fields
        /// <summary>
        /// Number of bpms that have elapsed since last frame was drawn.
        /// </summary>
        static public double ElapsedBpm;

        /// <summary>
        /// Spacing between the lanes at the bottom of the screen.
        /// </summary>
        static public double BottomLaneSpacing;

        /// <summary>
        /// Size of padding on either side of ball.
        /// </summary>
        static public double BallPadding;

        /// <summary>
        /// The diameter of the ball elements.
        /// </summary>
        static public int BallSize;

		/// <summary>
		/// The height of the lanes in pxs.
		/// </summary>
		static public double LaneAreaHeight;

        /// <summary>
        /// Distance from horizon to top of window
        /// </summary>
        static public double BallAreaHeight;

        /// <summary>
        /// Space between the edge of screen and the top of the lanes
        /// </summary>
        static public double LanePadding;

		/// <summary>
		/// Spacing between the lanes at the top of the queue area. Setting this also sets ball size.
		/// </summary>
		static public double TopLaneSpacing;
		
		/// <summary>
		/// The total width. Also sets the bottomLaneSpacing
		/// </summary>
		/// <value>The width of the bottom.</value>
		static public double BottomWidth;
		
		/// <summary>
		/// Gets or sets the width at the top of lanes.
		/// </summary>
		/// <value>The width of the top lane.</value>
		static public double TopLaneWidth;
		
		/// <summary>
		/// Gets or sets the height of the drawing.
		/// </summary>
		/// <value>The height.</value>
		static public double Height;
        #endregion

        #region Static protected properties
        /// <summary>
        /// Y value of the point where all lane lines converge
        /// </summary>
        static protected double Apex;

        /// <summary>
        /// Used by tick easing function.
        /// </summary>
        static protected double Factor;

        /// <summary>
        /// used by tick easing function.
        /// </summary>
        static protected double Denominator;

        /// <summary>
        /// The last recorded elapsedBPM, since beat started playing. Used to find the increment
        /// </summary>
        static protected double LastElapsedBpm;
        #endregion

        #region Static constructor
        static BounceHelper()
        {
            Metronome.Instance.Started += Instance_Started;
        }
        #endregion

        #region Static public methods
        static public void SetDimensions(nfloat width, nfloat height)
        {
            BottomWidth = width;
            BottomLaneSpacing = width / Metronome.Instance.Layers.Count;
            Height = height;
            LaneAreaHeight = height * UserSettings.GetSettings().BounceDivision;
            BallAreaHeight = height - LaneAreaHeight;
            TopLaneWidth = width - (width - height);
            TopLaneSpacing = TopLaneWidth / Metronome.Instance.Layers.Count;
            BallSize = (int)(TopLaneSpacing * .5);//(TopLaneSpacing - BallPadding * TopLaneSpacing * 2);
            BallPadding = TopLaneSpacing / 2 - BallSize / 2;
            LanePadding = (width - TopLaneWidth) / 2;

            // used by easing function
            Apex = LaneAreaHeight / LanePadding * (BottomWidth / 2) / LaneAreaHeight;
            Factor = -Math.Log(1 - (1 / Apex), 2);
            Denominator = -Math.Pow(2, -Factor) + 1;
        }

        /// <summary>
        /// Gets the left and right lane slope.
        /// </summary>
        /// <returns>The lane slope.</returns>
        /// <param name="index">Index.</param>
        static public (double,double) GetLaneSlope(int index)
        {
            var lp = LanePadding;
            var tls = TopLaneSpacing;
            var bls = BottomLaneSpacing;

            double run = LanePadding + TopLaneSpacing * index - BottomLaneSpacing * index;
            double left = LaneAreaHeight / run;

            run = LanePadding + TopLaneSpacing * (index + 1) - BottomLaneSpacing * (index + 1);
            double right = LaneAreaHeight / run;

            return (left, right);
        }

        static public double EasingFunction(double input)
        {
            if (UserSettings.GetSettings().BounceWidthPad <= .01)
			{
				return input;
			}

            var f = Factor;
            var d = Denominator;

			return (-1 / (Math.Pow(2, input * Factor)) + 1) / Denominator;
        }

        /// <summary>
        /// Updates the elapsed bpm.
        /// </summary>
        static public void UpdateElapsedBpm()
        {
            double newElapsed = Metronome.Instance.ElapsedBpm;
            ElapsedBpm = newElapsed - LastElapsedBpm;
            LastElapsedBpm = newElapsed;
        }

        /// <summary>
        /// Resets the elasped bpm.
        /// </summary>
        static public void ResetElaspedBpm()
        {
            ElapsedBpm = 0;
            LastElapsedBpm = 0;//Metronome.Instance.ElapsedBpm;
        }
        #endregion

        static void Instance_Started(object sender, Metronome.StartedEventArgs e)
        {
            if (e.PreviousState == Metronome.PlayStates.Stopped)
            {
				ElapsedBpm = 0;
            }
        }
    }
}
