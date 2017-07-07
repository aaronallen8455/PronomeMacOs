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
        private List<string> _data =
            new List<StreamInfoProvider>(StreamInfoProvider.CompleteSourceLibrary)
			.Select(x => x.ToString()).ToList();
        /// <summary>
        /// Gets or sets the data. From the Complete Source Library
        /// </summary>
        /// <value>The data.</value>
        public List<string> Data
        {
            get => _data;
            set
            {
                _data = value;
            }
        }
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
