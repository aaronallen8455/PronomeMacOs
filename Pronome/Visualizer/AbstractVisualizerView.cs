using System;
using AppKit;
using CoreAnimation;
using CoreGraphics;
using Pronome.Mac.Visualizer;

namespace Pronome.Mac
{
    public abstract class AbstractVisualizerView : NSView
    {
        protected BeatAnimationLayer AnimationLayer;

        public AbstractVisualizerView(IntPtr handle) : base(handle)
        {
			WantsLayer = true;

			AnimationLayer = new BeatAnimationLayer();
            AnimationLayer.ContentsScale = NSScreen.MainScreen.BackingScaleFactor;
			//AnimationLayer.Delegate = new T();
			AnimationLayer.Frame = Layer.Frame;
			Layer.AddSublayer(AnimationLayer);
        }

        public abstract void DrawFrame(double bpm);

		public override bool AcceptsFirstResponder()
		{
			// add this view's layer to the queue when the window becomes active
			if (!AnimationHelper.AnimationViews.Contains(this))
			{
				AnimationHelper.AnimationViews.Add(this);
			}

			// we have to do this here because Window isn't initialized yet at other points
			Window.WillClose -= Window_WillClose;
			Window.WillClose += Window_WillClose;

            Window.DidResize += Window_DidResize;

			return base.AcceptsFirstResponder();
		}

		void Window_WillClose(object sender, EventArgs e)
		{
			AnimationHelper.AnimationViews.Remove(this);
		}

        void Window_DidResize(object sender, EventArgs e)
        {
            // resize and reposition all layers to match the window
            double size = Math.Min(Window.Frame.Width, Window.Frame.Height);
            int posX = (int)(Window.Frame.Width / 2 - (size / 2));
            int posY = (int)(Window.Frame.Height / 2 - (size / 2));
            CGRect frame = new CGRect(posX, posY, size, size);

            Layer.Frame = frame;

            foreach (CALayer layer in Layer.Sublayers)
            {
                layer.Frame = frame;
            }
        }
    }
}
