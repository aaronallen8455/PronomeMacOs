﻿using System;
using CoreAnimation;

namespace Pronome.Mac.Visualizer
{
	public class BeatAnimationLayer : CALayer
	{
        public BeatAnimationLayer() : base()
        {
            // draw after playback has stopped
            Metronome.Instance.Stopped += (sender, e) => SetNeedsDisplay();
        }
	}
}
