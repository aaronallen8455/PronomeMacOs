// This file has been autogenerated from a class added in the UI designer.

using System;

using Foundation;
using AppKit;

namespace Pronome.Mac
{
	public partial class ReferenceDialog : NSViewController
	{
        public NSViewController Presentor;

        NSMutableArray<Layer> _layers;

        [Export("layerArray")]
        public NSArray Layers
        {
            get => _layers;
        }

        string _index = "1";
        [Export("Index")]
        public string Index
        {
            get => _index;
            set
            {
                WillChangeValue("Index");
                _index = value;
                DidChangeValue("Index");
            }
        }

		public ReferenceDialog (IntPtr handle) : base (handle)
		{
            _layers = new NSMutableArray<Layer>(Metronome.Instance.Layers.ToArray());
		}

		partial void AcceptAction(NSObject sender)
		{
			Close();

			Accepted?.Invoke(null, null);
		}

		partial void CancelAction(NSObject sender)
		{
			Close();

			Canceled?.Invoke(null, null);
		}

		private void Close()
		{
			Presentor.DismissViewController(this);
		}

		public event EventHandler Accepted;

		public event EventHandler Canceled;
	}
}
