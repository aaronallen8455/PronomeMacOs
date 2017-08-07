using System;
using System.Runtime.InteropServices;
using AudioToolbox;
using AudioUnit;
using CoreAudioKit;

namespace Pronome.Mac
{
    public class FileRecorder
    {
        public FileRecorder()
        {

        }

        public static void ExportWavFile(string fileName, double seconds)
        {
            long samples = (long)(seconds * 44100);

			var d = AudioStreamBasicDescription.CreateLinearPCM();
			d.BytesPerPacket = 4;
			d.FramesPerPacket = 1;

			
			var desc = new AudioStreamBasicDescription()
			{
				BytesPerPacket = 4,
				FramesPerPacket = 4,
				SampleRate = 44100,
				ChannelsPerFrame = 2,
				BitsPerChannel = 16,
				BytesPerFrame = 4,
			};

            var file = ExtAudioFile.CreateWithUrl(
                new Foundation.NSUrl(fileName, false),
                AudioFileType.WAVE,
                d,
                AudioFileFlags.EraseFlags,
                out ExtAudioFileError e
            );
            file.ClientDataFormat = d;


            //var file = AudioFile.Create(
            //    fileName,
            //    AudioFileType.WAVE,
            //    d,
            //    AudioFileFlags.EraseFlags
            //);
            int start = 0;

            while (samples > 0)
            {
				uint numSamples = (uint)(Math.Min(Mixer.BufferSize, samples));
                // initialize the buffers
                var leftBuffer = new AudioBuffer();
                leftBuffer.DataByteSize = Mixer.BufferSize * 4;
                leftBuffer.NumberChannels = 1;
                leftBuffer.Data = Marshal.AllocHGlobal(sizeof(float) * (int)numSamples);
                var rightBuffer = new AudioBuffer();
                rightBuffer.DataByteSize = Mixer.BufferSize * 4;
                rightBuffer.NumberChannels = 1;
                rightBuffer.Data = Marshal.AllocHGlobal(sizeof(float) * (int)numSamples);
                var buffers = new AudioBuffers(2);
                buffers[0] = leftBuffer;
                buffers[1] = rightBuffer;

                // get samples from the mixer

                Metronome.Instance.Mixer.Render(numSamples, buffers, 0);

                // write samples to the file
                if (file.Write(numSamples, buffers) != ExtAudioFileError.OK)
                {
                    throw new ApplicationException();
                }
                //var error = file.WritePackets(false, start, (int)numSamples, leftBuffer.Data, (int)(numSamples * 4));

                start += (int)numSamples;
                samples -= Mixer.BufferSize;
            }
            file.Dispose();
            // reset
            Metronome.Instance.Play();
            Metronome.Instance.Stop();
        }
    }
}
