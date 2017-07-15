using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System;

namespace Pronome
{
    public class SampleIntervalLoop : IEnumerable<long>, IDisposable
    {
        #region Private Variables
        Layer Layer;
        double[] Beats;
        double[] Bpm;
        #endregion

        #region Public Variables
        public IEnumerator<long> Enumerator;
        #endregion

        #region Constructors
        public SampleIntervalLoop(Layer layer, double[] bpm)
        {
            Layer = layer;
            Bpm = bpm;
            ConvertBpmValues(null, null);
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

                Layer.SampleRemainder += raw - whole;

                while (Layer.SampleRemainder >= 1)
                {
                    Layer.SampleRemainder--;
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
