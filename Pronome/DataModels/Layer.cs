﻿using System;
using Foundation;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Pronome
{
    /// <summary>
    /// An object that represents a single beat layer.
    /// </summary>
    [DataContract]
    public class Layer : NSObject
    {
        #region Public Variables
        /// <summary>
        /// Keeps track of partial samples to add back in when the value is >= 1.
        /// </summary>
        public double SampleRemainder = 0;

        /// <summary>
        /// The beat cells.
        /// </summary>
        public LinkedList<BeatCell> BeatCells = new LinkedList<BeatCell>();

        /// <summary>
        /// The base stream info.
        /// </summary>
        public StreamInfoProvider BaseStreamInfo;

		/** <summary>The beat code string that was passed in to create the rhythm of this layer.</summary> */
		[DataMember]
		public string ParsedString;

		/**<summary>The string that was parsed to get the offset value.</summary>*/
		[DataMember]
		public string ParsedOffset = "0";

		/** <summary>The name of the base source.</summary> */
		[DataMember]
		public string BaseSourceName;

		/** <summary>True if a solo group exists.</summary> */
		public static bool SoloGroupEngaged = false; // is there a solo group?

		/** <summary>Does the layer contain a hihat closed source?</summary> */
		public bool HasHiHatClosed = false;

		/** <summary>Does the layer contain a hihat open source?</summary> */
		public bool HasHiHatOpen = false;

		/** <summary>The audio sources that are not pitch or the base sound.</summary> */
		public Dictionary<string, IStreamProvider> AudioSources = new Dictionary<string, IStreamProvider>();

        /// <summary>
        /// The base audio source. Could be a pitch or a sound file.
        /// </summary>
        public IStreamProvider BaseAudioSource;

        /// <summary>
        /// The pitch source, if needed. Will also be the baseAudioSource if it's a pitch layer
        /// </summary>
        public PitchStream PitchSource;
        #endregion

        #region Databound Properties
        private string _beatCode = "1";
        /// <summary>
        /// Gets or sets the raw beat code string.
        /// </summary>
        /// <value>The beat code.</value>
        [Export("BeatCode")]
        public string BeatCode
        {
            get => _beatCode;
            set
            {
                WillChangeValue("BeatCode");
                _beatCode = value;
                DidChangeValue("BeatCode");
            }
        }

        private string _offset = "";
        /// <summary>
        /// Gets or sets the raw offset string.
        /// </summary>
        /// <value>The offset.</value>
        [Export("Offset")]
        public string Offset
        {
            get => _offset;
            set
            {
                WillChangeValue("Offset");
                _offset = value;
                DidChangeValue("Offset");
            }
        }

        [DataMember]
        private nfloat _volume = 1f;
        /// <summary>
        /// Gets or sets the volume.
        /// </summary>
        /// <value>The volume. 0 to 1</value>
        [Export("Volume")]
        public nfloat Volume
        {
            get => _volume;
            set
            {
                WillChangeValue("Volume");
                _volume = value;

				// set volume on sound sources
                double newVolume = value * Metronome.Instance.Volume;
				if (AudioSources != null)
				{
					foreach (IStreamProvider src in AudioSources.Values) src.Volume = newVolume;
				}
				if (BaseAudioSource != null) BaseAudioSource.Volume = newVolume;
                if (PitchSource != null && !BaseStreamInfo.IsPitch) PitchSource.Volume = newVolume;

                DidChangeValue("Volume");
            }
        }

        [DataMember]
        private nfloat _pan = 0f;
        /// <summary>
        /// Gets or sets the pan.
        /// </summary>
        /// <value>The pan. -1 to 1</value>
        [Export("Pan")]
        public nfloat Pan
        {
            get => _pan;
            set
            {
                WillChangeValue("Pan");
                _pan = value;
                // set on audio sources
                float newPan = (float)value;
				foreach (IStreamProvider src in AudioSources.Values) src.Pan = newPan;
                BaseAudioSource.Pan = newPan;
                if (PitchSource != null && !BaseStreamInfo.IsPitch) PitchSource.Pan = newPan;

                DidChangeValue("Pan");
            }
        }

        private bool _muted = false;
        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="T:Pronome.Layer"/> is muted.
        /// </summary>
        /// <value><c>true</c> if is muted; otherwise, <c>false</c>.</value>
        [Export("IsMuted")]
        public bool IsMuted
        {
            get => _muted;
            set
            {
                WillChangeValue("IsMuted");
                _muted = value;
                DidChangeValue("IsMuted");
            }
        }

        private bool _soloed = false;
        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="T:Pronome.Layer"/> is soloed.
        /// </summary>
        /// <value><c>true</c> if is soloed; otherwise, <c>false</c>.</value>
        [Export("IsSoloed")]
        public bool IsSoloed
        {
            get => _soloed;
            set
            {
                WillChangeValue("IsSoloed");
                _soloed = value;
                DidChangeValue("IsSoloed");
            }
        }
        #endregion

        #region Constructor
        public Layer()
        {
        }
        #endregion
    }
}
