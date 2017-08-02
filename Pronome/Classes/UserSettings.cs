using System.Runtime.Serialization;
using System.IO;
using System.IO.IsolatedStorage;
using System.Xml;
using System.Text;
using System.Collections.Generic;
using System;
using AppKit;
using Foundation;

namespace Pronome.Mac
{
    /// <summary>
    /// A class used to manage user's settings
    /// </summary>
    [DataContract]
    public class UserSettings : NSObject
    {
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
                WillChangeValue("BounceQueueSize");
                SaveDouble("BounceQueueSize", value, true);
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
        /// User's custom sources
        /// </summary>
        public NSMutableArray<StreamInfoProvider> UserSourceLibrary = new NSMutableArray<StreamInfoProvider>();


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
        /// Holds the current state of the persist session toggle
        /// </summary>
        //public static bool PersistSessionStatic = true;


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
        /// Apply the settings
        /// </summary>
        public void ApplySettings()
        {
            //Window mainWindow = Application.Current.MainWindow;

            //mainWindow.Left = WinX;
            //mainWindow.Top = WinY;
            //mainWindow.Width = WinWidth;
            //mainWindow.Height = WinHeight;
            //Application.Current.Resources["textBoxFontSize"] = BeatFontSize;

            //BeatGraphWindow.BlinkingIsEnabled = BlinkingEnabled;
            //BounceWindow.Tick.QueueSize = BounceQueueSize;
            //BounceWindow.divisionPoint = BounceDivision;
            //BounceWindow.widthPad = BounceWidthPad;
            //PitchStream.DecayLength = PitchDecayLength;
            //UserSourceLibrary s = (mainWindow.Resources["optionsWindow"] as Window).Resources["userSourceLibrary"] as UserSourceLibrary;
            //PersistSessionStatic = PersistSession;
            //
            //foreach (UserSource source in UserSourceLibrary)
            //{
            //	s.Add(source);
            //}
            //
            //// deserialize the peristed session beat if enabled
            //if (PersistSession && PersistedSession != string.Empty)
            //{
            //	DataContractSerializer ds = new DataContractSerializer(typeof(Metronome));
            //	byte[] bin = Encoding.UTF8.GetBytes(PersistedSession);
            //	using (var stream = new MemoryStream(bin))
            //	{
            //		using (var reader = XmlDictionaryReader.CreateTextReader(stream, XmlDictionaryReaderQuotas.Max))
            //		{
            //			try
            //			{
            //				ds.ReadObject(reader);
            //
            //				// need to initiate these values
            //				Metronome.GetInstance().TempoChangeCued = false;
            //				Metronome.GetInstance().TempoChangedSet = new HashSet<IStreamProvider>();
            //			}
            //			catch (SerializationException)
            //			{
            //				new TaskDialogWrapper(Application.Current.MainWindow).Show(
            //					"Session Persistence Failed", "An error occured while attempting to load the beat from your last session, sorry about that!",
            //					"", TaskDialogWrapper.TaskDialogButtons.Ok, TaskDialogWrapper.TaskDialogIcon.Error);
            //			}
            //		}
            //	}
            //}
            throw new NotImplementedException();
        }

        /// <summary>
        /// Get the stored settings
        /// </summary>
        /// <returns></returns>
        public void GetSettingsFromStorage()
        {
            if (PersistSession)
            {
				var ds = new DataContractSerializer(typeof(Pronome.Metronome));
                byte[] bytes = Encoding.UTF8.GetBytes(PersistedSession);
                using (XmlDictionaryReader reader = XmlDictionaryReader.CreateTextReader(bytes, XmlDictionaryReaderQuotas.Max))
                {
                    var m = ds.ReadObject(reader) as Pronome.Metronome;

                    SavedFileManager.ImportMetronome(m);
                }
            }

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
            }

            //DataContractSerializer ds = new DataContractSerializer(typeof(UserSettings));
            //using (IsolatedStorageFile isf = IsolatedStorageFile.GetUserStoreForAssembly())
            //{
            //	using (IsolatedStorageFileStream isfs = new IsolatedStorageFileStream("pronomeSettings", FileMode.OpenOrCreate, isf))
            //	{
            //		using (XmlDictionaryReader reader = XmlDictionaryReader.CreateBinaryReader(isfs, XmlDictionaryReaderQuotas.Max))
            //		{
            //			try
            //			{
            //				return (UserSettings)ds.ReadObject(reader);
            //			}
            //			catch (SerializationException)
            //			{
            //				// settings don't exist or are corrupt
            //				return null;
            //			}
            //		}
            //	}
            //}
            //throw new NotImplementedException();
        }

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
                //_settings.PitchDecayLength = (nfloat)LoadDouble("pitchDecayLength", .04);

                _settings.UserSourceLibrary.Add(new StreamInfoProvider(1, "test.wav", "Test Title", StreamInfoProvider.HiHatStatuses.None, false));

                _settings.UserSourceLibrary.Add(new StreamInfoProvider(2, "test2.wav", "Another Test", StreamInfoProvider.HiHatStatuses.None, false));
            }

            return _settings;
            //Window mainWindow = MainWindow.Instance;
            //
            //bool persistSession = PersistSessionStatic;
            //
            //string serializedBeat = "";
            //
            //// stringify the current beat if it is to be persisted.
            //if (persistSession)
            //{
            //	var ds = new DataContractSerializer(typeof(Metronome));
            //	using (var stream = new MemoryStream())
            //	{
            //		using (var writer = XmlDictionaryWriter.CreateTextWriter(stream, new UTF8Encoding(false)))
            //		{
            //			ds.WriteObject(writer, Metronome.GetInstance());
            //		}
            //		serializedBeat = Encoding.UTF8.GetString(stream.ToArray());
            //	}
            //}
            //
            //return new UserSettings()
            //{
            //	WinX = mainWindow.Left,
            //	WinY = mainWindow.Top,
            //	WinWidth = mainWindow.Width,
            //	WinHeight = mainWindow.Height,
            //	BeatFontSize = (double)Application.Current.Resources["textBoxFontSize"],
            //	BlinkingEnabled = BeatGraphWindow.BlinkingIsEnabled,
            //	BounceQueueSize = BounceWindow.Tick.QueueSize,
            //	BounceDivision = BounceWindow.divisionPoint,
            //	BounceWidthPad = BounceWindow.widthPad,
            //	PitchDecayLength = PitchStream.DecayLength,
            //	UserSourceLibrary = UserSource.Library,
            //	PersistSession = persistSession,
            //	PersistedSession = serializedBeat
            //};
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