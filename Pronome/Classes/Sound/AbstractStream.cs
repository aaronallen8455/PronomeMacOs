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
		double InitialOffset;

        private double _volume = 1;

		private float pan;

		long _silentInterval;
		bool _intervalIsSilent = true; // True if the phase of the interval is Silent
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
		/// Gets or sets the offset in samples.
		/// </summary>
		/// <value>The offset.</value>
		public double Offset
		{
			get => CurrentOffset;
			set
			{
				InitialOffset = CurrentOffset = value;
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

            // reset when playback stops
            Metronome.Instance.Stopped += Reset;
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
			_intervalIsSilent = true;
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
        }

        public virtual void SetInitialMuting() {}
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

        protected unsafe uint HandleOffset(float* leftBuffer, float* rightBuffer, uint count)
        {
			if (CurrentOffset > 0)
			{
				// account for the offset
				int amount = (int)Math.Min(CurrentOffset, count);
				CurrentOffset -= amount;
				count -= (uint)amount;

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

            return count;
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
				if (_silentInterval <= 0)
				{
					// get the intervals in samples
					long silent = (long)Metronome.Instance.ConvertBpmToSamples(Metronome.Instance.SilentInterval);
					long audible = (long)Metronome.Instance.ConvertBpmToSamples(Metronome.Instance.AudibleInterval);

					// account for possible proced cycles
					_silentInterval %= silent + audible;

					_intervalIsSilent = _silentInterval > audible;

					// reset the interval size
					_silentInterval = _intervalIsSilent ? silent : audible;
				}

				return _intervalIsSilent;
			}

			return false;
		}
		#endregion
	}
}
