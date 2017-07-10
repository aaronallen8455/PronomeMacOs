using System;
using Foundation;
using System.Collections.Generic;
using System.Threading;

namespace Pronome
{
    public class Metronome : NSObject
    {
        public const int SampleRate = 44100;

        #region Private variables
        private nfloat _volume = 1f;
        private nfloat _tempo = 120f;
        private bool _isPlaying; // used to enable/disable UI elements

        protected Mixer Mixer = new Mixer();
        #endregion

        #region Public variables
        public List<Layer> Layers = new List<Layer>();

        public enum PlayStates { Playing, Paused, Stopped };

        public PlayStates PlayState = PlayStates.Stopped;

        /// <summary>
        /// Is the random mute engaged?
        /// </summary>
        public bool IsRandomMuteEngaged = false;

        /// <summary>
        /// The randomness factor from 0 to 1.
        /// </summary>
        public float RandomnessFactor;

        /// <summary>
        /// Is the silent interval engaged?
        /// </summary>
        public bool IsSilentIntervalEngaged = false;

        /// <summary>
        /// The audible interval in BPM.
        /// </summary>
        public double AudibleInterval;

        /// <summary>
        /// The silent interval in BPM.
        /// </summary>
        public double SilentInterval;
        #endregion

        #region Computed Properties
        /// <summary>
        /// Gets or sets the master volume.
        /// </summary>
        /// <value>The volume. 0 to 1</value>
        [Export("Volume")]
        public nfloat Volume
        {
            get => _volume;
            set
            {
                WillChangeValue("Volume");
                _volume = value;
                // set the output volume of the mixer
                Mixer.SetOutputVolume((float)value);
                DidChangeValue("Volume");
            }
        }

        /// <summary>
        /// Gets or sets the tempo.
        /// </summary>
        /// <value>The tempo in BPM.</value>
        [Export("Tempo")]
        public nfloat Tempo
        {
            get => _tempo;
            set
            {
                WillChangeValue("Tempo");
                _tempo = value;
                DidChangeValue("Tempo");
            }
        }

        [Export("IsPlaying")]
        public bool IsPlaying
        {
            get => _isPlaying;
            set
            {
                WillChangeValue("IsPlaying");
                _isPlaying = value;
                DidChangeValue("IsPlaying");
            }
        }
        #endregion

        #region Static Properties
        static private Metronome _instance;
        /// <summary>
        /// Get the singleton instance.
        /// </summary>
        static public Metronome Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new Metronome();
                }
                return _instance;
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Converts a BPM value to bytes based on current tempo. As a double to preserve any left overs.
        /// </summary>
        /// <returns>The to bytes.</returns>
        /// <param name="bpm">Bpm.</param>
        public double ConvertBpmToSamples(double bpm)
        {
            double result = 60 / Tempo * bpm * SampleRate;

            if (result > long.MaxValue) throw new Exception(bpm.ToString());

            return result;
        }

        public bool Play()
        {
            if (PlayState != PlayStates.Playing)
            {
				Mixer.Start();

                PlayState = PlayStates.Playing;

                IsPlaying = true;

                return true;
            }
            return false;
        }

        public bool Stop()
        {
            if (PlayState != PlayStates.Stopped)
            {
				Mixer.Stop();
                // reset the streams to starting position

                PlayState = PlayStates.Stopped;

                IsPlaying = false;

                return true;
            }
            return false;
        }

        public bool Pause()
        {
            if (PlayState == PlayStates.Playing)
            {
				Mixer.Stop();

                PlayState = PlayStates.Paused;

                return true;
            }
            return false;
        }

        /// <summary>
        /// Sets the pan of a mixer input.
        /// </summary>
        /// <param name="stream">Stream.</param>
        /// <param name="value">Value.</param>
        public void SetPanOfMixerInput(IStreamProvider stream, float value)
        {
            Mixer.SetPan(stream, value);
        }

        /// <summary>
        /// Sets the volume of a mixer input.
        /// </summary>
        /// <param name="stream">Stream.</param>
        /// <param name="value">Value.</param>
        public void SetVolumeOfMixerInput(IStreamProvider stream, float value)
        {
            Mixer.SetInputVolume(stream, value);
        }

        /// <summary>
        /// Adds a layer.
        /// </summary>
        /// <param name="layer">Layer.</param>
        public void AddLayer(Layer layer)
        {
            Layers.Add(layer);
        }

        /// <summary>
        /// Removes the audio source.
        /// </summary>
        /// <param name="stream">Stream.</param>
        public void RemoveAudioSource(IStreamProvider stream)
        {
            Mixer.RemoveStream(stream);
        }

        /// <summary>
        /// Adds the audio source.
        /// </summary>
        /// <param name="stream">Stream.</param>
        public void AddAudioSource(IStreamProvider stream)
        {
            Mixer.AddStream(stream);
        }

		/** <summary>Add all the audio sources from each layer.</summary>
         * <param name="layer">Layer to add sources from.</param> */
		public void AddSourcesFromLayer(Layer layer)
		{
			// add sources to mixer
			foreach (IStreamProvider src in layer.AudioSources.Values)
			{
				AddAudioSource(src);
			}

			AddAudioSource(layer.BaseAudioSource);
			AddAudioSource(layer.PitchSource);

			// transfer silent interval if exists
            //if (IsSilentIntervalEngaged)
			//{
			//	foreach (IStreamProvider src in layer.AudioSources.Values)
			//	{
            //        
			//		src.SetSilentInterval(AudibleInterval, SilentInterval);
			//	}
			//
			//	layer.BaseAudioSource.SetSilentInterval(AudibleInterval, SilentInterval);
			//
			//	if (layer.BasePitchSource != default(PitchStream) && !layer.IsPitch)
			//		layer.BasePitchSource.SetSilentInterval(AudibleInterval, SilentInterval);
			//}
		}

        protected override void Dispose(bool disposing)
        {
            // Dispose the mixer
            Mixer.Dispose();

            base.Dispose(disposing);
        }
		#endregion

		#region Public Static Methods
		/** <summary>Used for random muting.</summary> */
		protected static ThreadLocal<Random> Rand;
		/**<summary>Get random number btwn 0 and 99.</summary>*/
		public static int GetRandomNum()
		{
			if (Rand == null)
			{
				Rand = new ThreadLocal<Random>(() => new Random());
			}

			int r = Rand.Value.Next(0, 99);
			return r;
		}
        #endregion

        /// <summary>
        /// private constructor, singleton class.
        /// </summary>
        private Metronome()
        {
        }
    }
}
