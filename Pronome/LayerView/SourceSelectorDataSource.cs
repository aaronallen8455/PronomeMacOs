using System;
using AppKit;
using Foundation;
using System.Collections.Generic;
using System.Linq;

namespace Pronome
{
    public class SourceSelectorDataSource : NSComboBoxDataSource
    {
        #region Static Properties
        public List<string> Data
        {
            get; set;
        } = new List<StreamInfoProvider> 
        {
            new StreamInfoProvider("", 1, "Option 1"),
            new StreamInfoProvider("", 2, "Option 2"),
            new StreamInfoProvider("", 3, "Option 3")
        }.Select(x => x.ToString()).ToList();
        #endregion

        public SourceSelectorDataSource()
        {
        }

        #region Overriden Methods
        public override nint ItemCount(NSComboBox comboBox)
        {
            return Data.Count;
        }

        public override NSObject ObjectValueForItem(NSComboBox comboBox, nint index)
        {
            NSString value = new NSString(Data[(int)index]);
            return value;
        }

        public override nint IndexOfItem(NSComboBox comboBox, string value)
        {
            return Data.IndexOf(value);
        }

        public override string CompletedString(NSComboBox comboBox, string uncompletedString)
        {
            return Data.FirstOrDefault(x => x.IndexOf(uncompletedString, StringComparison.InvariantCultureIgnoreCase) > -1);
        }
        #endregion
    }
}
