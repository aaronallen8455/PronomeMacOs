﻿using System;
using System.Text;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;

namespace Pronome.Mac
{
    public class BeatCell
    {
		#region Public Variables
		/**<summary>The time length in quarter notes</summary>*/
		public double Bpm; // value expressed in BPM time.

		///**<summary>Holds info about the sound source.</summary>*/
        public StreamInfoProvider SoundSource;

		/**<summary>The layer that this cell belongs to.</summary>*/
		public Layer Layer;

		/**<summary>The audio source used for this cell.</summary>*/
		public IStreamProvider AudioSource;
        #endregion

        #region Public Properties
        /// <summary>
        /// Gets the stream info from this cell, or from it's layer if not specified.
        /// </summary>
        /// <value>The stream info.</value>
        public StreamInfoProvider StreamInfo
        {
            get => SoundSource ?? Layer.BaseStreamInfo;
        }
        #endregion

        #region Constructors
        public BeatCell(string beat, Layer layer, StreamInfoProvider soundSource)
        {
            Layer = layer;
            SoundSource = soundSource;
            Bpm = Parse(beat);
        }
        #endregion

        #region Static Methods
        /// <summary>
        /// Parse the specified cell str into a BPM value.
        /// </summary>
        /// <returns>The parse.</returns>
        /// <param name="str">String.</param>
        static public double Parse(string str)
        {
            if (string.IsNullOrEmpty(str)) return 0;

			// holds value that will be added to current value after all mult and div have occurred
			double accum = 0;
            // builds up a numeric value from individual chars
			StringBuilder curNum = new StringBuilder();
            // the most recently converted (from chars) number and preceding mult/div operations
			double focusedNum = 0;
			char curOp = ' ';
            // true if accum will be added to focusedNum
			bool willAdd = true;
			foreach (char c in str)
			{
				if (char.IsNumber(c) || c == '.')
				{
					curNum.Append(c);
				}
				else
				{
					double num = double.Parse(curNum.ToString());

                    // perform * / on focused num
					if (curOp == '*')
					{
						focusedNum *= num;
					}
					else if (curOp == '/')
					{
						focusedNum /= num;
					}
					else
					{
                        // last op was + or -
						focusedNum = num;
					}

					curNum.Clear();

					switch (c)
					{
						case '-':
						case '+':
                            // we can now apply the waiting +/- operation
							if (willAdd)
							{
								accum += focusedNum;
							}
							else
							{
								accum -= focusedNum;
							}

							willAdd = c == '+';
							curOp = c;
							focusedNum = accum;
							break;
						case 'X':
						case 'x':
						case '*':
							curOp = '*';
							break;
						case '/':
							curOp = '/';
							break;
					}
				}
			}

            // tie up the loose ends

			double lastNum = double.Parse(curNum.ToString());

			switch (curOp)
			{
				case '+':
					accum += lastNum;
					break;
				case '-':
					accum -= lastNum;
					break;
				case '*':
					focusedNum *= lastNum;

					if (willAdd)
					{
						accum += focusedNum;
					}
					else
					{
						accum -= focusedNum;
					}
					break;
				case '/':
					focusedNum /= lastNum;

					if (willAdd)
					{
						accum += focusedNum;
					}
					else
					{
						accum -= focusedNum;
					}
					break;
			}

			return accum;

            //StringBuilder ops = new StringBuilder();
            //string operators = "";
            //StringBuilder number = new StringBuilder();
            //List<double> numbers = new List<double>();
            //// parse out the numbers and operators
            //for (int i = 0; i < str.Length; i++)
            //{
            //    if (str[i] == '+' || str[i] == '*' || str[i] == '/' || (str[i] == '-' && (str[i - 1] != '*' || str[i - 1] != '/')))
            //    {
            //        numbers.Add(double.Parse(number.ToString()));
            //        number.Clear();
            //        ops.Append(str[i]);
            //    }
            //    else if (str[i] == 'x' || str[i] == 'X')
            //    {
            //        ops.Append('*');
            //    }
            //    else
            //    {
            //        number.Append(str[i]);
            //    }
            //}
            //numbers.Add(double.Parse(number.ToString()));
            //operators = ops.ToString();
			//
            //double result = 0;
            //double current = numbers[0];
            //char connector = '+';
            //// perform arithmetic
            //for (int i = 0; i < operators.Length; i++)
            //{
            //    char op = operators[i];
            //    if (op == '*')
            //    {
            //        current *= numbers[i + 1];
            //    }
            //    else if (op == '/')
            //    {
            //        current /= numbers[i + 1];
            //    }
            //    else
            //    {
            //        if (connector == '+')
            //        {
            //            result += current;
            //        }
            //        else if (connector == '-')
            //        {
            //            result -= current;
            //        }
            //        //result += current;
            //        if (i < operators.Length - 1)
            //        {
			//
            //            if (operators[i + 1] == '+' || operators[i + 1] == '-')
            //            {
            //                current = 0;
			//
            //                if (op == '+')
            //                {
            //                    result += numbers[i + 1];
            //                }
            //                else
            //                {
            //                    result -= numbers[i + 1];
            //                }
            //            }
            //            else
            //            {
            //                connector = op;
            //                current = numbers[i + 1];
            //            }
            //        }
            //        else
            //        {
            //            current = 0;
			//
            //            if (op == '+')
            //            {
            //                result += numbers[i + 1];
            //            }
            //            else
            //            {
            //                result -= numbers[i + 1];
            //            }
            //        }
            //    }
            //}
            //result = connector == '+' ? result + current : result - current;
			//
            //return result;
        }

        /// <summary>
        /// Validate a beat code expression.
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        static public bool ValidateExpression(string str)
        {
            return Regex.IsMatch(str, @"(\d+\.?\d*[\-+*/xX]?)*\d+\.?\d*$");
        }

        /// <summary>
        /// Parse a math expression after testing validity
        /// </summary>
        /// <param name="str">String to parse</param>
        /// <param name="val">Output value</param>
        /// <returns></returns>
        static public bool TryParse(string str, out double val)
        {
            if (ValidateExpression(str))
            {
                val = Parse(str);

                return true;
            }
            val = 0;
            return false;
        }

        /// <summary>
        /// Simplify an expression so that fractions/decimals are combined.
        /// </summary>
        /// <param name="value">Ex. 1/3+1/6 = 1/2</param>
        /// <returns></returns>
        public static string SimplifyValue(string value)
        {
            value = value.Replace('x', '*').Replace('X', '*'); // replace multiply symbol

            // merge all fractions based on least common denominator
            int lcd = 1;

            Func<double, double, double> Gcf = null;
            Gcf = delegate (double x, double y)
            {
                double r = x % y;
                if (Math.Round(r, 5) == 0) return y;

                return Gcf(y, r);
            };

            Func<double, double, double> Lcm = delegate (double x, double y)
            {
                return x * y / Gcf(x, y);
            };

            //// replace common decimals with fraction if other fractions coexist in the string
            //if (value.IndexOf('/') > -1)
            //{
            //    value = ConvertCommonFractions(value);
            //}

            // simplify all fractions
            var multDiv = Regex.Matches(value, @"(?<!\.\d*)(\d+?[*/](?=\d+))+\d+(?!\d*\.)"); // get just the fractions or whole number multiplication. filter out decimals
            foreach (Match m in multDiv)
            {
                int n = 1; // numerator
                int d = 1; // denominator
                foreach (Match num in Regex.Matches(m.Value, @"(?<=\*)\d+"))
                {
                    n *= int.Parse(num.Value);
                }
                n *= int.Parse(Regex.Match(m.Value, @"\d+").Value);
                foreach (Match num in Regex.Matches(m.Value, @"(?<=/)\d+"))
                {
                    d *= int.Parse(num.Value);
                }

                int gcf = (int)Gcf(n, d);
                n /= gcf;
                d /= gcf;
                // replace with simplified fraction
                int index = value.IndexOf(m.Value, StringComparison.InvariantCulture);
                value = value.Substring(0, index) + n.ToString() + '/' + d.ToString() + value.Substring(index + m.Length);
            }

            var denoms = Regex.Matches(value, @"(?<=/)\d+(?!\d*\.)");

            foreach (Match m in denoms)
            {
                int d = int.Parse(m.Value);
                if (lcd == 1)
                {
                    lcd = d;
                }
                else
                {
                    lcd = (int)Lcm(lcd, d);
                }
            }

            // aggregate the fractions
            var fracs = Regex.Matches(value, @"(?<!\.\d*)(\+|-|^)(\d+/\d+)(?!\d*\.)");
            int numerator = 0;
            foreach (Match m in fracs)
            {
                int[] fraction = Array.ConvertAll(m.Groups[2].Value.Split('/'), Convert.ToInt32);
                int num = fraction[0]; // numerator
                int den = fraction[1]; // denominator

                int ratio = lcd / den;
                string sign = m.Groups[1].Value;
                numerator += num * ratio * (sign == "-" ? -1 : 1);

                // remove all individual fraction to be replaced by 1 big fraction
                int index = value.IndexOf(m.Value, StringComparison.InvariantCulture);
                value = value.Remove(index, m.Length);
            }
            //value = numerator.ToString() + '/' + lcd.ToString() + value;
            int whole = numerator / lcd;
            numerator -= whole * lcd;
            // if the fraction is negative, subtract it from the whole number
            if (numerator < 0)
            {
                numerator += lcd;
                whole--;
            }

            if (lcd > 1)
            {
                // simplify the fraction
                int gcf = (int)Gcf(numerator, lcd);
                numerator /= gcf;
                lcd /= gcf;
            }

            string fractionPart = numerator.ToString() + '/' + lcd.ToString();

            // merge all whole numbers and decimals
            double numbers = whole + Parse("0+0" + value);// 0;

            string result = numbers != 0 ? numbers.ToString().TrimStart('0') : "";

            bool recurse = false;
            // append the fractional portion of it's not zero
            if (numerator != 0)
            {
                // check if 'numbers' has some decimal that can become a fraction. If so, we'll recurse
                string r = ConvertCommonFractions(result);
                if (!string.IsNullOrEmpty(r))
                {
                    recurse = true;
                    result = r;
                }

                // put the positive value first.
                if (numbers < 0)
                {
                    result = fractionPart + result; // if both are negative, something went horribly wrong.
                }
                else
                {
                    if (numbers != 0)
                    {
                        result += fractionPart[0] != '-' ? "+" : "";
                    }
                    result += fractionPart;
                }
            }

            // if there's more than one fraction, recurse
            if (recurse)
            {
                return SimplifyValue(result);
            }

            return result;
        }

        /// <summary>
        /// Replace decimals with corresponding fractions if common.
        /// </summary>
        /// <param name="input"></param>
        /// <returns>String or Null</returns>
        static protected string ConvertCommonFractions(string input)
        {
            bool changed = false;

            foreach (Match m in Regex.Matches(input, @"(^|\+|-)(\d*)\.(\d+)(?=$|\+|-)"))
            {
                string sign = m.Groups[1].Value;
                string bridge = sign == "" ? "+" : sign;
                string wholeNum = m.Groups[2].Value;
                string fract = m.Groups[3].Value;
                // switch out decimal with fraction if matches
                bool switched = true;
                switch (fract)
                {
                    case "5":
                        fract = "1/2";
                        break;
                    case "25":
                        fract = "1/4";
                        break;
                    case "75":
                        fract = "3/4";
                        break;
                    default:
                        switched = false;
                        break;
                }

                if (switched)
                {
                    changed = true;
                    StringBuilder replace = new StringBuilder();
                    replace.Append(sign);
                    if (!string.IsNullOrEmpty(wholeNum))
                    {
                        replace.Append(wholeNum).Append(bridge);
                    }
                    replace.Append(fract);
                    // replace
                    int index = input.IndexOf(m.Value, StringComparison.InvariantCulture);
                    input = input.Substring(0, index) + replace.ToString() + input.Substring(index + m.Length);
                }
            }

            if (changed)
            {
                return input;
            }

            return null; // no change
        }

        /// <summary>
        /// Multiply all terms in an expression by the factor
        /// </summary>
        /// <param name="exp"></param>
        /// <param name="factor"></param>
        /// <returns></returns>
        static public string MultiplyTerms(string exp, double factor)
        {
            if (string.IsNullOrEmpty(exp)) return "";

            string[] terms = Regex.Split(exp, @"(?<!^)(?=[+\-])");
            // multiply each seperate term by the factor
            for (int i = 0; i < terms.Length; i++)
            {
                terms[i] += '*' + factor.ToString();
            }

            return string.Join(string.Empty, terms);
        }

        static public string MultiplyTerms(string exp, string factor)
        {
            StringBuilder val = new StringBuilder('0');

            string pattern = @"(^|[+\-])[^+\-]+";

            var fmatches = Regex.Matches(factor, pattern);

            foreach (Match em in Regex.Matches(exp, pattern))
            {
                foreach (Match fm in fmatches)
                {
                    // both negative, make positive
                    if (em.Value[0] == '-' && fm.Value[0] == '-')
                    {
                        val.Append('+').Append(em.Value.Skip(1)).Append('*').Append(fm.Value.Skip(1));
                    }
                    else if (em.Value[0] != '-' && fm.Value[0] != '-')
                    {
                        // both positive
                        val.Append('+').Append(em.Value.TrimStart('+')).Append('*').Append(fm.Value.TrimStart('+'));
                    }
                    else if (em.Value[0] != '-')
                    {
                        // factor is negative
                        val.Append(fm.Value).Append('*').Append(em.Value.TrimStart('+'));
                    }
                    else
                    {
                        // exp is negative
                        val.Append(em.Value).Append('*').Append(fm.Value.TrimStart('+'));
                    }
                }
            }

            return val.ToString();
        }

        /// <summary>
        /// Divides the terms.
        /// </summary>
        /// <returns>The terms.</returns>
        /// <param name="exp">Exp.</param>
        /// <param name="div">Div.</param>
        static public string DivideTerms (string exp, string div)
        {
            StringBuilder val = new StringBuilder('0');

            // need to consolidate the divisor into a single decimal or fraction
            string simpleDiv = SimplifyValue(div);
            // if it's a mixed number, make it a single fraction
            bool divIsFrac = simpleDiv.Contains('/');
            if (divIsFrac && simpleDiv.Contains('+'))
            {
                string[] split = simpleDiv.Split('+');
                string[] fracSplit = split[1].Split('/');
                string numerator = Add(fracSplit[0], MultiplyTerms(split[0], fracSplit[1]));
                // reverse the numerator and denominator b/c we want to multiply with the exp
                simpleDiv =  fracSplit[1] + '/' + numerator;
            }

            // split up the terms in exp
            foreach (Match m in Regex.Matches(exp, @"(^|[+\-])[^+\-]+"))
            {
                val.Append(m.Value);
                if (divIsFrac)
                {
                    val.Append('*'); // multiply by inverted fraction
                }
                else
                {
                    val.Append('/');
                }
                val.Append(simpleDiv);
            }

            return val.ToString();
        }

        /// <summary>
        /// Subtract the right term from the left.
        /// </summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        static public string Subtract(string left, string right)
        {
            right = Invert(right);

            return Add(left, right);
        }

        static public string Add(string left, string right)
        {
            return SimplifyValue($"{left}+0{right}");
        }

        /// <summary>
        /// Invert an expression, make it negative if positive.
        /// </summary>
        /// <param name="exp"></param>
        /// <returns></returns>
        static public string Invert(string exp)
        {
            if (string.IsNullOrEmpty(exp))
            {
                return "";
            }

            char[] ops = { '+', '-', '/', '*' };
            StringBuilder sb = new StringBuilder(exp);

            if (exp[0] == '-')
            {
                sb.Remove(0, 1);
            }
            else
            {
                sb.Insert(0, '-');
            }

            for (int i = 1; i < sb.Length; i++)
            {
                if (sb[i] == '+') sb[i] = '-';
                else if (sb[i] == '-')
                {
                    if (ops.Contains(sb[i - 1]))
                    {
                        sb.Remove(i, 1);
                        i--;
                    }
                    else
                    {
                        sb[i] = '+';
                    }
                }
            }

            return sb.ToString();
        }
        #endregion
    }
}