using System;
using CoreGraphics;

namespace Pronome.Mac.Visualizer
{
	public class ColorHelper
	{
		/// <summary>
		/// A random number to offset the color wheel by.
		/// </summary>
		static int RgbSeed;

		static bool seedSet = false;

		/// <summary>
		/// Gets a color from color wheel based on index and rgbSeed.
		/// </summary>
		/// <param name="index">Index of the layer</param>
		/// <param name="saturation">Saturation value</param>
		/// <returns></returns>
        public static CGColor ColorWheel(int index, float saturation = 1f)
		{
			if (!seedSet)
			{
                RgbSeed = Metronome.GetRandomNum();
				seedSet = true;
			}

			int degrees = ((25 * index) + (int)(360 * RgbSeed / 100)) % 360;
            nfloat min = 1 - saturation;
			int degreesMod = degrees == 0 ? 0 : degrees % 60 == 0 ? 60 : degrees % 60;
			nfloat stepSize = (1 - min) / 60;
            nfloat red, green, blue;

			if (degrees <= 60)
			{
				red = 1;
				green = min + degreesMod * stepSize;
				blue = min;
			}
			else if (degrees <= 120)
			{
				red = 1 - degreesMod * stepSize;
				green = 1;
				blue = min;
			}
			else if (degrees <= 180)
			{
				red = min;
				green = 1;
				blue = min + degreesMod * stepSize;
			}
			else if (degrees <= 240)
			{
				red = min;
				green = 1 - degreesMod * stepSize;
				blue = 1;
			}
			else if (degrees <= 300)
			{
				red = min + degreesMod * stepSize;
				green = min;
				blue = 1;
			}
			else
			{
				red = 1;
				green = min;
				blue = 1 - degreesMod * stepSize;
			}

            return new CGColor(
                red,
                green,
                blue,
                1
            );
		}

		/// <summary>
		/// Get new colors the next time ColorWheel is called.
		/// </summary>
		static public void ResetRgbSeed()
		{
			seedSet = false;
		}
	}
}