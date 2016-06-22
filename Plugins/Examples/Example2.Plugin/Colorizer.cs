using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Windows.Media;
using dnSpy.Contracts.Text;
using dnSpy.Contracts.Text.Classification;
using dnSpy.Contracts.Text.Tagging;
using dnSpy.Contracts.Themes;

// Colorizes all text files.
// All "if" words use the Yellow color (default: no background, yellow foreground color)
// All 2 letter words use green background (foreground not changed)
// All 3 letter words use a white foreground and a red background
// All 4 letter words use the Error color (default: no background, red foreground color)

namespace Example2.Plugin {
	// Define our classification types. A classification type is converted to a color
	static class Constants {
		//TODO: use your own GUIDs
		public const string Color1 = "34A18F9B-6789-4A83-86E2-1E8DF81EB64C";
		public const string Color2 = "4854F0DE-E51B-4544-9FA3-01F1A2576FAD";

		// Disable compiler warnings. The fields aren't referenced, just exported so
		// the metadata can be added to some table. The fields will always be null.
#pragma warning disable CS0169
		// Export the classes that define the name, and base types
		[ExportClassificationTypeDefinition(Color1)]
		[DisplayName("Color1")]
		[BaseClassificationType(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition Color1ClassificationTypeDefinition;

		[ExportClassificationTypeDefinition(Color2)]
		[DisplayName("Color2")]
		[BaseClassificationType(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition Color2ClassificationTypeDefinition;
#pragma warning restore CS0169

		// Export the classes that define the colors and order
		[ExportClassificationFormatDefinition(Color1, "Color1", Order = EditorFormatDefinitionPriority.BeforeHigh)]
		sealed class Color1ClassificationFormatDefinition : ClassificationFormatDefinition {
			public override Brush GetBackground(ITheme theme) => Brushes.Green;
		}

		[ExportClassificationFormatDefinition(Color2, "Color2", Order = EditorFormatDefinitionPriority.BeforeHigh)]
		sealed class Color2ClassificationFormatDefinition : ClassificationFormatDefinition {
			public override Brush GetForeground(ITheme theme) => Brushes.White;
			public override Brush GetBackground(ITheme theme) => Brushes.Red;
		}
	}

	// Export our tagger provider. Each time a new text editor is created with our supported
	// content type (TEXT), this class gets called to create the tagger.
	[ExportTaggerProvider(typeof(IClassificationTag), ContentTypes.TEXT)]
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
		// We don't raise it, so add empty add/remove methods to prevent compiler warnings
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
			this.color1 = classificationTypeRegistryService.GetClassificationType(Constants.Color1);
			this.color2 = classificationTypeRegistryService.GetClassificationType(Constants.Color2);
			// Get some classification types created by some other code
			this.color3 = classificationTypeRegistryService.GetClassificationType(ThemeClassificationTypeNames.Yellow);
			this.color4 = classificationTypeRegistryService.GetClassificationType(ThemeClassificationTypeNames.Error);
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
