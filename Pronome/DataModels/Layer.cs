﻿using System;
using Foundation;
using System.Collections.Generic;

namespace Pronome
{
    /// <summary>
    /// An object that represents a single beat layer.
    /// </summary>
    public class Layer : NSObject
    {
        #region Public Variables
        /// <summary>
        /// Keeps track of partial samples to add back in when the value is >= 1.
        /// </summary>
        public double ByteRemainder = 0;

        /// <summary>
        /// The beat cells.
        /// </summary>
        public LinkedList<BeatCell> BeatCells = new LinkedList<BeatCell>();

        public StreamInfoProvider BaseStreamInfo;

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
                DidChangeValue("Volume");
            }
        }

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
