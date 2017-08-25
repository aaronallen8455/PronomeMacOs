using System.Runtime.Serialization;
using System.IO;
using System.Xml;
using System.Text;
using System.Collections.Generic;
using System;
using Foundation;

namespace Pronome.Mac
{
    /// <summary>
    /// A class used to manage user's settings
    /// </summary>
    [DataContract]
    public class UserSettings : NSObject
    {
        #region Properties
        /// <summary>
        /// Beat graph blinking toggle
        /// </summary>
        [Export("BlinkingEnabled")]
        public bool BlinkingEnabled
        {
            get => LoadBool("BlinkingEnabled", true);
            set
            {
                WillChangeValue("BlinkedEnabled");
                SaveBool("BlinkingEnabled", value, true);
                DidChangeValue("BlinkingEnabled");
            }
        }

        /// <summary>
        /// Bounce animation queue size
        /// </summary>
        [Export("BounceQueueSize")]
        public nfloat BounceQueueSize
        {
            get => (nfloat)LoadDouble("BounceQueueSize", 6);
            set
            {
                double prev = BounceQueueSize;
                WillChangeValue("BounceQueueSize");
                SaveDouble("BounceQueueSize", value, true);
                OnBounceSettingsChanged(new BounceSettingsChangedArgs(prev));
                DidChangeValue("BounceQueueSize");
            }
        }

        /// <summary>
        /// Bounce animation screen division location
        /// </summary>
        [Export("BounceDivision")]
        public nfloat BounceDivision
        {
            get => (nfloat)LoadDouble("BounceDivision", .5);
            set
            {
                WillChangeValue("BounceDivision");
                SaveDouble("BounceDivision", value, true);
                OnBounceSettingsChanged(new BounceSettingsChangedArgs());
				DidChangeValue("BounceDivision");
            }
        }

        /// <summary>
        /// Bounce animation taper
        /// </summary>
        [Export("BounceWidthPad")]
        public nfloat BounceWidthPad
        {
            get => (nfloat)LoadDouble("BounceWidthPad", 20);
            set
            {
                WillChangeValue("BounceWidthPad");
                SaveDouble("BounceWidthPad", value, true);
                OnBounceSettingsChanged(new BounceSettingsChangedArgs());
				DidChangeValue("BounceWidthPad");
            }
        }

        /// <summary>
        /// Length of pitch decay
        /// </summary>
        [Export("PitchDecayLength")]
        public nfloat PitchDecayLength
        {
            get => (nfloat)LoadDouble("PitchDecayLength", .04);
            set
            {
                WillChangeValue("PitchDecayLength");
                if (value > 0)
                {
                    SaveDouble("PitchDecayLength", value, true);
                    // propagate to pitch sources
                    PitchStream.SetDecayLength(value);
                }
                DidChangeValue("PitchDecayLength");
            }
        }
		
		/// <summary>
		/// Whether to load the previous session on startup
		/// </summary>
		[Export("PersistSession")]
		public bool PersistSession
		{
			get => LoadBool("PersistSession", true);
			set
			{
				WillChangeValue("PersistSession");
				SaveBool("PersistSession", value, true);
				DidChangeValue("PersistSession");
			}
		}
		
		/// <summary>
		/// The serialized beat from the previous session.
		/// </summary>
		[Export("PersistedSession")]
		public string PersistedSession
		{
			get => LoadString("PersistedSession", "");
			set
			{
				WillChangeValue("PersistedSession");
				SaveString("PersistedSession", value, true);
				DidChangeValue("PersistedSession");
			}
		}

        #endregion

        #region Public fields
        /// <summary>
        /// User's custom sources
        /// </summary>
        public NSMutableArray<StreamInfoProvider> UserSourceLibrary = new NSMutableArray<StreamInfoProvider>();
        #endregion

        #region Public methods
        /// <summary>
        /// Store the settings
        /// </summary>
        public void SaveToStorage()
        {
            // save the serialized current beat
            if (PersistSession)
            {
				var ds = new DataContractSerializer(typeof(Pronome.Metronome));
                using (var stream = new MemoryStream())
                {
					using (var writer = XmlDictionaryWriter.CreateTextWriter(stream, new UTF8Encoding(false)))
					{
                        ds.WriteObject(writer, new Pronome.Metronome(Metronome.Instance));
					}
                    PersistedSession = Encoding.UTF8.GetString(stream.ToArray());
                }
            }

            // serialize the user source collection
            if (UserSourceLibrary.Count > 0)
            {
                string serialized;
                List<Serialization.StreamInfoProvider> collection = new List<Serialization.StreamInfoProvider>();
                foreach (StreamInfoProvider info in UserSourceLibrary)
                {
                    collection.Add(new Serialization.StreamInfoProvider(info));
                }

                var ds = new DataContractSerializer(typeof(List<Serialization.StreamInfoProvider>));
				using (var stream = new MemoryStream())
				{
					using (var writer = XmlDictionaryWriter.CreateTextWriter(stream, new UTF8Encoding(false)))
					{
                        ds.WriteObject(writer, collection);
					}
                    serialized = Encoding.UTF8.GetString(stream.ToArray());
                    SaveString("UserSourceLibrary", serialized, true);
				}
            }
            else
            {
                NSUserDefaults.StandardUserDefaults.RemoveObject("UserSourceLibrary");
            }
        }

        /// <summary>
        /// Instantiate the persisted session and load the user source library.
        /// </summary>
        /// <returns></returns>
        public void GetSettingsFromStorage()
        {
            // get the persisted session from storage
            if (PersistSession)
            {
				var ds = new DataContractSerializer(typeof(Pronome.Metronome));
                byte[] bytes = Encoding.UTF8.GetBytes(PersistedSession);
                using (XmlDictionaryReader reader = XmlDictionaryReader.CreateTextReader(bytes, XmlDictionaryReaderQuotas.Max))
                {
                    var m = ds.ReadObject(reader) as Pronome.Metronome;

                    // import the beat
                    SavedFileManager.ImportMetronome(m);
                }
            }

            // git the user source library from storage
            string lib = LoadString("UserSourceLibrary", "");
            if (lib != "")
            {
                var ds = new DataContractSerializer(typeof(List<Serialization.StreamInfoProvider>));
                byte[] bytes = Encoding.UTF8.GetBytes(lib);
                using (XmlDictionaryReader reader = XmlDictionaryReader.CreateTextReader(bytes, XmlDictionaryReaderQuotas.Max))
                {
                    var sources = ds.ReadObject(reader) as List<Serialization.StreamInfoProvider>;

                    foreach (Serialization.StreamInfoProvider src in sources)
                    {
                        UserSourceLibrary.Add(src.Deserialize());
                    }
                }

                StreamInfoProvider.OnUserSourcesChanged(null);
            }
        }
        #endregion

        #region Events
        public class BounceSettingsChangedArgs : EventArgs
        {
            public double PreviousQueueSize;

            public BounceSettingsChangedArgs(double prevQueue = 0)
            {
                PreviousQueueSize = prevQueue;
            }
        }

        static public event EventHandler<BounceSettingsChangedArgs> BounceSettingsChanged;

        protected virtual void OnBounceSettingsChanged(BounceSettingsChangedArgs e)
        {
            BounceSettingsChanged?.Invoke(this, e);
        }
        #endregion

        static UserSettings _settings;

        /// <summary>
        /// Get the current settings
        /// </summary>
        /// <returns></returns>
        public static UserSettings GetSettings()
        {
            if (_settings == null)
            {
                _settings = new UserSettings();
            }

            return _settings;
        }

        #region Protected Static Methods
        static protected void SaveString(string key, string value, bool sync)
        {
            NSUserDefaults.StandardUserDefaults.SetString(value, key);

            if (sync)
            {
                NSUserDefaults.StandardUserDefaults.Synchronize();
            }
        }

        static protected string LoadString(string key, string defaultValue)
        {
            if (NSUserDefaults.StandardUserDefaults[key] == null) return defaultValue;

            string value = NSUserDefaults.StandardUserDefaults.StringForKey(key);

            return value;
        }

        static protected void SaveBool(string key, bool value, bool sync)
        {
            NSUserDefaults.StandardUserDefaults.SetBool(value, key);

            if (sync)
            {
                NSUserDefaults.StandardUserDefaults.Synchronize();
            }
        }

        static protected bool LoadBool(string key, bool defaultValue)
        {
            if (NSUserDefaults.StandardUserDefaults[key] == null) return defaultValue;

            var value = NSUserDefaults.StandardUserDefaults.BoolForKey(key);

            return value;
        }

        static protected void SaveDouble(string key, double value, bool sync)
        {
            NSUserDefaults.StandardUserDefaults.SetDouble(value, key);
            if (sync)
            {
                NSUserDefaults.StandardUserDefaults.Synchronize();
            }
        }

        static protected double LoadDouble(string key, double def)
        {
            if (NSUserDefaults.StandardUserDefaults[key] == null) return def;

            double value = NSUserDefaults.StandardUserDefaults.DoubleForKey(key);

            return value;
        }
        #endregion
    }
}