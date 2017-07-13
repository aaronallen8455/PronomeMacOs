using System;
using AudioToolbox;

namespace Pronome
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
		protected double InitialOffset;

        double OffsetBpm;

        private double _volume = 1;

		private float pan;

		protected long _silentInterval;
		//protected bool _intervalIsSilent = true; // True if the phase of the interval is Silent

        protected long cycle; // tracks how many times the render callback has executed

        protected bool IsMuted;
        protected bool IsSilentIntervalSilent;
		#endregion

		#region public properties
		public StreamInfoProvider Info { get => _info; }

		public AudioStreamBasicDescription Format { get => _format; }

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
            Metronome.Instance.Stopped += Reset;
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
			_silentInterval = 0;
            SetInitialMuting();
		}

        public virtual void Reset(object sender, EventArgs e)
        {
            Reset();
        }

        unsafe abstract public void Read(float* leftBuffer, float* rightBuffer, uint count);

        public virtual void Dispose()
        {
            IntervalLoop.Dispose();

            Metronome.Instance.Stopped -= Reset;
            Metronome.Instance.TempoChanged -= TempoChanged;
        }

        /// <summary>
        /// Sets the initial muting (random and intervallic).
        /// </summary>
        public virtual void SetInitialMuting() 
        {
            IsMuted = WillRandomMute();
            // see if first note is in the silent interval
            _silentInterval -= (long)InitialOffset;
            IsSilentIntervalSilent = SilentIntervalMuted();
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
            else
            {
                // TODO: handle dynamic tempos
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
				for (int i = 0; i < amount; i++)
				{
					leftBuffer[i] = rightBuffer[i] = 0;
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

				return randomNum <= Metronome.Instance.RandomnessFactor * 100;
			}

			return false;
		}

		/// <summary>
		/// Check if silent interval is muted.
		/// </summary>
		/// <returns><c>true</c>, if interval muted was silented, <c>false</c> otherwise.</returns>
		protected bool SilentIntervalMuted()
		{
			if (Metronome.Instance.IsSilentIntervalEngaged)
			{
                _silentInterval -= SampleInterval;

				if (_silentInterval <= 0)
				{
					// get the intervals in samples
					long silent = (long)Metronome.Instance.ConvertBpmToSamples(Metronome.Instance.SilentInterval);
					long audible = (long)Metronome.Instance.ConvertBpmToSamples(Metronome.Instance.AudibleInterval);

					// account for possible proced cycles
					_silentInterval %= silent + audible;

                    IsSilentIntervalSilent = _silentInterval > audible;

					// reset the interval size
                    _silentInterval = IsSilentIntervalSilent ? silent : audible;
				}

                return IsSilentIntervalSilent;
			}

			return false;
		}
		#endregion
	}
}
