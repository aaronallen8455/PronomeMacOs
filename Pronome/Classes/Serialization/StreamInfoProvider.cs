using System.Runtime.Serialization;

namespace Pronome.Mac.Serialization
{
    [DataContract]
    public class StreamInfoProvider
    {
        [DataMember]
        public string Uri;

        [DataMember]
        public bool IsInternal;

        [DataMember]
        public Pronome.Mac.StreamInfoProvider.HiHatStatuses HiHatStatus;

        [DataMember]
        public string Title;

        [DataMember]
        public int Index;

        public StreamInfoProvider(Pronome.Mac.StreamInfoProvider info)
        {
            Uri = info.Uri;
            IsInternal = info.IsInternal;
            HiHatStatus = info.HiHatStatus;
            Title = info.Title;
            Index = (int)info.Index;
        }

        public StreamInfoProvider() {}

        /// <summary>
        /// Get the actual info object.
        /// </summary>
        /// <returns>The deserialize.</returns>
        public Pronome.Mac.StreamInfoProvider Deserialize()
        {
            return new Pronome.Mac.StreamInfoProvider(Index, Uri, Title, HiHatStatus, IsInternal);
        }
    }
}
