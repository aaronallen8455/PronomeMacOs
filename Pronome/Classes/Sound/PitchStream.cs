using System;
using AudioToolbox;
using System.Collections.Generic;
using System.Linq;

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
                FormatFlags = AudioStreamBasicDescription.AudioFormatFlagsAudioUnitNativeFloat, //AudioFormatFlags.IsFloat | AudioFormatFlags.IsPacked | AudioFormatFlags.IsNonInterleaved,
                BitsPerChannel = 32,
                ChannelsPerFrame = 2,
                FramesPerPacket = 1,
                BytesPerFrame = 4,
                BytesPerPacket = 4
            };

            Layer = layer;
        }
		#endregion

		#region Static Methods
		/**<summary>Convert a pitch symbol or raw number into a hertz value.</summary>
         * <param name="symbol">The symbol to convert from.</param>
         */
		public static double ConvertFromSymbol(string symbol)
		{
			// Remove leading P of raw pitch symbols
			symbol = symbol.TrimStart(new char[] { 'p', 'P' });

			string note = new string(symbol.TakeWhile((x) => !char.IsNumber(x)).ToArray()).ToLower();
			if (note == string.Empty) // raw pitch value
			{
				return Convert.ToDouble(symbol);
			}
			string o = new string(symbol.SkipWhile((x) => !char.IsNumber(x)).ToArray());
			int octave;
			if (o != string.Empty) octave = Convert.ToInt32(o) - 5;
			else octave = 4;

			float index = Notes[note];
			index += octave * 12;
			double frequency = 440 * Math.Pow(2, index / 12);
			return frequency;
		}

		/**<summary>Used in converting symbols to pitches.</summary>*/
		protected static Dictionary<string, int> Notes = new Dictionary<string, int>
		{
			{ "a", 12 }, { "a#", 13 }, { "bb", 13 }, { "b", 14 }, { "c", 3 },
			{ "c#", 4 }, { "db", 4 }, { "d", 5 }, { "d#", 6 }, { "eb", 6 },
			{ "e", 7 }, { "f", 8 }, { "f#", 9 }, { "gb", 9 }, { "g", 10 },
			{ "g#", 11 }, { "ab", 11 }
		};
        #endregion

        #region public methods
        /// <summary>
        /// Adds to the list of frequencies in order.
        /// </summary>
        /// <param name="freq">Freq.</param>
        public void AddFrequency(string symbol)
        {
            double freq = ConvertFromSymbol(symbol);
            _frequencies.AddLast(freq);
        }

        /// <summary>
        /// Clears the frequencies.
        /// </summary>
        public void ClearFrequencies()
        {
            _frequencies.Clear();
        }

        private float sampleValue = 0;

        public unsafe void Read(float* leftBuffer, float* rightBuffer, uint count)
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
                    //double oldWavelength = WaveLength;
					
                    MoveToNextFrequency();
                    if (!oldFreq.Equals(Frequency))
                    {
						WaveLength = Metronome.SampleRate / Frequency;
                    }
                    // set the sample index if transitioning from an active note
                    if (Gain > 0) 
                    {
                        //double positionRatio = (_sample % oldWavelength) / oldWavelength;
                        //_sample = (int)(WaveLength * positionRatio);

                        _sample = (int)(Math.Asin(sampleValue / Volume) / TwoPI / WaveLength) + 1;
                    }
                    else
                    {
                        _sample = 0;
                    }

					Gain = Volume; // back to full volume
                }

                if (Gain > 0)
                {
                    sampleValue = (float)(Math.Sin(_sample * TwoPI / WaveLength) * Gain);
                    _sample++;
                    Gain -= GainStep;
                    leftBuffer[i] = sampleValue * left;
                    rightBuffer[i] = sampleValue * right;
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
