using System;
using Foundation;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;
using System.Text;
using AppKit;
using CoreText;

namespace Pronome.Mac
{
    /// <summary>
    /// An object that represents a single beat layer.
    /// </summary>
    public class Layer : NSObject
    {
        #region Public Variables
        /// <summary>
        /// Keeps track of partial samples to add back in when the value is >= 1.
        /// </summary>
        public double SampleRemainder = 0;

        /// <summary>
        /// The beat cells.
        /// </summary>
        public List<BeatCell> Beat = new List<BeatCell>();

        /** <summary>The beat code string that was passed in to create the rhythm of this layer.</summary> */
        public string ParsedString;

        /**<summary>The string that was parsed to get the offset value.</summary>*/
        public string ParsedOffset = "0";

        /** <summary>The name of the base source.</summary> */
        public string BaseSourceName;

        /** <summary>True if a solo group exists.</summary> */
        public static bool SoloGroupEngaged = false; // is there a solo group?

        /** <summary>Does the layer contain a hihat closed source?</summary> */
        public bool HasHiHatClosed = false;

        /** <summary>Does the layer contain a hihat open source?</summary> */
        public bool HasHiHatOpen = false;

        /** <summary>The audio sources that are not pitch or the base sound.</summary> */
        public Dictionary<string, IStreamProvider> AudioSources = new Dictionary<string, IStreamProvider>();

        /// <summary>
        /// The base audio source. Could be a pitch or a sound file.
        /// </summary>
        public IStreamProvider BaseAudioSource;

        /// <summary>
        /// The pitch source, if needed. Will also be the baseAudioSource if it's a pitch layer
        /// </summary>
        public PitchStream PitchSource;

        public double OffsetBpm;

        public LayerItemController Controller;
        #endregion

        #region Databound Properties
        NSAttributedString _beatCode;
        /// <summary>
        /// Gets or sets the raw beat code string.
        /// </summary>
        /// <value>The beat code.</value>
        [Export("BeatCode")]
        public NSAttributedString BeatCode
        {
            get => _beatCode;
            set
            {
                WillChangeValue("BeatCode");
                if (value.Value != ParsedString)
                {
					// validate beat code
					try
					{
						ParsedString = value.Value;
						
						// check if the font is correct (may be pasteing from somewhere)
						var font = value.GetFontAttributes(new NSRange(0, value.Value.Length));
						if (font.TryGetValue(new NSString("NSFont"), out NSObject f))
						{
							NSFont nsFont = (NSFont)f;
							if (nsFont.FontName != "Geneva" || nsFont.PointSize != 16)
							{
								_beatCode = GetAttributedString(value.Value);
							}
							else _beatCode = value;
						}
					}
					catch (Exception) { }
					// parse the beatcode
					if (Metronome.Instance.PlayState == Metronome.PlayStates.Stopped)
					{
						ProcessBeat(ParsedString);
					}
					else
					{
						// change the layer while playing
						Metronome.Instance.ExecuteLayerChange(this);
					}
                    
                }
                DidChangeValue("BeatCode");
            }
        }

        /// <summary>
        /// Gets or sets the raw offset string.
        /// </summary>
        /// <value>The offset.</value>
        [Export("Offset")]
        public string Offset
        {
            get => ParsedOffset;
            set
            {
                WillChangeValue("Offset");
                // Validate the beatCode
                if (BeatCell.TryParse(value, out double newOffsetBpm) && newOffsetBpm != OffsetBpm)
                {
                    ParsedOffset = value;

                    // set the offset on all sources
                    double offsetSamples = newOffsetBpm - OffsetBpm;

                    OffsetBpm = newOffsetBpm;

                    if (Metronome.Instance.PlayState == Metronome.PlayStates.Stopped)
                    {
                        foreach (IStreamProvider stream in GetAllStreams())
                        {
                            stream.Offset = stream.Offset + offsetSamples;
                        }
                    }
                    else
                    {
                        // change offset while playing
                        Metronome.Instance.ExecuteLayerChange(this);
                    }

                }
                DidChangeValue("Offset");
            }
        }

        private nfloat _volume = 1f;
        /// <summary>
        /// Gets or sets the volume.
        /// </summary>
        /// <value>The volume. 0 to 1</value>
        [Export("Volume")]
        public nfloat Volume
        {
            get => _volume;
            set
            {
                WillChangeValue("Volume");
                _volume = value;

                // set the volume for all streams
                foreach (IStreamProvider src in GetAllStreams()) src.Volume = value;

                DidChangeValue("Volume");
            }
        }

        private nfloat _pan = 0f;
        /// <summary>
        /// Gets or sets the pan.
        /// </summary>
        /// <value>The pan. -1 to 1</value>
        [Export("Pan")]
        public nfloat Pan
        {
            get => _pan;
            set
            {
                WillChangeValue("Pan");
                _pan = value;
                // set on audio sources
                float newPan = (float)value;
                foreach (IStreamProvider src in GetAllStreams()) src.Pan = newPan;

                DidChangeValue("Pan");
            }
        }

        private string _sourceInput;
        /// <summary>
        /// Gets or sets the source input's value.
        /// </summary>
        /// <value>The source input.</value>
        [Export("Source")]
        public string SourceInput
        {
            get => _sourceInput;
            set
            {
                WillChangeValue("Source");
                var info = StreamInfoProvider.GetFromToString(value);

                _sourceInput = value;
                PitchInputVisible = value != "Pitch";

                if (info != null && !info.Equals(BaseAudioSource.Info))
                {

                    if (Metronome.Instance.PlayState == Metronome.PlayStates.Stopped)
                    {
                        NewBaseSource(info);
                    }
                    else
                    {
                        // change while playing
                        BaseAudioSource.Info = info;
                        Metronome.Instance.ExecuteLayerChange(this);
                    }

                }

                DidChangeValue("Source");
            }
        }

        private string _pitchInput;
        [Export("Pitch")]
        public string PitchInput
        {
            get => _pitchInput;
            set
            {
                WillChangeValue("Pitch");
                // validate pitch input
                // validate input
                if (Regex.IsMatch(value, @"^[A-Ga-g][#b]?\d{0,2}$|^[\d.]+$"))
                {
                    string src = value;
                    // assume octave 4 if non given
                    if (!char.IsDigit(src.Last()))
                    {
                        src += '4';
                    }

                    if (Metronome.Instance.PlayState == Metronome.PlayStates.Stopped)
                    {
                        NewBaseSource(StreamInfoProvider.GetFromPitch(src));
                    }
                    else
                    {
                        BaseAudioSource.Info = StreamInfoProvider.GetFromPitch(src);
                        Metronome.Instance.ExecuteLayerChange(this);
                    }

                    _pitchInput = value;
                }
                DidChangeValue("Pitch");
            }
        }

        private bool _pitchInputVisible;
        [Export("IsPitchHidden")]
        public bool PitchInputVisible
        {
            get => _pitchInputVisible;
            set
            {
                WillChangeValue("IsPitchHidden");
                _pitchInputVisible = value;
                DidChangeValue("IsPitchHidden");
            }
        }

        [Export("Index")]
        public string Index
        {
            get => (Metronome.Instance.Layers.IndexOf(this) + 1).ToString();
        }

        private bool _muted = false;
        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="T:Pronome.Layer"/> is muted.
        /// </summary>
        /// <value><c>true</c> if is muted; otherwise, <c>false</c>.</value>
        [Export("IsMuted")]
        public bool IsMuted
        {
            get => _muted;
            set
            {
                WillChangeValue("IsMuted");
                _muted = value;
                UpdateMuting();
                DidChangeValue("IsMuted");
            }
        }

        private bool _soloed = false;
        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="T:Pronome.Layer"/> is soloed.
        /// </summary>
        /// <value><c>true</c> if is soloed; otherwise, <c>false</c>.</value>
        [Export("IsSoloed")]
        public bool IsSoloed
        {
            get => _soloed;
            set
            {
                WillChangeValue("IsSoloed");
                _soloed = value;
                UpdateMuting();
                DidChangeValue("IsSoloed");
            }
        }
        #endregion

        #region Public Properties
        /// <summary>
        /// The base stream info.
        /// </summary>
        public StreamInfoProvider BaseStreamInfo
        {
            get => BaseAudioSource?.Info;
        }
        #endregion

        #region Constructors
        public Layer(string beat = "1", StreamInfoProvider baseSource = null, string offset = "", float pan = 0f, float volume = 1f)
        {
            if (baseSource == null) // auto generate a pitch if no source is specified
            {
                SetBaseSource(StreamInfoProvider.GetFromPitch(GetAutoPitch()));
            }
            else
            {
                SetBaseSource(baseSource);
            }

            if (Metronome.Instance.PlayState == Metronome.PlayStates.Stopped)
            {
                Offset = offset;
            }

			Metronome.Instance.AddLayer(this);

            if (Metronome.Instance.PlayState == Metronome.PlayStates.Stopped)
            {
                BeatCode = GetAttributedString(beat);
            }

            //Parse(beat); // parse the beat code into this layer
            Volume = volume;

            Pan = pan;

            if (BaseStreamInfo.IsPitch && Metronome.Instance.PlayState == Metronome.PlayStates.Stopped)
            {
                PitchInput = BaseStreamInfo.Uri;
            }

        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Parse beatcode string to get an array of beatcells.
        /// </summary>
        /// <returns>The parse.</returns>
        /// <param name="beat">Beat.</param>
        public BeatCell[] Parse(string beat)
        {
            ParsedString = beat;
            // remove comments
            beat = Regex.Replace(beat, @"!.*?!", "");
            // remove whitespace
            beat = Regex.Replace(beat, @"\s", "");

            //string pitchModifier = @"@[a-gA-G]?[#b]?[pP]?[1-9.]+";

            if (beat.Contains('$'))
            {
                // prep single cell repeat on ref if exists
                beat = Regex.Replace(beat, @"(\$[\ds]+)(\(\d\))", "[$1]$2");

                //resolve beat referencing
                while (beat.Contains('$'))
                {
                    var match = Regex.Match(beat, @"\$(\d+|s)");
                    string indexString = match.Groups[1].Value;
                    int refIndex;
                    int selfIndex = Metronome.Instance.Layers.IndexOf(this);
                    if (indexString == "s") refIndex = selfIndex;
                    else refIndex = int.Parse(indexString) - 1;

                    string refString = ResolveReferences(refIndex, new HashSet<int>(new int[] { selfIndex }));

                    // perform the replacement
                    beat = beat.Substring(0, match.Index) +
                        refString +
                        beat.Substring(match.Index + match.Length);
                }
            }

            // allow 'x' to be multiply operator
            beat = beat.Replace('x', '*');
            beat = beat.Replace('X', '*');

            // handle group multiply
            while (beat.Contains('{'))
            {
                var match = Regex.Match(beat, @"\{([^}{]*)}([^,\]}]+)"); // match the inside and the factor
                                                                         // insert the multiplication
                string inner = Regex.Replace(match.Groups[1].Value, @"(?<!\]\d*)(?=([\]\(\|,+-]|$))", "*" + match.Groups[2].Value);
                // switch the multiplier to be in front of pitch modifiers
                inner = Regex.Replace(inner, @"(@[a-gA-GpP]?[#b]?[\d.]+)(\*[\d.*/]+)", "$2$1");
                // insert into beat
                beat = beat.Substring(0, match.Index) + inner + beat.Substring(match.Index + match.Length);
            }

            // handle single cell repeats
            while (Regex.IsMatch(beat, @"[^\]]\(\d+\)"))
            {
                var match = Regex.Match(beat, @"([.\d+\-/*]+@?[a-gA-G]?[#b]?[Pp]?u?\d*)\((\d+)\)([\d\-+/*.]*)");
                StringBuilder result = new StringBuilder(beat.Substring(0, match.Index));
                for (int i = 0; i < int.Parse(match.Groups[2].Value); i++)
                {
                    result.Append(match.Groups[1].Value);
                    // add comma or last term modifier
                    if (i == int.Parse(match.Groups[2].Value) - 1)
                    {
                        result.Append("+0").Append(match.Groups[3].Value);
                    }
                    else result.Append(",");
                }
                // insert into beat
                beat = result.Append(beat.Substring(match.Index + match.Length)).ToString();
            }

            // handle multi-cell repeats
            while (beat.Contains('['))
            {
                var match = Regex.Match(beat, @"\[([^\][]+?)\]\(?(\d+)\)?([\d\-+/*.]*)");
                StringBuilder result = new StringBuilder();
                int itr = int.Parse(match.Groups[2].Value);
                for (int i = 0; i < itr; i++)
                {
                    // if theres a last time exit point, only copy up to that
                    if (i == itr - 1 && match.Value.Contains('|'))
                    {
                        result.Append(match.Groups[1].Value.Substring(0, match.Groups[1].Value.IndexOf('|')));
                    }
                    else result.Append(match.Groups[1].Value); // copy the group

                    if (i == itr - 1)
                    {
                        result.Append("+0").Append(match.Groups[3].Value);
                    }
                    else result.Append(",");
                }
                result.Replace('|', ',');
                beat = beat.Substring(0, match.Index) + result.Append(beat.Substring(match.Index + match.Length)).ToString();
            }

            // fix instances of a pitch modifier being following by +0 from repeater
            beat = Regex.Replace(beat, $@"(@[a-gA-G]?[#b]?[pP]?u?[0-9.]+)(\+[\d.\-+/*]+)", "$2$1");

            if (beat != string.Empty)
            {
                BeatCell[] cells = beat.Split(',').Select((x) =>
                {
                    var match = Regex.Match(x, @"([\d.+\-/*]+)@?(.*)");
                    string source = match.Groups[2].Value;

                    // get the correct sound source stub
                    StreamInfoProvider src = StreamInfoProvider.GetFromModifier(source);

                    return new BeatCell(match.Groups[1].Value, this, src);

                }).ToArray();

                return cells;
                //SetBeat(cells);


            }
            return null;
        }

        /**<summary>Get a random pitch based on existing pitch layers</summary>*/
        public string GetAutoPitch()
        {
            string note;
            byte octave;

            string[] noteNames =
            {
                "A", "A#", "B", "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#"
            };

            ushort[] intervals = { 3, 4, 5, 7, 8, 9 };

            int cycles = 0;
            do
            {
                // determine the octave
                octave = Metronome.GetRandomNum() > 49 ? (byte)5 : (byte)4;
                // 80% chance to make a sonorous interval with last pitch layer
                if (Metronome.Instance.Layers.Exists(x => x.BaseStreamInfo.IsPitch) && Metronome.GetRandomNum() < 80)
                {
                    var last = Metronome.Instance.Layers.Last(x => x.BaseStreamInfo.IsPitch);
                    int index = Array.IndexOf(noteNames, last.BaseAudioSource.Info.Uri.TakeWhile(x => !char.IsNumber(x)));
                    index += intervals[(int)(Metronome.GetRandomNum() / 16.6667)];
                    if (index > 11) index -= 12;
                    note = noteNames[index];
                }
                else
                {
                    // randomly pick note
                    note = noteNames[(int)(Metronome.GetRandomNum() / 8.3333)];
                }
                cycles++;
            }
            while (cycles < 24 && Metronome.Instance.Layers.Where(x => x.BaseStreamInfo.IsPitch).Any(x => x.BaseAudioSource.Info.Uri == note + octave));

            return note + octave;
        }

        /**<summary>Sum up all the Bpm values for beat cells.</summary>*/
        public double GetTotalBpmValue()
        {
            return Beat.Select(x => x.Bpm).Sum();
        }

        /// <summary>
        /// Adds the streams to mixer. Removes them first so that hihat order is correct.
        /// </summary>
        public void AddStreamsToMixer()
        {
            foreach (IStreamProvider src in GetAllStreams())
            {
                Metronome.Instance.RemoveAudioSource(src);
            }

            Metronome.Instance.AddSourcesFromLayer(this);
        }

        /// <summary>
        /// Gets all streams.
        /// </summary>
        /// <returns>The all streams.</returns>
        public IEnumerable<IStreamProvider> GetAllStreams()
        {
            IEnumerable<IStreamProvider> sources = AudioSources.Values.Concat(new IStreamProvider[] { BaseAudioSource });
            if (!BaseStreamInfo.IsPitch && PitchSource != null)
            {
                sources.Concat(new IStreamProvider[] { PitchSource });
            }

            return sources;
        }

        /** <summary>Reset this layer so that it will play from the start.</summary> */
        public void Reset()
        {
            SampleRemainder = 0;
            foreach (IStreamProvider src in GetAllStreams())
            {
                src.Reset();
            }
        }
        #endregion

        #region Static Methods
        /// <summary>
        /// Updates the muting for all layers.
        /// </summary>
        static public void UpdateMuting()
        {
            // is there an open solo group?
            bool soloGroup = Metronome.Instance.Layers.Any(x => x.IsSoloed);

            foreach (Layer layer in Metronome.Instance.Layers)
            {
                // mute all streams that are not soloed if a solo group is open
                if (soloGroup)
                {
                    foreach (IStreamProvider src in layer.GetAllStreams())
                    {
                        Metronome.Instance.SetMutingOfMixerInput(src, !layer.IsMuted && layer.IsSoloed);
                    }
                }
                else
                {
                    // mute streams that have the mute switch toggled
                    foreach (IStreamProvider src in layer.GetAllStreams())
                    {
                        Metronome.Instance.SetMutingOfMixerInput(src, !layer.IsMuted);
                    }
                }
            }
        }

        static protected NSAttributedString GetAttributedString(string content)
        {
            // Configure the NSAttributedString, which we use for the beat code editor
            var d = new NSMutableDictionary();
            d.SetValueForKey(NSColor.White, new NSString("NSColor"));
            var s = new CTStringAttributes(d);
            s.ForegroundColorFromContext = false;
            s.Font = new CTFont("CourierNewPS-BoldMT", 16); // geneva also works

            return new NSAttributedString(content, s);
        }
        #endregion

        #region Protected Methods
        /// <summary>
        /// Recursively build a beat string based on the reference index.
        /// </summary>
        /// <param name="reference">Referenced beat index</param>
        /// <param name="visitedIndexes">Holds the previously visited beats</param>
        /// <returns>The replacement string</returns>
        protected string ResolveReferences(int reference, HashSet<int> visitedIndexes = null)
        {
            Metronome met = Metronome.Instance;

            if (visitedIndexes == null) visitedIndexes = new HashSet<int>();

            if (met.Layers.Count == 0) return "1"; // if no layers, emit a dummy value

            if (reference >= met.Layers.Count || reference < 0) reference = 0;

            string refString = met.Layers[reference].ParsedString;
            // remove comments
            refString = Regex.Replace(refString, @"!.*?!", "");
            // remove whitespace
            refString = Regex.Replace(refString, @"\s", "");
            // prep single cell repeats
            refString = Regex.Replace(refString, @"(\$[\ds]+)(\(\d\))", "[$1]$2");

            if (refString.IndexOf('$') > -1 && visitedIndexes.Contains(reference))
            {
                // strip references and their inner nests
                while (refString.Contains('$'))
                {
                    if (Regex.IsMatch(refString, @"[[{][^[{\]}]*\$[^[{\]}]*[\]}][^\]},]*"))
                        refString = Regex.Replace(refString, @"[[{][^[{\]}]*\$[^[{\]}]*[\]}][^\]},]*", "$s");
                    else
                        refString = Regex.Replace(refString, @"\$[\ds]+,?", ""); // straight up replace
                }
                // clean out empty cells
                refString = Regex.Replace(refString, @",,", ",");
                refString = refString.Trim(',');
            }
            else
            {
                // recurse over references of the reference
                visitedIndexes.Add(reference);

                while (refString.IndexOf('$') > -1)
                {
                    int refIndex;
                    var match = Regex.Match(refString, @"\$(\d+|s)");
                    string embedIndex = match.Groups[1].Value;
                    if (embedIndex == "s")
                    {
                        refIndex = reference;
                    }
                    else
                    {
                        refIndex = int.Parse(embedIndex) - 1;
                    }

                    refString = refString.Substring(0, match.Index) +
                        ResolveReferences(refIndex, new HashSet<int>(visitedIndexes)) +
                        refString.Substring(match.Index + match.Length);
                }
            }

            return refString;
        }

        /**<summary>Apply a new base source to the layer.</summary>*/
        public void NewBaseSource(StreamInfoProvider baseSource)
        {
            if (BaseAudioSource != null && Beat != null)
            {
                BaseAudioSource.Dispose();
                Metronome.Instance.RemoveAudioSource(BaseAudioSource);

                PitchSource?.Dispose();
                PitchSource = null;

                IStreamProvider newBaseSource = null;

                var met = Metronome.Instance;
                // is new source a pitch or a wav?
                if (baseSource.IsPitch)
                {
                    // Pitch

                    PitchStream newSource = new PitchStream(baseSource, this)
                    {
                        //BaseFrequency = PitchStream.ConvertFromSymbol(baseSource.Uri),
                        Volume = Volume * met.Volume,
                        Pan = (float)Pan
                    };

                    if (BaseAudioSource.Info.IsPitch)
                    {
                        // we can resuse the old beat collection
                        newSource.IntervalLoop = BaseAudioSource.IntervalLoop;

                        foreach (BeatCell bc in Beat.Where(x => x.AudioSource == BaseAudioSource))
                        {
                            if (bc.SoundSource == null)
                            {
                                newSource.AddFrequency(baseSource.Uri);
                            }
                            else
                            {
                                newSource.AddFrequency(bc.SoundSource.Uri);
                            }

                            bc.AudioSource = newSource;
                        }
                    }
                    else
                    {
                        // old base was a wav, we need to rebuild the beatcollection
                        List<double> beats = new List<double>();
                        double accumulator = 0;
                        int indexOfFirst = Beat.FindIndex(x => x.AudioSource.Info.IsPitch || x.SoundSource == null);

                        if (indexOfFirst > -1)
                        {
                            for (int i = 0; i < Beat.Count; i++)
                            {
                                int index = indexOfFirst + i;
                                if (index >= Beat.Count) index -= Beat.Count;
                                if (Beat[index].AudioSource.Info.IsPitch || Beat[index].SoundSource == null)
                                {
                                    Beat[index].AudioSource = newSource;
                                    newSource.AddFrequency(Beat[index].SoundSource == null ? baseSource.Uri : Beat[index].SoundSource.Uri);
                                    if (i > 0)
                                    {
                                        beats.Add(accumulator);
                                        accumulator = 0;
                                    }
                                }
                                accumulator += Beat[index].Bpm;
                            }
                        }
                        beats.Add(accumulator);
                        var sbc = new SampleIntervalLoop(this, beats.ToArray());
                        newSource.IntervalLoop = sbc;
                    }

                    // Done
                    newBaseSource = newSource;
                    PitchSource = newSource;
                    //IsPitch = true;
                }
                else
                {
                    // Wav
                    WavFileStream newSource = new WavFileStream(baseSource, this)
                    {
                        Volume = Volume * met.Volume,
                        Pan = (float)Pan
                    };

                    foreach (BeatCell bc in Beat.Where(x => x.SoundSource == null))
                    {
                        bc.AudioSource = newSource;
                    }
                    // if this was formerly a pitch layer, we'll need to rebuild the pitch source, freq enumerator and beatCollection
                    // this is because of cells that have a pitch modifier - not using base pitch
                    if (BaseStreamInfo.IsPitch && Beat.Where(x => x.SoundSource != null).Any(x => !char.IsNumber(x.SoundSource.Uri[0])))
                    {
                        // see if we need to make a new pitch source
                        int indexOfFirstPitch = Beat.FindIndex(x => x.AudioSource.Info.IsPitch);
                        List<double> beats = new List<double>();
                        double accumulator = 0;

                        if (indexOfFirstPitch > -1)
                        {
                            // build the new pitch source
                            var newPitchSource = new PitchStream(StreamInfoProvider.GetDefault(), this)
                            {
                                Layer = this,
                                Volume = Volume * met.Volume,
                                Pan = (float)Pan
                            };

                            // build its Beat collection and freq enum
                            for (int i = 0; i < Beat.Count; i++)
                            {
                                int index = indexOfFirstPitch + i;
                                if (index >= Beat.Count) index -= Beat.Count;
                                if (Beat[index].AudioSource.Info.IsPitch)
                                {
                                    Beat[index].AudioSource = newPitchSource;
                                    newPitchSource.AddFrequency(Beat[index].SoundSource.Uri);
                                    if (i > 0)
                                    {
                                        beats.Add(accumulator);
                                        accumulator = 0;
                                    }
                                }
                                accumulator += Beat[index].Bpm;
                            }
                            beats.Add(accumulator);
                            var sbc = new SampleIntervalLoop(this, beats.ToArray());
                            newPitchSource.IntervalLoop = sbc;
                            PitchSource = newPitchSource;
                            // get the offset
                            double pOffset = Beat.TakeWhile(x => x.AudioSource != PitchSource).Select(x => x.Bpm).Sum() + OffsetBpm;
                            //pOffset = met.ConvertBpmToSamples(pOffset);
                            PitchSource.Offset = pOffset;
                            //Metronome.Instance.AddAudioSource(PitchSource);
                        }

                        // build the beatcollection for the new wav base source.
                        beats.Clear();
                        accumulator = 0;
                        int indexOfFirst = Beat.FindIndex(x => x.SoundSource == null);
                        if (indexOfFirst > -1)
                        {
                            for (int i = 0; i < Beat.Count; i++)
                            {
                                int index = indexOfFirst + i;
                                if (index >= Beat.Count) index -= Beat.Count;
                                if (Beat[index].SoundSource == null && i > 0)
                                {
                                    beats.Add(accumulator);
                                    accumulator = 0;
                                }
                                accumulator += Beat[index].Bpm;
                            }
                        }
                        beats.Add(accumulator);
                        var baseSbc = new SampleIntervalLoop(this, beats.ToArray());
                        newSource.IntervalLoop = baseSbc;
                    }
                    else
                    {
                        newSource.IntervalLoop = BaseAudioSource.IntervalLoop;
                        //BaseAudioSource.BeatCollection.isWav = true;
                    }

                    newBaseSource = newSource;
                }

                // update hihat statuses

                BaseSourceName = baseSource.Uri;

                BaseAudioSource = null;
                BaseAudioSource = newBaseSource;

                HasHiHatOpen = GetAllStreams().Where(x => x.Info.HiHatStatus == StreamInfoProvider.HiHatStatuses.Open).Any();
                HasHiHatClosed = GetAllStreams().Where(x => x.Info.HiHatStatus == StreamInfoProvider.HiHatStatuses.Down).Any();

                // set initial offset
                double offset;
                if (!BaseStreamInfo.IsPitch)
                {
                    offset = Beat.TakeWhile(x => x.SoundSource != null).Select(x => x.Bpm).Sum() + OffsetBpm;
                }
                else
                {
                    // find the bpm values of any silent cells at the front
                    offset = Beat.TakeWhile(x => x.SoundSource == StreamInfoProvider.InternalSourceLibrary[0]).Select(x => x.Bpm).Sum() + OffsetBpm;
                }
                //offset = met.ConvertBpmToSamples(offset);
                BaseAudioSource.Offset = offset;

                if (BaseAudioSource.IntervalLoop.Enumerator != null)
                {
                    AddStreamsToMixer();
                }
            }
        }

        /** <summary>Set the base source. Will also set Base pitch if a pitch layer.</summary>
         * <param name="baseSourceName">Name of source to use.</param> */
        public void SetBaseSource(StreamInfoProvider baseSource)
        {
            // is sample or pitch source?
            if (baseSource.IsPitch)
            {
                if (PitchSource == default(PitchStream))
                {
                    PitchSource = new PitchStream(baseSource, this)
                    {
                        Volume = Volume
                    };
                    BaseAudioSource = PitchSource;
                }
                else
                {
                    BaseAudioSource = PitchSource;
                }

                //IsPitch = true;
            }
            else
            {
                if (BaseAudioSource != null)
                {
                    Metronome.Instance.RemoveAudioSource(BaseAudioSource);
                }

                //if (AudioSources.ContainsKey(""))
                //{
                //    Metronome.GetInstance().RemoveAudioSource(AudioSources[""]);
                //    AudioSources.Remove("");
                //}

                BaseAudioSource = new WavFileStream(baseSource, this)
                {
                    Volume = Volume
                };

                //AudioSources.Add("", BaseAudioSource);
                //IsPitch = false;

                HasHiHatClosed = baseSource.HiHatStatus == StreamInfoProvider.HiHatStatuses.Down;
                HasHiHatOpen = baseSource.HiHatStatus == StreamInfoProvider.HiHatStatuses.Open;
                //if (BeatCell.HiHatOpenFileNames.Contains(baseSourceName)) HasHiHatOpen = true;a
                //else if (BeatCell.HiHatClosedFileNames.Contains(baseSourceName)) HasHiHatClosed = true;
            }

            BaseSourceName = baseSource.Uri;

            // reassign source to existing cells that use the base source. base source beats will have an empty string
            if (Beat.Any())
            {
                SetBeat(Beat.ToArray());
            }
        }

        /** <summary>Add array of beat cells and create all audio sources.</summary>
         * <param name="beat">Array of beat cells.</param> */
        public BeatCell[] SetBeat(BeatCell[] beat)
        {
            // deal with the old audio sources.
            if (Beat.Any())
            {
                // dispose wav audio sources if not the base
                foreach (IStreamProvider src in AudioSources.Values)//.Where(x => x != BaseAudioSource))
                {
                    Metronome.Instance.RemoveAudioSource(src);
                    src.Dispose();
                }
                AudioSources.Clear();
                BaseAudioSource.Offset = 0;

                if (BaseStreamInfo.IsPitch) // need to rebuild the pitch source
                {
                    PitchSource?.ClearFrequencies();
                    //BasePitchSource.BaseFrequency = PitchStream.ConvertFromSymbol(BaseSourceName);
                }
                else
                {
                    if (PitchSource != null)
                    {
                        Metronome.Instance.RemoveAudioSource(PitchSource);
                        //PitchSource.Dispose();
                        PitchSource.Dispose();
                        PitchSource = null;
                    }
                }
            }

            // refresh the hashihatxxx bools
            HasHiHatOpen = BaseAudioSource.Info.HiHatStatus == StreamInfoProvider.HiHatStatuses.Open;
            if (!HasHiHatOpen)
                HasHiHatClosed = BaseAudioSource.Info.HiHatStatus == StreamInfoProvider.HiHatStatuses.Down;

            // add the audio streams to each beat cell
            for (int i = 0; i < beat.Count(); i++)
            {
                beat[i].Layer = this;
                if (beat[i].SoundSource != null && !beat[i].SoundSource.IsPitch)// !Regex.IsMatch(beat[i].SourceName, @"^[A-Ga-g][#b]?\d+$|^[Pp][\d.]+$"))
                {
                    // Wavs
                    // should cells of the same source use the same audiosource instead of creating new source each time? Yes
                    if (!AudioSources.ContainsKey(beat[i].SoundSource.Uri))
                    {
                        var wavStream = new WavFileStream(beat[i].SoundSource, this);

                        AudioSources.Add(beat[i].SoundSource.Uri, wavStream);
                    }
                    beat[i].AudioSource = AudioSources[beat[i].SoundSource.Uri];
                    // set hihat status for beat sources
                    if (beat[i].SoundSource.HiHatStatus == StreamInfoProvider.HiHatStatuses.Down) HasHiHatClosed = true;
                    else if (beat[i].SoundSource.HiHatStatus == StreamInfoProvider.HiHatStatuses.Open) HasHiHatOpen = true;
                }
                else
                {
                    if (beat[i].SoundSource != null)
                    {
                        // beat has a defined pitch
                        // check if basepitch source exists
                        if (PitchSource == default(PitchStream))
                        {
                            PitchSource = new PitchStream(beat[i].SoundSource, this)
                            {
                                Volume = Volume
                                //BaseFrequency = PitchStream.ConvertFromSymbol(beat[i].SourceName)
                            };
                        }
                        PitchSource.AddFrequency(beat[i].SoundSource.Uri);
                        beat[i].AudioSource = PitchSource;
                    }
                    else
                    {
                        if (BaseStreamInfo.IsPitch)
                        {
                            // no pitch defined, use base pitch
                            PitchSource.AddFrequency(PitchSource.Info.Uri);
                        }
                        beat[i].AudioSource = BaseAudioSource;
                    }
                }
            }

            Beat = beat.ToList();

            return beat;

            //SetBeatCollectionOnSources();
        }

        /** <summary>Set the beat collections for each sound source.</summary> 
         * <param name="Beat">The cells to process</param>
         */
        public BeatCell[] SetBeatCollectionOnSources(BeatCell[] beat)
        {
            HashSet<IStreamProvider> completed = new HashSet<IStreamProvider>();

            // for each beat, iterate over all beats and build a beat list of values from beats of same source.
            for (int i = 0; i < beat.Length; i++)
            {
                List<double> cells = new List<double>();
                double accumulator = 0;
                // Once per audio source
                if (completed.Contains(beat[i].AudioSource)) continue;
                // if selected beat is not first in cycle, set it's offset
                //if (i != 0)
                //{
                double offsetAccumulate = OffsetBpm;
                for (int p = 0; p < i; p++)
                {
                    offsetAccumulate += beat[p].Bpm;
                }

                beat[i].AudioSource.Offset = offsetAccumulate;
                //}
                // iterate over beats starting with current one. Aggregate with cells that have the same audio source.
                for (int p = i; ; p++)
                {

                    if (p == beat.Length) p = 0;

                    if (beat[p].AudioSource == beat[i].AudioSource)
                    {

                        // add accumulator to previous element in list
                        if (cells.Count != 0)
                        {
                            cells[cells.Count - 1] += accumulator;
                            accumulator = 0f;
                        }
                        cells.Add(beat[p].Bpm);
                    }
                    else accumulator += beat[p].Bpm;

                    // job done if current beat is one before the outer beat.
                    if (p == i - 1 || (i == 0 && p == beat.Length - 1))
                    {
                        cells[cells.Count - 1] += accumulator;
                        break;
                    }
                }
                completed.Add(beat[i].AudioSource);

                beat[i].AudioSource.IntervalLoop = new SampleIntervalLoop(this, cells.ToArray());
            }

            foreach (IStreamProvider source in AudioSources.Values.Concat(new IStreamProvider[] { BaseAudioSource }))
            {
                if (!completed.Contains(source))
                {
                    // remove empty sources (if base source was being used but now it isn't - 1@34,1@34,1 to 1@34,1@34)
                    source.IntervalLoop.Enumerator = null;
                    Metronome.Instance.RemoveAudioSource(source);
                    source.Dispose();
                    continue;
                }
            }

            return beat;
            // re-add all the sources to ensure that hihat srcs are added in the correct order
            //AddStreamsToMixer();
        }

        public void ProcessBeat(string beatcode, HashSet<int> parsedReferencers = null)
        {
            BeatCell[] cells = Parse(beatcode);
            SetBeat(cells);
            SetBeatCollectionOnSources(cells);
            Beat = cells.ToList();

            if (Metronome.Instance.PlayState == Metronome.PlayStates.Stopped)
            {
                AddStreamsToMixer();
            }

            // reparse any layers that reference this one
            Metronome met = Metronome.Instance;
            int index = met.Layers.IndexOf(this);
            if (parsedReferencers == null)
            {
                parsedReferencers = new HashSet<int>();
            }
            parsedReferencers.Add(index);
            var layers = met.Layers.Where(
                x => x != this
                && x.ParsedString.Contains($"${index + 1}")
                && !parsedReferencers.Contains(met.Layers.IndexOf(x)));
            foreach (Layer layer in layers)
            {
                // account for deserializing a beat
                if (layer.Beat != null && layer.Beat.Count > 0)
                {
                    if (met.PlayState == Metronome.PlayStates.Stopped)
                    {
                        layer.ProcessBeat(layer.ParsedString, parsedReferencers);
                    }
                    else
                    {
                        // create dummy layers for refs
                        Layer copy = new Layer(layer.ParsedString, layer.BaseStreamInfo, layer.ParsedOffset, (float)layer.Pan, (float)layer.Volume);

                        met.LayersToChange.Add(
                            met.Layers.IndexOf(layer),
                            copy
                        );
                    }
                }
            }
        }

        /// <summary>
        /// Release resources
        /// </summary>
        public void Cleanup()
        {
            Beat = null;

            Controller = null;
            foreach (IStreamProvider src in GetAllStreams())
            {
                src.Dispose();
            }
        }

        public void Deserialize()
        {
            AudioSources = new Dictionary<string, IStreamProvider>();
            var source = StreamInfoProvider.GetFromUri(BaseSourceName);
            SetBaseSource(source);
            ProcessBeat(ParsedString);
            if (ParsedOffset != string.Empty)
            {
                Offset = ParsedOffset;
            }
            Pan = _pan;
            Volume = _volume;
        }

        #endregion
    }
}
