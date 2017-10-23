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
        /// True if sound is muted or not part of a solo group. Used to keep mixer synced.
        /// </summary>
        /// <value><c>true</c> if is muted; otherwise, <c>false</c>.</value>
        bool IsMuted { get; set; }

        /// <summary>
        /// Gets or sets the offset in bpm.
        /// </summary>
        /// <value>The offset.</value>
        double Offset { get; set; }

        /// <summary>
        /// Gets or sets the initial offset in samples.
        /// </summary>
        /// <value>The initial offset.</value>
        double InitialOffset { get; set; }

        double CurrentOffset { get; }

        SampleIntervalLoop IntervalLoop { get; set; }

        Layer Layer { get; set; }

        unsafe void Read(float* leftBuffer, float* rightBuffer, uint count, bool writeToBuffer = true);

		/// <summary>
		/// Keeps track of partial samples to add back in when the value is >= 1.
		/// </summary>
		double SampleRemainder { get; set; }

        bool SilentIntervalMuted(long sampleInterval);

        void Dispose();

        /// <summary>
        /// Reset this instance so that it plays from the start. Attaches to the 
        /// </summary>
        void Reset();
    }
}
