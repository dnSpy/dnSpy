/*
    Copyright (C) 2014-2019 de4dot@gmail.com

    This file is part of dnSpy

    dnSpy is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    dnSpy is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with dnSpy.  If not, see <http://www.gnu.org/licenses/>.
*/

using System.Runtime.CompilerServices;

namespace dnSpy.Contracts.DnSpy.Text.WPF {
	/// <summary>
	/// Workaround for a WPF bug that terminates the process if any WPF control tries to format
	/// a string with too many combining marks.
	/// Test string: new string('\u0300', 512)
	/// </summary>
	static class WpfUnicodeUtils {
		// The real limit seems to be 512
		public const int MAX_BAD_CHARS = 200;

		// See below for code that detects the bad values
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsBadWpfFormatterChar(uint cp) =>
			cp >= 0x0300 &&
			(cp <= 0x036F ||
			(cp >= 0x0483 && cp <= 0x0489) ||
			(cp >= 0x064B && cp <= 0x0655) ||
			cp == 0x0670 ||
			(cp >= 0x0816 && cp <= 0x0819) ||
			(cp >= 0x081B && cp <= 0x0823) ||
			(cp >= 0x0825 && cp <= 0x0827) ||
			(cp >= 0x0829 && cp <= 0x082D) ||
			(cp >= 0x0859 && cp <= 0x085B) ||
			(cp >= 0x0951 && cp <= 0x0952) ||
			(cp >= 0x0AFA && cp <= 0x0AFF) ||
			cp == 0x0D00 ||
			(cp >= 0x0D3B && cp <= 0x0D3C) ||
			(cp >= 0x135D && cp <= 0x135F) ||
			(cp >= 0x1AB0 && cp <= 0x1ABE) ||
			(cp >= 0x1CD0 && cp <= 0x1CD2) ||
			(cp >= 0x1CD4 && cp <= 0x1CE8) ||
			cp == 0x1CED ||
			(cp >= 0x1CF2 && cp <= 0x1CF4) ||
			(cp >= 0x1CF7 && cp <= 0x1CF9) ||
			(cp >= 0x1DC0 && cp <= 0x1DF9) ||
			(cp >= 0x1DFB && cp <= 0x1DFF) ||
			cp == 0x200D ||
			(cp >= 0x20D0 && cp <= 0x20F0) ||
			(cp >= 0x2CEF && cp <= 0x2CF1) ||
			(cp >= 0x2DE0 && cp <= 0x2DFF) ||
			(cp >= 0x302A && cp <= 0x302D) ||
			(cp >= 0x3099 && cp <= 0x309A) ||
			(cp >= 0xA66F && cp <= 0xA672) ||
			(cp >= 0xA674 && cp <= 0xA67D) ||
			(cp >= 0xA69E && cp <= 0xA69F) ||
			(cp >= 0xA6F0 && cp <= 0xA6F1) ||
			(cp >= 0xA8E0 && cp <= 0xA8F1) ||
			(cp >= 0xFE00 && cp <= 0xFE0F) ||
			(cp >= 0xFE20 && cp <= 0xFE2F) ||
			cp == 0x101FD ||
			cp == 0x102E0 ||
			(cp >= 0x10376 && cp <= 0x1037A) ||
			(cp >= 0x1171D && cp <= 0x1172B) ||
			(cp >= 0x16AF0 && cp <= 0x16AF4) ||
			(cp >= 0x16F51 && cp <= 0x16F7E) ||
			(cp >= 0x16F8F && cp <= 0x16F92) ||
			(cp >= 0x1D165 && cp <= 0x1D169) ||
			(cp >= 0x1D16D && cp <= 0x1D172) ||
			(cp >= 0x1D17B && cp <= 0x1D182) ||
			(cp >= 0x1D185 && cp <= 0x1D18B) ||
			(cp >= 0x1D1AA && cp <= 0x1D1AD) ||
			(cp >= 0x1D242 && cp <= 0x1D244) ||
			(cp >= 0x1DA00 && cp <= 0x1DA36) ||
			(cp >= 0x1DA3B && cp <= 0x1DA6C) ||
			cp == 0x1DA75 ||
			cp == 0x1DA84 ||
			(cp >= 0x1DA9B && cp <= 0x1DA9F) ||
			(cp >= 0x1DAA1 && cp <= 0x1DAAF) ||
			(cp >= 0x1E000 && cp <= 0x1E006) ||
			(cp >= 0x1E008 && cp <= 0x1E018) ||
			(cp >= 0x1E01B && cp <= 0x1E021) ||
			(cp >= 0x1E023 && cp <= 0x1E024) ||
			(cp >= 0x1E026 && cp <= 0x1E02A) ||
			(cp >= 0x1E8D0 && cp <= 0x1E8D6) ||
			(cp >= 0xE0100 && cp <= 0xE01EF));

		public static string ReplaceBadChars(string s) {
			bool hasBadChar = false;
			for (int i = 0; i < s.Length; i++) {
				uint cp = s[i];
				if (char.IsHighSurrogate((char)cp) && i + 1 < s.Length) {
					uint lo = s[i + 1];
					if (char.IsLowSurrogate((char)lo)) {
						cp = 0x10000 + ((cp - 0xD800) << 10) + (lo - 0xDC00);
						i++;
					}
				}
				if (IsBadWpfFormatterChar(cp)) {
					hasBadChar = true;
					break;
				}
			}
			if (!hasBadChar)
				return s;

			var chars = new char[s.Length];
			int badChars = 0;
			for (int i = 0; i < s.Length; i++) {
				char hi = s[i];
				var lo = (char)0;
				uint cp = hi;
				if (char.IsHighSurrogate((char)cp) && i + 1 < s.Length) {
					lo = s[i + 1];
					uint ls = lo;
					if (char.IsLowSurrogate((char)ls))
						cp = 0x10000 + ((cp - 0xD800) << 10) + (ls - 0xDC00);
				}
				if (IsBadWpfFormatterChar(cp)) {
					badChars++;
					if (badChars == MAX_BAD_CHARS) {
						badChars = 0;
						lo = hi = '?';
					}
				}
				chars[i] = hi;
				if (cp > 0xFFFF) {
					i++;
					chars[i] = lo;
				}
			}
			return new string(chars);
		}
	}
}

#if false
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.TextFormatting;
namespace WpfUnicode {
	/// <summary>
	/// Build it, and run it, attach a debugger (eg. WinDbg) since it crashes if there's no native
	/// debugger attached. When the debugger stops, hit continue (eg. hold down F5).
	/// </summary>
	sealed class Program {
		readonly TextRunPropertiesImpl textProps;
		readonly TextParagraphProperties paraProps;
		readonly TextFormatter[] formatters;
		readonly List<uint> codePoints;
		readonly List<uint> badCodePoints;
		[STAThread]
		public static void Main() {
			Console.WriteLine("Attach to the process with WinDbg and press any key to continue");
			Console.ReadKey();
			new Program().DoIt();
			while (true) {
				Console.WriteLine("DONE");
				Console.ReadKey();
			}
		}
		Program() {
			textProps = new TextRunPropertiesImpl {
				typeface = new Typeface("Consolas"),
				fontRenderingEmSize = 13,
				fontHintingEmSize = 16,
				textDecorations = null,
				foregroundBrush = Brushes.Black,
				backgroundBrush = Brushes.White,
				cultureInfo = null,
				textEffects = null,
			};
			formatters = new TextFormatter[] {
				TextFormatter.Create(TextFormattingMode.Ideal),
				TextFormatter.Create(TextFormattingMode.Display),
			};
			paraProps = new TextParagraphPropertiesImpl(textProps);
			codePoints = new List<uint>();
			badCodePoints = new List<uint>();
			const uint MAX = 0xFFFF;
			for (int i = 0; i <= (int)MAX; i++)
				codePoints.Add((uint)i);
			// Latest version: https://www.unicode.org/Public/zipped/latest/UCD.zip
			foreach (var line in File.ReadAllLines(@"C:\UCD\extracted\DerivedName.txt")) {
				if (line.StartsWith("#") || line.Length == 0)
					continue;
				var s = line.Substring(0, line.IndexOf(' '));
				var ss = s.Split(new[] { ".." }, StringSplitOptions.None);
				if (ss.Length == 1) {
					uint cp = uint.Parse(s, NumberStyles.HexNumber, null);
					if (cp > MAX)
						codePoints.Add(cp);
				}
				else if (ss.Length == 2) {
					uint lo = uint.Parse(ss[0], NumberStyles.HexNumber, null);
					uint hi = uint.Parse(ss[1], NumberStyles.HexNumber, null);
					while (lo <= hi) {
						if (lo > MAX)
							codePoints.Add(lo);
						lo++;
					}
				}
				else
					throw new InvalidOperationException();
			}
			codePoints.Sort();
		}
		void DoIt() {
			const int MAX_CHARS = 2048;
			var charData = new char[1 + MAX_CHARS * 2];
			foreach (var cp in codePoints) {
				int i = 0;
				charData[i++] = 'a';
				if (cp <= 0xFFFF) {
					for (int j = 0; j < MAX_CHARS; j++)
						charData[i++] = (char)cp;
				}
				else {
					uint v = cp - 0x10000;
					char lo = (char)(0xDC00 + (v & 0x3FF));
					char hi = (char)(0xD800 + ((v >> 10) & 0x3FF));
					for (int j = 0; j < MAX_CHARS; j++) {
						charData[i++] = hi;
						charData[i++] = lo;
					}
				}
				var s = new string(charData, 0, i);
				if (!FormatText(s)) {
					badCodePoints.Add(cp);
					Console.WriteLine($"U+{cp:X4}");
				}
			}
			Write(badCodePoints);
		}
		bool FormatText(string text) {
			try {
				foreach (var formatter in formatters) {
					var textSource = new TextSourceImpl(text, textProps);
					formatter.FormatLine(textSource, 0, 1000000, paraProps, null);
				}
				return true;
			}
			catch (Exception) {
				return false;
			}
		}
		static void Write(List<uint> badCps) {
			for (int i = 0; i < badCps.Count; i++) {
				var cp = badCps[i];
				var hi = cp;
				while (i + 1 < badCps.Count && hi + 1 == badCps[i + 1]) {
					i++;
					hi++;
				}
				if (cp == hi)
					Console.WriteLine($"cp == 0x{cp:X4} ||");
				else
					Console.WriteLine($"(cp >= 0x{cp:X4} && cp <= 0x{hi:X4}) ||");
			}
		}
	}
	sealed class TextSourceImpl : TextSource {
		readonly string text;
		readonly TextRunProperties textProps;
		public TextSourceImpl(string text, TextRunProperties textProps) {
			this.text = text;
			this.textProps = textProps;
		}
		public override TextSpan<CultureSpecificCharacterBufferRange> GetPrecedingText(int textSourceCharacterIndexLimit) =>
			new TextSpan<CultureSpecificCharacterBufferRange>(0, new CultureSpecificCharacterBufferRange(CultureInfo.CurrentUICulture, new CharacterBufferRange(string.Empty, 0, 0)));
		public override int GetTextEffectCharacterIndexFromTextSourceCharacterIndex(int textSourceCharacterIndex) => textSourceCharacterIndex;
		public override TextRun GetTextRun(int textSourceCharacterIndex) {
			int length = text.Length - textSourceCharacterIndex;
			if (length == 0)
				return endOfLine;
			return new TextCharacters(text, textSourceCharacterIndex, length, textProps);
		}
		static readonly TextEndOfLine endOfLine = new TextEndOfLine(1);
	}
	sealed class TextRunPropertiesImpl : TextRunProperties {
		public override Typeface Typeface => typeface;
		public override double FontRenderingEmSize => fontRenderingEmSize;
		public override double FontHintingEmSize => fontHintingEmSize;
		public override TextDecorationCollection TextDecorations => textDecorations;
		public override Brush ForegroundBrush => foregroundBrush;
		public override Brush BackgroundBrush => backgroundBrush;
		public override CultureInfo CultureInfo => CultureInfo.CurrentUICulture;
		public override TextEffectCollection TextEffects => textEffects;
		public Typeface typeface;
		public double fontRenderingEmSize;
		public double fontHintingEmSize;
		public TextDecorationCollection textDecorations;
		public Brush foregroundBrush;
		public Brush backgroundBrush;
		public CultureInfo cultureInfo;
		public TextEffectCollection textEffects;
	}
	sealed class TextParagraphPropertiesImpl : TextParagraphProperties {
		public override FlowDirection FlowDirection => FlowDirection.LeftToRight;
		public override TextAlignment TextAlignment => TextAlignment.Left;
		public override double LineHeight => 0;
		public override bool FirstLineInParagraph => false;
		public override TextRunProperties DefaultTextRunProperties { get; }
		public override TextWrapping TextWrapping => TextWrapping.Wrap;
		public override TextMarkerProperties TextMarkerProperties => null;
		public override double Indent => 0;
		public override double DefaultIncrementalTab => 28;
		public TextParagraphPropertiesImpl(TextRunProperties textProps) => DefaultTextRunProperties = textProps;
	}
}
#endif
