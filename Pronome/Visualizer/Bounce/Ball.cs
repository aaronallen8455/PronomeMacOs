using System;
using AppKit;
using CoreAnimation;
using CoreGraphics;
using Foundation;

namespace Pronome.Mac.Visualizer.Bounce
{
    public class Ball
    {
        #region protected fields
        protected Layer Layer;

        protected CALayer BallLayer;

        protected double CurrentInterval;

        protected double Apex;

        protected double[] CellValues;

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
                _beatIndex = value % CellValues.Length;
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
                Delegate = new BallDelegate(ColorHelper.ColorWheel(layerInd))
            };

            InitLayer(layerInd);

            superLayer.AddSublayer(BallLayer);

            //BallLayer.SetNeedsDisplay();

            // init the cell values
        }
        #endregion

        #region Public methods
        public void DrawFrame()
        {
            double elapsedBpm = BounceHelper.ElapsedBpm;
            double ballAreaH = BounceHelper.BallAreaHeight;
            double horiz = BounceHelper.LaneAreaHeight;

            CurrentInterval -= elapsedBpm;

            while (CurrentInterval < 0)
            {
				CurrentInterval += Layer.Beat[BeatIndex].Bpm;
                double amt = Layer.Beat[BeatIndex++].Bpm;

                while (Layer.Beat[BeatIndex].StreamInfo == StreamInfoProvider.InternalSourceLibrary[0])
                {
                    amt += Layer.Beat[BeatIndex].Bpm;
                    CurrentInterval += Layer.Beat[BeatIndex++].Bpm;
                }

                Apex = amt;
            }

            // position the ball layer
            CATransaction.Begin();
            CATransaction.DisableActions = true;

            // func is y=-4(x-.5)^2+1
            double factor = CurrentInterval / Apex;
            factor = -4 * Math.Pow(factor - .5, 2) + 1;

			var curPos = BallLayer.Position;
            BallLayer.Position = new CGPoint(curPos.X, factor * ballAreaH + horiz);

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
                context.AddEllipseInRect(layer.Frame);

                context.Clip();

                var gradient = new CGGradient(
                    CGColorSpace.CreateDeviceRGB(),
                    new CGColor[] { NSColor.White.CGColor, Color }
                );

                var specCenter = new CGPoint(
                    layer.Frame.Width * .4,
                    layer.Frame.Height * .6
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
