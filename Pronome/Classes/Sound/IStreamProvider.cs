using System;
using AudioToolbox;

namespace Pronome
{
    public interface IStreamProvider
    {
        StreamInfoProvider Info { get; }

        AudioStreamBasicDescription Format { get; }

        double Volume { get; set; }

        float Pan { get; set; }

        /// <summary>
        /// Gets or sets the offset in samples.
        /// </summary>
        /// <value>The offset.</value>
        double Offset { get; set; }

        SampleIntervalLoop IntervalLoop { get; set; }

        Layer Layer { get; }

        unsafe void Read(float* leftBuffer, float* rightBuffer, uint count);

        void Dispose();

        void SetInitialMuting();
    }
}
