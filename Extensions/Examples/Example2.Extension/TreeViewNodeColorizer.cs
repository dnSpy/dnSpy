using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Windows.Media;
using dnSpy.Contracts.Documents.TreeView;
using dnSpy.Contracts.Text.Classification;
using dnSpy.Contracts.TreeView.Text;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;

// Adds an underline to Assembly Explorer nodes: Assembly / Method
// Adds light green background in the middle of all text

namespace Example2.Extension {
	static class TreeViewNodeColorizerClassifications {
		public const string UnderlineClassificationType = "Example2.Extension.UnderlineClassificationType";
		public const string LightgreenBackgroundClassificationType = "Example2.Extension.LightgreenBackgroundClassificationType";

		// Disable compiler warnings. The fields aren't referenced, just exported so
		// the metadata can be added to some table. The fields will always be null.
#pragma warning disable CS0169
		// Export the classes that define the name, and base types
		[Export(typeof(ClassificationTypeDefinition))]
		[Name(UnderlineClassificationType)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition UnderlineClassificationTypeDefinition;

		[Export(typeof(ClassificationTypeDefinition))]
		[Name(LightgreenBackgroundClassificationType)]
		[BaseDefinition(PredefinedClassificationTypeNames.FormalLanguage)]
		static ClassificationTypeDefinition LightgreenBackgroundClassificationTypeDefinition;
#pragma warning restore CS0169

		// Export the classes that define the colors and order
		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = UnderlineClassificationType)]
		[Name("Underline")]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class UnderlineClassificationFormatDefinition : ClassificationFormatDefinition {
			UnderlineClassificationFormatDefinition() => TextDecorations = System.Windows.TextDecorations.Underline;
		}

		[Export(typeof(EditorFormatDefinition))]
		[ClassificationType(ClassificationTypeNames = LightgreenBackgroundClassificationType)]
		[Name("Lightgreen Background")]
		[UserVisible(true)]
		[Order(After = Priority.Default)]
		sealed class LightGreenBackgroundClassificationFormatDefinition : ClassificationFormatDefinition {
			LightGreenBackgroundClassificationFormatDefinition() => BackgroundBrush = Brushes.LightGreen;
		}
	}

	[Export(typeof(ITextClassifierProvider))]
	// You can also add more content types or use the base content type TreeViewContentTypes.TreeViewNode
	[ContentType(TreeViewContentTypes.TreeViewNodeAssemblyExplorer)]
	sealed class TreeViewNodeColorizerProvider : ITextClassifierProvider {
		readonly IClassificationTypeRegistryService classificationTypeRegistryService;

		[ImportingConstructor]
		TreeViewNodeColorizerProvider(IClassificationTypeRegistryService classificationTypeRegistryService) => this.classificationTypeRegistryService = classificationTypeRegistryService;

		public ITextClassifier Create(IContentType contentType) => new TreeViewNodeColorizer(classificationTypeRegistryService);
	}

	sealed class TreeViewNodeColorizer : ITextClassifier {
		readonly IClassificationTypeRegistryService classificationTypeRegistryService;

		public TreeViewNodeColorizer(IClassificationTypeRegistryService classificationTypeRegistryService) => this.classificationTypeRegistryService = classificationTypeRegistryService;

		public IEnumerable<TextClassificationTag> GetTags(TextClassifierContext context) {
			var tvContext = context as TreeViewNodeClassifierContext;
			if (tvContext == null)
				yield break;

			// Don't do a thing if it's a tooltip
			if (tvContext.IsToolTip)
				yield break;

			// Add the underline
			if (tvContext.Node is AssemblyDocumentNode || tvContext.Node is MethodNode) {
				yield return new TextClassificationTag(new Span(0, context.Text.Length),
					classificationTypeRegistryService.GetClassificationType(TreeViewNodeColorizerClassifications.UnderlineClassificationType));
			}

			// Add light green background in the middle of the text
			yield return new TextClassificationTag(new Span(context.Text.Length / 4, context.Text.Length / 2),
				classificationTypeRegistryService.GetClassificationType(TreeViewNodeColorizerClassifications.LightgreenBackgroundClassificationType));
		}
	}
}
