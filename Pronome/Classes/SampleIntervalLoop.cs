﻿using System.Collections.Generic;
using System.Collections;
using System.Linq;

namespace Pronome
{
    public class SampleIntervalLoop : IEnumerable<long>
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
            Enumerator = GetEnumerator();
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

                Layer.ByteRemainder += raw - whole;

                while (Layer.ByteRemainder >= 1)
                {
                    Layer.ByteRemainder--;
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
        public void ConvertBpmValues()
        {
            Beats = Bpm.Select(x => Metronome.Instance.ConvertToBytes(x)).ToArray();
        }
        #endregion
    }
}
