using System;
using AppKit;
using CoreAnimation;
using CoreGraphics;
using System.Collections.Generic;
using System.Linq;
using Foundation;

namespace Pronome.Mac.Visualizer.Graph
{
    /// <summary>
    /// Encompasses the drawing of the background and tick marks for a single ring section of a beat graph.
    /// </summary>
    public class Ring : IDisposable
    {
        const double TWOPI = 2 * Math.PI;

        #region Private Fields
        /// <summary>
        /// Draws the background gradient
        /// </summary>
        CALayer BackgroundLayer;

        /// <summary>
        /// Draws the tick marks
        /// </summary>
        CALayer TickMarksLayer;

        CALayer _superLayer;

        /// <summary>
        /// Index of the current beat cell
        /// </summary>
        int BeatIndex;

        double CurrentBpmInterval;

        /// <summary>
        /// The inner radius location.
        /// </summary>
        public nfloat InnerRadiusLocation;

        /// <summary>
        /// The outer radius location. 
        /// </summary>
        public nfloat OuterRadiusLocation;

        /// <summary>
        /// The value form 0 to 1 showing where the start point is. Multiply by frame size
        /// </summary>
        public double StartPoint;

        /// <summary>
        /// Value from 0 to 1 showing where the end point is. Multiply by frame size to get outerRadius
        /// </summary>
        public double EndPoint;
        #endregion

        #region Public Properties
        /// <summary>
        /// The beat layer to draw from
        /// </summary>
        /// <value>The layer.</value>
        public Layer Layer
        {
            get;
            protected set;
        }

        public LinkedList<nfloat> TickRotations
        {
            get;
            protected set;
        }
        #endregion

        #region constructor
        public Ring(Layer layer, CALayer superLayer, double startPoint, double endPoint, double beatLength)
        {
            Layer = layer;
            _superLayer = superLayer;

            // init the CALayers
            BackgroundLayer = new CALayer()
            {
				ContentsScale = NSScreen.MainScreen.BackingScaleFactor,
				//Frame = superLayer.Frame,
				Delegate = new BackgroundLayerDelegate(this)
            };

            TickMarksLayer = new CALayer()
            {
                ContentsScale = NSScreen.MainScreen.BackingScaleFactor,
                //Frame = superLayer.Frame,
                Delegate = new TickLayerDelegate(this, beatLength, layer),
                ZPosition = 5
            };

            superLayer.AddSublayer(BackgroundLayer);
            superLayer.AddSublayer(TickMarksLayer);

            // find the tick rotations
            TickRotations = new LinkedList<nfloat>();

            if (!Layer.GetAllStreams().All(x => StreamInfoProvider.IsSilence(x.Info)))
            {
                nfloat frontOffset = 0;
				foreach(BeatCell bc in layer.Beat)
				{
					if (StreamInfoProvider.IsSilence(bc.StreamInfo))
					{
						// add a silent value to the previous cell value
						if (TickRotations.Last != null)
						{
							TickRotations.Last.Value += (nfloat)(bc.Bpm / beatLength * TWOPI);
						}
                        else
                        {
                            frontOffset = (nfloat)(bc.Bpm / beatLength * TWOPI);
                        }
					}
					else
					{
						TickRotations.AddLast((nfloat)(bc.Bpm / beatLength * TWOPI));
					}
				}

                if (frontOffset > 0)
                {
                    TickRotations.Last.Value += frontOffset;
                }
            }

            InnerRadiusLocation = (nfloat)startPoint * superLayer.Frame.Width;
            OuterRadiusLocation = (nfloat)endPoint * superLayer.Frame.Width;

            StartPoint = startPoint;
            EndPoint = endPoint;

            //DrawStaticElements();

            // set the offset
            CurrentBpmInterval = Layer.OffsetBpm;
            while (StreamInfoProvider.IsSilence(Layer.Beat[BeatIndex].StreamInfo))
            {
                CurrentBpmInterval += Layer.Beat[BeatIndex++].Bpm;
                BeatIndex %= Layer.Beat.Count;
            }

            // do some reseting when playback stops
            Metronome.Instance.Stopped += Instance_Stopped;
        }
        #endregion

        #region Public methods
        public void Progress(double elapsedBpm)
        {
            if (Metronome.Instance.PlayState == Metronome.PlayStates.Stopped)
            {
                return;
            }

            CurrentBpmInterval -= elapsedBpm;

            var bgDelegate = (BackgroundLayerDelegate)BackgroundLayer.Delegate;

            // see if a cell played
            while (CurrentBpmInterval <= 0)
            {
                CurrentBpmInterval += Layer.Beat[BeatIndex++].Bpm;
                BeatIndex %= Layer.Beat.Count;

                // fold in the silent cells
                while (StreamInfoProvider.IsSilence(Layer.Beat[BeatIndex].StreamInfo))
                {
                    CurrentBpmInterval += Layer.Beat[BeatIndex++].Bpm;
                    BeatIndex %= Layer.Beat.Count;
                }

                //CurrentBpmInterval += Layer.Beat[BeatIndex].Bpm;

                // tell the ring to start a blink
                bgDelegate.BlinkingCountdown = BackgroundLayerDelegate.BlinkCount;
            }

            // trigger blink animation if enabled
            if (UserSettings.GetSettings().BlinkingEnabled && bgDelegate.BlinkingCountdown > 0) 
            {
                BackgroundLayer.SetNeedsDisplay();
            }
        }

        /// <summary>
        /// Draws the static elements - ticks and ring backgrounds.
        /// </summary>
        public void DrawStaticElements()
        {
            // draw background first
            BackgroundLayer.SetNeedsDisplay();
            // draw tick marks
            TickMarksLayer.SetNeedsDisplay();
        }

        public void SizeToSuperLayer()
        {
            var rect = _superLayer.Bounds;

            BackgroundLayer.Frame = rect;
            TickMarksLayer.Frame = rect;
        }

        public void Dispose()
        {
            Metronome.Instance.Stopped -= Instance_Stopped;
            BackgroundLayer.RemoveFromSuperLayer();
            TickMarksLayer.RemoveFromSuperLayer();
            BackgroundLayer.Dispose();
            TickMarksLayer.Dispose();
        }
        #endregion

        #region Layer delegates
        /// <summary>
        /// Draw the background
        /// </summary>
        class BackgroundLayerDelegate : NSObject, ICALayerDelegate
        {
            public const int BlinkCount = 2;

            /// <summary>
            /// Used to animate the blinking effect
            /// </summary>
            public int BlinkingCountdown = 0;

            protected CGColor Color;
            protected CGColor DarkendedColor;

            Ring Ring;

            public BackgroundLayerDelegate(Ring ring)
            {
                Ring = ring;

                Color = ColorHelper.ColorWheel(Metronome.Instance.Layers.IndexOf(ring.Layer));
                // use semi-transparent color for darkened portion
                DarkendedColor = new CGColor(Color, .2f);
            }

            [Export("drawLayer:inContext:")]
            public void DrawLayer(CALayer layer, CGContext context)
            {
                nfloat gradStart = .15f;
                if (BlinkingCountdown > 0)
                {
					BlinkingCountdown--;
                    gradStart += .15f * ((nfloat)BlinkingCountdown / (BlinkCount - 1));
                }

                var gradient = new CGGradient(
                    CGColorSpace.CreateDeviceRGB(),
                    new CGColor[] { DarkendedColor, Color },
                    new nfloat[] { gradStart, 1 }
                );

                context.DrawRadialGradient(
                    gradient,
                    new CGPoint(layer.Frame.Width / 2, layer.Frame.Width / 2),
                    Ring.InnerRadiusLocation,
                    new CGPoint(layer.Frame.Width / 2, layer.Frame.Width / 2),
                    Ring.OuterRadiusLocation,
                    CGGradientDrawingOptions.None
                );
            }
        }

        /// <summary>
        /// Draw the tick elements
        /// </summary>
        class TickLayerDelegate : NSObject, ICALayerDelegate
        {
            Ring Ring;

            double BeatLength;

            Layer Layer;

            public TickLayerDelegate(Ring ring, double beatLength, Layer layer)
            {
                Ring = ring;
                BeatLength = beatLength;
                Layer = layer;
            }

            [Export("drawLayer:inContext:")]
            public void DrawLayer(CALayer layer, CGContext context)
            {
                context.SaveState();

                context.SetLineWidth(2);

                // draw each tickmark
				int center = (int)(layer.Frame.Width / 2);
				context.TranslateCTM(center,center);

                nfloat initialRotation = (nfloat)((Layer.OffsetBpm + Layer.Beat.TakeWhile(x => StreamInfoProvider.IsSilence(x.StreamInfo)).Select(x => x.Bpm).Sum()) / BeatLength * -TWOPI);
                context.RotateCTM(initialRotation);

                double total = 0;
                int start = (int)(Ring.InnerRadiusLocation);
                int end = (int)(Ring.OuterRadiusLocation);
                if (Ring.TickRotations.Any())
                {
                    var rotation = Ring.TickRotations.First;
                    while (total < TWOPI)
                    {
                        if (rotation == null) rotation = Ring.TickRotations.First;

                        context.MoveTo(0, start);
                        context.AddLineToPoint(0, end);


                        context.RotateCTM(-rotation.Value);

                        total += rotation.Value;

                        rotation = rotation.Next;
                    }

                    context.ReplacePathWithStrokedPath();
                }

                context.Clip();

				// clipped gradient
				var gradient = new CGGradient(
					CGColorSpace.CreateDeviceRGB(),
                    new CGColor[] { NSColor.Gray.CGColor, NSColor.White.CGColor }
				);

				context.DrawRadialGradient(
					gradient,
					new CGPoint(0, 0),
					Ring.InnerRadiusLocation,
					new CGPoint(0, 0),
					Ring.OuterRadiusLocation,
					CGGradientDrawingOptions.None
				);

                context.RestoreState();
            }
        }
        #endregion

        void Instance_Stopped(object sender, EventArgs e)
        {
            ((BackgroundLayerDelegate)BackgroundLayer.Delegate).BlinkingCountdown = 0;
            BackgroundLayer.SetNeedsDisplay();

			BeatIndex = 0;

            CurrentBpmInterval = Layer.OffsetBpm;

			while (StreamInfoProvider.IsSilence(Layer.Beat[BeatIndex].StreamInfo))
			{
				CurrentBpmInterval += Layer.Beat[BeatIndex++].Bpm;
				BeatIndex %= Layer.Beat.Count;
			}
        }
    }
}
