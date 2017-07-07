using System;
using System.Runtime.InteropServices;
using AudioToolbox;
using AudioUnit;
using CoreFoundation;

namespace Pronome
{
    public class WavFileStream : AbstractStream
    {
        #region Protected Fields
        protected IntPtr Data;

        protected long TotalFrames;

        private int _sampleNum;
        #endregion

        #region Constructors
        public WavFileStream(StreamInfoProvider info, Layer layer) : base(info, layer)
        {
            _format = new AudioStreamBasicDescription()
            {
                BitsPerChannel = 32,
                Format = AudioFormatType.LinearPCM,
                FormatFlags = AudioStreamBasicDescription.AudioFormatFlagsAudioUnitNativeFloat,
                SampleRate = 44100,
                ChannelsPerFrame = 2,
                FramesPerPacket = 1,
                BytesPerFrame = sizeof(float),
                BytesPerPacket = sizeof(float)
            };

            LoadAudioFile(info);
        }
        #endregion

        #region Public Methods
        public unsafe override void Read(float* leftBuffer, float* rightBuffer, uint count)
        {
            
        }

        public override void Dispose()
        {
            Marshal.DestroyStructure<int[]>(Data);
        }
        #endregion

        #region Protected Methods
        protected void LoadAudioFile(StreamInfoProvider info)
        {
			var url = CFUrl.FromFile(info.Uri);

			using (var file = ExtAudioFile.OpenUrl(url))
			{
				var clientFormat = file.FileDataFormat;
				clientFormat.FormatFlags = AudioStreamBasicDescription.AudioFormatFlagsNativeFloat;
				clientFormat.ChannelsPerFrame = 1;
				clientFormat.FramesPerPacket = 1;
				clientFormat.BitsPerChannel = 8 * sizeof(int);
				clientFormat.BytesPerPacket =
					clientFormat.BytesPerFrame = clientFormat.ChannelsPerFrame * sizeof(int);

				file.ClientDataFormat = clientFormat;

				double rateRatio = Metronome.SampleRate / clientFormat.SampleRate;

				var numFrames = file.FileLengthFrames;
				numFrames = (uint)(numFrames * rateRatio);

				TotalFrames = numFrames;

				UInt32 samples = (uint)(numFrames * clientFormat.ChannelsPerFrame);
				var dataSize = (int)(sizeof(uint) * samples);
				Data = Marshal.AllocHGlobal(dataSize);

				// set up a AudioBufferList to read data into
				var bufList = new AudioBuffers(1);
				bufList[0] = new AudioBuffer
				{
					NumberChannels = 1,
					Data = Data,
					DataByteSize = dataSize
				};

				ExtAudioFileError error;
				file.Read((uint)numFrames, bufList, out error);
				if (error != ExtAudioFileError.OK)
				{
					throw new ApplicationException();
				}
			}
			url.Dispose();
        }
        #endregion
    }
}
