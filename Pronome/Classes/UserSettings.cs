using System.Runtime.Serialization;
using System.IO;
using System.IO.IsolatedStorage;
using System.Xml;
using System.Text;
using System.Collections.Generic;
using System;
using AppKit;
using Foundation;

namespace Pronome
{
	/// <summary>
	/// A class used to manage user's settings
	/// </summary>
	[DataContract]
    public class UserSettings : NSObject
	{
        [DataMember]
        private bool _blinkingEnabled;

		/// <summary>
		/// Beat graph blinking toggle
		/// </summary>
        [Export("BlinkingEnabled")]
        public bool BlinkingEnabled
        {
            get => _blinkingEnabled;
            set
            {
                WillChangeValue("BlinkedEnabled");
                _blinkingEnabled = value;
                DidChangeValue("BlinkingEnabled");
            }
        }

        [DataMember]
        private double _bounceQueueSize;

		/// <summary>
		/// Bounce animation queue size
		/// </summary>
        [Export("BounceQueueSize")]
        public nfloat BounceQueueSize
        {
            get => (nfloat)_bounceQueueSize;
            set
            {
                WillChangeValue("BounceQueueSize");
                _bounceQueueSize = value;
                DidChangeValue("BounceQueueSize");
            }
        }

        [DataMember]
        private double _bounceDivision;

		/// <summary>
		/// Bounce animation screen division location
		/// </summary>
        [Export("BounceDivision")]
        public nfloat BounceDivision
        {
            get => (nfloat)_bounceDivision;
            set
            {
                WillChangeValue("BounceDivision");
                _bounceDivision = value;
                DidChangeValue("BounceDivision");
            }
        }

        [DataMember]
        private double _bounceWidthPad;

		/// <summary>
		/// Bounce animation taper
		/// </summary>
        [Export("BounceWidthPad")]
        public nfloat BounceWidthPad
        {
            get => (nfloat)_bounceWidthPad;
            set
            {
                WillChangeValue("BounceWidthPad");
                _bounceWidthPad = value;
                DidChangeValue("BounceWidthPad");
            }
        }

        [DataMember]
        private double _pitchDecayLength;

		/// <summary>
		/// Length of pitch decay
		/// </summary>
        [Export("PitchDecayLength")]
        public nfloat PitchDecayLength
        {
            get => (nfloat)_pitchDecayLength;
            set
            {
				WillChangeValue("PitchDecayLength");
                if (value > 0)
                {
					_pitchDecayLength = value;
                    // propagate to pitch sources
                    PitchStream.SetDecayLength(value);
                }
				DidChangeValue("PitchDecayLength");
            }
        }

		/// <summary>
		/// User's custom sources
		/// </summary>
		[DataMember]
        public NSMutableArray<StreamInfoProvider> UserSourceLibrary = new NSMutableArray<StreamInfoProvider>();

        [DataMember(IsRequired = false)]
        private bool _persistSession = true;

		/// <summary>
		/// Whether to load the previous session on startup
		/// </summary>
        [Export("PersistSession")]
        public bool PersistSession
        {
            get => _persistSession;
            set
            {
                WillChangeValue("PersistSession");
                _persistSession = value;
                DidChangeValue("PersistSession");
            }
        }

		/// <summary>
		/// Holds the current state of the persist session toggle
		/// </summary>
		public static bool PersistSessionStatic = true;

        [DataMember(IsRequired = false)]
        private string _persistedSession;

		/// <summary>
		/// The serialized beat from the previous session.
		/// </summary>
        [Export("PersistedSession")]
        public string PersistedSession
        {
            get => _persistedSession;
            set
            {
                WillChangeValue("PersistedSession");
                _persistedSession = value;
                DidChangeValue("PersistedSession");
            }
        }

		/// <summary>
		/// Store the settings
		/// </summary>
		public void SaveToStorage()
		{
			DataContractSerializer ds = new DataContractSerializer(typeof(UserSettings));
			using (IsolatedStorageFile isf = IsolatedStorageFile.GetUserStoreForAssembly())
			{
				using (IsolatedStorageFileStream isfs = new IsolatedStorageFileStream("pronomeSettings", FileMode.Create, isf))
				{
					//using (XmlDictionaryWriter writer = XmlDictionaryWriter.CreateBinaryWriter(isfs))
					//{
					//	ds.WriteObject(writer, this);
					//}
				}
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
		public static UserSettings GetSettingsFromStorage()
		{
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
            throw new NotImplementedException();
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

                _settings.PitchDecayLength = (nfloat).04;

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
	}
}