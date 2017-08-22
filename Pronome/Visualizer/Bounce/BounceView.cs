// This file has been autogenerated from a class added in the UI designer.

using System;

using Foundation;
using AppKit;
using Pronome.Mac.Visualizer.Bounce;
using System.Linq;
using CoreGraphics;
using CoreAnimation;
using Pronome.Mac.Visualizer;

namespace Pronome.Mac
{
    public partial class BounceView : AbstractVisualizerView
    {
        #region Protected properties
        protected Lane[] Lanes;

        protected Ball[] Balls;

        protected CALayer TickLayer;

        protected AnimationTimer Timer = new AnimationTimer();
        #endregion

        public BounceView(IntPtr handle) : base(handle)
        {
            AnimationLayer.Delegate = new BounceLayerDelegate();

            TickLayer = new CALayer();
            TickLayer.ContentsScale = NSScreen.MainScreen.BackingScaleFactor;

            Layer.AddSublayer(TickLayer);

            Metronome.Instance.Started += Instance_Started;
            Metronome.Instance.Stopped += Instance_Stopped;
            Metronome.Instance.BeatChanged += Instance_BeatChanged;
        }

        #region public methods
        public override void DrawFrame()
        {
			if (Metronome.Instance.PlayState == Metronome.PlayStates.Stopped)
			{
                // sometimes the drawFrame will run after playback is started, so we handle that case
                ReturnToInitialState();
			}
            else
            {
                BounceHelper.ElapsedBpm = Timer.GetElapsedBpm();
				
				// animate the balls
				//foreach (Ball ball in Balls)
				//{
				//	ball.DrawFrame();
				//}
            }
			// animate the tick marks
			//TickLayer.SetNeedsDisplay();
            DrawElements();
        }
        #endregion

        #region protected methods
        protected override void CreateAssets()
        {
			// dispose old ones if exist
			if (Lanes != null)
			{
				for (int i = 0; i < Lanes.Length; i++)
				{
					Lanes[i].Dispose();
					Balls[i].Dispose();
				}

				TickLayer.Delegate.Dispose();
			}

			Lanes = Metronome.Instance.Layers.Select(x => new Lane(x)).ToArray();
			Balls = Metronome.Instance.Layers.Select(x => new Ball(x, TickLayer)).ToArray();

			TickLayer.Delegate = new TickMarksDelegate(Lanes);
        }

        protected override CGRect GetRect(nfloat winWidth, nfloat winHeight)
        {
            nfloat min = winWidth < winHeight ? winWidth : winHeight;

            // a value from 0 to 1
            nfloat spread = UserSettings.GetSettings().BounceWidthPad * 2;
            nfloat width, height, xPos, yPos;

            var ratio = winHeight / winWidth;
            var spreadRatio = 1 / (spread + 1);

            if (spreadRatio > ratio)
            {
                height = winHeight;
                width = height * (spread + 1);

                yPos = 0;
                xPos = winWidth / 2 - width / 2;
            }
            else
            {
                width = winWidth;
				height = width / (1 + spread);

                xPos = 0;
                yPos = winHeight / 2 - height / 2;
            }

            BounceHelper.SetDimensions(width, height);

            // create the elements. We need to initialize them here when all the dimensions are set.
            //CreateAssets();

            return new CGRect(xPos, yPos, width, height);
        }

        /// <summary>
        /// Render the ball and tick elements
        /// </summary>
        protected void DrawElements()
        {
			foreach (Ball ball in Balls)
			{
				ball.DrawFrame();
			}

            TickLayer.SetNeedsDisplay();
        }

        /// <summary>
        /// Return to initial state and draw it.
        /// </summary>
        protected void ReturnToInitialState()
        {
			BounceHelper.ElapsedBpm = 0;
            Timer.Reset();

			foreach (Ball ball in Balls)
			{
				ball.GoToInitialPosition();
			}

            ((TickMarksDelegate)TickLayer.Delegate).Reset();
        }
        #endregion

        void Instance_Started(object sender, Metronome.StartedEventArgs e)
        {
            if (e.PreviousState == Metronome.PlayStates.Stopped)
            {
				foreach (Lane lane in Lanes)
				{
					lane.Reset();
				}
				foreach (Ball ball in Balls)
				{
					ball.Reset();
				}
            }
        }

        void Instance_Stopped(object sender, EventArgs e)
        {
            ReturnToInitialState();
            DrawElements();
        }

        void Instance_BeatChanged(object sender, EventArgs e)
        {
            // need to create all new elements; recreate everything
            BounceHelper.SetDimensions(Layer.Frame.Width, Layer.Frame.Height);
            AnimationLayer.SetNeedsDisplay();
            CreateAssets();

            ReturnToInitialState();

			//Timer.Reset();
            BounceHelper.ElapsedBpm = Metronome.Instance.ElapsedBpm;

            // progress the tick layer
            ((TickMarksDelegate)TickLayer.Delegate).BpmToProgress = Metronome.Instance.ElapsedBpm;

            //if (Metronome.Instance.PlayState != Metronome.PlayStates.Playing)
            //{
            DrawElements();
            //}
        }

        protected override void Window_DidResize(object sender, EventArgs e)
        {
            // get relative y-position of balls to use after superLayer frame is resized
            double[] yPos = new double[Balls.Length];
            for (int i = 0; i < Balls.Length; i++)
            {
                yPos[i] = Balls[i].BallLayer.Frame.Y / BounceHelper.Height;
            }

            base.Window_DidResize(sender, e);

            CATransaction.Begin();
            CATransaction.DisableActions = true;
            for (int i = 0; i < Balls.Length; i++)
            {
                Balls[i].InitLayer(i);
                // need to preserve the y-position
                var frame = Balls[i].BallLayer.Frame;
                frame.Y = (nfloat)(BounceHelper.Height * yPos[i]);
                Balls[i].BallLayer.Frame = frame;
                Balls[i].BallLayer.SetNeedsDisplay();
            }
            CATransaction.Commit();
        }
    }
}
