using System;
using AppKit;
using Foundation;

namespace Pronome.Mac
{
    [Register("OptionListView")]
    public class OptionListView : NSOutlineView
    {
        #region Computed Properties
        public OptionListDataSource Data
        {
            get => (OptionListDataSource)DataSource;
        }
        #endregion

        #region Constructors
        public OptionListView()
        {
        }

        public OptionListView(IntPtr handle) : base(handle)
        {

        }

        public OptionListView(NSCoder coder) : base(coder) { }

        public OptionListView(NSObjectFlag t) : base(t) { }
        #endregion

        #region Override Methods
        public override void AwakeFromNib()
        {
            base.AwakeFromNib();
        }
        #endregion

        #region Public Methods
        public void Initialize()
        {
            DataSource = new OptionListDataSource(this);
            Delegate = new OptionListDelegate(this);
        }

        public void AddItem(OptionItem item)
        {
            Data?.Items.Add(item);
        }
        #endregion

        #region Events
        public delegate void ItemSelectedDelegate(OptionItem item);
        public event ItemSelectedDelegate ItemSelected;

        internal void RaiseItemSelected(OptionItem item)
        {
            ItemSelected?.Invoke(item);
        }
        #endregion
    }
}
