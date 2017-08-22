﻿using System;
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

			AnimationLayer = new BeatAnimationLayer()
			{
				ContentsScale = NSScreen.MainScreen.BackingScaleFactor,
				ZPosition = 50
			};

            Layer.AddSublayer(AnimationLayer);
        }

        /// <summary>
        /// Draws a frame.
        /// </summary>
        public abstract void DrawFrame();//double bpm);

        protected abstract void CreateAssets();

        /// <summary>
        /// Sizes and positions used for the frames
        /// </summary>
        /// <returns>The frame.</returns>
        protected abstract CGRect GetRect(nfloat winWidth, nfloat winHeight);

        #region Overriden Methods
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

            Window.DidResize -= Window_DidResize;
            Window.DidResize += Window_DidResize;

			return base.AcceptsFirstResponder();
		}

        public override void ViewWillMoveToWindow(NSWindow newWindow)
        {
            base.ViewWillMoveToWindow(newWindow);

            Layer.Frame = GetRect(newWindow.Frame.Width, newWindow.Frame.Height);
            CreateAssets();

			var innerFrame = new CGRect(0, 0, Layer.Frame.Width, Layer.Frame.Height);
            			
			foreach (CALayer layer in Layer.Sublayers)
			{
                layer.Frame = innerFrame;
                // draw initial state
                layer.SetNeedsDisplay();
			}
        }

        public override void AwakeFromNib()
        {
            base.AwakeFromNib();

            var x = Window;
            var y = Layer.Frame;
        }
        #endregion

        void Window_WillClose(object sender, EventArgs e)
		{
			AnimationHelper.AnimationViews.Remove(this);
		}

        protected virtual void Window_DidResize(object sender, EventArgs e)
        {
            // resize and reposition all layers to match the window
            var frame = GetRect(Window.Frame.Width, Window.Frame.Height);

            CATransaction.Begin();
            CATransaction.DisableActions = true;
            CATransaction.AnimationDuration = 0;

            Layer.Frame = frame;

            nfloat width = frame.Width;
            nfloat height = frame.Height;

            CGRect subFrame = new CGRect(0, 0, width, height);

            foreach (CALayer layer in Layer.Sublayers)
            {
                layer.Frame = subFrame;

                layer.SetNeedsDisplay();
            }
            CATransaction.Commit();
        }
    }
}
