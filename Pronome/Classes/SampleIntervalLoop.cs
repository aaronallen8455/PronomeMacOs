using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System;

namespace Pronome.Mac
{
    public class SampleIntervalLoop : IEnumerable<long>, IDisposable
    {
        #region Private Variables
        IStreamProvider Stream;
        double[] Beats;
        double[] Bpm;
        #endregion

        #region Public Variables
        public IEnumerator<long> Enumerator;
        #endregion

        #region Constructors
        public SampleIntervalLoop(IStreamProvider stream, double[] bpm)
        {
            Stream = stream;
            Bpm = bpm;
            ConvertBpmValues();
            Metronome.Instance.TempoChanged += ConvertBpmValues;
            Enumerator = bpm.Length == 1 && bpm[0] == 0 ? null : GetEnumerator();
        }
        #endregion

        #region Public Methods
        public IEnumerator<long> GetEnumerator()
        {
            for (int i = 0; ; i++)
            {
                if (i == Beats.Length) i = 0; // wrap to beginning

                double raw = Beats[i];

                long whole = (long)raw;

                Stream.SampleRemainder += raw - whole;

                while (Stream.SampleRemainder >= 1)
                {
                    Stream.SampleRemainder--;
                    whole++;
                }

                yield return whole;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// Converts the bpm values.
        /// </summary>
        public void ConvertBpmValues(object sender, EventArgs e)
        {
            if (Metronome.Instance.PlayState == Metronome.PlayStates.Stopped)
            {
                ConvertBpmValues();
            }
        }

        public void ConvertBpmValues()
        {
            Beats = Bpm.Select(x => Metronome.Instance.ConvertBpmToSamples(x)).ToArray();
        }

        public void Dispose()
        {
            // release events
            Metronome.Instance.TempoChanged -= ConvertBpmValues;
        }
        #endregion
    }
}
