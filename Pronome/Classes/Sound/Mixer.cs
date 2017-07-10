using System;
using AudioToolbox;
using AudioUnit;
using System.Collections.Generic;

namespace Pronome
{
    public class Mixer : IDisposable
    {
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
        #endregion

        #region Public Variables
        public bool IsPlaying = false;
        #endregion

        #region Constructors
        public Mixer()
        {
            BuildAUGraph();
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
			}
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
                if (MixerNode.SetParameter(AudioUnitParameterType.MultiChannelMixerPan, value, AudioUnitScopeType.Output, (uint)index) != AudioUnitStatus.OK)
                {
                    throw new ApplicationException();
                }
                return true;
            }
            return false;
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
        }
        #endregion

        #region Protected Methods
        /// <summary>
        /// Builds the audio graph, initializes the mixer.
        /// </summary>
        protected void BuildAUGraph()
        {
            Graph = new AUGraph();

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
            IStreamProvider source = Streams[(int)busNumber];

            var outLeft = (float*)data[0].Data;
            var outRight = (float*)data[1].Data;

            if (source.Info.IsPitch)
            {
                PitchStream pitchStream = source as PitchStream;

                pitchStream.Read(outLeft, outRight, numberFrames);
            }

            var x = outLeft[0];

            return AudioUnitStatus.OK;
		}
        #endregion
    }
}

