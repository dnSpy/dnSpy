/*
	Copyright (c) 2015 Ki

	Permission is hereby granted, free of charge, to any person obtaining a copy
	of this software and associated documentation files (the "Software"), to deal
	in the Software without restriction, including without limitation the rights
	to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
	copies of the Software, and to permit persons to whom the Software is
	furnished to do so, subject to the following conditions:

	The above copyright notice and this permission notice shall be included in
	all copies or substantial portions of the Software.

	THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
	IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
	FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
	AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
	LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
	OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
	THE SOFTWARE.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.TextFormatting;
using Expr = System.Linq.Expressions.Expression;

namespace dnSpy.Contracts.Controls {
	interface ITextFormatter : IDisposable {
		TextLine FormatLine(TextSource textSource, int firstCharIndex, double paragraphWidth,
			TextParagraphProperties paragraphProperties, TextLineBreak previousLineBreak);
	}

	static class TextFormatterFactory {
		public static ITextFormatter Create(DependencyObject owner, bool useNewFormatter) {
			if (useNewFormatter)
				return new GlyphRunFormatter(TextOptions.GetTextFormattingMode(owner));
			return new WpfTextFormatter(TextOptions.GetTextFormattingMode(owner));
		}
	}

	sealed class WpfTextFormatter : ITextFormatter {
		readonly TextFormatter formatter;

		public WpfTextFormatter(TextFormattingMode mode) => formatter = TextFormatter.Create(mode);

		public TextLine FormatLine(TextSource textSource, int firstCharIndex, double paragraphWidth,
			TextParagraphProperties paragraphProperties, TextLineBreak previousLineBreak) => formatter.FormatLine(textSource, firstCharIndex, paragraphWidth, paragraphProperties, previousLineBreak);

		public void Dispose() => formatter.Dispose();
	}

	sealed class GlyphRunFormatter : ITextFormatter {
		static readonly FieldInfo _textFormattingMode;
		static readonly Func<CharacterBufferReference, IList<char>> getCharBuf;
		static readonly Func<CharacterBufferReference, int> getCharOffset;
		static readonly Func<Rect, TextBounds> makeBounds;
		readonly object mode;

		static GlyphRunFormatter() {
			var refType = typeof(CharacterBufferReference);
			_textFormattingMode = typeof(GlyphRun).GetField("_textFormattingMode", BindingFlags.NonPublic | BindingFlags.Instance);

			{
				var property = refType.GetProperty("CharacterBuffer", BindingFlags.NonPublic | BindingFlags.Instance);
				var param0 = Expr.Parameter(refType);
				var expr = Expr.Convert(Expr.Property(param0, property), typeof(IList<char>));
				var lambda = Expr.Lambda<Func<CharacterBufferReference, IList<char>>>(expr, param0);
				getCharBuf = lambda.Compile();
			}

			{
				var property = refType.GetProperty("OffsetToFirstChar", BindingFlags.NonPublic | BindingFlags.Instance);
				var param0 = Expr.Parameter(refType);
				var expr = Expr.Property(param0, property);
				var lambda = Expr.Lambda<Func<CharacterBufferReference, int>>(expr, param0);
				getCharOffset = lambda.Compile();
			}

			{
				var ctor = typeof(TextBounds).GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance)[0];
				var param0 = Expr.Parameter(typeof(Rect));
				var expr = Expr.New(ctor, param0, Expr.Constant(FlowDirection.LeftToRight), Expr.Constant(null, typeof(IList<TextRunBounds>)));
				var lambda = Expr.Lambda<Func<Rect, TextBounds>>(expr, param0);
				makeBounds = lambda.Compile();
			}
		}

		public GlyphRunFormatter(object mode) => this.mode = mode;

		public void Dispose() {
			//
		}

		public TextLine FormatLine(TextSource textSource, int firstCharIndex, double paragraphWidth, TextParagraphProperties paragraphProperties, TextLineBreak previousLineBreak) {
			var runs = new List<Tuple<TextRun, GlyphRun, int, double>>();

			int index = firstCharIndex;
			double x = paragraphProperties.Indent, height = 0, baseline = 0;
			double trailWhitespaceWidth = 0;

			while (true) {
				var run = textSource.GetTextRun(index);
				var textProps = run.Properties ?? paragraphProperties.DefaultTextRunProperties;
				var fontSize = textProps.FontRenderingEmSize;
				var len = run.Length;
				if (textProps != null) {
					height = Math.Max(height, (int)(textProps.Typeface.FontFamily.LineSpacing * fontSize));
					baseline = Math.Max(baseline, (int)(textProps.Typeface.FontFamily.Baseline * fontSize));
				}

				if (run is TextEndOfLine || run == null) {
					index += len;
					runs.Add(Tuple.Create(run, (GlyphRun)null, 0, 0.0));
					break;
				}
				else if (run is TextCharacters chrs) {
					var charBuf = getCharBuf(chrs.CharacterBufferReference);
					var charOffset = getCharOffset(chrs.CharacterBufferReference);

					if (!textProps.Typeface.TryGetGlyphTypeface(out var gl))
						throw new Exception("GlyphTypeface does not exists for font '" + textProps.Typeface.FontFamily + "'.");

					ushort[] glyphIndexes = new ushort[len];
					double[] advanceWidths = new double[len];

					double totalWidth = 0;
					int trailWhitespace = 0;
					trailWhitespaceWidth = 0;
					for (int n = 0; n < len; n++) {
						var c = charBuf[charOffset + n];

						ushort glyphIndex;
						double width;

						if (c == '\t') {
							glyphIndex = gl.CharacterToGlyphMap[' '];
							width = paragraphProperties.DefaultIncrementalTab - x % paragraphProperties.DefaultIncrementalTab;
						}
						else {
							if (!gl.CharacterToGlyphMap.TryGetValue(c, out glyphIndex))
								glyphIndex = gl.CharacterToGlyphMap['?'];
							width = gl.AdvanceWidths[glyphIndex] * fontSize;
						}

						glyphIndexes[n] = glyphIndex;
						advanceWidths[n] = width;

						if (char.IsWhiteSpace(c)) {
							trailWhitespace++;
							trailWhitespaceWidth += width;
						}
						else {
							totalWidth += trailWhitespaceWidth + width;
							trailWhitespaceWidth = 0;
							trailWhitespace = 0;
						}
					}
					var origin = new Point(x, 0);

					var glyphRun = new GlyphRun(
						gl, 0, false, fontSize, glyphIndexes, origin, advanceWidths,
						null, null, null, null, null, null);
					runs.Add(Tuple.Create(run, glyphRun, trailWhitespace, trailWhitespaceWidth));

					x += totalWidth + trailWhitespaceWidth;

					index += len;
				}
				else if (run is TextEmbeddedObject obj) {
					var metrics = obj.Format(paragraphWidth - x);
					runs.Add(Tuple.Create(run, (GlyphRun)null, 0, metrics.Width));

					height = Math.Max(height, obj.Format(paragraphWidth - x).Height);
					x += metrics.Width;

					index += len;
				}
			}

			return new GlyphRunLine {
				entries = runs.ToArray(),
				baseline = baseline,
				width = x - trailWhitespaceWidth,
				height = height,
				mode = mode
			};
		}

		class GlyphRunLine : TextLine {
			internal Tuple<TextRun, GlyphRun, int, double>[] entries;
			internal double baseline;
			internal double width;
			internal double height;
			internal object mode;

			#region Unused members

			public override TextLine Collapse(params TextCollapsingProperties[] collapsingPropertiesList) => throw new NotSupportedException();

			public override int DependentLength {
				get { throw new NotSupportedException(); }
			}

			public override double Extent {
				get { throw new NotSupportedException(); }
			}

			public override bool HasCollapsed {
				get { throw new NotSupportedException(); }
			}

			public override bool HasOverflowed {
				get { throw new NotSupportedException(); }
			}

			public override double MarkerBaseline {
				get { throw new NotSupportedException(); }
			}

			public override double MarkerHeight {
				get { throw new NotSupportedException(); }
			}

			public override int NewlineLength {
				get { throw new NotSupportedException(); }
			}

			public override double OverhangAfter {
				get { throw new NotSupportedException(); }
			}

			public override double OverhangLeading {
				get { throw new NotSupportedException(); }
			}

			public override double OverhangTrailing {
				get { throw new NotSupportedException(); }
			}

			public override double Start {
				get { throw new NotSupportedException(); }
			}

			public override double TextBaseline {
				get { throw new NotSupportedException(); }
			}

			public override double TextHeight {
				get { throw new NotSupportedException(); }
			}

			public override CharacterHit GetBackspaceCaretCharacterHit(CharacterHit characterHit) => throw new NotSupportedException();

			public override IEnumerable<IndexedGlyphRun> GetIndexedGlyphRuns() => throw new NotSupportedException();

			public override CharacterHit GetNextCaretCharacterHit(CharacterHit characterHit) => throw new NotSupportedException();

			public override CharacterHit GetPreviousCaretCharacterHit(CharacterHit characterHit) => throw new NotSupportedException();

			public override IList<TextCollapsedRange> GetTextCollapsedRanges() => throw new NotSupportedException();

			#endregion

			public override void Dispose() {
				//
			}

			static GlyphRun Clone(GlyphRun run, Point origin) => new GlyphRun(
					run.GlyphTypeface, run.BidiLevel, run.IsSideways,
					run.FontRenderingEmSize, run.GlyphIndices, origin,
					run.AdvanceWidths, run.GlyphOffsets, run.Characters,
					run.DeviceFontName, run.ClusterMap, run.CaretStops, run.Language);

			public override void Draw(DrawingContext drawingContext, Point origin, InvertAxes inversion) {
				foreach (var entry in entries) {
					if (entry.Item2 == null)
						continue;
					if (entry.Item3 == entry.Item1.Length) // All whitespace, no need to render
						continue;

					var textRun = entry.Item1;
					var glyphRun = entry.Item2;
					var textProps = textRun.Properties;

					var newRun = Clone(glyphRun, new Point {
						X = origin.X + glyphRun.BaselineOrigin.X,
						Y = (int)(origin.Y + glyphRun.GlyphTypeface.Baseline * textProps.FontRenderingEmSize)
					});
					if (_textFormattingMode != null)
						_textFormattingMode.SetValue(glyphRun, mode);

					var box = newRun.ComputeAlignmentBox();
					if (textProps.BackgroundBrush != null) {
						drawingContext.DrawRectangle(
							textProps.BackgroundBrush, null,
							new Rect(origin, box.Size));
					}

					drawingContext.DrawGlyphRun(textProps.ForegroundBrush, newRun);

					if (textProps.TextDecorations != null)
						foreach (var deco in textProps.TextDecorations) {
							var thickness = Math.Round(glyphRun.GlyphTypeface.UnderlineThickness * textProps.FontRenderingEmSize);
							var pos = glyphRun.GlyphTypeface.UnderlinePosition -
							          glyphRun.GlyphTypeface.Baseline +
							          glyphRun.GlyphTypeface.Height;
							pos = Math.Round(pos * textProps.FontRenderingEmSize) + thickness / 2;

							var pen = new Pen(textProps.ForegroundBrush, thickness);
							drawingContext.DrawLine(pen,
								new Point(newRun.BaselineOrigin.X, newRun.BaselineOrigin.Y + pos),
								new Point(newRun.BaselineOrigin.X + box.Width, newRun.BaselineOrigin.Y + pos));
						}
				}
			}

			public override CharacterHit GetCharacterHitFromDistance(double distance) {
				double currentDistance = 0;
				int index = 0;
				foreach (var entry in entries) {
					if (entry.Item2 == null) {
						var newDistance = currentDistance + entry.Item4;
						if (newDistance > distance)
							return new CharacterHit(index, 0);
						currentDistance = newDistance;
						index += entry.Item1.Length;

						continue;
					}

					index = getCharOffset(entry.Item1.CharacterBufferReference);
					var widthList = entry.Item2.AdvanceWidths;
					for (int i = 0; i < widthList.Count; i++) {
						var newDistance = currentDistance + widthList[i];
						if (newDistance > distance + widthList[i] * 2 / 3)
							return new CharacterHit(index, 0);
						currentDistance = newDistance;
						index++;
					}
				}
				return new CharacterHit(index, 0);
			}

			public override double GetDistanceFromCharacterHit(CharacterHit characterHit) {
				double distance = 0;
				int index = 0;
				foreach (var entry in entries) {
					if (entry.Item2 == null) {
						if (index == characterHit.FirstCharacterIndex)
							return distance;
						distance += entry.Item4;
						index += entry.Item1.Length;

						continue;
					}

					index = getCharOffset(entry.Item1.CharacterBufferReference);
					var widthList = entry.Item2.AdvanceWidths;
					for (int i = 0; i < widthList.Count; i++) {
						if (index == characterHit.FirstCharacterIndex)
							return distance;
						distance += widthList[i];
						index++;
					}
				}
				return distance;
			}

			public override IList<TextBounds> GetTextBounds(int firstTextSourceCharacterIndex, int textLength) {
				bool found = false;
				double d = 0;
				double x = 0;
				double width = 0;

				int index = 0;
				foreach (var entry in entries) {
					if (entry.Item2 == null) {
						if (index == firstTextSourceCharacterIndex) {
							found = true;
							x = d;
						}
						if (found) {
							width += entry.Item4;
							textLength -= entry.Item1.Length;
							if (textLength == 0)
								return new[] { makeBounds(new Rect(x, 0, width, height)) };
						}
						d += entry.Item4;
						index += entry.Item1.Length;

						continue;
					}

					index = getCharOffset(entry.Item1.CharacterBufferReference);
					var widthList = entry.Item2.AdvanceWidths;
					for (int i = 0; i < widthList.Count; i++) {
						if (index == firstTextSourceCharacterIndex) {
							found = true;
							x = d;
						}
						if (found) {
							width += widthList[i];
							textLength--;
							if (textLength == 0)
								return new[] { makeBounds(new Rect(x, 0, width, height)) };
						}
						d += widthList[i];
						index++;
					}
				}

				return new[] { makeBounds(new Rect(x, 0, width, height)) };
			}

			public override TextLineBreak GetTextLineBreak() => null;

			public override IList<TextSpan<TextRun>> GetTextRunSpans() => entries.Select(entry => new TextSpan<TextRun>(entry.Item1.Length, entry.Item1)).ToList();

			public override double Width {
				get { return width; }
			}

			public override double Height {
				get { return height; }
			}

			public override double Baseline {
				get { return baseline; }
			}

			public override int Length {
				get { return entries.Sum(entry => entry.Item1.Length); }
			}

			public override int TrailingWhitespaceLength {
				get {
					var entry = entries.LastOrDefault(e => e.Item2 != null);
					return entry == null ? 0 : entry.Item3;
				}
			}

			public override double WidthIncludingTrailingWhitespace {
				get {
					var entry = entries.LastOrDefault(e => e.Item2 != null);
					return entry == null ? width : width + entry.Item4;
				}
			}
		}
	}
}