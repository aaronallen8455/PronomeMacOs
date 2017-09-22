﻿using System;
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
                    if (ProduceBytes)
                    {
						double oldFreq = Frequency;
						//double oldWavelength = WaveLength;
						
						double newFreq = MoveToNextFrequency();
						
						// check for random or interval muting
						if (!WillMute())
						{
							Frequency = newFreq;
							if (!oldFreq.Equals(Frequency))
							{
								WaveLength = Metronome.SampleRate / Frequency;
							}
							// set the sample index if transitioning from an active note
							if (Gain > 0)
							{
								_sample = (int)(Math.Asin(sampleValue / Volume) / TwoPI / WaveLength) + 1;
							}
							else
							{
								_sample = 0;
							}
							
							Gain = 1; // back to full volume
							
							// propagate a change of the gain step
							GainStep = NewGainStep;
						}
                    }

                    MoveToNextSampleInterval();
                }

                if (ProduceBytes)
                {
					if (Gain > 0)
					{
						sampleValue = (float)(Math.Sin(_sample * TwoPI / WaveLength) * Gain);
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
