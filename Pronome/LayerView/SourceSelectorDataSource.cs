using System;
using AppKit;
using Foundation;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Pronome.Mac
{
    public class SourceSelectorDataSource : NSComboBoxDataSource
    {
        #region Static Properties
        //private static List<string> _data = StreamInfoProvider.CompleteSourceLibraryStrings;
            //new List<StreamInfoProvider>(StreamInfoProvider.CompleteSourceLibrary)
			//.Select(x => x.ToString()).ToList();
        /// <summary>
        /// Gets or sets the data. From the Complete Source Library
        /// </summary>
        /// <value>The data.</value>
        public static List<string> Data
        {
            get => StreamInfoProvider.CompleteSourceLibraryStrings;//StreamInfoProvider.CompleteSourceLibrary.Select(x => x.ToString()).ToList();
        }
        #endregion

        public SourceSelectorDataSource(NSComboBox parent)
        {
            // update the data whenever a change to user sources is made
            StreamInfoProvider.UserSourcesChanged += (sender, e) =>
            {
                string currentString = parent.StringValue;
                int index = (int)parent.SelectedIndex;
                // if name of source was changed or removed
                string atIndex = StreamInfoProvider.CompleteSourceLibraryStrings.ElementAtOrDefault(index);
                if (atIndex != currentString)
                {
                    // do nothing if source exists but at different index
                    if (!StreamInfoProvider.CompleteSourceLibraryStrings.Contains(currentString))
                    {
                        if (atIndex == default(string))
                        {
                            parent.SelectItem(0);
                        }
                        else
                        {
                            // change to new string
                            parent.StringValue = atIndex;
                        }
                    }
                }

                parent.ReloadData();
            };
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
            if (uncompletedString.Length > 3)
            {
				return Data.FirstOrDefault(x => Regex.IsMatch(x, $@"\b{uncompletedString}", RegexOptions.IgnoreCase));
            }
            return null;
        }
        #endregion
    }
}
