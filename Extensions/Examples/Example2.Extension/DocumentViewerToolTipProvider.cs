using dnSpy.Contracts.Documents.Tabs.DocViewer.ToolTips;
using dnSpy.Contracts.Text;

namespace Example2.Extension {
	// This reference is added to the "decompiled" code by ModuleChildNode.Decompile()
	sealed class StringInfoReference {
		public string Message { get; }

		public StringInfoReference(string msg) => Message = msg;
	}

	// Called by dnSpy to create a tooltip when hovering over a reference in the text editor
	[ExportDocumentViewerToolTipProvider]
	sealed class DocumentViewerToolTipProvider : IDocumentViewerToolTipProvider {
		public object? Create(IDocumentViewerToolTipProviderContext context, object? @ref) {
			// This reference is added to the "decompiled" code by ModuleChildNode.Decompile()
			if (@ref is StringInfoReference sref) {
				var provider = context.Create();
				provider.Output.Write(BoxedTextColor.String, sref.Message);
				return provider.Create();
			}

			return null;
		}
	}
}
