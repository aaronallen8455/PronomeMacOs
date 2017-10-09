using System;
using AudioToolbox;

namespace Pronome.Mac
{
    public abstract class AbstractStream : IStreamProvider, IDisposable
    {
		#region private/protected variables
		protected StreamInfoProvider _info;

		protected AudioStreamBasicDescription _format;

		protected long SampleInterval;

		/// <summary>
		/// The current offset in samples.
		/// </summary>
		double CurrentOffset;

		/// <summary>
		/// The initial offset in samples.
		/// </summary>
        public double InitialOffset { get; set; }

        double OffsetBpm;

        private double _volume = 1;

		private float pan;

        /// <summary>
        /// holds the current position within the silent/audible interval continuum
        /// </summary>
		protected long _silentInterval;

        /// <summary>
        /// Number of samples until full randomness is reached
        /// </summary>
        long RandomMuteCountdown;

        long RandomMuteCountdownTotal;
		#endregion

		#region public properties
        public StreamInfoProvider Info { get => _info; set { _info = value; } }

		public AudioStreamBasicDescription Format { get => _format; }

        /// <summary>
        /// False if the read method is not producing any output. Used to efficiently progress a stream.
        /// </summary>
        /// <value><c>true</c> if producing bytes; otherwise, <c>false</c>.</value>
        public bool ProduceBytes { get; set; } = true;

        public bool IsMuted { get; set; }

		/// <summary>
		/// Gets or sets the volume.
		/// </summary>
		/// <value>The volume.</value>
		public double Volume
		{
			get => _volume;
			set
			{
				if (value >= 0 && value <= 1)
				{
					_volume = value;

					Metronome.Instance.SetVolumeOfMixerInput(this, (float)_volume);
				}

				// queue the gain step to change
				//NewGainStep = value / (Metronome.SampleRate * DecayLength);
			}
		}

		/// <summary>
		/// Gets or sets the offset in Bpm.
		/// </summary>
		/// <value>The offset.</value>
		public double Offset
		{
            get => OffsetBpm;
			set
			{
                OffsetBpm = value;
                SetSampleOffset(value);
			}
		}

		/// <summary>
		/// Gets or sets the interval loop which provides the beat cell duration values in samples.
		/// </summary>
		/// <value>The interval loop.</value>
		public SampleIntervalLoop IntervalLoop { get; set; }

		/// <summary>
		/// Gets or sets the pan from -1 to 1.
		/// </summary>
		/// <value>The pan.</value>
		public float Pan
		{
			get { return pan; }
			set
			{
				if (value >= -1 && value <= 1)
				{
					pan = value;

					Metronome.Instance.SetPanOfMixerInput(this, pan);
				}

				pan = value;
			}
		}

		public Layer Layer { get; set; }
		#endregion

		#region constructors
        public AbstractStream(StreamInfoProvider info, Layer layer)
		{
            Layer = layer;
            _info = info;


            // subscribe to events
            Metronome.Instance.Started += Reset;
            Metronome.Instance.TempoChanged += TempoChanged;
		}
		#endregion

		#region public methods
		/// <summary>
		/// Reset the internals of this instance so that it plays from the start.
		/// </summary>
        public virtual void Reset()
		{
			CurrentOffset = InitialOffset;
			SampleInterval = 0;
			IntervalLoop.Enumerator = IntervalLoop.GetEnumerator();
            //_silentInterval = (long)InitialOffset * -1;
            if (Metronome.Instance.IsSilentIntervalEngaged)
            {
				long silent = (long)Metronome.Instance.ConvertBpmToSamples(Metronome.Instance.SilentIntervalBpm);
				long audible = (long)Metronome.Instance.ConvertBpmToSamples(Metronome.Instance.AudibleIntervalBpm);
                _silentInterval = silent + audible - (long)InitialOffset;
            }
            RandomMuteCountdownTotal = 0;
		}

        public virtual void Reset(object sender, EventArgs e)
        {
            Reset();
        }

        unsafe abstract public void Read(float* leftBuffer, float* rightBuffer, uint count, bool writeToBuffer = true);

        /// <summary>
        /// Propagates the tempo change.
        /// </summary>
        /// <param name="ratio">Ratio.</param>
        public virtual void PropagateTempoChange(double ratio)
        {
            IntervalLoop.ConvertBpmValues();

            // silent interval
            _silentInterval = (long)(_silentInterval * ratio);
            // offset
            InitialOffset *= ratio;
            CurrentOffset *= ratio;
            // sample interval
            long newSI = (long)(SampleInterval * ratio);
            Layer.SampleRemainder += (SampleInterval * ratio) - newSI;
            SampleInterval = newSI;
        }

        public virtual void Dispose()
        {
            IntervalLoop.Dispose();

            Metronome.Instance.Stopped -= Reset;
            Metronome.Instance.TempoChanged -= TempoChanged;
        }
		#endregion

		#region protected methods
        /// <summary>
        /// Moves to next byte interval.
        /// </summary>
		protected void MoveToNextSampleInterval()
		{
			IntervalLoop.Enumerator.MoveNext();

			SampleInterval = IntervalLoop.Enumerator.Current;
		}

        void TempoChanged(object sender, EventArgs e)
        {
            if (Metronome.Instance.PlayState == Metronome.PlayStates.Stopped)
            {
                SetSampleOffset(OffsetBpm);
            }
        }

        protected void SetSampleOffset(double bpm)
        {
            InitialOffset = CurrentOffset = Metronome.Instance.ConvertBpmToSamples(bpm);
        }

        /// <summary>
        /// Handles the offset and returns current index of the buffers.
        /// </summary>
        /// <returns>The offset.</returns>
        /// <param name="leftBuffer">Left buffer.</param>
        /// <param name="rightBuffer">Right buffer.</param>
        /// <param name="count">Count.</param>
        protected unsafe int HandleOffset(float* leftBuffer, float* rightBuffer, uint count)
        {
            int amount = 0; // number of samples to offset by
			if (CurrentOffset > 0)
			{
				// account for the offset
				amount = (int)Math.Min(CurrentOffset, count);
				CurrentOffset -= amount;

                // zero out buffers
                if (leftBuffer != null)
                {
					for (int i = 0; i < amount; i++)
					{
						leftBuffer[i] = rightBuffer[i] = 0;
					}
                }

				// add remainder to the layer
				if (CurrentOffset < 1)
				{
					Layer.SampleRemainder += CurrentOffset;
					CurrentOffset = 0;
				}
			}

            return amount;
        }

		/// <summary>
		/// Find if the current note should be muted.
		/// </summary>
		/// <returns><c>true</c>, if random mute was willed, <c>false</c> otherwise.</returns>
		protected bool WillRandomMute()
		{
			if (Metronome.Instance.IsRandomMuteEngaged)
			{
				int randomNum = Metronome.GetRandomNum();

                if (Metronome.Instance.RandomMuteCountdown > 0)
                {
                    if (RandomMuteCountdownTotal == 0)
                    {
                        RandomMuteCountdownTotal = RandomMuteCountdown = (long)(Metronome.Instance.RandomMuteCountdown * Metronome.SampleRate) - (long)InitialOffset;
                    }
                    // remove elapsed time
                    RandomMuteCountdown -= Math.Min(SampleInterval, RandomMuteCountdown);

                    return randomNum <= (RandomMuteCountdownTotal - RandomMuteCountdown) / RandomMuteCountdownTotal * Metronome.Instance.RandomnessFactor;
                }

				return randomNum <= Metronome.Instance.RandomnessFactor;
			}

			return false;
		}

		/// <summary>
		/// Check if silent interval is muted. Works by checking if current interval position is before the cut-off point
		/// </summary>
		/// <returns><c>true</c>, if interval muted was silented, <c>false</c> otherwise.</returns>
		public bool SilentIntervalMuted(long sampleInterval)
		{
			if (Metronome.Instance.IsSilentIntervalEngaged)
			{
				long silent = (long)Metronome.Instance.ConvertBpmToSamples(Metronome.Instance.SilentIntervalBpm);
				long audible = (long)Metronome.Instance.ConvertBpmToSamples(Metronome.Instance.AudibleIntervalBpm);
				
				// check result before decrementing so that initial cycle is not stunted
                bool isSilentIntervalSilent = _silentInterval <= silent;

                _silentInterval -= sampleInterval;

                if (_silentInterval <= 0)
                {
					// account for possible proced cycles
					_silentInterval %= silent + audible;

                    _silentInterval += silent + audible;
				}

                return isSilentIntervalSilent;
			}

			return false;
		}

        /// <summary>
        /// True if either random muted or interval muted.
        /// </summary>
        /// <returns><c>true</c>, if mute was willed, <c>false</c> otherwise.</returns>
        protected bool WillMute()
        {
            bool mute = WillRandomMute();

            return SilentIntervalMuted(SampleInterval) || mute;
        }
		#endregion
	}
}
