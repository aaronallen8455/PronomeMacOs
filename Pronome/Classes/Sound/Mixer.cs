using System;
using AudioToolbox;
using AudioUnit;
using System.Collections.Generic;

namespace Pronome.Classes.Sound
{
    public class Mixer
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

        /// <summary>
        /// The audio unit which mixes the streams.
        /// </summary>
        protected AudioUnit.AudioUnit MixerNode;
        #endregion

        #region Constructors
        public Mixer()
        {
        }
        #endregion

        #region Public Methods
        public void AddStream(IStreamProvider stream)
        {
            Streams.Add(stream);
        }
        #endregion

        #region Protected Methods
        protected void BuildAUGraph()
        {
            Graph = new AUGraph();

            // output unit
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

            // set bus count
            uint numBuses = (uint)Streams.Count;

            if (MixerNode.SetElementCount(AudioUnitScopeType.Input, numBuses) != AudioUnitStatus.OK)
            {
                throw new ApplicationException();
            }

            AudioStreamBasicDescription desc;

            for (uint i = 0; i < numBuses; ++i)
            {
                // setup render callback
                if (Graph.SetNodeInputCallback(mixerNode, i, HandleRenderDelegate) != AUGraphError.OK)
                {
                    throw new ApplicationException();
                }

                // set input stream format
                desc = Streams[(int)i].Format;
                //desc = MixerNode.GetAudioFormat(AudioUnitScopeType.Input, i);
                //desc.SampleRate = Metronome.SampleRate;

                if (MixerNode.SetFormat(desc, AudioUnitScopeType.Input, i) != AudioUnitStatus.OK)
                {
                    throw new ApplicationException();
                }
            }

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

        unsafe AudioUnitStatus HandleRenderDelegate(AudioUnitRenderActionFlags actionFlags, AudioTimeStamp timeStamp, uint busNumber, uint numberFrames, AudioBuffers data)
		{
            IStreamProvider source = Streams[(int)busNumber];

            var outLeft = (short*)data[0].Data;
            var outRight = (short*)data[1].Data;

            if (source.Info.IsPitch)
            {
                PitchStream pitchStream = source as PitchStream;

                pitchStream.Read(outLeft, outRight, numberFrames);
            }

            return AudioUnitStatus.OK;
		}
		#endregion
	}
}

