using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Windows.Media;
using dnSpy.Contracts.Hex;
using dnSpy.Contracts.Hex.Tagging;
using dnSpy.Contracts.Text.Classification;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;

namespace Example2.Extension {
	// Define our classification types. A classification type is converted to a color
	static class HexConstants {
		// Use unique names
		public const string HexColor1_ClassificationTypeName = "Example2.Extension.HexColor1";
		public const string HexColor2_ClassificationTypeName = "Example2.Extension.HexColor2";

		// Disable compiler warnings. The fields aren't referenced, just exported so
		// the metadata can be added to some table. The fields will always be null.
#pragma warning disable 0169
		// Export the classes that define the name, and base types
		[Export(typeof(ClassificationTypeDefinition))]
		[Name(HexColor1_ClassificationTypeName)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition HexColor1ClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(HexColor2_ClassificationTypeName)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition HexColor2ClassificationTypeDefinition;
#pragma warning restore 0169

		// Export the classes that define the colors and order
		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = HexColor1_ClassificationTypeName)]
		[Name("My Hex Color #1")]
		[UserVisible(true)]
		[Order(After = Priority.High)]
		sealed class Color1ClassificationFormatDefinition : ClassificationFormatDefinition {
			Color1ClassificationFormatDefinition() {
				BackgroundBrush = Brushes.LightGray;
			}
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = HexColor2_ClassificationTypeName)]
		[Name("My Hex Color #2")]
		[UserVisible(true)]
		[Order(After = Priority.High)]
		sealed class Color2ClassificationFormatDefinition : ClassificationFormatDefinition {
			Color2ClassificationFormatDefinition() {
				ForegroundBrush = Brushes.White;
				BackgroundBrush = Brushes.Red;
			}
		}
	}

	// Export our tagger provider
	[Export(typeof(HexTaggerProvider))]
	[HexTagType(typeof(HexClassificationTag))]
	sealed class HexTaggerProviderImpl : HexTaggerProvider {
		readonly IClassificationTypeRegistryService classificationTypeRegistryService;

		[ImportingConstructor]
		HexTaggerProviderImpl(IClassificationTypeRegistryService classificationTypeRegistryService) {
			this.classificationTypeRegistryService = classificationTypeRegistryService;
		}

		public override IHexTagger<T> CreateTagger<T>(HexBuffer buffer) =>
			new HexTaggerImpl(classificationTypeRegistryService) as IHexTagger<T>;
	}

	// This class gets called to colorize supported files
	sealed class HexTaggerImpl : HexTagger<HexClassificationTag> {
		// We don't raise it, so add empty add/remove methods to prevent compiler warnings.
		// This event must be raised when you detect changes to spans in the document. If
		// your GetTags() method does async work, you should raise it when the async work
		// is completed.
		public override event EventHandler<HexBufferSpanEventArgs> TagsChanged {
			add { }
			remove { }
		}

		readonly IClassificationType color1;
		readonly IClassificationType color2;
		readonly IClassificationType color3;
		readonly IClassificationType color4;

		public HexTaggerImpl(IClassificationTypeRegistryService classificationTypeRegistryService) {
			// Get the classification types we need
			color1 = classificationTypeRegistryService.GetClassificationType(HexConstants.HexColor1_ClassificationTypeName);
			color2 = classificationTypeRegistryService.GetClassificationType(HexConstants.HexColor2_ClassificationTypeName);
			// Get some classification types created by some other code
			color3 = classificationTypeRegistryService.GetClassificationType(ThemeClassificationTypeNames.Yellow);
			color4 = classificationTypeRegistryService.GetClassificationType(ThemeClassificationTypeNames.Error);
		}

		// Called to classify a span of data, it can only classify bytes, not characters on a line
		public override IEnumerable<IHexTagSpan<HexClassificationTag>> GetTags(NormalizedHexBufferSpanCollection spans) {
			if (spans.Count == 0)
				yield break;

			var ourSpan = new HexBufferSpan(spans[0].Buffer, 10, 128);
			foreach (var span in spans) {
				if (!span.IntersectsWith(ourSpan))
					continue;

				// Classify both columns (values and ASCII)
				var flags = HexSpanSelectionFlags.Values | HexSpanSelectionFlags.Ascii | HexSpanSelectionFlags.Cell;
				yield return new HexTagSpan<HexClassificationTag>(ourSpan, flags, new HexClassificationTag(color2));
			}
		}

		// Called to classify a view line, any character on the line can be classified
		public override IEnumerable<IHexTextTagSpan<HexClassificationTag>> GetTags(HexTaggerContext context) {
			// Highlight every 16th line
			if (context.Line.LineNumber.ToUInt64() % 16 == 0)
				yield return new HexTextTagSpan<HexClassificationTag>(context.LineSpan, new HexClassificationTag(color1));

			// If there are too many zeroes, change the color of the line
			int zeroes = 0;
			for (int i = 0; i < context.Line.HexBytes.Length; i++) {
				if (context.Line.HexBytes.TryReadByte(i) == 0)
					zeroes++;
			}
			if (zeroes >= context.Line.HexBytes.Length / 3)
				yield return new HexTextTagSpan<HexClassificationTag>(context.LineSpan, new HexClassificationTag(color4));
		}
	}
}
