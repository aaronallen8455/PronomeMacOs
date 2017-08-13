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
    public class Ring
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

        /// <summary>
        /// Index of the current beat cell
        /// </summary>
        int BeatIndex;

        /// <summary>
        /// The inner radius location. Multiply by frame size.
        /// </summary>
        public nfloat InnerRadiusLocation;

        /// <summary>
        /// The outer radius location. Multiply by frame size
        /// </summary>
        public nfloat OuterRadiusLocation;
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

            // init the CALayers
            BackgroundLayer = new CALayer();
            BackgroundLayer.ContentsScale = NSScreen.MainScreen.BackingScaleFactor;
            BackgroundLayer.Frame = superLayer.Frame;
            BackgroundLayer.Delegate = new BackgroundLayerDelegate(this);

            TickMarksLayer = new CALayer();
            TickMarksLayer.ContentsScale = NSScreen.MainScreen.BackingScaleFactor;
            TickMarksLayer.Frame = superLayer.Frame;
            TickMarksLayer.Delegate = new TickLayerDelegate(this, beatLength, layer);

            superLayer.AddSublayer(BackgroundLayer);
            superLayer.AddSublayer(TickMarksLayer);

            // find the tick rotations
            //TickRotations = layer.Beat.Select(x => x.Bpm / beatLength).ToList();

            TickRotations = new LinkedList<nfloat>();

            foreach(BeatCell bc in layer.Beat)
            {
                if (bc.StreamInfo == StreamInfoProvider.InternalSourceLibrary[0])
                {
                    // add a silent value to the previous cell value
                    if (TickRotations.Last != null)
                    {
                        TickRotations.Last.Value += (nfloat)(bc.Bpm / beatLength * TWOPI);
                    }
                }
                else
                {
                    TickRotations.AddLast((nfloat)(bc.Bpm / beatLength * TWOPI));
                }
            }

            InnerRadiusLocation = (nfloat)startPoint * superLayer.Frame.Width;
            OuterRadiusLocation = (nfloat)endPoint * superLayer.Frame.Width;

            DrawStaticElements();
        }
        #endregion

        #region Public methods
        public void Progress(double elapsedBpm)
        {

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
        #endregion

        #region Layer delegates
        /// <summary>
        /// Draw the background
        /// </summary>
        class BackgroundLayerDelegate : NSObject, ICALayerDelegate
        {
            /// <summary>
            /// Used to animate the blinking effect
            /// </summary>
            public int BlinkingCountdown;

            Ring Ring;

            public BackgroundLayerDelegate(Ring ring)
            {
                Ring = ring;
            }

            [Export("drawLayer:inContext:")]
            public void DrawLayer(CALayer layer, CGContext context)
            {
                var gradient = new CGGradient(
                    CGColorSpace.CreateDeviceRGB(),
                    new CGColor[] { NSColor.Black.CGColor, NSColor.Red.CGColor },
                    new nfloat[] { .15f, 1 }
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
                nfloat initialRotation = (nfloat)((Layer.OffsetBpm + Layer.Beat.TakeWhile(x => x.StreamInfo == StreamInfoProvider.InternalSourceLibrary[0]).Select(x => x.Bpm).Sum()) / BeatLength * TWOPI);
                context.RotateCTM(initialRotation);

                double total = 0;
                int center = (int)(layer.Frame.Width / 2);
                context.TranslateCTM(center,center);
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
    }
}
