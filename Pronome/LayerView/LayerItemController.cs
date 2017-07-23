﻿using System;
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
        /// <summary>
        /// The color for even numbered elements.
        /// </summary>
        static public NSColor EvenColor = NSColor.FromRgb(70,130,180);

        /// <summary>
        /// The color for odd numbered elements.
        /// </summary>
        static public NSColor OddColor = NSColor.FromRgb(0,139,139);

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

        [Export("Metronome")]
        public Metronome Metronome
        {
            get => Metronome.Instance;
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
            ((TransportViewController)NSApplication.SharedApplication.MainWindow.ContentViewController)
                .RemoveLayer(Layer);

            Layer = null;

            Dispose();
        }

        public override void AwakeFromNib()
        {
            // add sources to source selector
            SoundSourceSelector.DataSource = new SourceSelectorDataSource();

            SoundSourceSelector.VisibleItems = 10;

            // autoselect the first source
            SoundSourceSelector.StringValue = 
                (NSString)SoundSourceSelector.DataSource.ObjectValueForItem(SoundSourceSelector, 0);

            // Epand the selector to show full items, not truncated
            var cell = SoundSourceSelector.Cell;
            var frame = SoundSourceSelector.Frame;
            bool open = false;
            SoundSourceSelector.WillPopUp += (sender, e) => {
                if (!open){
					SoundSourceSelector.SetFrameSize(new CoreGraphics.CGSize(220,23));
                    // force it to close then reopen to apply frame size to list
					cell.AccessibilityExpanded = false;
					cell.AccessibilityExpanded = true;
                    open = true;
                }
            };

            SoundSourceSelector.WillDismiss += (sender, e) => {
				SoundSourceSelector.Frame = frame;
                open = false;
            };
        }
        #endregion

        #region Public Methods

        public void SetBackgroundColor(NSColor color)
        {
            BackgroundBox.FillColor = color;
        }

        public void HighlightBeatCodeSyntax()
        {
            BeatCodeInput.HighlightSyntax();
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
