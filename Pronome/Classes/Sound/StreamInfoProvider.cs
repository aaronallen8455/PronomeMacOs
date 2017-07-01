using System;
namespace Pronome
{
    public class StreamInfoProvider
    {
        #region private variables
        private string _uri;
        private bool _isPitch;
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
        #endregion

        #region constructors
        public StreamInfoProvider(string uri)
        {
            _uri = uri;

            _isPitch = IsPitchUri(uri);
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
    }
}
