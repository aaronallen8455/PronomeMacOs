using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

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
        public StreamInfoProvider(int index, string uri, string title, HiHatStatuses hiHatStatus = HiHatStatuses.None, bool isInternal = true)
        {
            _uri = uri;

            _isPitch = IsPitchUri(uri);

            _index = index;

            _title = title;

            HiHatStatus = hiHatStatus;

            _isInternal = isInternal;
        }

		static StreamInfoProvider()
		{
            // initialize the source library
			CompleteSourceLibrary =
				new StreamInfoProvider[] { GetDefault() }.Concat(InternalSourceLibrary).Concat(UserSourceLibrary);
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

        /// <summary>
        /// Gets the default stream info (a440 pitch).
        /// </summary>
        /// <returns>The default.</returns>
        static public StreamInfoProvider GetDefault()
        {
            return GetFromPitch("A4");
        }

        /// <summary>
        /// Gets from a pitch, note symbol or frequency in Hz.
        /// </summary>
        /// <returns>The from pitch.</returns>
        /// <param name="uri">URI.</param>
        static public StreamInfoProvider GetFromPitch(string uri)
        {
            return new StreamInfoProvider(-1, uri, "Pitch");
        }

        /// <summary>
        /// Gets the wav file from it's label.
        /// </summary>
        /// <returns>The wav from label.</returns>
        /// <param name="title">Title.</param>
        static public StreamInfoProvider GetWavFromLabel(string title)
		{
            var ss = CompleteSourceLibrary.FirstOrDefault(x => x.Title == title);

			return ss == null ? GetDefault() : ss;
		}

        /// <summary>
        /// Gets from the ToString method which is what is found in the dropdown selectors.
        /// </summary>
        /// <param name="str">String.</param>
        static public StreamInfoProvider GetFromToString(string str)
        {
            return CompleteSourceLibrary.FirstOrDefault(x => x.ToString() == str);
        }

		/// <summary>
		/// Get the source stub from a modifier value, i.e. the 5 in '@5'
		/// </summary>
		/// <param name="modifier"></param>
		/// <returns></returns>
		static public StreamInfoProvider GetFromModifier(string modifier)
		{
			if (Regex.IsMatch(modifier, @"^[a-gA-GpP]"))
			{
				// is a pitch reference
				return GetFromPitch(modifier);
			}
			else if (Regex.IsMatch(modifier, @"^u\d+$"))
			{
				// it's a custom source
				int id = int.Parse(modifier.Substring(1));
                var s = UserSourceLibrary.FirstOrDefault(x => x.Index == id);
				return s ?? (StreamInfoProvider)GetDefault();
			}
			else // ref is a plain number (wav source) or "" base source.
			{
				StreamInfoProvider src = null;
				int id;
				if (modifier != "" && int.TryParse(modifier, out id))
				{
					src = InternalSourceLibrary.ElementAtOrDefault(id);
				}
				return src; // will return null for a base source mirror
			}
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

        public override bool Equals(object obj)
        {
            if (obj == null) return false;

            return ToString() == (obj as StreamInfoProvider).ToString();
        }

        public enum HiHatStatuses { None, Open, Down };

        public static IEnumerable<StreamInfoProvider> CompleteSourceLibrary;// =
            //InternalSourceLibrary.Concat(UserSourceLibrary);
            //new StreamInfoProvider[] { GetDefault() }.Concat(InternalSourceLibrary).Concat(UserSourceLibrary);

        /// <summary>
        /// The user source library.
        /// </summary>
        public static List<StreamInfoProvider> UserSourceLibrary = new List<StreamInfoProvider>();

        /// <summary>
        /// The internal source library for audio files.
        /// </summary>
        public static List<StreamInfoProvider> InternalSourceLibrary = new List<StreamInfoProvider>
		{
			new StreamInfoProvider(0, "silent.wav", "Silent"),
			new StreamInfoProvider(1, "crash1_edge_v5.wav", "Crash Edge V1"),
			new StreamInfoProvider(2, "crash1_edge_v8.wav", "Crash Edge V2"),
			new StreamInfoProvider(3, "crash1_edge_v10.wav", "Crash Edge V3"),
			new StreamInfoProvider(4, "floortom_v6.wav", "FloorTom V1"),
			new StreamInfoProvider(5, "floortom_v11.wav", "FloorTom V2"),
			new StreamInfoProvider(6, "floortom_v16.wav", "FloorTom V3"),
			new StreamInfoProvider(7, "hihat_closed_center_v4.wav", "HiHat Closed Center V1"),
			new StreamInfoProvider(8, "hihat_closed_center_v7.wav", "HiHat Closed Center V2"),
			new StreamInfoProvider(9, "hihat_closed_center_v10.wav", "HiHat Closed Center V3"),
			new StreamInfoProvider(10, "hihat_closed_edge_v7.wav", "HiHat Closed Edge V1"),
			new StreamInfoProvider(11, "hihat_closed_edge_v10.wav", "HiHat Closed Edge V2"),
			new StreamInfoProvider(12, "hihat_half_center_v4.wav", "HiHat Half Center V1", HiHatStatuses.Open),
			new StreamInfoProvider(13, "hihat_half_center_v7.wav", "HiHat Half Center V2", HiHatStatuses.Open),
			new StreamInfoProvider(14, "hihat_half_center_v10.wav", "HiHat Half Center V3", HiHatStatuses.Open),
			new StreamInfoProvider(15, "hihat_half_edge_v7.wav", "HiHat Half Edge V1", HiHatStatuses.Open),
			new StreamInfoProvider(16, "hihat_half_edge_v10.wav", "HiHat Half Edge V2", HiHatStatuses.Open),
			new StreamInfoProvider(17, "hihat_open_center_v4.wav", "HiHat Open Center V1", HiHatStatuses.Open),
			new StreamInfoProvider(18, "hihat_open_center_v7.wav", "HiHat Open Center V2", HiHatStatuses.Open),
			new StreamInfoProvider(19, "hihat_open_center_v10.wav", "HiHat Open Center V3", HiHatStatuses.Open),
			new StreamInfoProvider(20, "hihat_open_edge_v7.wav", "HiHat Open Edge V1", HiHatStatuses.Open),
			new StreamInfoProvider(21, "hihat_open_edge_v10.wav", "HiHat Open Edge V2", HiHatStatuses.Open),
			new StreamInfoProvider(22, "hihat_pedal_v3.wav", "HiHat Pedal V1", HiHatStatuses.Down),
			new StreamInfoProvider(23, "hihat_pedal_v5.wav", "HiHat Pedal V2", HiHatStatuses.Down),
			new StreamInfoProvider(24, "kick_v7.wav", "Kick Drum V1"),
			new StreamInfoProvider(25, "kick_v11.wav", "Kick Drum V2"),
			new StreamInfoProvider(26, "kick_v16.wav", "Kick Drum V3"),
			new StreamInfoProvider(27, "racktom_v6.wav", "RackTom V1"),
			new StreamInfoProvider(28, "racktom_v11.wav", "RackTom V2"),
			new StreamInfoProvider(29, "racktom_v16.wav", "RackTom V3"),
			new StreamInfoProvider(30, "ride_bell_v5.wav", "Ride Bell V1"),
			new StreamInfoProvider(31, "ride_bell_v8.wav", "Ride Bell V2"),
			new StreamInfoProvider(32, "ride_bell_v10.wav", "Ride Bell V3"),
			new StreamInfoProvider(33, "ride_center_v5.wav", "Ride Center V1"),
			new StreamInfoProvider(34, "ride_center_v6.wav", "Ride Center V2"),
			new StreamInfoProvider(35, "ride_center_v8.wav", "Ride Center V3"),
			new StreamInfoProvider(36, "ride_center_v10.wav", "Ride Center V4"),
			new StreamInfoProvider(37, "ride_edge_v4.wav", "Ride Edge V1"),
			new StreamInfoProvider(38, "ride_edge_v7.wav", "Ride Edge V2"),
			new StreamInfoProvider(39, "ride_edge_v10.wav", "Ride Edge V3"),
			new StreamInfoProvider(40, "snare_center_v6.wav", "Snare Center V1"),
			new StreamInfoProvider(41, "snare_center_v11.wav", "Snare Center V2"),
			new StreamInfoProvider(42, "snare_center_v16.wav", "Snare Center V3"),
			new StreamInfoProvider(43, "snare_edge_v6.wav", "Snare Edge V1"),
			new StreamInfoProvider(44, "snare_edge_v11.wav", "Snare Edge V2"),
			new StreamInfoProvider(45, "snare_edge_v16.wav", "Snare Edge V3"),
			new StreamInfoProvider(46, "snare_rim_v6.wav", "Snare Rim V1"),
			new StreamInfoProvider(47, "snare_rim_v11.wav", "Snare Rim V2"),
			new StreamInfoProvider(48, "snare_rim_v16.wav", "Snare Rim V3"),
			new StreamInfoProvider(49, "snare_xstick_v6.wav", "Snare XStick V1"),
			new StreamInfoProvider(50, "snare_xstick_v11.wav", "Snare XStick V2"),
			new StreamInfoProvider(51, "snare_xstick_v16.wav", "Snare XStick V3")
		};
    }
}
