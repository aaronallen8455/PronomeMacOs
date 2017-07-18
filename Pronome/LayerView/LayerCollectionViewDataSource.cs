using System;
using AppKit;
using Foundation;
using System.Collections.Generic;

namespace Pronome
{
    public class LayerCollectionViewDataSource : NSCollectionViewDataSource
    {
        #region Computed properties
        public NSCollectionView ParentCollectionView { get; set; }

        public List<Layer> Data { get; set; } = new List<Layer>();
        #endregion

        #region Constructors
        public LayerCollectionViewDataSource(NSCollectionView parent)
        {
            ParentCollectionView = parent;

            // Attach to collection view
            parent.DataSource = this;
        }
        #endregion

        #region Overriden methods
        public override NSCollectionViewItem GetItem(NSCollectionView collectionView, NSIndexPath indexPath)
        {
            var item = collectionView.MakeItem("LayerCell", indexPath) as LayerItemController;
            item.Layer = Data[(int)indexPath.Item];

            // set item's background color based on index number
            NSColor color = indexPath.Item % 2 == 0 ? LayerItemController.EvenColor : LayerItemController.OddColor;
            item.SetBackgroundColor(color);

            return item;
        }

        public override nint GetNumberofItems(NSCollectionView collectionView, nint section)
        {
            return Data.Count;
        }

        public override nint GetNumberOfSections(NSCollectionView collectionView)
        {
            return 1;
        }
        #endregion
    }
}
