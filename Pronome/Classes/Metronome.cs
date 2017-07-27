using System;
using Foundation;
using System.Collections.Generic;
using System.Threading;
using System.Linq;

namespace Pronome
{
    public class Metronome : NSObject
    {
        public const int SampleRate = 44100;

        #region Private variables
        private nfloat _volume = 1f;
        private nfloat _tempo = 120f;
        private bool _isPlaying; // used to enable/disable UI elements
        private bool _isSilentIntervalEngaged = false;
        private string _parsedAudibleInterval;
        private string _parsedSilentInterval;
        private bool _isRandomMuteEngaged;
        private float _randomnessFactor;
        private float _randomMuteCountdown;
        #endregion

        #region Public variables
        public List<Layer> Layers = new List<Layer>();

        public enum PlayStates { Playing, Paused, Stopped };

        public PlayStates PlayState = PlayStates.Stopped;

		public Mixer Mixer;

        /// <summary>
        /// The audible interval in BPM.
        /// </summary>
        public double AudibleIntervalBpm;

        /// <summary>
        /// The silent interval in BPM.
        /// </summary>
        public double SilentIntervalBpm;

        /// <summary>
        /// Used to pass the number of the cycle to fast forward a changing layer to
        /// </summary>
        public double CycleToChange;
        /// <summary>
        /// True if a layer change has been requested. Null if layers are ready to be re-added.
        /// </summary>
        public bool? NeedToChangeLayer = false;
        /// <summary>
        /// The layers to change keyed by index.
        /// </summary>
        public Dictionary<int, Layer> LayersToChange = new Dictionary<int, Layer>();
        /// <summary>
        /// The change layer turnstyle.
        /// </summary>
        public AutoResetEvent ChangeLayerTurnstyle = new AutoResetEvent(false);
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
                // send the change ratio in event arg
                if (value > 0)
                {
                    float oldTempo = (float)_tempo;
                    _tempo = value;
                    OnTempoChanged(new TempoChangedEventArgs(oldTempo, (float)_tempo));
                }
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

		/// <summary>
		/// Is the silent interval engaged?
		/// </summary>
		[Export("IsSilentIntervalEnabled")]
        public bool IsSilentIntervalEngaged
        {
            get => _isSilentIntervalEngaged;
            set
            {
                WillChangeValue("IsSilentIntervalEnabled");
                _isSilentIntervalEngaged = value;
                DidChangeValue("IsSilentIntervalEnabled");
            }
        }

        [Export("AudibleInterval")]
        public string AudibleInterval
        {
            get => _parsedAudibleInterval;
            set
            {
                WillChangeValue("AudibleInterval");
                if (BeatCell.TryParse(value, out double bpm))
                {
                    if (bpm >= 0)
                    {
						_parsedAudibleInterval = value;
                        AudibleIntervalBpm = bpm;
                    }
                }
                DidChangeValue("AudibleInterval");
            }
        }

        [Export("SilentInterval")]
        public string SilentInterval
        {
            get => _parsedSilentInterval;
            set
            {
                WillChangeValue("SilentInterval");
                if (BeatCell.TryParse(value, out double bpm))
                {
                    _parsedSilentInterval = value;
                    SilentIntervalBpm = bpm;
                }
                DidChangeValue("SilentInterval");
            }
        }

		/// <summary>
		/// Is the random mute engaged?
		/// </summary>
        [Export("IsRandomMuteEnabled")]
        public bool IsRandomMuteEngaged
        {
            get => _isRandomMuteEngaged;
            set
            {
                WillChangeValue("IsRandomMuteEnabled");
                _isRandomMuteEngaged = value;
                DidChangeValue("IsRandomMuteEnabled");
            }
        }

		/// <summary>
		/// The randomness factor from 0 to 1.
		/// </summary>
        [Export("RandomnessFactor")]
		public nfloat RandomnessFactor
        {
            get => _randomnessFactor;
            set
            {
                WillChangeValue("RandomnessFactor");
                if (value >= 0 && value <= 100)
                {
					_randomnessFactor = (float)value;
                }
                DidChangeValue("RandomnessFactor");
            }
        }

		/// <summary>
		/// The number of seconds until full randomness is reached.
		/// </summary>
        [Export("RandomMuteCountdown")]
		public nfloat RandomMuteCountdown
        {
            get => _randomMuteCountdown;
            set
            {
                WillChangeValue("RandomMuteCountdown");
                if (value >= 0)
                {
					_randomMuteCountdown = (float)value;
                }
                DidChangeValue("RandomMuteCountdown");
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

					_instance.Mixer = new Mixer();
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

        /// <summary>
        /// Start playback.
        /// </summary>
        public bool Play()
        {
            if (PlayState != PlayStates.Playing)
            {
				Mixer.Start();

                PlayState = PlayStates.Playing;

                IsPlaying = true;

                OnStarted(EventArgs.Empty);

                return true;
            }
            return false;
        }

        /// <summary>
        /// Stop playback.
        /// </summary>
        public bool Stop()
        {
            if (PlayState != PlayStates.Stopped)
            {
				Mixer.Stop();
                // reset the streams to starting position

                PlayState = PlayStates.Stopped;

                IsPlaying = false;

                OnStopped(EventArgs.Empty);

                return true;
            }
            return false;
        }

        /// <summary>
        /// Pause playback.
        /// </summary>
        public bool Pause()
        {
            if (PlayState == PlayStates.Playing)
            {
				Mixer.Stop();

                PlayState = PlayStates.Paused;

                OnPaused(EventArgs.Empty);

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
        /// Sets the muting of a mixer input.
        /// </summary>
        /// <param name="stream">Stream.</param>
        /// <param name="isOn">If set to <c>true</c> is on.</param>
        public void SetMutingOfMixerInput(IStreamProvider stream, bool isOn)
        {
            Mixer.EnableInput(stream, isOn);
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
        /// Removes the layer, removes all it's streams from the mixer and cleans up.
        /// </summary>
        /// <param name="layer">Layer.</param>
        public void RemoveLayer(Layer layer)
        {
            foreach (IStreamProvider src in layer.GetAllStreams())
            {
                RemoveAudioSource(src);
            }

            Layers.Remove(layer);

            layer.Cleanup();
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
			// add sources to mixer, put hihat down sounds in first
            foreach (IStreamProvider src in layer.GetAllStreams().OrderBy(x => x.Info.HiHatStatus != StreamInfoProvider.HiHatStatuses.Down))
			{
				AddAudioSource(src);
			}
		}

        public void ExecuteLayerChange(Layer layer)
        {
            Layer copyLayer = new Layer(
                "1",
                layer.BaseStreamInfo,
                layer.ParsedOffset,
                (float)layer.Pan,
                (float)layer.Volume
            );

            LayersToChange.Add(Layers.IndexOf(layer), copyLayer);

            copyLayer.ProcessBeat(layer.ParsedString);

            var t = new Thread(() =>
            {
                NeedToChangeLayer = true;
                // wait until the cycle number is set
                ChangeLayerTurnstyle.WaitOne();

                FastForwardChangedLayers(CycleToChange);

                // signal the audio callback to finish the process
                NeedToChangeLayer = null;
            });

            t.Start();
        }

        public unsafe void FastForwardChangedLayers(double cycles)
        {
            int floatsPerCycle = Mixer.BufferSize;
            long totalFloats = (long)(cycles * floatsPerCycle);

            foreach (KeyValuePair<int, Layer> pair in LayersToChange)
            {
                Layer l = pair.Value;
                foreach (IStreamProvider src in l.GetAllStreams())
                {
                    long floats = totalFloats;

                    while (floats > 0)
                    {
                        uint intsToCopy = (uint)Math.Min(uint.MaxValue, floats);

                        src.Read(null, null, intsToCopy, false);

                        floats -= uint.MaxValue;
                    }
                }
            }
        }

        public void Cleanup()
        {
			// Dispose the mixer
			Mixer.Dispose();
			// null the events
			TempoChanged = null;
			Started = null;
			Stopped = null;
			Paused = null;
        }
        #endregion

        #region Events
        /// <summary>
        /// Tempo changed event arguments.
        /// </summary>
        public class TempoChangedEventArgs : EventArgs
        {
            public double ChangeRatio;

            public TempoChangedEventArgs(float oldTempo, float newTempo)
            {
                // find the tempo change ratio
                ChangeRatio = oldTempo / newTempo;
            }
        }

        /// <summary>
        /// Occurs when tempo changed.
        /// </summary>
        public event EventHandler<TempoChangedEventArgs> TempoChanged;

        protected virtual void OnTempoChanged(TempoChangedEventArgs e)
        {
            TempoChanged?.Invoke(this, e);
        }

        /// <summary>
        /// Occurs when playback started.
        /// </summary>
        public event EventHandler Started;

        protected virtual void OnStarted(EventArgs e)
        {
            Started?.Invoke(this, e);
        }

        /// <summary>
        /// Occurs when playback paused.
        /// </summary>
        public event EventHandler Paused;

        protected virtual void OnPaused(EventArgs e)
        {
            Paused?.Invoke(this, e);
        }

        /// <summary>
        /// Occurs when playback stopped.
        /// </summary>
        public event EventHandler Stopped;

        protected virtual void OnStopped(EventArgs e)
        {
            Stopped?.Invoke(this, e);
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
