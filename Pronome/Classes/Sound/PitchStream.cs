using System;
using AudioToolbox;
using System.Collections.Generic;

namespace Pronome
{
    public class PitchStream : IStreamProvider
    {
        const double TwoPI = Math.PI * 2;

        #region static variables
        public static double DecayLength = .04;
        #endregion

        #region private/protected variables
        protected StreamInfoProvider _info;

        protected AudioStreamBasicDescription _format;

        protected LinkedList<double> _frequencies = new LinkedList<double>();

        LinkedListNode<double> _currentFrequency;

        protected long SampleInterval;

        /// <summary>
        /// The current frequency.
        /// </summary>
        protected double Frequency;

        /// <summary>
        /// Index of the sample within the wave.
        /// </summary>
        int _sample = 0;

        /// <summary>
        /// The volume.
        /// </summary>
        protected double _volume = 1;

        /// <summary>
        /// The gain, which is decremented to create the fade.
        /// </summary>
        protected double Gain = 1;

        /// <summary>
        /// The amount to decrement the gain by per sample
        /// </summary>
        protected double GainStep = .0004;

        private double NewGainStep;

        /// <summary>
        /// The current offset in samples.
        /// </summary>
        double CurrentOffset;

        /// <summary>
        /// The initial offset in samples.
        /// </summary>
        double InitialOffset;

        /// <summary>
        /// The length of the wave in samples.
        /// </summary>
        double WaveLength;

		private float pan;
		private float left; // left channel coeficient
		private float right; // right channel coeficient
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
                _volume = value;
                // queue the gain step to change
                NewGainStep = value / (Metronome.SampleRate * DecayLength);
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
				pan = value;

				left = (Pan + 1f) / 2;
				right = (2 - (Pan + 1f)) / 2;
			}
        }

        public Layer Layer { get; set; }
        #endregion

        #region constructors
        public PitchStream(StreamInfoProvider info, Layer layer)
        {
            _info = info;

            // Create the audio format
            _format = new AudioStreamBasicDescription()
            {
                SampleRate = Metronome.SampleRate,
                Format = AudioFormatType.LinearPCM,
                FormatFlags = AudioFormatFlags.IsFloat | AudioFormatFlags.IsPacked | AudioFormatFlags.IsNonInterleaved,
                BitsPerChannel = 16,
                ChannelsPerFrame = 2,
                FramesPerPacket = 1,
                BytesPerFrame = sizeof(short),
                BytesPerPacket = sizeof(short)
            };

            Layer = layer;
        }
        #endregion

        #region public methods
        /// <summary>
        /// Adds to the list of frequencies in order.
        /// </summary>
        /// <param name="freq">Freq.</param>
        public void AddFrequency(double freq)
        {
            _frequencies.AddLast(freq);
        }

        public unsafe void Read(short* leftBuffer, short* rightBuffer, uint count)
        {
            if (CurrentOffset > 0)
            {
                // account for the offset
                int amount = (int)Math.Min(CurrentOffset, count);
                CurrentOffset -= amount;
                count -= (uint)amount;

                // add remainder to the layer
                if (CurrentOffset < 1)
                {
                    Layer.SampleRemainder += CurrentOffset;
                    CurrentOffset = 0;
                }
            }

            for (uint i = 0; i < count; i++)
            {
                if (SampleInterval == 0)
                {
                    MoveToNextByteInterval();

                    double oldFreq = Frequency;
                    MoveToNextFrequency();
                    // set the sample index
                    if (_sample != 0) {
                        if (WaveLength.Equals(0.0f)) {
                            WaveLength = Metronome.SampleRate / oldFreq;
                        }
                        double positionRatio = (_sample % WaveLength) / WaveLength;
                        WaveLength = Metronome.SampleRate / Frequency;
                        _sample = (int)(WaveLength * positionRatio);
                    }
                }

                if (Gain > 0)
                {
                    double sampleValue = Math.Sin(_sample * TwoPI / WaveLength) * Gain;
                    _sample++;
                    Gain -= GainStep;
                    leftBuffer[i] = (short)(sampleValue * left);
                    rightBuffer[i] = (short)(sampleValue * right);
                }
            }
        }
        #endregion

        #region protected methods
        /// <summary>
        /// Moves to the next frequency.
        /// </summary>
        protected void MoveToNextFrequency()
        {
            // get the next frequency in the list
            _currentFrequency = _currentFrequency?.Next;

            // wrap to the front of the list
            if (_currentFrequency == null)
            {
                _currentFrequency = _frequencies.First;
            }

            Frequency = _currentFrequency.Value;
        }

        protected void MoveToNextByteInterval()
        {
            IntervalLoop.Enumerator.MoveNext();

            SampleInterval = IntervalLoop.Enumerator.Current;
        }
        #endregion
    }
}
