using System;
using Foundation;
using System.Collections.Generic;
using System.Threading;
using System.Linq;
using CoreVideo;
using Pronome.Mac.Visualizer;

namespace Pronome.Mac
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
        private CVDisplayLink Animator;

        /// <summary>
        /// Used to determine the total elapsed BPMs
        /// </summary>
        private AnimationTimer _animationTimer;
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

        private double _elapsedBpm;
        /// <summary>
        /// Gets the elapsed bpm since playback started.
        /// </summary>
        /// <value>The elapsed bpm.</value>
        public double ElapsedBpm
        {
            get
            {
                _elapsedBpm += _animationTimer.GetElapsedBpm();
                return _elapsedBpm;
            }

            protected set 
            {
                _animationTimer.Reset();
                //_animationTimer.GetElapsedTime();
                _elapsedBpm = value;
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

                    _instance.Animator = new CVDisplayLink();
                    _instance.Animator.SetOutputCallback(AnimationHelper.RequestDraw);
                    _instance._animationTimer = new AnimationTimer();
				}
                return _instance;
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Converts a BPM value to samples based on current tempo. As a double to preserve any left overs.
        /// </summary>
        /// <returns>The to bytes.</returns>
        /// <param name="bpm">Bpm.</param>
        public double ConvertBpmToSamples(double bpm)
        {
            double result = 60 / Tempo * bpm * SampleRate;

            if (result > long.MaxValue) throw new Exception(bpm.ToString());

            return result;
        }

        public double ConvertSamplesToBpm(double samples)
        {
            double seconds = samples / SampleRate;
            return seconds * (Tempo / 60);
        }

        /// <summary>
        /// Start playback.
        /// </summary>
        public bool Play()
        {
            if (PlayState != PlayStates.Playing)
            {
                Mixer.Start();

                var prevState = PlayState;

                PlayState = PlayStates.Playing;

                IsPlaying = true;

                OnStarted(new StartedEventArgs(prevState));

				Animator.Start();

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

                Animator.Stop();

                OnStopped(EventArgs.Empty);

				ElapsedBpm = 0;

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

                Animator.Stop();

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

            OnLayerAdded(new EventArgs());
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

            OnLayerRemoved(new EventArgs());

            // if no layers, clear the currently opened file field
            SavedFileManager.CurrentlyOpenFile = null;

			OnBeatChanged(new EventArgs());
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

            // triggers the beat changed event if stopped
            if (PlayState == PlayStates.Stopped)
            {
				OnBeatChanged(null);
            }
		}

        public void ExecuteLayerChange(Layer layer)
        {
            Layer copyLayer = new Layer(
                "1",
                layer.BaseStreamInfo,
                "",
                (float)layer.Pan,
                (float)layer.Volume
            );

            copyLayer.OffsetBpm = layer.OffsetBpm;

            LayersToChange.Add(Layers.IndexOf(layer), copyLayer);

            copyLayer.ProcessBeat(layer.ParsedString);

            // transfer muting
            //foreach (IStreamProvider src in copyLayer.GetAllStreams())

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

                double x = l.GetTotalBpmValue();
                // don't run extraneous samples
                double bytesToRun = totalFloats % ConvertBpmToSamples(l.GetTotalBpmValue());

                foreach (IStreamProvider src in l.GetAllStreams())
                {
                    // Need to deal with interval muting when compressing
                    //long floats;
					//
                    //if (totalFloats > src.InitialOffset)
                    //{
                    //    // compress the number of samples to run
                    //    floats = (long)(bytesToRun + src.InitialOffset);
					//
                    //    l.SampleRemainder += bytesToRun + src.InitialOffset - floats;
                    //}
                    //else
                    //{
                    //    floats = totalFloats;
                    //}

					long floats = totalFloats;

                    long interval = (long)src.InitialOffset + 1;
                    if (interval < totalFloats)
                    {
                        // turn off byte production to increase efficiency
                        src.ProduceBytes = false;
                    
                    	while (interval <= floats)
                    	{
                            src.Read(null, null, (uint)interval, false);
                            floats -= (uint)interval;
                    		interval = src.IntervalLoop.Enumerator.Current;
                    	}
                    
                        src.ProduceBytes = true;
                    }
                    // do produce bytes for the last interval
                    //src.ProduceBytes = false;
                    while (floats > 0)
                    {
                        uint intsToCopy = (uint)Math.Min(uint.MaxValue, floats);

                        src.Read(null, null, intsToCopy, false);

                        floats -= uint.MaxValue;
                    }
                    //src.ProduceBytes = true;
                }
            }
        }

        /// <summary>
        /// Gets the number of quarter notes for a complete beat cycle.
        /// </summary>
        /// <returns>The quarters for complete cycle.</returns>
		public double GetQuartersForCompleteCycle()
		{
			Func<double, double, double> Gcf = null;
			Gcf = delegate (double x, double y)
			{
				double r = x % y;
				if (Math.Round(r, 5) == 0) return y;

				return Gcf(y, r);
			};

			Func<double, double, double> Lcm = delegate (double x, double y)
			{
				return x * y / Gcf(x, y);
			};

            if (Layers.Count == 0) return 0;

			return Layers.Select(x => x.GetTotalBpmValue()).Aggregate((a, b) => Lcm(a, b));
		}

        public void Cleanup()
        {
            // null the events
            TempoChanged = null;
            Started = null;
            Stopped = null;
            Paused = null;

            foreach (Layer layer in Layers)
            {
                layer.Cleanup();
            }

			// Dispose the mixer
			Mixer.Dispose();
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
            // update the elapsed BPM
            ElapsedBpm *= e.ChangeRatio;

            TempoChanged?.Invoke(this, e);
        }

        public class StartedEventArgs : EventArgs
        {
            public PlayStates PreviousState;

            public StartedEventArgs(PlayStates prevState)
            {
                PreviousState = prevState;
            }
        }

        /// <summary>
        /// Occurs when playback started.
        /// </summary>
        public event EventHandler<StartedEventArgs> Started;

        protected virtual void OnStarted(StartedEventArgs e)
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

        /// <summary>
        /// Occurs when layer added.
        /// </summary>
		public event EventHandler LayerAdded;

		protected virtual void OnLayerAdded(EventArgs e)
		{
			LayerAdded?.Invoke(this, e);
		}

        /// <summary>
        /// Occurs when layer removed.
        /// </summary>
		public event EventHandler LayerRemoved;

		protected virtual void OnLayerRemoved(EventArgs e)
		{
			LayerRemoved?.Invoke(this, e);
		}

        public event EventHandler BeatChanged;

        public virtual void OnBeatChanged(EventArgs e)
        {
            BeatChanged?.Invoke(this, e);
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
