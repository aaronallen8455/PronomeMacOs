using System;
using AppKit;
using Foundation;

namespace Pronome.Mac
{
    public class OptionListDelegate : NSOutlineViewDelegate
    {
        #region Private Variables
        private OptionListView _controller;
        #endregion

        #region Constructors
        public OptionListDelegate(OptionListView controller)
        {
            _controller = controller;
        }
        #endregion

        #region Override Methods
        public override bool ShouldEditTableColumn(NSOutlineView outlineView, NSTableColumn tableColumn, NSObject item)
        {
            return false;
        }

        public override bool IsGroupItem(NSOutlineView outlineView, NSObject item)
        {
            return (item as OptionItem).HasChildren;
        }

        public override NSView GetView(NSOutlineView outlineView, NSTableColumn tableColumn, NSObject item)
        {
            NSTableCellView view = null;

            // is this a group item?
            if ((item as OptionItem).HasChildren)
            {
                view = (NSTableCellView)outlineView.MakeView("HeaderCell", this);
            }
            else
            {
                view = (NSTableCellView)outlineView.MakeView("DataCell", this);
                view.ImageView.Image = (item as OptionItem).Icon;
            }

            // initialize view
            view.TextField.StringValue = (item as OptionItem).Title;

            return view;
        }

        public override bool ShouldSelectItem(NSOutlineView outlineView, NSObject item)
        {
            return outlineView.GetParent(item) != null;
        }

        public override void SelectionDidChange(NSNotification notification)
        {
            NSIndexSet selectedIndexes = _controller.SelectedRows;

            // more than one item selected?
            if (selectedIndexes.Count > 1)
            {
                // not handling this case
            }
            else
            {
                // grab the item
                var item = _controller.Data.ItemForRow((int)selectedIndexes.FirstIndex);

                // was an item found?
                if (item != null)
                {
                    // fire the clicked event for the item
                    item.RaiseClickedEvent();

                    // inform caller of selection
                    _controller.RaiseItemSelected(item);
                }
            }
        }
        #endregion
    }
}
