using System;
using System.Collections.Generic;
using System.Linq;
using CoreGraphics;

namespace Pronome.Mac.Visualizer.Bounce
{
    public class Lane : IDisposable
    {
        #region Public fields
        public CGColor Color;

		/// <summary>
		/// Holds the remaining BPM of all ticks currently in the queue
		/// </summary>
		public List<double> Ticks;
        #endregion

        #region protected fields
        /// <summary>
        /// The layer represented by this lane.
        /// </summary>
        protected Layer Layer;

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
        public double CurrentInterval;

        protected double LeftSlope;

        protected double RightSlope;

        public int Index;
        #endregion

        #region constructors
        public Lane(Layer layer)
        {
            Layer = layer;

            Index = Metronome.Instance.Layers.IndexOf(layer);

            Color = ColorHelper.ColorWheel(Index);
            (LeftSlope, RightSlope) = BounceHelper.GetLaneSlope(Index);

            InitTicks();
        }
        #endregion

        #region public methods

        public void SetSlope()
        {
            (LeftSlope, RightSlope) = BounceHelper.GetLaneSlope(Index);
        }

        /// <summary>
        /// Draw this layers ticks into the given context with the given lane height
        /// Requires that the context be translated to the lane base
        /// </summary>
        /// <param name="ctx">Context.</param>
        public void DrawFrame(CGContext ctx, double elapsedBpm)
        {
            //double elapsedBpm = BounceHelper.ElapsedBpm;
            if (Layer.Beat == null) return;

			ctx.SetStrokeColor(Color);

			LinkedList<int> indexesToRemove = new LinkedList<int>();

            bool isFirst = true;
            bool needToSubtract = true;
            // draw each tick
            for (int i = 0; i < Ticks.Count; i++)
            {
                Ticks[i] += elapsedBpm;

                // check if new tick(s) should be added
                if (i == Ticks.Count - 1)
                {
                    if (needToSubtract)
                    {
                        // don't subtract a second time (when a new tick is added and then reiterated on)
						CurrentInterval -= elapsedBpm;
                        needToSubtract = false;
                    }

                    while (CurrentInterval < 0)
                    {
						// need to queue the next cell
                        Ticks.Add(-CurrentInterval - elapsedBpm); // add elapsed because it will get subtracted on next iteration

                        CurrentInterval += Layer.Beat[BeatIndex++].Bpm;
                        // handle silence
                        while (StreamInfoProvider.IsSilence(Layer.Beat[BeatIndex].StreamInfo))
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
                ctx.MoveTo((nfloat)lx, (nfloat)y);
                ctx.AddLineToPoint((nfloat)rx, (nfloat)y);

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

        public void Reset()
        {
            BeatIndex = 0;
            CurrentInterval = Layer.OffsetBpm;
            // handle silence at start of beat
            while (StreamInfoProvider.IsSilence(Layer.Beat[BeatIndex].StreamInfo))
            {
                CurrentInterval += Layer.Beat[BeatIndex++].Bpm;
            }
            InitTicks();
        }

        public void Dispose()
        {
            Color.Dispose();
        }
        #endregion

        #region protected methods
        /// <summary>
        /// For the current queue size, get all ticks present in the starting frame
        /// </summary>
        protected void InitTicks()
        {
            Ticks = new List<double>();

            if (Layer.Beat.All(x => StreamInfoProvider.IsSilence(x.StreamInfo))) return;

            double qSize = UserSettings.GetSettings().BounceQueueSize - Layer.OffsetBpm;
            while (qSize > 0)
            {
                while (StreamInfoProvider.IsSilence(Layer.Beat[BeatIndex].StreamInfo))
                {
                    //acc += Layer.Beat[BeatIndex].Bpm;
                    qSize -= Layer.Beat[BeatIndex].Bpm;
                    CurrentInterval += Layer.Beat[BeatIndex].Bpm;
                    BeatIndex++;

                    if (qSize <= 0) return;
                }

                Ticks.Add(qSize);

				CurrentInterval = Layer.Beat[BeatIndex].Bpm - qSize;
                qSize -= Layer.Beat[BeatIndex].Bpm;

                BeatIndex++;
            }

            // if proceeding cells are silent, add to currentInterval
            while (StreamInfoProvider.IsSilence(Layer.Beat[BeatIndex].StreamInfo))
			{
                CurrentInterval += Layer.Beat[BeatIndex].Bpm;
				BeatIndex++;
			}
        }
        #endregion
    }
}
