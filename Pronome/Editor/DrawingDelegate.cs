using System;
using Foundation;
using CoreAnimation;

namespace Pronome.Mac.Editor
{
    public class DrawingDelegate : NSObject, ICALayerDelegate
    {
        const int LayerHeight = 50;
        const int LayerSpacing = 20;

        #region Public Fields
        /// <summary>
        /// The indexes of beat layers that need to be drawn.
        /// </summary>
        public int[] LayerIndexesToDraw;
        #endregion

        public DrawingDelegate()
        {
        }

        [Export("drawLayer:inContext:")]
        public void DrawLayer(CALayer layer, CoreGraphics.CGContext context)
        {
            throw new System.NotImplementedException();
        }
    }
}
