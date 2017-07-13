﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using AudioToolbox;
using AudioUnit;
using CoreFoundation;
using Foundation;

namespace Pronome
{
    public class WavFileStream : AbstractStream
    {
        #region Protected Fields
        protected IntPtr Data;

        protected long TotalFrames;

        private long _sampleNum;

        long CurrentHiHatDuration;
        protected long HiHatSampleToMute;
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
            int offset = HandleOffset(leftBuffer, rightBuffer, count);

            for (int i = offset; i < count; i++)
            {
                if (SampleInterval == 0)
                {
                    if (!IsSilentIntervalSilent && !IsMuted)
					{
                        _sampleNum = 0;
					}

                    MoveToNextSampleInterval();

					IsMuted = WillRandomMute();
					IsSilentIntervalSilent = SilentIntervalMuted();

					// if this is a hihat down, pass it's time position to all hihat opens in this layer
					if (Info.HiHatStatus == StreamInfoProvider.HiHatStatuses.Down && Layer.HasHiHatOpen && !IsSilentIntervalSilent && !IsMuted)
					{
						PropagateHiHatDown(i);
					}

                    //if (Info.HiHatStatus == StreamInfoProvider.HiHatStatuses.Open && Layer.HasHiHatClosed)
                    //{
                    //    CurrentHiHatDuration = HiHatSampleToMute;
                    //}
                }

                if (Layer.HasHiHatClosed && Info.HiHatStatus == StreamInfoProvider.HiHatStatuses.Open && CurrentHiHatDuration > 0)
				{
					//HiHatSampleToMute--;
                    CurrentHiHatDuration--;
                    HiHatSampleToMute--;

                    // mute the hihat open sound on hihat closed
                    if (CurrentHiHatDuration == 0)
                    {
                        _sampleNum = TotalFrames;
                        CurrentHiHatDuration = HiHatSampleToMute;
                    }
				}

                if (_sampleNum < TotalFrames)
                {
                    var input = (float*)Data;
                    leftBuffer[i] = rightBuffer[i] = input[_sampleNum++];
                }
                else
                {
                    // if end of file reached, fill with silence
                    leftBuffer[i] = rightBuffer[i] = 0;
                }

                SampleInterval--;
            }
        }

        public override void SetInitialMuting()
        {
            base.SetInitialMuting();

            // set this so that if first note is muted, it won't play
            _sampleNum = TotalFrames;

            // if this is a hihat down sound and there are open sounds, we set the timer on them
            if (Info.HiHatStatus == StreamInfoProvider.HiHatStatuses.Down
                && Layer.HasHiHatOpen && !IsSilentIntervalSilent && !IsMuted && Offset > 0)
            {
				IEnumerable<IStreamProvider> hhos = Layer.GetAllStreams().Where(x => x.Info.HiHatStatus == StreamInfoProvider.HiHatStatuses.Open);
				foreach (WavFileStream hho in hhos)
				{
                    hho.HiHatSampleToMute = hho.CurrentHiHatDuration = (long)(InitialOffset - hho.InitialOffset);
				}
            }
        }

        public override void Reset()
        {
            base.Reset();

            HiHatSampleToMute = 0;
            CurrentHiHatDuration = 0;
        }

        public override void Dispose()
        {
            Marshal.DestroyStructure<float[]>(Data);
        }
        #endregion

        #region Protected Methods
        protected void LoadAudioFile(StreamInfoProvider info)
        {
            // get the path to the file
            string path;
            if (info.IsInternal)
            {
                path = NSBundle.MainBundle.PathForSoundResource(info.Uri);
            }
            else
            {
                // file path is the Uri for user sources
                path = info.Uri;
            }

            using (var url = CFUrl.FromFile(path))
            {
				using (var file = ExtAudioFile.OpenUrl(url))
				{
					var clientFormat = file.FileDataFormat;
					clientFormat.FormatFlags = AudioStreamBasicDescription.AudioFormatFlagsNativeFloat;
					clientFormat.ChannelsPerFrame = 1;
					clientFormat.FramesPerPacket = 1;
					clientFormat.BitsPerChannel = 8 * sizeof(float);
					clientFormat.BytesPerPacket =
						            clientFormat.BytesPerFrame = clientFormat.ChannelsPerFrame * sizeof(float);
					
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
            }
        }

        protected void PropagateHiHatDown(int i)
        {
            //int count = Mixer.BufferSize;

			long total = i + SampleInterval;
			//long cycles = total / count + cycle;
			//long bytes = total % count;

			// assign the hihat cutoff to all open hihat sounds in this layer.
			IEnumerable<IStreamProvider> hhos = Layer.GetAllStreams().Where(x => x.Info.HiHatStatus == StreamInfoProvider.HiHatStatuses.Open);
			foreach (WavFileStream hho in hhos)
			{
                // if target is queued before this one, subtract cycle length
                if (Metronome.Instance.Mixer.GetIndexOfStream(this) > Metronome.Instance.Mixer.GetIndexOfStream(hho))
                {
                    total -= Mixer.BufferSize;

                    if (total <= 0) total = 1;
                }

				hho.HiHatSampleToMute = total;
                if (hho.CurrentHiHatDuration == 0) hho.CurrentHiHatDuration = total;
			}
        }
        #endregion
    }
}
