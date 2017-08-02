using System;
using System.Runtime.Serialization;

namespace Pronome
{
    [DataContract]
    public class Layer
    {
        [DataMember]
        public string ParsedString;

        [DataMember]
        public string ParsedOffset;

        [DataMember]
        public double volume;

        [DataMember]
        public float pan;

        [DataMember]
        public string BaseSourceName;

        public Layer(Pronome.Mac.Layer layer)
        {
            ParsedString = layer.ParsedString;
            ParsedOffset = layer.ParsedOffset;
            volume = (float)layer.Volume;
            pan = (float)layer.Pan;
            BaseSourceName = layer.BaseSourceName;
        }

        public Layer() {}

        /// <summary>
        /// Get the actual Layer instance
        /// </summary>
        /// <returns>The deserialize.</returns>
        public Pronome.Mac.Layer Deserialize()
        {
            return new Pronome.Mac.Layer(
                ParsedString, 
                Pronome.Mac.StreamInfoProvider.GetFromUri(BaseSourceName), 
                ParsedOffset, 
                pan,
                (float)volume);
        }
    }
}
