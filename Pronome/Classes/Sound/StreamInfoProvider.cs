using System;
namespace Pronome
{
    public class StreamInfoProvider
    {
        #region private variables
        private string _uri;
        private bool _isPitch;
        private int _index;
        private string _title;
        private bool _isInternal;
        #endregion

        #region public properties
        /// <summary>
        /// Gets the URI. A note name or frequency for pitch, a file path for samples.
        /// </summary>
        /// <value>The URI.</value>
        public string Uri { get => _uri; }

        /// <summary>
        /// True if this is a pitch source.
        /// </summary>
        /// <value><c>true</c> if is pitch; otherwise, <c>false</c>.</value>
        public bool IsPitch { get => _isPitch; }

        public int Index { get => _index; }

        public string Title { get => _title; }

        /// <summary>
        /// Whether this is an internal or user defined source
        /// </summary>
        /// <value><c>true</c> if is internal; otherwise, <c>false</c>.</value>
        public bool IsInternal { get => _isInternal; }

        public HiHatStatuses HiHatStatus { get; set; }
        #endregion

        #region constructors
        public StreamInfoProvider(string uri, int index, string title, bool isInternal = true)
        {
            _uri = uri;

            _isPitch = IsPitchUri(uri);

            _index = index;

            _title = title;

            _isInternal = isInternal;
        }
        #endregion

        #region static methods
        /// <summary>
        /// Checks if the given uri is for a pitch source
        /// </summary>
        /// <param name="uri">URI.</param>
        public static bool IsPitchUri(string uri)
        {
            return !uri.Contains(".wav");
        }
        #endregion

        /// <summary>
        /// The string that will be displayed in the source selectors.
        /// </summary>
        /// <returns>A <see cref="T:System.String"/> that represents the current <see cref="T:Pronome.StreamInfoProvider"/>.</returns>
        public override string ToString()
        {
            if (IsPitch)
            {
                // Don't include index number for pitch source
                return Title;
            }
            string prefix = IsInternal ? "" : "u";
            return $"{prefix}{Index}.".PadRight(4) + Title;
        }

        public enum HiHatStatuses { Open, Down };
    }
}
