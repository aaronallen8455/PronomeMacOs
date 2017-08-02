using System;
using System.Collections;
using System.Collections.Generic;
using AppKit;
using Foundation;

namespace Pronome.Mac
{
    /// <summary>
    /// Represents a section of the options window, which can be nested
    /// </summary>
    public class OptionItem : NSObject, IEnumerator, IEnumerable
    {
        #region Private Properties
        private string _title;
        private NSImage _icon;
        private string _tag;
        private List<OptionItem> _items = new List<OptionItem>();
        #endregion

        #region Computed Properties
        public string Title
        {
            get => _title;
            set { _title = value; }
        }

        public NSImage Icon
        {
            get => _icon;
            set { _icon = value; }
        }

        public string Tag
        {
            get => _tag;
            set { _tag = value; }
        }
        #endregion

        #region Indexer
        public OptionItem this[int index]
        {
            get => _items[index];
            set { _items[index] = value; }
        }

        public int Count
        {
            get => _items.Count;
        }

        public bool HasChildren
        {
            get => Count > 0;
        }
        #endregion

        #region Enumerable Routines
        private int _position = -1;

        public IEnumerator GetEnumerator()
        {
            _position = -1;
            return (IEnumerator)this;
        }

        public bool MoveNext()
        {
            _position++;
            return _position < _items.Count;
        }

        public void Reset()
        {
            _position = -1;
        }

        public object Current
        {
            get
            {
                try
                {
                    return _items[_position];
                }
                catch (IndexOutOfRangeException)
                {
                    throw new InvalidOperationException();
                }
            }
        }
        #endregion

        #region Constructors
        public OptionItem(string title)
        {
            _title = title;
        }

        public OptionItem(string title, string icon, ClickedDelegate clicked)
        {
            _title = title;
            _icon = NSImage.ImageNamed(icon);
            Clicked = clicked;
        }
        #endregion

        #region Public Methods
        public void AddItem(OptionItem item)
        {
            _items.Add(item);
        }

        public void Insert(int n, OptionItem item)
        {
            _items.Insert(n, item);
        }

        public void RemoveItem(OptionItem item)
        {
            _items.Remove(item);
        }

        public void RemoveItem(int n)
        {
            _items.RemoveAt(n);
        }

        public void Clear()
        {
            _items.Clear();
        }
        #endregion

        #region Events
        public delegate void ClickedDelegate();
        public event ClickedDelegate Clicked;

        internal void RaiseClickedEvent()
        {
            Clicked?.Invoke();
        }
        #endregion
    }
}
