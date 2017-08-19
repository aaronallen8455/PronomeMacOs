using System;
using CoreGraphics;

namespace Pronome.Mac.Visualizer.Bounce
{
    public class BeatLayerModel
    {
        #region protected fields
        protected double CurrentInterval;

        protected Layer Layer;

        protected Ball Ball;

        protected Lane Lane;
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

        #region constructor
        public BeatLayerModel(Layer layer, CGRect frame)
        {
            Layer = layer;
            //Ball = new Ball(layer, frame);
            Lane = new Lane(layer);
        }
        #endregion
    }
}
