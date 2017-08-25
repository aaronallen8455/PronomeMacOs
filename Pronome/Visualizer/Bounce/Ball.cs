using System;
using AppKit;
using CoreAnimation;
using CoreGraphics;
using Foundation;
using System.Linq;

namespace Pronome.Mac.Visualizer.Bounce
{
    public class Ball
    {
        const double SecsForMaxHeight = .6;

        #region protected fields
        protected Layer Layer;

        public CALayer BallLayer;

        protected double CurrentInterval;

        protected double Apex;

        protected int BounceHeight;

        protected double Offset;
        #endregion

        #region protected properties
        int _beatIndex;
		/// <summary>
		/// Index of the next cell to be in queue of this lane's layer
		/// </summary>
		protected int BeatIndex
		{
			get => _beatIndex;
			set
			{
                _beatIndex = value % Layer.Beat.Count;
			}
		}

        public int Index;

        protected bool LayerIsSilent;
        #endregion

        #region Constructor
        public Ball(Layer layer, CALayer superLayer)
        {
            Layer = layer;
            Index = Metronome.Instance.Layers.IndexOf(layer);

            BallLayer = new CALayer()
            {
                ContentsScale = NSScreen.MainScreen.BackingScaleFactor,
                Delegate = new BallDelegate(ColorHelper.ColorWheel(Index)),
            };

            // check if layer is silent
            LayerIsSilent = Layer.GetAllStreams().All(x => StreamInfoProvider.IsSilence(x.Info));

            InitLayer();

            superLayer.AddSublayer(BallLayer);
            // position element (with offset)
            GoToInitialPosition();
            // draw initial state
            BallLayer.SetNeedsDisplay();
        }
        #endregion

        #region Public methods

        //public void SetXPosition()
        //{
        //    var frame = BallLayer.Frame;
		//
        //    frame.X = (nfloat)(BounceHelper.LanePadding + BounceHelper.TopLaneSpacing * Index + BounceHelper.BallPadding);
		//
        //    BallLayer.Frame = frame;
        //}

        /// <summary>
        /// Place the ball at it's starting position
        /// </summary>
        public void GoToInitialPosition()
        {
            if (LayerIsSilent) return;

            CurrentInterval = Layer.OffsetBpm;
            BeatIndex = 0;

            while (StreamInfoProvider.IsSilence(Layer.Beat[BeatIndex].StreamInfo))
            {
                CurrentInterval += Layer.Beat[BeatIndex++].Bpm;
            }

            Apex = CurrentInterval * 2;

			double secs = Apex * 60 / Metronome.Instance.Tempo;
			if (secs >= SecsForMaxHeight)
			{
				BounceHeight = (int)(BounceHelper.BallAreaHeight - BounceHelper.BallSize);
			}
			else
			{
				BounceHeight = (int)(secs / SecsForMaxHeight * (BounceHelper.BallAreaHeight - BounceHelper.BallSize));
			}

            BounceHelper.ElapsedBpm = 0;

			DrawFrame();
        }

        /// <summary>
        /// Draws a frame.
        /// </summary>
        public void DrawFrame()
        {
            if (LayerIsSilent || Layer.Beat == null) return;

            double elapsedBpm = BounceHelper.ElapsedBpm;
            double horiz = BounceHelper.LaneAreaHeight;

            CurrentInterval -= elapsedBpm;

            while (CurrentInterval < 0)
            {
                (Apex, BounceHeight, BeatIndex, CurrentInterval) = GetApexAndBounceHeight(BeatIndex, CurrentInterval);
            }

            // position the ball layer
            CATransaction.Begin();
            CATransaction.DisableActions = true;

            double y = horiz + BounceHelper.BallSize / 2;
            if (Apex > 0)
            {
				// func is y=-4(x-.5)^2+1
				double factor = CurrentInterval / Apex;
				factor = -4 * Math.Pow(factor - .5, 2) + 1;
				
                y += factor * BounceHeight;
            }

			var curPos = BallLayer.Position;
            BallLayer.Position = new CGPoint(curPos.X, y);

            CATransaction.Commit();
        }

        /// <summary>
        /// Sets the rect size of the ball layer
        /// </summary>
        /// <param name="index">Index.</param>
        public void InitLayer()
        {
            CGRect frame = new CGRect(
                BounceHelper.LanePadding + BounceHelper.TopLaneSpacing * Index + BounceHelper.BallPadding,
                BounceHelper.LaneAreaHeight,
                BounceHelper.BallSize,
                BounceHelper.BallSize
            );

            BallLayer.Frame = frame;
        }

        public void Dispose()
        {
            CATransaction.Begin();
            CATransaction.DisableActions = true;
            BallLayer.RemoveFromSuperLayer();
            CATransaction.Commit();

            BallLayer.Dispose();
            //Metronome.Instance.Started -= Instance_Started;
        }

        public void Reset()
        {
            BeatIndex = 0;
            CurrentInterval = Layer.OffsetBpm;
            // account for silence at start of beat.
            while (StreamInfoProvider.IsSilence(Layer.Beat[BeatIndex].StreamInfo))
            {
                CurrentInterval += Layer.Beat[BeatIndex].Bpm;
                BeatIndex++;
            }
        }
        #endregion

        #region Protected Methods
        public (double Apex, int BounceHeight, int Index, double CurrentInterval) GetApexAndBounceHeight(int beatIndex, double currentInterval)
        {
            currentInterval += Layer.Beat[beatIndex].Bpm;
            double apex = Layer.Beat[beatIndex++].Bpm;
            beatIndex %= Layer.Beat.Count;

            while (StreamInfoProvider.IsSilence(Layer.Beat[beatIndex].StreamInfo))
			{
				apex += Layer.Beat[beatIndex].Bpm;
				currentInterval += Layer.Beat[beatIndex++].Bpm;
                beatIndex %= Layer.Beat.Count;
			}

            int bounceHeight;
			// calculate bounce height based on duration in sec of current interval
			// if greater than or equal to .4 secs, use whole area, otherwise
			// use a fraction of height

			double secs = apex * 60 / Metronome.Instance.Tempo;
			if (secs >= SecsForMaxHeight)
			{
				bounceHeight = (int)(BounceHelper.BallAreaHeight - BounceHelper.BallSize);
			}
			else
			{
				bounceHeight = (int)(secs / SecsForMaxHeight * (BounceHelper.BallAreaHeight - BounceHelper.BallSize));
			}

            return (apex, bounceHeight, beatIndex, currentInterval);
        }
        #endregion

        /// <summary>
        /// Used to draw the ball image to a CALayer
        /// </summary>
        public class BallDelegate : NSObject, ICALayerDelegate
        {
            CGColor Color;

            public BallDelegate(CGColor color)
            {
                Color = color;
            }

            [Export("drawLayer:inContext:")]
            public void DrawLayer(CALayer layer, CGContext context)
            {
                CATransaction.DisableActions = true;
                CATransaction.AnimationDuration = 0;
                // draw the ball
                context.AddEllipseInRect(layer.Bounds);

                context.Clip();
				
                var gradient = new CGGradient(
                    CGColorSpace.CreateDeviceRGB(),
                    new CGColor[] { NSColor.White.CGColor, Color },
                    new nfloat[] { 0, .5f }
                );
				
                var specCenter = new CGPoint(
                    layer.Frame.Width * .3,
                    layer.Frame.Height * .7
                );
				
                context.DrawRadialGradient(
                    gradient,
                    specCenter,
                    0,
                    specCenter,
                    layer.Frame.Width,
                    CGGradientDrawingOptions.None
                );
            }
        }
    }
}
