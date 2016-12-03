using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Windows.Media;
using dnSpy.Contracts.Text;
using dnSpy.Contracts.Text.Classification;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Tagging;
using Microsoft.VisualStudio.Utilities;

// Colorizes all text files.
// All "if" words use the Yellow color (default: no background, yellow foreground color)
// All 2 letter words use green background (foreground not changed)
// All 3 letter words use a white foreground and a red background
// All 4 letter words use the Error color (default: no background, red foreground color)

namespace Example2.Extension {
	// Define our classification types. A classification type is converted to a color
	static class Constants {
		// Use unique names
		public const string Color1_ClassificationTypeName = "Example2.Extension.Color1";
		public const string Color2_ClassificationTypeName = "Example2.Extension.Color2";

		// Disable compiler warnings. The fields aren't referenced, just exported so
		// the metadata can be added to some table. The fields will always be null.
#pragma warning disable 0169
		// Export the classes that define the name, and base types
		[Export(typeof(ClassificationTypeDefinition))]
		[Name(Color1_ClassificationTypeName)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition Color1ClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(Color2_ClassificationTypeName)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition Color2ClassificationTypeDefinition;
#pragma warning restore 0169

		// Export the classes that define the colors and order
		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = Color1_ClassificationTypeName)]
		[Name("My Color #1")]
		[UserVisible(true)]
		[Order(After = Priority.High)]
		sealed class Color1ClassificationFormatDefinition : ClassificationFormatDefinition {
			Color1ClassificationFormatDefinition() {
				BackgroundBrush = Brushes.Green;
			}
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = Color2_ClassificationTypeName)]
		[Name("My Color #2")]
		[UserVisible(true)]
		[Order(After = Priority.High)]
		sealed class Color2ClassificationFormatDefinition : ClassificationFormatDefinition {
			Color2ClassificationFormatDefinition() {
				ForegroundBrush = Brushes.White;
				BackgroundBrush = Brushes.Red;
			}
		}
	}

	// Export our tagger provider. Each time a new text editor is created with our supported
	// content type (TEXT), this class gets called to create the tagger.
	[Export(typeof(ITaggerProvider))]
	[TagType(typeof(IClassificationTag))]
	[ContentType(ContentTypes.Text)]
	sealed class TextTaggerProvider : ITaggerProvider {
		readonly IClassificationTypeRegistryService classificationTypeRegistryService;

		[ImportingConstructor]
		TextTaggerProvider(IClassificationTypeRegistryService classificationTypeRegistryService) {
			this.classificationTypeRegistryService = classificationTypeRegistryService;
		}

		public ITagger<T> CreateTagger<T>(ITextBuffer buffer) where T : ITag {
			// All text content types (including C#/VB code) derive from the TEXT content
			// type, so our tagger will get called to colorize every text file that's shown
			// in a text editor.
			return new TextTagger(classificationTypeRegistryService) as ITagger<T>;
		}
	}

	// This class gets called to colorize supported files
	sealed class TextTagger : ITagger<IClassificationTag> {
		// We don't raise it, so add empty add/remove methods to prevent compiler warnings.
		// This event must be raised when you detect changes to spans in the document. If
		// your GetTags() method does async work, you should raise it when the async work
		// is completed.
		public event EventHandler<SnapshotSpanEventArgs> TagsChanged {
			add { }
			remove { }
		}

		readonly IClassificationType color1;
		readonly IClassificationType color2;
		readonly IClassificationType color3;
		readonly IClassificationType color4;

		public TextTagger(IClassificationTypeRegistryService classificationTypeRegistryService) {
			// Get the classification types we need
			color1 = classificationTypeRegistryService.GetClassificationType(Constants.Color1_ClassificationTypeName);
			color2 = classificationTypeRegistryService.GetClassificationType(Constants.Color2_ClassificationTypeName);
			// Get some classification types created by some other code
			color3 = classificationTypeRegistryService.GetClassificationType(ThemeClassificationTypeNames.Yellow);
			color4 = classificationTypeRegistryService.GetClassificationType(ThemeClassificationTypeNames.Error);
		}

		// Gets called to colorize a range of the file. It's typically called once per visible line
		public IEnumerable<ITagSpan<IClassificationTag>> GetTags(NormalizedSnapshotSpanCollection spans) {
			if (spans.Count == 0)
				yield break;
			// All spans have the same snapshot since it's a normalized collection (sorted, and no span intersects any other span)
			var snapshot = spans[0].Snapshot;
			foreach (var span in spans) {
				foreach (var word in GetWords(span.GetText())) {
					// Create a new span. word.Item2 is the offset within the string, so add span.Start to
					// get the offset in the snapshot.
					var wordSpan = new SnapshotSpan(snapshot, new Span(span.Span.Start + word.Item2, word.Item1.Length));
					if (word.Item1 == "if")
						yield return new TagSpan<IClassificationTag>(wordSpan, new ClassificationTag(color3));
					else if (word.Item1.Length == 2)
						yield return new TagSpan<IClassificationTag>(wordSpan, new ClassificationTag(color1));
					else if (word.Item1.Length == 3)
						yield return new TagSpan<IClassificationTag>(wordSpan, new ClassificationTag(color2));
					else if (word.Item1.Length == 4)
						yield return new TagSpan<IClassificationTag>(wordSpan, new ClassificationTag(color4));
					else {
						// Ignore the rest
					}
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
