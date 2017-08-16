using System.Collections.Generic;
using System.Linq;
using CoreAnimation;

namespace Pronome.Mac.Visualizer.Graph
{
    public class GraphingHelper
    {
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
    }
}
