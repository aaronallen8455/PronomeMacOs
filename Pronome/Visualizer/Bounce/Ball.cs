using System;
using AppKit;
using CoreAnimation;
using CoreGraphics;
using Foundation;

namespace Pronome.Mac.Visualizer.Bounce
{
    public class Ball
    {
        const double SecsForMaxHeight = .6;

        #region protected fields
        protected Layer Layer;

        protected CALayer BallLayer;

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
        #endregion

        #region Constructor
        public Ball(Layer layer, CALayer superLayer)
        {
            Layer = layer;
            int layerInd = Metronome.Instance.Layers.IndexOf(layer);

            BallLayer = new CALayer()
            {
                ContentsScale = NSScreen.MainScreen.BackingScaleFactor,
                Delegate = new BallDelegate(ColorHelper.ColorWheel(layerInd)),
            };

            InitLayer(layerInd);

            superLayer.AddSublayer(BallLayer);

            GoInitialPosition();

            //DrawFrame();

            BallLayer.SetNeedsDisplay();
        }
        #endregion

        #region Public methods
        public void GoInitialPosition()
        {
            CurrentInterval = Layer.OffsetBpm;

            while (Layer.Beat[BeatIndex].StreamInfo == StreamInfoProvider.InternalSourceLibrary[0])
            {
                CurrentInterval += Layer.Beat[BeatIndex++].Bpm;
            }

            Apex = CurrentInterval;

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

            if (Apex > 0)
            {
				DrawFrame();
            }
        }

        public void DrawFrame()
        {
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

            // func is y=-4(x-.5)^2+1
            double factor = CurrentInterval / Apex;
            factor = -4 * Math.Pow(factor - .5, 2) + 1;

			var curPos = BallLayer.Position;
            var y = factor * BounceHeight + horiz + BounceHelper.BallSize / 2;
            BallLayer.Position = new CGPoint(curPos.X, y);

            CATransaction.Commit();
        }

        /// <summary>
        /// Sets the rect size of the ball layer
        /// </summary>
        /// <param name="index">Index.</param>
        public void InitLayer(int index)
        {

            CGRect frame = new CGRect(
                BounceHelper.LanePadding + BounceHelper.TopLaneSpacing * index + BounceHelper.BallPadding,
                BounceHelper.LaneAreaHeight,
                BounceHelper.BallSize,
                BounceHelper.BallSize
            );

            BallLayer.Frame = frame;
        }

        public void Dispose()
        {
            BallLayer.Dispose();
            //Metronome.Instance.Started -= Instance_Started;
        }

        public void Reset()
        {
            BeatIndex = 0;
            CurrentInterval = 0;
        }
        #endregion

        #region Protected Methods
        public (double Apex, int BounceHeight, int Index, double CurrentInterval) GetApexAndBounceHeight(int beatIndex, double currentInterval)
        {
            currentInterval += Layer.Beat[beatIndex].Bpm;
            double apex = Layer.Beat[beatIndex++].Bpm;
            beatIndex %= Layer.Beat.Count;

			while (Layer.Beat[beatIndex].StreamInfo == StreamInfoProvider.InternalSourceLibrary[0])
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
