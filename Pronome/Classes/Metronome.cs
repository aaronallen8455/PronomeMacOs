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
        #endregion

        #region Public variables
        public List<Layer> Layers = new List<Layer>();
        #endregion

        #region Computed Properties
        /// <summary>
        /// Gets or sets the volume.
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
