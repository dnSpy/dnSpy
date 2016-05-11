using dnSpy.Contracts.Files.Tabs.TextEditor.ToolTips;
using dnSpy.Contracts.TextEditor;

namespace Example2.Plugin {
	// This reference is added to the "decompiled" code by ModuleChildNode.Decompile()
	sealed class StringInfoReference {
		public string Message { get; }

		public StringInfoReference(string msg) {
			this.Message = msg;
		}
	}

	// Called by dnSpy to create a tooltip when hovering over a reference in the text editor
	[ExportToolTipContentCreator(Order = 0)]
	sealed class ToolTipContentCreator : IToolTipContentCreator {
		public object Create(IToolTipContentCreatorContext context, object @ref) {
			// This reference is added to the "decompiled" code by ModuleChildNode.Decompile()
			var sref = @ref as StringInfoReference;
			if (sref != null) {
				var creator = context.Create();
				creator.Output.Write(BoxedOutputColor.String, sref.Message);
				return creator.Create();
			}

			return null;
		}
	}
}
