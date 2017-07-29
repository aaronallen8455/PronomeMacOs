﻿// This file has been autogenerated from a class added in the UI designer.

using System;
using Foundation;
using AppKit;
using System.Linq;

namespace Pronome
{
    public partial class BeatCodeEditor : NSTextView
	{
        public BeatCodeEditor(IntPtr handle) : base(handle)
		{
			// necessary to set this, otherwise text is black
			TextColor = NSColor.White;
			InsertionPointColor = NSColor.White;
            Font = NSFont.FromFontName("Geneva", 16);

            // allow for endless horizontal space
            HorizontallyResizable = true;
            AutoresizingMask = NSViewResizingMask.WidthSizable;
            TextContainer.Size = new CoreGraphics.CGSize(nfloat.MaxValue, 26);
            TextContainer.WidthTracksTextView = false;
		}

		public void HighlightSyntax()
		{
            LayoutManager.RemoveTemporaryAttribute(NSStringAttributeKey.ForegroundColor, new NSRange(0, Value.Length));

			Rule currentRule = null;
			int ruleStartIndex = 0;
			int index = 0;
			foreach (char c in Value)
			{
				if (currentRule != null)
				{
					// check if body of match ended
					switch (currentRule.CheckStatus(c))
					{
						case Rule.Status.End:
							LayoutManager.AddTemporaryAttribute(
								NSStringAttributeKey.ForegroundColor,
								currentRule.Color,
								new NSRange(ruleStartIndex, index - ruleStartIndex));
							currentRule = null;
							break;

						case Rule.Status.Terminate:
							LayoutManager.AddTemporaryAttribute(
								NSStringAttributeKey.ForegroundColor,
								currentRule.Color,
								new NSRange(ruleStartIndex, index - ruleStartIndex + 1));
							currentRule = null;
							index++;
							continue;
					}
				}

				// check for a match
				if (currentRule == null)
				{
					currentRule = Rules.FirstOrDefault(x => x.Initiators.Contains(c));

					ruleStartIndex = index;
				}

				index++;
			}

			if (currentRule != null)
			{
				LayoutManager.AddTemporaryAttribute(
					NSStringAttributeKey.ForegroundColor,
					currentRule.Color,
					new NSRange(ruleStartIndex, Value.Length - ruleStartIndex));
			}
		}

		public override void KeyDown(NSEvent theEvent)
		{
            // don't allow new lines
            if (theEvent.Characters == "\r") 
            {
                NSApplication.SharedApplication.MainWindow.MakeFirstResponder(null);
                return;
			}

			base.KeyDown(theEvent);
			// add some color
			HighlightSyntax();
		}

        /// <summary>
        /// The syntax color rules.
        /// </summary>
		Rule[] Rules = {
            new Rule(NSColor.FromRgb(0,163,217), ",|"), // delimiter

            new Rule(NSColor.Gray, "!", "", "!"), // comment

            new Rule(NSColor.FromRgb(105,186,30), "["), // repeats
            new Rule(NSColor.FromRgb(105,186,30), "]", "1234567890"),
			new Rule(NSColor.FromRgb(105,186,30), "(", "1234567890)+-/*xX."),

            new Rule(NSColor.FromRgb(204,185,110), "{"), // group multiply
            new Rule(NSColor.FromRgb(204,185,110), "}", "1234567890.+-/*xX"),

            new Rule(NSColor.FromRgb(209,66,235), "@", "1234567890ABCDEFGabcdefgXpu"), // source modifier

            new Rule(NSColor.FromRgb(152,118,170), "$", "1234567890s"), // reference

            new Rule(NSColor.FromRgb(204,99,34), "+-/*xX") // operator
        };

		class Rule
		{
			/// <summary>
			/// The charactor that initiates a match.
			/// </summary>
			public string Initiators;

			/// <summary>
			/// Characters that are inside the match
			/// </summary>
			public string Body;

			/// <summary>
			/// Characters that terminate the match
			/// </summary>
			public string Terminators;

			public NSColor Color;

			public Rule(NSColor color, string initiators, string body = "", string terminators = "")
			{
				Color = color;
				Initiators = initiators;
				Terminators = terminators;
				Body = body;
			}

			public enum Status { Terminate, End, Inside }

			public Status CheckStatus(char c)
			{
				if (!string.IsNullOrEmpty(Terminators))
				{
					if (Terminators.IndexOf(c) != -1)
					{
						return Status.Terminate;
					}

					if (string.IsNullOrEmpty(Body))
					{
						return Status.Inside;
					}
				}

				if (!string.IsNullOrEmpty(Body))
				{
					if (Body.IndexOf(c) != -1)
					{
						return Status.Inside;
					}
				}

				return Status.End;
			}
		}
	}
}
