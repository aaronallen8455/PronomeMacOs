using System;
using System.Runtime.Serialization;
using System.Collections.Generic;

namespace Pronome
{
    /// <summary>
    /// A shell for serialization of a metronome
    /// </summary>
    [DataContract]
    public class Metronome
    {
        [DataMember]
        public double Tempo; //prop

        [DataMember]
        public double Volume; //prop

        [DataMember]
        public List<Layer> Layers;

        public Metronome(Pronome.Mac.Metronome met)
        {
            Tempo = met.Tempo;
            Volume = met.Volume;
            Layers = new List<Layer>();

            // get serializable version of all the layers
            foreach (Pronome.Mac.Layer layer in met.Layers)
            {
                Layers.Add(new Layer(layer));
            }
        }
    }
}
