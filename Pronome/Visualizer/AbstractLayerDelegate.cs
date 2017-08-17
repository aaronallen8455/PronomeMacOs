using System;
using CoreAnimation;
using Foundation;

namespace Pronome.Mac.Visualizer
{
    public abstract class AbstractLayerDelegate : NSObject, ICALayerDelegate
    {
        //private object _ebpmLock = new object();
		//
        //private double _elapsedBpm;
        ///// <summary>
        ///// Number of quarternotes that have elapsed since the last frame was drawn.
        ///// </summary>
        ///// <value>The elapsed bpm.</value>
        //public double ElapsedBpm
        //{
        //    get
        //    {
        //        lock(_ebpmLock)
        //        {
        //            return _elapsedBpm;
        //        }
        //    }
		//
        //    set
        //    {
        //        lock(_ebpmLock)
        //        {
        //            _elapsedBpm = value;
        //        }
        //    }
        //}


        public AbstractLayerDelegate()
        {
        }
    }
}
