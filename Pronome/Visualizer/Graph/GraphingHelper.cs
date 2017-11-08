using System.Collections.Generic;
using System.Linq;
using CoreAnimation;

namespace Pronome.Mac.Visualizer.Graph
{
    public class GraphingHelper
    {
        #region public static fields
        public static double ElapsedBpm;
        #endregion

        #region protected static fields
        protected static double LastElapsedBpm;
        #endregion

        #region public static methods
        /// <summary>
        /// Builds the models used in the graph
        /// </summary>
        /// <returns>The graph.</returns>
        /// <param name="superLayer">Super layer.</param>
        /// <param name="beatLength">Beat length.</param>
        public static LinkedList<Ring> BuildGraph(CALayer superLayer, double beatLength)
        {
            LinkedList<Ring> rings = new LinkedList<Ring>();

            // create a ring for each layer
            double outerRadius = .48;
            double ringSize = .35 / (Metronome.Instance.Layers.Count + 1);
            foreach (Layer beatLayer in Metronome.Instance.Layers.Reverse<Layer>())
            {
                // find the position of the inner and outer radius
                double innerRadius = outerRadius - ringSize;
                Ring ring = new Ring(beatLayer, superLayer, innerRadius, outerRadius, beatLength);
                outerRadius = innerRadius;

                rings.AddLast(ring);
            }

            return rings;
        }

        /// <summary>
        /// Updates the elapsed bpm.
        /// </summary>
        public static void UpdateElapsedBpm()
        {
            double newElapsed = Metronome.Instance.ElapsedBpm;
            ElapsedBpm = newElapsed - LastElapsedBpm;
            LastElapsedBpm = newElapsed;
        }

        /// <summary>
        /// Resets the elapsed bpm.
        /// </summary>
        public static void ResetElapsedBpm()
        {
            ElapsedBpm = LastElapsedBpm = 0;
        }
        #endregion
    }
}
