using System;
using System.IO;
using System.Runtime.Serialization;
using System.Xml;
using AppKit;

namespace Pronome
{
    public class SavedFileManager
    {
        #region Public Properties
        public string CurrentlyOpenFile { get; set; }
        #endregion

        #region Constructors
        public SavedFileManager()
        {
        }
        #endregion

        #region Static Methods
        /// <summary>
        /// Save the current beat to the given file
        /// </summary>
        /// <param name="name">Name.</param>
        static public void Save(string name)
        {
            var ds = new DataContractSerializer(typeof(Metronome));
            using (Stream s = File.Create(name))
            using (var w = XmlDictionaryWriter.CreateBinaryWriter(s))
            {
                ds.WriteObject(w, Metronome.Instance);
            }
        }

        static public void Load(string fileName)
        {
            var ds = new DataContractSerializer(typeof(Metronome));
            using (Stream s = File.OpenRead(fileName))
            using (var r = XmlDictionaryReader.CreateBinaryReader(s, XmlDictionaryReaderQuotas.Max))
            {
                try
                {
                    ds.ReadObject(r);
                }
                catch (SerializationException)
                {
                    string name = Path.GetFileName(fileName);

                    var alert = new NSAlert()
                    {
                        AlertStyle = NSAlertStyle.Critical,
                        InformativeText = $"{name} could not be used because it isn't a valid beat file.",
                        MessageText = "Invalid Beat File"
                    };
                }
            }
        }
        #endregion
    }
}
