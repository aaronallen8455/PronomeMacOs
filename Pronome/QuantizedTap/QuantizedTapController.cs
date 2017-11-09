// This file has been autogenerated from a class added in the UI designer.

using System;

using Foundation;
using AppKit;
using System.Collections.Generic;
using System.Linq;
using Pronome.Mac.Editor;
using Pronome.Mac.Editor.Groups;

namespace Pronome.Mac
{
    public partial class QuantizedTapController : NSViewController
    {
        #region Computed Fields
        private string _quantizationIntervalString;
        /// <summary>
        /// The string enumerating the intervals to be used in quantization
        /// </summary>
        /// <value>The quantization interval string.</value>
        [Export("QuantizationIntervalString")]
        public string QuantizationIntervalString
        {
            get => _quantizationIntervalString;
            set
            {
                WillChangeValue("QuantizationIntervalString");
                // break into chunks
                string[] chunks = value.Split(',');

                //bool isValid = true;

                LinkedList<string> newIntervals = new LinkedList<string>();
                HashSet<double> touchedVals = new HashSet<double>();
                // check that each value is valid
                foreach (string chunk in chunks)
                {
                    if (BeatCell.TryParse(chunk, out double val) && !touchedVals.Contains(val))
                    {
                        //newIntervals.AddLast(val);
                        newIntervals.AddLast(BeatCell.SimplifyValue(chunk));
                        touchedVals.Add(val);
                    }
                }

                if (newIntervals.Any())//isValid)
                {
                    // every value is valid
                    QuantizeIntervals = newIntervals;
                    _quantizationIntervalString = value;
                }

                DidChangeValue("QuantizationIntervalString");
            }
        }

        private Layer _layer;
        /// <summary>
        /// The target layer
        /// </summary>
        /// <value>The layer.</value>
        [Export("Layer")]
        public Layer Layer
        {
            get => _layer;
            set
            {
                WillChangeValue("Layer");
                _layer = value;
                DidChangeValue("Layer");
            }
        }

        private NSMutableArray<Layer> _layerArray;
        [Export("layerArray")]
        public NSMutableArray LayerArray
        {
            get => _layerArray;
        }

        [Export("Metronome")]
        public Metronome Met
        {
            get => Metronome.Instance;
        }

        private bool _isListening = false;
        /// <summary>
        /// True if taps are being registered.
        /// </summary>
        /// <value><c>true</c> if is listening; otherwise, <c>false</c>.</value>
        [Export("IsListening")]
        public bool IsListening
        {
            get => _isListening;
            set
            {
                WillChangeValue("IsListening");
                _isListening = value;
                DidChangeValue("IsListening");
            }
        }

        private bool _countOff;
        /// <summary>
        /// True if a countoff will play if begining from stopped state
        /// </summary>
        /// <value><c>true</c> if count off; otherwise, <c>false</c>.</value>
        [Export("CountOff")]
        public bool CountOff
        {
            get => _countOff;
            set
            {
                WillChangeValue("CountOff");
                _countOff = value;
                Metronome.Instance.Mixer.SetCountOff(value);
                DidChangeValue("CountOff");
            }
        }

        [Export("IsInsert")]
        public bool IsInsert
        {
            get => ModeDropdown.SelectedTag == MODE_INSERT;
        }

        private nint _modeSelection = 0;
        [Export("ModeSelection")]
        public nint ModeSelection
        {
            get => _modeSelection;
            set
            {
                WillChangeValue("ModeSelection");
                WillChangeValue("IsInsert");
                _modeSelection = value;
                DidChangeValue("ModeSelection");
                DidChangeValue("IsInsert");
            }
        }
        #endregion

        #region Public fields
        public NSViewController Presentor;
        #endregion

        #region Protected Variables
        /// <summary>
        /// The BPM position of each tap captured
        /// </summary>
        protected LinkedList<double> Taps = new LinkedList<double>();

        // TODO: should be a user setting
        /// <summary>
        /// The intervals to check against when quantizing
        /// </summary>
        protected LinkedList<string> QuantizeIntervals = new LinkedList<string>();

        const int MODE_OVERWRITE = 0;

        const int MODE_INSERT = 1;
        #endregion

        #region Constructor
        public QuantizedTapController(IntPtr handle) : base(handle)
        {
            _layerArray = new NSMutableArray<Layer>(Metronome.Instance.Layers.ToArray());
        }
        #endregion

        #region Protected Methods
        protected void Close()
        {
            IsListening = false;
            Metronome.Instance.Mixer.SetCountOff(false);
            Presentor.DismissViewController(this);
        }
        #endregion

        #region Overridden Methods

        public override void KeyDown(NSEvent theEvent)
        {
            if (IsListening)
            {
                // check if we are in the count off
                if (Metronome.Instance.Mixer.CountOffSampleDuration > 0)
                {
                    Taps.AddLast(0);
                }
                else
                {
					Taps.AddLast(Metronome.Instance.ElapsedBpm);
                }
            }
        }

        partial void BeginAction(NSObject sender)
        {
            IsListening = true;
            // lose focus for any inputs
            View.Window.MakeFirstResponder(this);

            // start playing the beat if it isn't already (plus count-down)
            if (Metronome.Instance.PlayState != Metronome.PlayStates.Playing)
            {
				TransportViewController.Instance.Play();

            }
        }

        // if started listening when already playing,
        // find modulo of tapped entry length against
        // elapsed time of first tap
        // we use this to find the first part,
        // which will be a portion of the tap cycle
        // that may involve an offset

        /// <summary>
        /// Inserts the tapped cells into the layer
        /// </summary>
        /// <param name="sender">Sender.</param>
        partial void DoneAction(NSObject sender)
        {
            if (QuantizeIntervals.Count == 0 || !Metronome.Instance.IsPlaying)
            {
                Close();
                return;
            }

            string beatCode = "";
            string offset = "";

            if (ModeDropdown.SelectedTag == MODE_OVERWRITE)
            {
				if (Taps.Count <= 1) 
				{
                    // need at least 2 to define a cell
                    Close();
					return;
				}
				
				LinkedList<string> cellDurs = new LinkedList<string>();
				string last = "0";
				
				// get the quantized values
				foreach (string t in Taps.Select(x => Quantize(x)))
				{
					if (t == last) continue;
					cellDurs.AddLast(BeatCell.Subtract(t, last));
					last = t;
				}
				
				// determine the offset
				string length = BeatCell.Subtract(last, cellDurs.First.Value);
				long cycles = (long)(BeatCell.Parse(cellDurs.First()) / BeatCell.Parse(length));
				// remove extraneous cycles
				cellDurs.First.Value = BeatCell.Subtract(cellDurs.First(), BeatCell.MultiplyTerms(length, cycles));
				
				// rotate until offset is found
				offset = cellDurs.First();
				cellDurs.RemoveFirst();
				
				while (BeatCell.Parse(offset) >= BeatCell.Parse(cellDurs.Last.Value))
				{
					offset = BeatCell.Subtract(offset, cellDurs.Last.Value);
					// rotate
					cellDurs.AddFirst(cellDurs.Last.Value);
					cellDurs.RemoveLast();
				}
				
				// modify the layer
				beatCode = string.Join(",", cellDurs);
                // don't allow empty cells
                beatCode = beatCode.Replace(",,", ",").Trim(',');
            }
            else if (ModeDropdown.SelectedTag == MODE_INSERT)
            {
                // how to deal with inserting cells into a rep group?
                // easy way is to just flatten it out, use raw values from .Beat
                // ideally we want to insert the cell and retain the original beatCode aspects

                // retain a int for the current cell index in the beatcode
                // so when we run a rep group, the index will backtrack as needed

                // will also need to deal with mult groups
                // when accumulating cell values, we 'expand' out the multgroups
                // so that we are dealing with actual values
                // after all work is done, we 'contract' the multgroups

                // could use the modeling objects from the editor?

                // could reuse the editor insert cell operation
                // only difference is that if we insert a cell into a rep group,
                // theres some special cases:
                // 1) if inserting into first or last group of a rep with >2 times,
                // we break off the cycle being inserted into and decrement
                // the times of the group
                // 2) if the group has times == 2 then the group is de-sugared.
                // this entails adding the LTM to the last cell
                // 3) if the same insertion is made on each repeat, we can use the
                // default behaviour of the editor insert action.

                // if a tap occurs during a reference, we desugar the reference

                // get the objects representing the beatcode
                Row row = new Row(Layer)
                {
                    Offset = Layer.OffsetBpm,
                    OffsetValue = Layer.Offset
                };

                // get the layers total length
                double bpmLength = Layer.GetTotalBpmValue();

                //string offset = "";

                foreach (double t in Taps)
                {
                    // check if tap was done in the offset area
                    if (t <= Layer.OffsetBpm)
                    {
                        offset = Quantize(t);

                        string cellValue = BeatCell.SimplifyValue(BeatCell.Subtract(Layer.ParsedOffset, offset));

                        if (cellValue != "" && cellValue != "0")
                        {
                            // insert cell at start of beat and change the offset
                            Cell cell = new Cell(row)
                            {
                                Value = cellValue,
                                Duration = BeatCell.Parse(cellValue),
                                Position = 0
                            };

                            // reposition all other cells
                            foreach (Cell c in row.Cells)
                            {
                                c.Position += cell.Duration;
                            }

                            if (row.Cells.Insert(cell))
                            {
                                Layer.ParsedOffset = BeatCell.SimplifyValue(offset);
                                Layer.OffsetBpm = BeatCell.Parse(offset);
                            }
                        }

                        continue;
                    }

                    // get the number of elapsed cycles
                    int cycles = (int)((t - Layer.OffsetBpm) / bpmLength);

                    // subtract the elapsed cycles
                    double pos = (t - Layer.OffsetBpm) - cycles * bpmLength;
                    string belowValue = Quantize(pos);
                    double qPos = BeatCell.Parse(belowValue); // quantized BPM position double
                    double newCellPosition = qPos;

                    // going to iterate over all the cells
                    CellTreeNode cellNode = row.Cells.Min;

                    // rep groups that have been traversed and should'nt be touched again
                    HashSet<Repeat> touchedReps = new HashSet<Repeat>();

					Repeat repWithLtmToInsertInto = null;

                    // the nested repeat groups paired with the number of the repeat in which to insert
                    Dictionary<Repeat, int> repToInsertInto = new Dictionary<Repeat, int>();

                    LinkedList<Repeat> openRepGroups = new LinkedList<Repeat>();

                    int completeReps = 0; // the times run due to values being subtracted at each step

                    while (cellNode != null)
                    {
                        Cell c = cellNode.Cell;
                        // will need to desugar if it's a ref

                        if (c.RepeatGroups.Any())
                        {
                            //bool repGroup

							foreach (Repeat rep in c.RepeatGroups.Where(x => !touchedReps.Contains(x)))
							{
                                
                                // see if the total duration of this rep group is shorter than tap position
                                // then we know that we will be inserting into this rep group at one of it's times. need to know which one.
                                // rep.Length does not include the times, it's only one cycle
                                if (qPos < rep.Position + rep.Length * rep.Times * (completeReps + 1))
                                {
                                    // find the cycle on which the tap is placed
                                    int times = (int)((qPos - rep.Times * rep.Length * completeReps) / rep.Length);

                                    repToInsertInto.Add(rep, times);

									completeReps *= rep.Times;
									completeReps += times;
								}
								else
								{
                                    completeReps *= rep.Times;
								}

                                int reps = (repToInsertInto.ContainsKey(rep) ? completeReps : completeReps - 1);// - collateralRuns.Peek();

                                // subtract out all the complete reps of this group, except for very last time, which is covered by the cell iteration
                                foreach (Cell ce in rep.ExclusiveCells)
                                {
                                    qPos -= ce.Duration * reps;
                                    belowValue = BeatCell.Subtract(belowValue, BeatCell.MultiplyTerms(ce.GetValueWithMultFactors(), reps));
                                }

                                openRepGroups.AddLast(rep);
								touchedReps.Add(rep);
							}

							// close any open groups that have ended
                            while (openRepGroups.Any() && c.GroupActions.Contains((false, openRepGroups.Last.Value)))
							{
								Repeat last = openRepGroups.Last();
								openRepGroups.RemoveLast();
								// factor out from global reps times
								if (openRepGroups.Any())
								{
									completeReps /= last.Times;
								}
								else
								{
									completeReps = 0;
								}
								
                                if (qPos < BeatCell.Parse(last.GetLtmWithMultFactor(true)))
                                {
                                    // should be done with the tap at this point.
                                    repWithLtmToInsertInto = last;
                                    break;

                                }
                                else
                                {
									// subtract the ltm
									string ltm = last.GetLtmWithMultFactor(true);
									qPos -= BeatCell.Parse(ltm);
									belowValue = BeatCell.Subtract(belowValue, ltm);
                                }
							}

                        }

                        // check if this is the cell that will be above the tap
                        if (qPos < c.Duration)
                        {
                            break;
                        }
                        else if (repWithLtmToInsertInto != null)
                        {
                            break;
                        }

                        // subtract the cell value
                        qPos -= c.Duration;
                        belowValue = BeatCell.Subtract(belowValue, c.GetValueWithMultFactors());

                        cellNode = cellNode.Next();
                    }

                    if (!string.IsNullOrEmpty(belowValue))
                    {
                        foreach (var pair in repToInsertInto)
                        {
                            // make the two copies of the first nested rep
                            // we then recurse into the next nested rep
                            // until we reach the rep where the new cell will
                            // exist, then we're done
                            Repeat actual = pair.Key;
                            Repeat before = actual.DeepCopy() as Repeat;
                            Repeat after = actual.DeepCopy() as Repeat;

                            //before.Length *= (double)pair.Value / before.Times;
                            before.Times = pair.Value;

                            //after.Length *= (double)(actual.Times - pair.Value - 1) / after.Times;
                            after.Times = actual.Times - pair.Value - 1;

                            //actual.Length *= 1d / actual.Times;
                            actual.Times = 1;

                            // only the after-copy should have the LTM, or the
                            // actual one if the after copy has 0 times
                            before.LastTermModifier = "";
                            if (after.Times > 0)
                            {
                                pair.Key.LastTermModifier = "";
                            }

                            // get rid of actual group
                            actual.Cells.First.Value.GroupActions.Remove((true, actual));
                            actual.Cells.Last.Value.GroupActions.Remove((false, actual));
                            foreach (Cell c in actual.Cells)
                            {
                                c.RepeatGroups.Remove(actual);
                            }

                            // get rid of group if the times is 1
                            if (before.Times == 1)
                            {
                                before.Cells.First().GroupActions.RemoveFirst();
                                before.Cells.Last().GroupActions.RemoveLast();
                                foreach (Cell c in before.Cells)
                                {
                                    c.RepeatGroups.Remove(before);
                                }
                            }
                            if (after.Times == 1)
                            {
                                after.Cells.First().GroupActions.RemoveFirst();
                                after.Cells.Last().GroupActions.RemoveLast();
                                foreach (Cell c in after.Cells)
                                {
                                    c.RepeatGroups.Remove(after);
                                }
                            }

                            // if the before-copy isn't nulled, and the first cell of
                            // this inner nested rep group is also the first cell of it's
                            // containing group, then we need to transfer ownership of the
                            // groupAction to the before-copy. And likewise with the 
                            // after-copy
                            if (before.Times > 0)
                            {
                                var fstCell = actual.Cells.First();
                                var gAction = fstCell.GroupActions.First;
                                var actionsToPrepend = new LinkedList<(bool, AbstractGroup)>();
                                // grab all groups that need to be transfered
                                while (gAction != null && gAction.Value.Item2 != actual)
                                {
                                    actionsToPrepend.AddLast(gAction.Value);
                                    gAction = gAction.Next;
                                    fstCell.GroupActions.RemoveFirst();
                                }
                                // transfer the groups
                                before.Cells.First().GroupActions = new LinkedList<(bool, AbstractGroup)>(actionsToPrepend.Concat(before.Cells.First().GroupActions));
                            }
                            // copy actions from last cell of actual group to the after-copy
                            if (after.Times > 0)
                            {
                                var lstCell = actual.Cells.Last();
                                var gAction = lstCell.GroupActions.Last;
                                var actionsToAppend = new LinkedList<(bool, AbstractGroup)>();

                                while (gAction != null && gAction.Value.Item2 != actual)
                                {
                                    actionsToAppend.AddFirst(gAction.Value);
                                    gAction = gAction.Previous;
                                    lstCell.GroupActions.RemoveLast();
                                }
                                after.Cells.Last().GroupActions = new LinkedList<(bool, AbstractGroup)>(after.Cells.Last().GroupActions.Concat(actionsToAppend));
                            }

                            // reposition the actual group and the after-copy
                            double curOffset = before.Length * before.Times;
                            if (before.Times > 0)
                            {
                                foreach (Cell c in actual.Cells)
                                {
                                    c.Position += curOffset;
                                    foreach (var action in c.GroupActions)
                                    {
                                        if (action.Item1)
                                            action.Item2.Position += curOffset;
                                    }
                                }
                            }

                            curOffset += actual.Length;
                            if (after.Times > 0)
                            {
                                foreach (Cell c in after.Cells)
                                {
                                    c.Position += curOffset;
                                    foreach (var action in c.GroupActions)
                                    {
                                        if (action.Item1)
                                            action.Item2.Position += curOffset;
                                    }
                                }
                            }

                            // add the copies to the row
                            if (before.Times > 0)
                            {
                                foreach (Cell c in before.Cells)
                                {
                                    row.Cells.Insert(c);
                                }
                            }
                            if (after.Times > 0)
                            {
                                foreach (Cell c in after.Cells)
                                {
                                    row.Cells.Insert(c);
                                }
                            }
                        }

                        Cell newCell = new Cell(row)
                        {
                            Position = newCellPosition,
                            Source = row.Layer.BaseStreamInfo
                        };

                        // add cell to row
                        if (row.Cells.Insert(newCell))
                        {
                            // add cell to repeat groups
                            Repeat lastRep = null;
                            foreach (var pair in repToInsertInto)
                            {
                                pair.Key.Cells.AddFirst(newCell);
                                lastRep = pair.Key;
                            }
                            if (lastRep != null)
                            {
                                // transfer the group action if it's the last cell
                                Cell lastCell = lastRep.Cells.Last.Value;
                                if (lastCell == cellNode.Cell)
                                {
                                    newCell.GroupActions = new LinkedList<(bool, AbstractGroup)>(lastCell.GroupActions.Where(x => !x.Item1));
                                    lastCell.GroupActions = new LinkedList<(bool, AbstractGroup)>(lastCell.GroupActions.Where(x => x.Item1));
                                    // move new cell to the back
                                    lastRep.Cells.RemoveFirst();
                                    lastRep.Cells.AddLast(newCell);
                                }
                                // it's exclusive for the last rep group
                                lastRep.ExclusiveCells.AddLast(newCell);
                            }

                            // add cell to mult groups
                            Multiply lastMult = null;
                            foreach (Multiply mult in cellNode.Cell.MultGroups)
                            {
                                mult.Cells.AddLast(newCell);
                                lastMult = mult;
                            }
                            if (lastMult != null)
                            {
                                // transfer group actions if it's the new last cell
                                Cell lastCell = lastMult.Cells.Last.Value;
                                if (lastCell == cellNode.Cell)
                                {
                                    newCell.GroupActions = new LinkedList<(bool, AbstractGroup)>(lastCell.GroupActions.Where(x => !x.Item1));
                                    lastCell.GroupActions = new LinkedList<(bool, AbstractGroup)>(lastCell.GroupActions.Where(x => x.Item1));
                                    // move new cell to the back
                                    lastMult.Cells.RemoveFirst();
                                    lastMult.Cells.AddLast(newCell);
                                }
                                // will have the same multfactor as former last cell
                                newCell.MultFactor = lastCell.MultFactor;
                                lastMult.ExclusiveCells.AddLast(newCell);
                            }

                            if (repWithLtmToInsertInto == null)
                            {
                                // insert new cell
                                Cell below = cellNode.Cell;

                                string newCellValue = BeatCell.Subtract(below.GetValueWithMultFactors(true), belowValue);

                                newCell.Duration = below.Duration - qPos;

                                newCell.Value = BeatCell.SimplifyValue(newCell.GetValueDividedByMultFactors(newCellValue, true));
                                // modify below cell
                                below.Value = BeatCell.SimplifyValue(below.GetValueDividedByMultFactors(belowValue, true));
                                below.ResetMultipliedValue();
                                below.Duration = qPos;
                            }
                            else
                            {
                                string newCellValue = BeatCell.Subtract(repWithLtmToInsertInto.GetLtmWithMultFactor(true), belowValue);
                                // insert into LTM
                                newCell.Duration = BeatCell.Parse(repWithLtmToInsertInto.LastTermModifier) - qPos;

                                newCell.Value = BeatCell.SimplifyValue(repWithLtmToInsertInto.GetValueDividedByMultFactor(newCellValue, true));
                                // modify the rep group
                                repWithLtmToInsertInto.LastTermModifier = BeatCell.SimplifyValue(repWithLtmToInsertInto.GetValueDividedByMultFactor(belowValue, true));
                                repWithLtmToInsertInto.ResetMultedLtm();

                            }
                        }
                    }
                    else
                    {
                        // tap value was a duplicate, don't alter beatcode
                        continue;
                    }
                }

                beatCode = row.Stringify();
                offset = row.OffsetValue;
            }

            Layer.SetBeatCode(beatCode, offset == string.Empty ? "0" : offset);

            Layer.Controller.HighlightBeatCodeSyntax();

            Close();
        }

        #endregion

        #region Protected Methods
        protected string Quantize(double value)
        {
            // use doubles to determine which interval is the match
            //double dval = BeatCell.Parse(value);

            var qs = QuantizeIntervals
                .Select(x => (x, BeatCell.Parse(x)))
                .SelectMany(x => { 
                int div = (int)(value / x.Item2); 
                return new[] { (x.Item1, div * x.Item2, div), (x.Item1, (div + 1) * x.Item2, div + 1) }; 
            });

            string r = "";
            double diff = double.MaxValue;

            foreach ((string interval, double total, int div) in qs)
            {
                double d = Math.Abs(value - total);
                if (d < diff)
                {
                    r = BeatCell.MultiplyTerms(interval, div);
                    diff = d;
                }
            }

            return r;
        }

        #endregion
    }
}
