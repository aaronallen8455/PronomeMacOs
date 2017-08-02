using System;
using System.IO;
using System.Runtime.Serialization;
using System.Xml;
using AppKit;

namespace Pronome.Mac
{
    public class SavedFileManager
    {
        #region Public Properties
        static public string CurrentlyOpenFile { get; set; }
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
            var ds = new DataContractSerializer(typeof(Pronome.Metronome));
            using (Stream s = File.Create(name))
            using (var w = XmlDictionaryWriter.CreateBinaryWriter(s))
            {
                ds.WriteObject(w, new Pronome.Metronome(Metronome.Instance));

                CurrentlyOpenFile = name;
            }
        }

        /// <summary>
        /// Load the beat from the specified file name.
        /// </summary>
        /// <returns>The load.</returns>
        /// <param name="fileName">File name.</param>
        static public void Load(string fileName)
        {
            var ds = new DataContractSerializer(typeof(Pronome.Metronome));
            using (Stream s = File.OpenRead(fileName))
            {
                
				using (var r = XmlDictionaryReader.CreateBinaryReader(s, XmlDictionaryReaderQuotas.Max))
				{
					try
					{
						var m = ds.ReadObject(r) as Pronome.Metronome;
						
						ImportMetronome(m);
						
						CurrentlyOpenFile = fileName;
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
						
						alert.RunModal();
					}
				}
                
            }
        }

        public static void ImportMetronome(Pronome.Metronome m)
        {
			// remove all current layers
			foreach (Layer layer in Metronome.Instance.Layers.ToArray())
			{
				layer.Controller.Remove();
			}

			// add new layers
			foreach (Pronome.Layer layer in m.Layers)
			{
                var controller = TransportViewController.Instance;
				controller.NewLayer(
					layer.ParsedString,
					StreamInfoProvider.GetFromUri(layer.BaseSourceName),
					layer.ParsedOffset,
					layer.pan,
                    (float)layer.volume);
			}

			// set the tempo and volume
			Metronome.Instance.Volume = (nfloat)m.Volume;
			Metronome.Instance.Tempo = (nfloat)m.Tempo;
        }

        #endregion
    }
}
