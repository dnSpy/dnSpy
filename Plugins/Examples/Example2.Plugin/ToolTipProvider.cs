using dnSpy.Contracts.Files.Tabs.DocViewer.ToolTips;
using dnSpy.Contracts.Text;

namespace Example2.Plugin {
	// This reference is added to the "decompiled" code by ModuleChildNode.Decompile()
	sealed class StringInfoReference {
		public string Message { get; }

		public StringInfoReference(string msg) {
			this.Message = msg;
		}
	}

	// Called by dnSpy to create a tooltip when hovering over a reference in the text editor
	[ExportToolTipProvider]
	sealed class ToolTipProvider : IToolTipProvider {
		public object Create(IToolTipProviderContext context, object @ref) {
			// This reference is added to the "decompiled" code by ModuleChildNode.Decompile()
			var sref = @ref as StringInfoReference;
			if (sref != null) {
				var provider = context.Create();
				provider.Output.Write(BoxedTextColor.String, sref.Message);
				return provider.Create();
			}

			return null;
		}
	}
}
