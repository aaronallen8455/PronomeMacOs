using System;
using System.Collections.Generic;
using System.Linq;
using CoreGraphics;

namespace Pronome.Mac.Visualizer.Bounce
{
    public class Lane
    {
        #region Public fields
        public CGColor Color;
        #endregion

        #region protected fields
        /// <summary>
        /// The layer represented by this lane.
        /// </summary>
        protected Layer Layer;

        /// <summary>
        /// Holds the remaining BPM of all ticks currently in the queue
        /// </summary>
        protected List<double> Ticks;

        private int _beatIndex;
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

        /// <summary>
        /// number of bpms before a new tick needs to be queued.
        /// </summary>
        protected double CurrentInterval;

        protected double LeftSlope;

        protected double RightSlope;
        #endregion

        #region constructors
        public Lane(Layer layer)
        {
            Layer = layer;

            int layerIndex = Metronome.Instance.Layers.IndexOf(layer);

            Color = ColorHelper.ColorWheel(layerIndex);
            (LeftSlope, RightSlope) = BounceHelper.GetLaneSlope(layerIndex);

            InitTicks();
        }
        #endregion

        #region public methods
        /// <summary>
        /// Draw this layers ticks into the given context with the given lane height
        /// Requires that the context be translated to the lane base
        /// </summary>
        /// <param name="ctx">Context.</param>
        public void DrawFrame(CGContext ctx)
        {
            double elapsedBpm = BounceHelper.ElapsedBpm;

			ctx.SetStrokeColor(Color);

			LinkedList<int> indexesToRemove = new LinkedList<int>();

            bool isFirst = true;
            // draw each tick
            for (int i = 0; i < Ticks.Count; i++)
            {
                Ticks[i] += elapsedBpm;

                // check if new tick(s) should be added
                if (i == Ticks.Count - 1)
                {
                    CurrentInterval -= elapsedBpm;

                    if (CurrentInterval <= 0)
                    {
						// need to queue the next cell
                        Ticks.Add(-CurrentInterval);

                        CurrentInterval += Layer.Beat[BeatIndex++].Bpm;
                        // handle silence
                        while (Layer.Beat[BeatIndex].StreamInfo == StreamInfoProvider.InternalSourceLibrary[0])
                        {
                            CurrentInterval += Layer.Beat[BeatIndex++].Bpm;
                        }
                    }
                }

                var factor = Ticks[i] / UserSettings.GetSettings().BounceQueueSize;
                if (factor > 1)
                {
                    // remove completed tick
                    indexesToRemove.AddFirst(i);
                    continue;
                }

                double yFactor = BounceHelper.EasingFunction(factor);

                // draw line
                double y = yFactor * BounceHelper.LaneAreaHeight;
                double lx = LeftSlope == double.NaN ? 0 : y / LeftSlope;
                double rx = RightSlope == double.NaN ? BounceHelper.BottomLaneSpacing : y / RightSlope + BounceHelper.BottomLaneSpacing;
                ctx.MoveTo((int)lx, (int)y);
                ctx.AddLineToPoint((int)rx, (int)y);

                if (isFirst)
                {
                    ctx.SetLineWidth(2);
                    ctx.StrokePath();
                    ctx.SetLineWidth(1);

					isFirst = false;
                }
            }

            ctx.StrokePath();

            // perform removal operations
            foreach (int i in indexesToRemove)
            {
                Ticks.RemoveAt(i);
            }
        }
        #endregion

        #region protected methods
        /// <summary>
        /// For the current queue size, get all ticks present in the starting frame
        /// </summary>
        protected void InitTicks()
        {
            Ticks = new List<double>();

            if (Layer.Beat.All(x => x.StreamInfo == StreamInfoProvider.InternalSourceLibrary[0])) return;

            double qSize = UserSettings.GetSettings().BounceQueueSize - Layer.OffsetBpm;
            while (qSize >= 0)
            {
                while (Layer.Beat[BeatIndex].StreamInfo == StreamInfoProvider.InternalSourceLibrary[0])
                {
                    //acc += Layer.Beat[BeatIndex].Bpm;
                    qSize -= Layer.Beat[BeatIndex].Bpm;
                    CurrentInterval += Layer.Beat[BeatIndex].Bpm;
                    BeatIndex++;

                    if (qSize < 0) return;
                }

                Ticks.Add(qSize);

				CurrentInterval = Layer.Beat[BeatIndex].Bpm - qSize;
                qSize -= Layer.Beat[BeatIndex].Bpm;

                BeatIndex++;
            }

            // if proceeding cells are silent, add to currentInterval
			while (Layer.Beat[BeatIndex].StreamInfo == StreamInfoProvider.InternalSourceLibrary[0])
			{
                CurrentInterval += Layer.Beat[BeatIndex].Bpm;
				BeatIndex++;
			}
        }
        #endregion
    }
}
