using System;
using System.Collections;
using System.Collections.Generic;
using AppKit;
using Foundation;

namespace Pronome.Mac
{
    public class OptionListDataSource : NSOutlineViewDataSource
    {
        #region Private Variables
        private OptionListView _controller;
        #endregion

        #region Public Variables
        public List<OptionItem> Items = new List<OptionItem>();
        #endregion

        #region Constructors
        public OptionListDataSource(OptionListView controller)
        {
            _controller = controller;
        }
        #endregion

        #region Override Properties
        public override nint GetChildrenCount(NSOutlineView outlineView, NSObject item)
        {
            return item == null ? Items.Count : ((OptionItem)item).Count;
        }

        public override bool ItemExpandable(NSOutlineView outlineView, NSObject item)
        {
            return ((OptionItem)item).HasChildren;
        }

        public override NSObject GetChild(NSOutlineView outlineView, nint childIndex, NSObject item)
        {
            return item == null ? Items[(int)childIndex] : ((OptionItem)item)[(int)childIndex];
        }

        public override NSObject GetObjectValue(NSOutlineView outlineView, NSTableColumn tableColumn, NSObject item)
        {
            return new NSString((item as OptionItem).Title);
        }
        #endregion

        #region Internal Methods
        internal OptionItem ItemForRow(int row)
        {
            int index = 0;

            // look at each group
            foreach (OptionItem item in Items)
            {
                // is the row inside this group?
                if (row >= index && row <= (index + item.Count))
                {
                    return item[row - index - 1];
                }

                index += item.Count + 1;
            }

            return null;
        }
        #endregion
    }
}
