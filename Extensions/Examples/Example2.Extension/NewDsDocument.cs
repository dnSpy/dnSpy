using System;
using System.ComponentModel.Composition;
using System.IO;
using dnSpy.Contracts.Decompiler;
using dnSpy.Contracts.Documents;
using dnSpy.Contracts.Documents.Tabs.DocViewer;
using dnSpy.Contracts.Documents.TreeView;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.Text;

// Adds a new IDsDocument that can be loaded into the document treeview. It gets its own DsDocumentNode.
// Open a .txt/.xml/.cs/.vb (see supportedExtensions) file to trigger this code.

namespace Example2.Extension {
	// All root nodes in the document treeview contain a IDsDocument instance. They don't need to be
	// .NET files or even PE files, they can be any file or even non-file (eg. in-memory data).
	sealed class MyDsDocument : DsDocument {
		//TODO: Use your own guid
		public static readonly Guid THE_GUID = new Guid("9058B02C-1FE0-4EC4-93D3-A378D4B6FCE1");

		// We support serialization, so return a non-null value
		public override DsDocumentInfo? SerializedDocument => new DsDocumentInfo(Filename, THE_GUID);

		// Since we open files from disk, we return a FilenameKey.
		// If this gets changed, also update MyDsDocumentProvider.CreateKey()
		public override IDsDocumentNameKey Key => new FilenameKey(Filename);

		// Used by MyDsDocumentNode.Decompile() to show the file in the text editor
		public string Text {
			get {
				if (text is not null)
					return text;
				try {
					return text = File.ReadAllText(Filename);
				}
				catch {
					return text = $"Couldn't read the file: {Filename}";
				}
			}
		}
		string? text;

		public static MyDsDocument? TryCreate(string filename) {
			if (!File.Exists(filename))
				return null;
			return new MyDsDocument(filename);
		}

		MyDsDocument(string filename) => Filename = filename;
	}

	// Gets called by the IDsDocumentService instance to create IDsDocument instances. If it's a .txt file
	// or our MyDsDocument.THE_GUID, then create a MyDsDocument instance.
	[Export(typeof(IDsDocumentProvider))]
	sealed class MyDsDocumentProvider : IDsDocumentProvider {
		public double Order => 0;

		public IDsDocument? Create(IDsDocumentService documentService, DsDocumentInfo documentInfo) {
			if (documentInfo.Type == MyDsDocument.THE_GUID)
				return MyDsDocument.TryCreate(documentInfo.Name);
			// Also check for normal files
			if (documentInfo.Type == DocumentConstants.DOCUMENTTYPE_FILE && IsSupportedFile(documentInfo.Name))
				return MyDsDocument.TryCreate(documentInfo.Name);
			return null;
		}

		public IDsDocumentNameKey? CreateKey(IDsDocumentService documentService, DsDocumentInfo documentInfo) {
			if (documentInfo.Type == MyDsDocument.THE_GUID)
				return new FilenameKey(documentInfo.Name);  // Must match the key in MyDsDocument.Key
			// Also check for normal files
			if (documentInfo.Type == DocumentConstants.DOCUMENTTYPE_FILE && IsSupportedFile(documentInfo.Name))
				return new FilenameKey(documentInfo.Name);  // Must match the key in MyDsDocument.Key
			return null;
		}

		static bool IsSupportedFile(string filename) {
			foreach (var ext in supportedExtensions) {
				if (filename.EndsWith(ext, StringComparison.OrdinalIgnoreCase))
					return true;
			}
			return false;
		}
		static readonly string[] supportedExtensions = new string[] {
			".txt", ".xml", ".cs", ".vb",
		};
	}

	// Gets called by dnSpy to create a DsDocumentNode
	[ExportDsDocumentNodeProvider]
	sealed class MyDsDocumentNodeProvider : IDsDocumentNodeProvider {
		public DsDocumentNode? Create(IDocumentTreeView documentTreeView, DsDocumentNode? owner, IDsDocument document) {
			if (document is MyDsDocument myDocument)
				return new MyDsDocumentNode(myDocument);
			return null;
		}
	}

	// Our MyDsDocument tree node class. It implements IDecompileSelf to "decompile" itself. You could
	// also export a IDecompileNode instance to do it, see TreeNodeDataProvider.cs for an example.
	// Or you could create a completely new DocumentTabContent for these nodes, see AssemblyChildNodeTabContent.cs
	sealed class MyDsDocumentNode : DsDocumentNode, IDecompileSelf {
		//TODO: Use your own guid
		public static readonly Guid THE_GUID = new Guid("4174A21D-D746-4658-9A44-DB8235EE5186");

		readonly MyDsDocument document;

		public override Guid Guid => THE_GUID;

		public MyDsDocumentNode(MyDsDocument document)
			: base(document) => this.document = document;

		protected override ImageReference GetIcon(IDotNetImageService dnImgMgr) => DsImages.TextFile;
		protected override void WriteCore(ITextColorWriter output, IDecompiler decompiler, DocumentNodeWriteOptions options) =>
			output.WriteFilename(Path.GetFileName(document.Filename));

		public bool Decompile(IDecompileNodeContext context) {
			context.ContentTypeString = ContentTypes.PlainText;
			context.Output.Write(document.Text, BoxedTextColor.Text);
			return true;
		}
	}
}
