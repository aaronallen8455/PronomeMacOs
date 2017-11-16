using System;
using AudioToolbox;
using AudioUnit;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace Pronome.Mac
{
    public class Mixer : IDisposable
    {
        public const int BufferSize = 512;

        #region Protected Variables
        /// <summary>
        /// Connects the mixer to the output device
        /// </summary>
        protected AUGraph Graph;

        protected AudioUnit.AudioUnit Output;

        /// <summary>
        /// Collection of streams to read from.
        /// </summary>
        protected List<IStreamProvider> Streams = new List<IStreamProvider>();
        //protected Dictionary<StreamInfoProvider, IStreamProvider> Streams = new Dictionary<StreamInfoProvider, IStreamProvider>();
        //protected List<KeyValuePair<StreamInfoProvider, IStreamProvider>> Streams = new List<KeyValuePair<StreamInfoProvider, IStreamProvider>>();

        /// <summary>
        /// The audio unit which mixes the streams.
        /// </summary>
        protected AudioUnit.AudioUnit MixerNode;

        bool _tempoChanged;
        double _tempoChangeRatio;

        private double cycle;

        bool _fileRecordingQueued;
        ExtAudioFile _file;
        //AudioBuffers _conversionBuffer;

        AudioConverter _converter;

        /// <summary>
        /// Used to play a count-off when doing a tap entry
        /// </summary>
        PitchStream _countOff;

        long _countOffTotal;
        #endregion

        #region Public Variables
        public bool IsPlaying = false;

		/// <summary>
		/// number of samples that constitutes the count-off
		/// </summary>
        public long CountOffSampleDuration;
        #endregion

        #region Constructors
        public Mixer()
        {
			BuildAUGraph();

            _converter = AudioConverter.Create(MixerNode.GetAudioFormat(AudioUnitScopeType.Output), AudioStreamBasicDescription.CreateLinearPCM());

            Metronome.Instance.TempoChanged += TempoChanged;

            _countOff = new PitchStream(StreamInfoProvider.GetDefault(), null);
            _countOff.IntervalLoop = new SampleIntervalLoop(_countOff, new double[] { 1 });
            _countOff.AddFrequency("A4");
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Adds the stream to the mixer.
        /// </summary>
        /// <param name="stream">Stream.</param>
        public void AddStream(IStreamProvider stream)
        {
            if (Streams.Contains(stream)) return;

            Streams.Add(stream);

            if (MixerNode != null)
            {
                // bus index of new stream
                uint i = (uint)(Streams.Count - 1);

                // add input to the mixer
                if (MixerNode.SetElementCount(AudioUnitScopeType.Input, (uint)Streams.Count) != AudioUnitStatus.OK)
                {
                    throw new ApplicationException();
                }

                // add the callback
                //if (Metronome.Instance.PlayState == Metronome.PlayStates.Stopped || i != 0)
                if (MixerNode.SetRenderCallback(MixerRenderDelegate, AudioUnitScopeType.Global, i) != AudioUnitStatus.OK)
				{
					throw new ApplicationException();
				}
                			
				// set input stream format
				var desc = stream.Format;
			
				if (MixerNode.SetFormat(desc, AudioUnitScopeType.Input, i) != AudioUnitStatus.OK)
				{
					throw new ApplicationException();
				}

                // set volume and pan from layer
                stream.Volume = stream.Layer.Volume;
                stream.Pan = (float)stream.Layer.Pan;
            }
        }

        /// <summary>
        /// Removes the stream.
        /// </summary>
        /// <param name="stream">Stream.</param>
        public void RemoveStream(IStreamProvider stream)
        {
            if (!Streams.Contains(stream)) return;

            bool needToReset = Streams.IndexOf(stream) != Streams.Count - 1;

            Streams.Remove(stream);

            if (needToReset)
            {
                foreach (IStreamProvider src in Streams)
                {
                    SetPan(src, src.Pan);
                    SetInputVolume(src, (float)src.Volume);
                    EnableInput(src, !src.IsMuted);
                }
            }

            // re-configure the mixer inputs
            if (Metronome.Instance.PlayState == Metronome.PlayStates.Stopped)
                ConfigureMixerInputs();
        }

        /// <summary>
        /// Reset this instance.
        /// </summary>
        public void Reset()
        {
            Streams.Clear();
        }

        /// <summary>
        /// Start playing.
        /// </summary>
		public void Start()
		{
			if (Graph.Start() != AUGraphError.OK)
				throw new ApplicationException();

            IsPlaying = true;
		}

        /// <summary>
        /// Stop playing.
        /// </summary>
		public void Stop()
		{
			if (Graph.IsRunning)
			{
				if (Graph.Stop() != AUGraphError.OK)
					throw new ApplicationException();

                //if (Metronome.Instance.PlayState == Metronome.PlayStates.Stopped)
                //{
					_file?.Dispose(); // finished recording to file
					_fileRecordingQueued = false;
					
					IsPlaying = false;
					_tempoChanged = false;
					cycle = 0;
                //}
			}
		}

        /// <summary>
        /// Queues up a file to record to the next time the beat is played
        /// </summary>
        /// <param name="fileName">File name.</param>
        public void QueueFileRecording(string fileName)
        {
            _file = ExtAudioFile.CreateWithUrl(
                new Foundation.NSUrl(fileName, false),
                AudioFileType.WAVE,
                AudioStreamBasicDescription.CreateLinearPCM(),
                AudioFileFlags.EraseFlags,
                out ExtAudioFileError e
            );

            _fileRecordingQueued = true;
        }

        /// <summary>
        /// Renders out the beat to the given buffers
        /// </summary>
        /// <returns>The render.</returns>
        /// <param name="samples">Samples.</param>
        /// <param name="buffers">Buffers.</param>
        /// <param name="offset">Offset.</param>
        public void Render(uint samples, AudioBuffers buffers, double offset)
        {
            AudioTimeStamp timeStamp = new AudioTimeStamp();
            timeStamp.SampleTime = offset;

            var flag = AudioUnitRenderActionFlags.DoNotCheckRenderArgs;

			var e = MixerNode.Render(
                ref flag,
				timeStamp,
				0,
                samples,
				buffers
			);

            if (e != AudioUnitStatus.OK) throw new ApplicationException();
        }

        /// <summary>
        /// Renders the given number of seconds to the given wav file
        /// </summary>
        /// <param name="fileName">File name.</param>
        /// <param name="seconds">Seconds.</param>
        public void RenderToFile(string fileName, double seconds)
        {
            long samples = (long)(seconds * Metronome.SampleRate);

            var inputStream = MixerNode.GetAudioFormat(AudioUnitScopeType.Output);

            var outputStream = AudioStreamBasicDescription.CreateLinearPCM(44100, 2);

            AudioConverter converter = AudioConverter.Create(inputStream, outputStream);

			var file = ExtAudioFile.CreateWithUrl(
                new Foundation.NSUrl(fileName, false),
                AudioFileType.WAVE,
                outputStream,
                AudioFileFlags.EraseFlags,
                out ExtAudioFileError e
            );

            long samplesRead = 0;

			// initialize the buffers
			var buffers = new AudioBuffers(2);
			buffers[0] = new AudioBuffer()
			{
				DataByteSize = BufferSize * 4,
				NumberChannels = 1,
                Data = Marshal.AllocHGlobal(sizeof(float) * BufferSize)
			};
			buffers[1] = new AudioBuffer()
			{
				DataByteSize = BufferSize * 4,
				NumberChannels = 1,
                Data = Marshal.AllocHGlobal(sizeof(float) * BufferSize)
			};

			var convBuffers = new AudioBuffers(1);
			convBuffers[0] = new AudioBuffer()
			{
				DataByteSize = BufferSize * 4,
				NumberChannels = 2,
                Data = Marshal.AllocHGlobal(sizeof(float) * BufferSize)
			};

			while (samples > 0)
			{
				int numSamples = (int)(Math.Min(BufferSize, samples));

				// get samples from the mixer
				Render((uint)numSamples, buffers, samplesRead);

                // conver to the file's format
                converter.ConvertComplexBuffer(numSamples, buffers, convBuffers);

                // write samples to the file
                var error = file.Write((uint)numSamples, convBuffers);
                if (error != ExtAudioFileError.OK)
				{
					throw new ApplicationException();
				}
				
				samples -= BufferSize;
                samplesRead += numSamples;
			}

            buffers.Dispose();
            convBuffers.Dispose();
            converter.Dispose();
            file.Dispose();
        }

        /// <summary>
        /// Enables the input.
        /// </summary>
        /// <returns><c>true</c>, if input was enabled, <c>false</c> otherwise.</returns>
        /// <param name="stream">Stream.</param>
        /// <param name="isOn">If set to <c>true</c> is on.</param>
        public bool EnableInput(IStreamProvider stream, bool isOn)
		{
            int inputNum = Streams.IndexOf(stream);

            if (inputNum > -1)
            {
                //if (isOn)
                //{
                //    stream.IsMuted = false;
                //}
                //else
                //{
                //    stream.IsMuted = true;
                //}

                if (MixerNode.SetParameter(AudioUnitParameterType.MultiChannelMixerEnable, Convert.ToSingle(isOn), AudioUnitScopeType.Input, (uint)inputNum) != AudioUnitStatus.OK)
                {
                    throw new ApplicationException();
                }
                return true;
            }
            return false;
		}

        /// <summary>
        /// Sets the input volume.
        /// </summary>
        /// <returns><c>true</c>, if input volume was set, <c>false</c> otherwise.</returns>
        /// <param name="stream">Stream.</param>
        /// <param name="value">Value.</param>
        public bool SetInputVolume(IStreamProvider stream, float value)
		{
            int i = Streams.IndexOf(stream);

            if (i > -1)
            {
                if (MixerNode.SetParameter(AudioUnitParameterType.MultiChannelMixerVolume, value, AudioUnitScopeType.Input, (uint)i) != AudioUnitStatus.OK)
                {
                    throw new ApplicationException();
                }

                return true;
            }
            return false;
		}

        /// <summary>
        /// Sets the output volume.
        /// </summary>
        /// <param name="value">Value.</param>
		public void SetOutputVolume(float value)
		{
            if (Output.SetParameter(AudioUnitParameterType.HALOutputVolume, value, AudioUnitScopeType.Output) != AudioUnitStatus.OK)
				throw new ApplicationException();
		}

        /// <summary>
        /// Sets the pan.
        /// </summary>
        /// <returns><c>true</c>, if pan was set, <c>false</c> otherwise.</returns>
        /// <param name="stream">Stream.</param>
        /// <param name="value">Value.</param>
        public bool SetPan(IStreamProvider stream, float value)
        {
            int index = Streams.IndexOf(stream);

            if (index > -1)
            {
                if (MixerNode.SetParameter(AudioUnitParameterType.MultiChannelMixerPan, value, AudioUnitScopeType.Input, (uint)index) != AudioUnitStatus.OK)
                {
                    throw new ApplicationException();
                }
                return true;
            }
            return false;
        }

        /// <summary>
        /// Gets the index of stream.
        /// </summary>
        /// <returns>The index of stream.</returns>
        /// <param name="stream">Stream.</param>
        public int GetIndexOfStream(IStreamProvider stream)
        {
            return Streams.IndexOf(stream);
        }

        /// <summary>
        /// Releases all resource used by the <see cref="T:Pronome.Mixer"/> object.
        /// </summary>
        /// <remarks>Call <see cref="Dispose"/> when you are finished using the <see cref="T:Pronome.Mixer"/>. The
        /// <see cref="Dispose"/> method leaves the <see cref="T:Pronome.Mixer"/> in an unusable state. After calling
        /// <see cref="Dispose"/>, you must release all references to the <see cref="T:Pronome.Mixer"/> so the garbage
        /// collector can reclaim the memory that the <see cref="T:Pronome.Mixer"/> was occupying.</remarks>
        public void Dispose()
        {
            Graph.Dispose();
            MixerNode.Dispose();
            _converter.Dispose();

            Metronome.Instance.TempoChanged -= TempoChanged;
        }

        /// <summary>
        /// If true, sets a count-off length of 4 bpm, otherwise disables the count-off
        /// </summary>
        /// <param name="enabled">If set to <c>true</c> enabled.</param>
        public void SetCountOff(bool enabled = true)
        {
            if (enabled)
            {
                _countOff.Reset();

                CountOffSampleDuration = (long)Metronome.Instance.ConvertBpmToSamples(4);

                uint countOffPad = (uint)(CountOffSampleDuration > BufferSize
                    ? BufferSize - CountOffSampleDuration % BufferSize
                    : BufferSize - CountOffSampleDuration);

                CountOffSampleDuration += countOffPad;

                _countOffTotal = CountOffSampleDuration;

                // if the countoff pad doesn't align with cycle, we need to set an offset on the
                // count off source
                _countOff.Offset = Metronome.Instance.ConvertSamplesToBpm(countOffPad);

                _countOffTotal = (long)(CountOffSampleDuration + _countOff.Offset);

                var lastStream = Streams[Streams.Count - 1];
                if (lastStream.IsMuted)
                {
                    EnableInput(lastStream, true);
                }
            }
            else
            {
                CountOffSampleDuration = _countOffTotal = 0;

                var lastStream = Streams[Streams.Count - 1];
                if (lastStream.IsMuted)
                {
                    EnableInput(lastStream, false);
                }
            }
        }
        #endregion

        #region Protected Methods
        /// <summary>
        /// Builds the audio graph, initializes the mixer.
        /// </summary>
        protected void BuildAUGraph()
        {
            Graph = new AUGraph();

            // use splitter sub-type to create file writer tap

            // output unit. output to default audio device
            int outputNode = Graph.AddNode(AudioComponentDescription.CreateOutput(AudioTypeOutput.Default));

            // mixer unit
            //int mixerNode = Graph.AddNode(AudioComponentDescription.CreateMixer(AudioTypeMixer.MultiChannel));

            //var mixerDesc = AudioComponentDescription.CreateMixer(AudioTypeMixer.MultiChannel);
            MixerNode = AudioComponent.FindComponent(AudioTypeMixer.MultiChannel).CreateAudioUnit();

            // connect the mixer's output to the output's input
            //if (Graph.ConnnectNodeInput(mixerNode, 0, outputNode, 0) != AUGraphError.OK)
            //{
            //    throw new ApplicationException();
            //}

            // open the graph
            if (Graph.TryOpen() != 0)
            {
                throw new ApplicationException();
            }

            Graph.SetNodeInputCallback(outputNode, 0, OutputRenderDelegate);

            Output = Graph.GetNodeInfo(outputNode);
            //MixerNode = Graph.GetNodeInfo(mixerNode);
            // must set ouput volume because it defaults to 0
            MixerNode.SetParameter(AudioUnitParameterType.MultiChannelMixerVolume, 1, AudioUnitScopeType.Output, 0);
            //MixerNode.SetMaximumFramesPerSlice(4096, AudioUnitScopeType.Global);

            ConfigureMixerInputs();

            AudioStreamBasicDescription desc;

            // set output stream format
            desc = MixerNode.GetAudioFormat(AudioUnitScopeType.Output);
            desc.SampleRate = Metronome.SampleRate;
            if (MixerNode.SetFormat(desc, AudioUnitScopeType.Output) != AudioUnitStatus.OK)
            {
                throw new ApplicationException();
            }

            // now that we;ve set everything up we can initialize the graph, this will aslo validate the connections
            if (Graph.Initialize() != AUGraphError.OK)
            {
                throw new ApplicationException();
            }

            MixerNode.Initialize();
        }

        /// <summary>
        /// Configures the mixer inputs.
        /// </summary>
        protected void ConfigureMixerInputs()
        {
            if (MixerNode != null)
            {
                
                // set the element count
                if (MixerNode.SetElementCount(AudioUnitScopeType.Input, (uint)Streams.Count) != AudioUnitStatus.OK)
                {
                    throw new ApplicationException();
                }

                // add each stream as a mixer input
                for (int i = 0; i < Streams.Count; i++)
                {
                    // set the render callback
                    if (MixerNode.SetRenderCallback(MixerRenderDelegate, AudioUnitScopeType.Global, (uint)i) != AudioUnitStatus.OK)
                    {
                        throw new ApplicationException();
                    }

                    // set the ASBD
                    var format = Streams[i].Format;
                    if (MixerNode.SetFormat(format, AudioUnitScopeType.Input, (uint)i) != AudioUnitStatus.OK)
                    {
                        throw new ApplicationException();
                    }
                }
            }
        }

        /// <summary>
        /// Renders the mixer node. Orchestrates dynamic changes to tempo and beatcode.
        /// </summary>
        /// <returns>The render delegate.</returns>
        /// <param name="actionFlags">Action flags.</param>
        /// <param name="timeStamp">Time stamp.</param>
        /// <param name="busNumber">Bus number.</param>
        /// <param name="numberFrames">Number frames.</param>
        /// <param name="data">Data.</param>
        unsafe AudioUnitStatus MixerRenderDelegate(AudioUnitRenderActionFlags actionFlags, AudioTimeStamp timeStamp, uint busNumber, uint numberFrames, AudioBuffers data)
		{
            if (busNumber >= Streams.Count) 
            {
                // this prevents the buffer from doubling up with unused buses
                return AudioUnitStatus.InvalidElement;
            }

            var outLeft = (float*)data[0].Data;
            var outRight = (float*)data[1].Data;

            // if theres a count-off, we read from the countoff source
            if (CountOffSampleDuration > 0)
            {
                // skip all inputs but the last one so that non-count off cycle starts with bus 0
                if (busNumber != Streams.Count - 1) return AudioUnitStatus.InvalidElement;

                var stream = Streams[(int)busNumber];

                //if (stream.IsMuted)
                //{
                //    EnableInput(stream, true);
                //}

                _countOff.Read(outLeft, outRight, numberFrames);

                CountOffSampleDuration -= numberFrames;

                // set elapsed bpm and cycles to 0
                if (CountOffSampleDuration == 0)
                {
                    Metronome.Instance.ElapsedBpm -= Metronome.Instance.ConvertSamplesToBpm(_countOffTotal);
                    cycle = -1;
                    EnableInput(stream, !stream.IsMuted);
                }

                return AudioUnitStatus.OK;
            }

            IStreamProvider source = Streams[(int)busNumber];

            source.Read(outLeft, outRight, numberFrames);

            return AudioUnitStatus.OK;
		}

        /// <summary>
        /// Render callback for the output node. Can simulataneously write to a file.
        /// </summary>
        /// <returns>The render delegate.</returns>
        /// <param name="actionFlags">Action flags.</param>
        /// <param name="timeStamp">Time stamp.</param>
        /// <param name="busNumber">Bus number.</param>
        /// <param name="numberFrames">Number frames.</param>
        /// <param name="data">Data.</param>
        AudioUnitStatus OutputRenderDelegate(AudioUnitRenderActionFlags actionFlags, AudioTimeStamp timeStamp, uint busNumber, uint numberFrames, AudioBuffers data)
        {
			// propagate tempo change on start of a new cycle
			if (_tempoChanged)
			{
				PropagateTempoChange(_tempoChangeRatio);

				_tempoChanged = false;
			}

            var e = MixerNode.Render(ref actionFlags, timeStamp, busNumber, numberFrames, data);

            cycle++;

			// check for a queued layer change
			if (Metronome.Instance.NeedToChangeLayer == true)
			{
				Metronome.Instance.CycleToChange = cycle;
				Metronome.Instance.NeedToChangeLayer = false;
				Metronome.Instance.ChangeLayerTurnstyle.Set();
			}
			else if (Metronome.Instance.NeedToChangeLayer == null)
			{
				// top off the fat forward
				double cycleDiff = cycle - Metronome.Instance.CycleToChange;

				Metronome.Instance.FastForwardChangedLayers(cycleDiff);

				foreach (KeyValuePair<int, Layer> pair in Metronome.Instance.LayersToChange)
				{
					Layer copy = pair.Value;
					Layer real = Metronome.Instance.Layers[pair.Key];

					int numberRemoved = 0;
                    bool isMuted = false;
					// remove old sources
					foreach (IStreamProvider src in real.GetAllStreams())
					{
                        RemoveStream(src);
						src.Dispose();
						numberRemoved++;
                        isMuted = src.IsMuted;
					}

					// transfer sources to real layer
					real.AudioSources = copy.AudioSources;
					real.BaseAudioSource = copy.BaseAudioSource;
					real.PitchSource = copy.PitchSource;
					real.BaseSourceName = copy.BaseSourceName;
                    real.HasHiHatOpen = copy.HasHiHatOpen;
                    real.HasHiHatClosed = copy.HasHiHatClosed;
					real.Beat = copy.Beat;

					foreach (IStreamProvider src in real.GetAllStreams().OrderBy(x => x.Info.HiHatStatus != StreamInfoProvider.HiHatStatuses.Down))
					{
                        src.IsMuted = isMuted;
						src.Layer = real;
						if (numberRemoved <= 0)
						{
							// it crashes if we try to add a rendercallback for preexisting bus
							Metronome.Instance.AddAudioSource(src);
						}
						else
						{
							Streams.Add(src);

                        }

						numberRemoved--;
					}

					copy.AudioSources = null;
					copy.BaseAudioSource = null;
					copy.PitchSource = null;
					copy.Beat = null;
					Metronome.Instance.Layers.Remove(copy);

                    foreach (IStreamProvider src in Streams)
                    {
						// keep muting consistent when shuffling buffer indexs
						if (src.IsMuted)
						{
							EnableInput(src, false);
						}
						else
						{
							EnableInput(src, true);
						}

                        SetPan(src, src.Pan);
                        SetInputVolume(src, (float)src.Volume);
                    }
				}

				Metronome.Instance.LayersToChange.Clear();
				Metronome.Instance.NeedToChangeLayer = false;


				// trigger beat changed event 
				AppKit.NSApplication.SharedApplication.BeginInvokeOnMainThread(
                    () => { Metronome.Instance.OnBeatChanged(null); });
			}

            // check if recording to file
            if (_fileRecordingQueued)
            {
                // convert the buffer
                using (AudioBuffers convBuffer = new AudioBuffers(1))
                {
                    convBuffer[0] = new AudioBuffer()
                    {
                        DataByteSize = data[0].DataByteSize,
                        NumberChannels = 1,
                        Data = Marshal.AllocHGlobal(sizeof(float) * data[0].DataByteSize)
                    };

                    _converter.ConvertComplexBuffer((int)numberFrames, data, convBuffer);

                    _file.Write(numberFrames, convBuffer);
                }
            }

            return AudioUnitStatus.OK;
        }

        object _tempoLock = new object();

        /// <summary>
        /// Handle the tempo change event
        /// </summary>
        /// <param name="sender">Sender.</param>
        /// <param name="e">E.</param>
        public void TempoChanged(object sender, Metronome.TempoChangedEventArgs e)
		{
            lock (_tempoLock)
            {
                if (Metronome.Instance.PlayState != Metronome.PlayStates.Stopped)
                {
                    // queue tempo change to happend on the start of next render cycle
					_tempoChanged = true;
                    _tempoChangeRatio = e.ChangeRatio;
                }
            }
		}

        /// <summary>
        /// Propagates the tempo change.
        /// </summary>
        /// <param name="ratio">Ratio.</param>
        void PropagateTempoChange(double ratio)
        {
            foreach (AbstractStream stream in Streams)
            {
                stream.PropagateTempoChange(ratio);
                stream.SampleRemainder *= ratio;
            }

            cycle *= ratio;
        }
        #endregion
    }
}

