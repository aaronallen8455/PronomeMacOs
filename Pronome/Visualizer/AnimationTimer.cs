using System.Diagnostics;

namespace Pronome.Mac.Visualizer
{
	public class AnimationTimer
	{
		static protected Stopwatch _stopwatch;

        static AnimationTimer()
        {
            Metronome.Instance.Started += (sender, e) => Start();
            Metronome.Instance.Stopped += (sender, e) => Stop();
            Metronome.Instance.Paused += (sender, e) => Pause();
        }

		public static void Init()
		{
			_stopwatch = Stopwatch.StartNew();
		}

        public static void Pause()
        {
            _stopwatch?.Stop();
        }

		public static void Stop()
		{
			_stopwatch?.Reset();
		}

		public static void Start()
		{
			_stopwatch?.Start();
		}

		protected double lastTime;

		public AnimationTimer()
		{
			if (_stopwatch == null)
			{
				_stopwatch = new Stopwatch();
			}

			//if (_stopwatch.IsRunning)
			//{
			//	lastTime = _stopwatch.ElapsedMilliseconds;
			//}
			//else
			//{
			//	lastTime = 0;
			//}
		}

        /// <summary>
        /// Gets the elapsed time in seconds since the last time this was called.
        /// </summary>
        /// <returns>The elapsed time.</returns>
		public double GetElapsedTime()
		{
			double curTime = _stopwatch.ElapsedMilliseconds;

			double result = curTime - lastTime;

			lastTime = curTime;

			return result / 1000;
		}

        public double GetElapsedBpm()
        {
            return GetElapsedTime() * (Metronome.Instance.Tempo / 60);
        }

		public void Reset()
		{
			lastTime = _stopwatch.ElapsedMilliseconds;
		}
	}
}
