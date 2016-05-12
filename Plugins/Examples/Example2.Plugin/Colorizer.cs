using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Windows.Media;
using dnSpy.Contracts.Text;
using dnSpy.Contracts.Themes;

// Colorizes all text files.
// All "if" words use the Yellow color (default: no background, yellow foreground color)
// All 2 letter words use green background (foreground not changed)
// All 3 letter words use a white foreground and a red background
// All 4 letter words use the Error color (default: no background, red foreground color)

namespace Example2.Plugin {
	// Export our snapshot colorizer provider
	[Export(typeof(ITextSnapshotColorizerProvider))]
	sealed class TextSnapshotColorizerProvider : ITextSnapshotColorizerProvider {
		public IEnumerable<ITextSnapshotColorizer> Create(ITextBuffer textBuffer) {
			// If it's a supported content type, return our colorizer. All text content
			// types (including C#/VB code) derive from the TEXT content type, so our colorizer
			// will get called to colorize every text file that's shown in a text editor.
			if (textBuffer.ContentType.IsOfType(ContentTypes.TEXT))
				yield return new TextSnapshotColorizer();
		}
	}

	// This class gets called to colorize supported files
	sealed class TextSnapshotColorizer : ITextSnapshotColorizer {
		static readonly ITextColor color1 = new TextColor(null, Brushes.Green);
		static readonly ITextColor color2 = new TextColor(Brushes.White, Brushes.Red);

		// Gets called to colorize a range of the file. It's typically called once per visible line
		public IEnumerable<ColorSpan> GetColorSpans(ITextSnapshot snapshot, Span span) {
			foreach (var word in GetWords(snapshot.GetText(span))) {
				// Create a new span. word.Item2 is the offset within the string, so add span.Start to
				// get the offset in the snapshot.
				var wordSpan = new Span(span.Start + word.Item2, word.Item1.Length);
				if (word.Item1 == "if")
					yield return new ColorSpan(wordSpan, ColorType.Yellow);
				else if (word.Item1.Length == 2)
					yield return new ColorSpan(wordSpan, color1);
				else if (word.Item1.Length == 3)
					yield return new ColorSpan(wordSpan, color2);
				else if (word.Item1.Length == 4)
					yield return new ColorSpan(wordSpan, ColorType.Error);
				else {
					// Ignore the rest
				}
			}
		}

		IEnumerable<Tuple<string, int>> GetWords(string s) {
			int offset = 0;
			for (;;) {
				while (offset < s.Length && char.IsWhiteSpace(s[offset]))
					offset++;
				int wordOffset = offset;
				while (offset < s.Length && !char.IsWhiteSpace(s[offset]))
					offset++;
				if (wordOffset == offset)
					break;
				yield return Tuple.Create(s.Substring(wordOffset, offset - wordOffset), wordOffset);
			}
		}
	}
}
