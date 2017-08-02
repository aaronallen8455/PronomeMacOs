using System;
using AudioToolbox;

namespace Pronome.Mac
{
    public interface IStreamProvider
    {
        StreamInfoProvider Info { get; set; }

        AudioStreamBasicDescription Format { get; }

        double Volume { get; set; }

        float Pan { get; set; }

        /// <summary>
        /// Gets or sets the offset in samples.
        /// </summary>
        /// <value>The offset.</value>
        double Offset { get; set; }

        SampleIntervalLoop IntervalLoop { get; set; }

        Layer Layer { get; set; }

        unsafe void Read(float* leftBuffer, float* rightBuffer, uint count, bool writeToBuffer = true);

        void Dispose();

        /// <summary>
        /// Reset this instance so that it plays from the start. Attaches to the 
        /// </summary>
        void Reset();
    }
}
