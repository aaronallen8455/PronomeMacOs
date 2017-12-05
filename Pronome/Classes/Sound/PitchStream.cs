using System;
using AudioToolbox;
using System.Collections.Generic;
using System.Linq;

namespace Pronome.Mac
{
    public class PitchStream : AbstractStream
    {
        const double TwoPI = Math.PI * 2;

        #region static variables
        //public static double DecayLength = .04;

        static double NewGainStep = 1 / (Metronome.SampleRate * UserSettings.GetSettings().PitchDecayLength);
		#endregion

		#region private/protected variables

        protected LinkedList<double> _frequencies = new LinkedList<double>();

        LinkedListNode<double> _currentFrequency;

        /// <summary>
        /// Generates the sin wave values
        /// </summary>
        protected IEnumerator<float> SinWave;

        /// <summary>
        /// The current frequency.
        /// </summary>
        protected double Frequency;

        /// <summary>
        /// Index of the sample within the wave.
        /// </summary>
        float _sample = 0;

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
        protected double GainStep;

        /// <summary>
        /// The length of the wave in samples.
        /// </summary>
        double WaveLength;
        #endregion

        #region public properties

        #endregion

        #region constructors
        public PitchStream(StreamInfoProvider info, Layer layer) : base(info, layer)
        {
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
        }
		#endregion

		#region Static Methods
        /// <summary>
        /// Sets the length of the decay in seconds.
        /// </summary>
        /// <param name="value">Value.</param>
        public static void SetDecayLength(double value)
        {
            NewGainStep = 1 / (Metronome.SampleRate * value);
        }

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
            if (o != string.Empty) octave = Convert.ToInt32(o);
			else octave = 4;

			float index = Notes[note];
			index += octave * 12;
            index = ApplyStretch(index);
            double frequency = 440 * Math.Pow(2, (index-48) / 12);
			return frequency;
		}

		/**<summary>Used in converting symbols to pitches.</summary>*/
		protected static Dictionary<string, int> Notes = new Dictionary<string, int>
		{
			{ "a", 0 }, { "a#", 1 }, { "bb", 1 }, { "b", 2 }, { "c", -9 },
			{ "c#", -8 }, { "db", -8 }, { "d", -7 }, { "d#", -6 }, { "eb", -6 },
			{ "e", -5 }, { "f", -4 }, { "f#", -3 }, { "gb", -3 }, { "g", -2 },
			{ "g#", -1 }, { "ab", -1 }
		};

        protected static float ApplyStretch(float index)
        {
            float cents = 0;

            if (index > 48)
            {
                cents = StretchSharp.TakeWhile(x => x.Key <= index).Select(x => x.Value).Sum();
            }
            else if (index < 48)
            {
                cents = StretchFlat.TakeWhile(x => x.Key >= index).Select(x => x.Value).Sum();
            }

            return index + cents;
        }

        protected static Dictionary<int, float> StretchSharp = new Dictionary<int, float>
        {
            {54, .01f}, {60, .01f}, {64, .01f}, {68, .01f}, {70, .01f}, {72, .01f}, {73, .01f}, {74, .01f},
            {75, .01f}, {76, .01f}, {77, .01f}, {78, .01f}, {79, .01f}, {80, .01f}, {81, .02f}, {82, .02f},
            {83, .02f}, {84, .02f}, {85, .02f}, {86, .02f}, {87, .03f}
        };

        protected static Dictionary<int, float> StretchFlat = new Dictionary<int, float>
        {
            {47, -.01f}, {41, -.01f}, {24, -.01f}, {22, -.01f}, {17, -.01f}, {15, -.01f}, {13, -.01f},
            {12, -.01f}, {11, -.01f}, {10, -.01f}, {9, -.01f}, {8, -.01f}, {7, -.01f}, {6, -.01f}, {5, -.01f},
            {4, -.01f}, {3, -.01f}, {2, -.01f}, {1, -.01f}, {0, -.01f}
        };

        #endregion

        #region public methods
        /// <summary>
        /// Adds to the list of frequencies in order.
        /// </summary>
        /// <param name="symbol">Freq.</param>
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


        public unsafe override void Read(float* leftBuffer, float* rightBuffer, uint count, bool writeToBuffer = true)
        {
            // account for any offset
            int offset = HandleOffset(leftBuffer, rightBuffer, count);

            for (int i = offset; i < count; i++)
            {
                if (SampleInterval == 0)
                {
					double oldFreq = Frequency;
					//double oldWavelength = WaveLength;
					
					double newFreq = MoveToNextFrequency();

					MoveToNextSampleInterval();

					// check for random or interval muting
					if (!WillMute())
					{
						Frequency = newFreq;

                        double wavePosition = (_sample % WaveLength) / WaveLength;

                        if (!oldFreq.Equals(Frequency))
                        {
                            WaveLength = Metronome.SampleRate / Frequency;
                        }

                        // set the sample index if transitioning from an active note
                        if (Gain > 0 && SinWave != null)
                        {
							_sample = (float)(Math.Asin(sampleValue) / TwoPI * WaveLength);
							
                            // reposition to correct quadrant of waveform
							if (wavePosition > .25 && wavePosition <= .5) 
							{
								_sample += (float)(WaveLength / 4 - _sample) * 2;
							}
							else if (wavePosition > .5 && wavePosition <= .75)
							{
								_sample -= (float)(WaveLength / 4 + _sample) * 2;
							}
                        }
                        else
                        {
                            _sample = 0;
                        }

						SinWave = new SinWaveGenerator(_sample, Frequency).GetEnumerator();
						SinWave.MoveNext();

						Gain = 1; // back to full volume
						
						// propagate a change of the gain step
						GainStep = NewGainStep;
					}
                }

				if (Gain > 0)
				{
                    sampleValue = (float)(SinWave.Current * Gain);
                    SinWave.MoveNext();
					//sampleValue = (float)(Math.Sin(_sample * TwoPI / WaveLength) * Gain);
					_sample++;
					Gain -= GainStep;
					
					if (writeToBuffer)
					{
						leftBuffer[i] = rightBuffer[i] = sampleValue;
					}
				}
				else if (writeToBuffer)
				{
					leftBuffer[i] = rightBuffer[i] = 0;
				}

                SampleInterval--;
            }
        }

        /// <summary>
        /// Reset the internals of this instance so that it plays from the start.
        /// </summary>
        public override void Reset()
        {
            _currentFrequency = null;

            base.Reset();
        }

        public override void Dispose() 
        {
            //IntervalLoop.Dispose();
        }
        #endregion

        #region protected methods
        /// <summary>
        /// Moves the freq linked list to the next frequency. Returns the new frequency.
        /// </summary>
        protected double MoveToNextFrequency()
        {
            // get the next frequency in the list
            _currentFrequency = _currentFrequency?.Next;

            // wrap to the front of the list
            if (_currentFrequency == null)
            {
                _currentFrequency = _frequencies.First;
            }

            return _currentFrequency.Value;
        }
        #endregion
    }
}
