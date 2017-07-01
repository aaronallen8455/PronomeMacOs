// WARNING
//
// This file has been generated automatically by Visual Studio to store outlets and
// actions made in the UI designer. If it is removed, they will be lost.
// Manual changes to this file may not be handled correctly.
//
using Foundation;
using System.CodeDom.Compiler;

namespace Pronome
{
	[Register ("LayerViewController")]
	partial class LayerViewController
	{
		[Outlet]
		AppKit.NSCollectionView LayerCollection { get; set; }
		
		void ReleaseDesignerOutlets ()
		{
			if (LayerCollection != null) {
				LayerCollection.Dispose ();
				LayerCollection = null;
			}
		}
	}
}
