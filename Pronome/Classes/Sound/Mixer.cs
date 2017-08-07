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
        #endregion

        #region Public Variables
        public bool IsPlaying = false;
        #endregion

        #region Constructors
        public Mixer()
        {
			BuildAUGraph();

            Metronome.Instance.TempoChanged += TempoChanged;
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
                if (MixerNode.SetRenderCallback(HandleRenderDelegate, AudioUnitScopeType.Global, i) != AudioUnitStatus.OK)
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

            Streams.Remove(stream);

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

				IsPlaying = false;
                _tempoChanged = false;
                cycle = 0;
			}
		}

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

			while (samples > 0)
			{
				int numSamples = (int)(Math.Min(BufferSize, samples));
				// initialize the buffers
				var leftBuffer = new AudioBuffer();
				leftBuffer.DataByteSize = BufferSize * 4;
				leftBuffer.NumberChannels = 1;
				leftBuffer.Data = Marshal.AllocHGlobal(sizeof(float) * numSamples);
				var rightBuffer = new AudioBuffer();
				rightBuffer.DataByteSize = BufferSize * 4;
				rightBuffer.NumberChannels = 1;
				rightBuffer.Data = Marshal.AllocHGlobal(sizeof(float) * numSamples);
				var buffers = new AudioBuffers(2);
				buffers[0] = leftBuffer;
				buffers[1] = rightBuffer;
				
				// get samples from the mixer
				
				Render((uint)numSamples, buffers, samplesRead);

                // conver to the file's format
                var convBuffers = new AudioBuffers(1);
                convBuffers[0] = new AudioBuffer()
                {
                    DataByteSize = BufferSize * 4,
                    NumberChannels = 2,
                    Data = Marshal.AllocHGlobal(sizeof(float) * numSamples)
                };

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
            if (MixerNode.SetParameter(AudioUnitParameterType.MultiChannelMixerVolume, value, AudioUnitScopeType.Output) != AudioUnitStatus.OK)
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

            Metronome.Instance.TempoChanged -= TempoChanged;
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
            int mixerNode = Graph.AddNode(AudioComponentDescription.CreateMixer(AudioTypeMixer.MultiChannel));

            // connect the mixer's output to the output's input
            if (Graph.ConnnectNodeInput(mixerNode, 0, outputNode, 0) != AUGraphError.OK)
            {
                throw new ApplicationException();
            }

            // open the graph
            if (Graph.TryOpen() != 0)
            {
                throw new ApplicationException();
            }

            MixerNode = Graph.GetNodeInfo(mixerNode);
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
                    if (MixerNode.SetRenderCallback(HandleRenderDelegate, AudioUnitScopeType.Global, (uint)i) != AudioUnitStatus.OK)
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

        unsafe AudioUnitStatus HandleRenderDelegate(AudioUnitRenderActionFlags actionFlags, AudioTimeStamp timeStamp, uint busNumber, uint numberFrames, AudioBuffers data)
		{
            if (busNumber >= Streams.Count) 
            {
                // this prevents the buffer from doubling up with unused buses
                return AudioUnitStatus.InvalidElement;
            }

            if (busNumber == 0)
            {
				// propagate tempo change on start of a new cycle
				if (_tempoChanged)
				{
					PropagateTempoChange(_tempoChangeRatio);
					
					_tempoChanged = false;
				}

                //cycle++;
            }


            IStreamProvider source = Streams[(int)busNumber];

            var outLeft = (float*)data[0].Data;
            var outRight = (float*)data[1].Data;

            source.Read(outLeft, outRight, numberFrames);

            // make changes last because new streams won't be read until a new cycle
            if (busNumber == Streams.Count - 1)
            {
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
						// remove old sources
						foreach (IStreamProvider src in real.GetAllStreams())
						{
							Metronome.Instance.RemoveAudioSource(src);
							src.Dispose();
							numberRemoved++;
						}

						// transfer sources to real layer
						real.AudioSources = copy.AudioSources;
						real.BaseAudioSource = copy.BaseAudioSource;
						real.PitchSource = copy.PitchSource;
						real.BaseSourceName = copy.BaseSourceName;
						real.Beat = copy.Beat;

						foreach (IStreamProvider src in real.GetAllStreams().OrderBy(x => x.Info.HiHatStatus != StreamInfoProvider.HiHatStatuses.Down))
						{
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
					}

					Metronome.Instance.LayersToChange.Clear();
					Metronome.Instance.NeedToChangeLayer = false;
				}
            }

            return AudioUnitStatus.OK;
		}

        object _tempoLock = new object();

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
            }

            // apply to layer's remainder
            foreach (Layer layer in Metronome.Instance.Layers)
            {
                layer.SampleRemainder *= ratio;
            }

            cycle *= ratio;
        }
        #endregion
    }
}

