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

        double WaveLength;
        #endregion

        #region public properties
        public StreamInfoProvider Info { get => _info; }

        public AudioStreamBasicDescription Format { get => _format; }

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

        public double Offset
        {
            get => CurrentOffset;
            set
            {
                InitialOffset = CurrentOffset = value;
            }
        }

        public SampleIntervalLoop IntervalLoop { get; set; }
        #endregion

        #region constructors
        public PitchStream(StreamInfoProvider info)
        {
            _info = info;

            // Create the audio format
            _format = new AudioStreamBasicDescription()
            {
                SampleRate = Metronome.SampleRate,
                Format = AudioFormatType.LinearPCM,
                FormatFlags = AudioFormatFlags.IsFloat | AudioFormatFlags.IsPacked,
                BitsPerChannel = 16,
                ChannelsPerFrame = 1,
                FramesPerPacket = 1,
                BytesPerFrame = 2,
                BytesPerPacket = 2
            };
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

        public void Read(float[] leftBuffer, float[] rightBuffer, int count)
        {
            for (int i = 0; i < count; i++)
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
                    float sampleValue = (float)(Math.Sin(_sample / WaveLength * TwoPI));
                    leftBuffer[i] = rightBuffer[i] = sampleValue;
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
