using System;
using System.Collections.Generic;
using System.Linq;
using Foundation;
using AppKit;

namespace Pronome
{
    /// <summary>
    /// Layer item controller. The controller for the layer items in the layer collection view
    /// </summary>
    public partial class LayerItemController : NSCollectionViewItem
    {
        #region Private Variables
        private Layer _layer;
        #endregion

        #region Computed properties
        [Export("Layer")]
        public Layer Layer
        {
            get => _layer;
            set
            {
                WillChangeValue("Layer");
                _layer = value;
                DidChangeValue("Layer");
            }
        }
        #endregion

        #region Constructors

        // Called when created from unmanaged code
        public LayerItemController(IntPtr handle) : base(handle)
        {
            Initialize();
        }

        // Called when created directly from a XIB file
        [Export("initWithCoder:")]
        public LayerItemController(NSCoder coder) : base(coder)
        {
            Initialize();
        }

        // Call to load from the XIB/NIB file
        public LayerItemController() : base("LayerItem", NSBundle.MainBundle)
        {
            Initialize();
        }

        public LayerItemController(string nibName, NSBundle nibBundle) : base(nibName, nibBundle)
        {
            Initialize();
        }

        // Shared initialization code
        void Initialize()
        {
        }
        #endregion

        #region Overriden Methods
        partial void CloseLayerAction(NSObject sender)
        {
            LayerViewController.Instance.RemoveLayer(Layer);

            Layer = null;
        }

        public override void AwakeFromNib()
        {
            // add sources to source selector
            SoundSourceSelector.DataSource = new SourceSelectorDataSource();

            // autoselect the first source
            SoundSourceSelector.StringValue = 
                (NSString)SoundSourceSelector.DataSource.ObjectValueForItem(SoundSourceSelector, 0);
        }
        #endregion

        //strongly typed view accessor
        public new LayerItem View
        {
            get
            {
                return (LayerItem)base.View;
            }
        }
    }
}
