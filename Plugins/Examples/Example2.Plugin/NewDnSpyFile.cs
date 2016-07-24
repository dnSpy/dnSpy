using System;
using System.ComponentModel.Composition;
using System.IO;
using dnSpy.Contracts.Files;
using dnSpy.Contracts.Files.Tabs.DocViewer;
using dnSpy.Contracts.Files.TreeView;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.Languages;
using dnSpy.Contracts.Text;

// Adds a new IDnSpyFile that can be loaded into the file treeview. It gets its own IDnSpyFileNode.
// Open a .txt/.xml/.cs/.vb (see supportedExtensions) file to trigger this code.

namespace Example2.Plugin {
	// All root nodes in the file treeview contain a IDnSpyFile instance. They don't need to be
	// .NET files or even PE files, they can be any file or even non-file (eg. in-memory data).
	sealed class MyDnSpyFile : DnSpyFile {
		//TODO: Use your own guid
		public static readonly Guid THE_GUID = new Guid("9058B02C-1FE0-4EC4-93D3-A378D4B6FCE1");

		// We support serialization, so return a non-null value
		public override DnSpyFileInfo? SerializedFile => new DnSpyFileInfo(Filename, THE_GUID);

		// Since we open files from disk, we return a FilenameKey.
		// If this gets changed, also update MyDnSpyFileCreator.CreateKey()
		public override IDnSpyFilenameKey Key => new FilenameKey(Filename);

		// Used by MyDnSpyFileNode.Decompile() to show the file in the text editor
		public string Text {
			get {
				if (text != null)
					return text;
				try {
					return text = File.ReadAllText(Filename);
				}
				catch {
					return text = string.Format("Couldn't read the file: {0}", Filename);
				}
			}
		}
		string text;

		public static MyDnSpyFile TryCreate(string filename) {
			if (!File.Exists(filename))
				return null;
			return new MyDnSpyFile(filename);
		}

		MyDnSpyFile(string filename) {
			this.Filename = filename;
		}
	}

	// Gets called by the IFileManager instance to create IDnSpyFile instances. If it's a .txt file
	// or our MyDnSpyFile.THE_GUID, then create a MyDnSpyFile instance.
	[Export(typeof(IDnSpyFileCreator))]
	sealed class MyDnSpyFileCreator : IDnSpyFileCreator {
		public double Order => 0;

		public IDnSpyFile Create(IFileManager fileManager, DnSpyFileInfo fileInfo) {
			if (fileInfo.Type == MyDnSpyFile.THE_GUID)
				return MyDnSpyFile.TryCreate(fileInfo.Name);
			// Also check for normal files
			if (fileInfo.Type == FileConstants.FILETYPE_FILE && IsSupportedFile(fileInfo.Name))
				return MyDnSpyFile.TryCreate(fileInfo.Name);
			return null;
		}

		public IDnSpyFilenameKey CreateKey(IFileManager fileManager, DnSpyFileInfo fileInfo) {
			if (fileInfo.Type == MyDnSpyFile.THE_GUID)
				return new FilenameKey(fileInfo.Name);  // Must match the key in MyDnSpyFile.Key
			// Also check for normal files
			if (fileInfo.Type == FileConstants.FILETYPE_FILE && IsSupportedFile(fileInfo.Name))
				return new FilenameKey(fileInfo.Name);  // Must match the key in MyDnSpyFile.Key
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

	// Gets called by dnSpy to create a IDnSpyFileNode
	[ExportDnSpyFileNodeCreator]
	sealed class MyDnSpyFileNodeCreator : IDnSpyFileNodeCreator {
		public IDnSpyFileNode Create(IFileTreeView fileTreeView, IDnSpyFileNode owner, IDnSpyFile file) {
			var myFile = file as MyDnSpyFile;
			if (myFile != null)
				return new MyDnSpyFileNode(myFile);
			return null;
		}
	}

	// Our MyDnSpyFile tree node class. It implements IDecompileSelf to "decompile" itself. You could
	// also export a IDecompileNode instance to do it, see TreeNodeDataCreator.cs for an example.
	// Or you could create a completely new IFileTabContent for these nodes, see AssemblyChildNodeTabContent.cs
	sealed class MyDnSpyFileNode : FileTreeNodeData, IDnSpyFileNode, IDecompileSelf {
		//TODO: Use your own guid
		public static readonly Guid THE_GUID = new Guid("4174A21D-D746-4658-9A44-DB8235EE5186");

		public IDnSpyFile DnSpyFile => file;
		readonly MyDnSpyFile file;

		public override Guid Guid => THE_GUID;
		public override NodePathName NodePathName => new NodePathName(Guid, file.Filename.ToUpperInvariant());

		public MyDnSpyFileNode(MyDnSpyFile file) {
			this.file = file;
		}

		protected override ImageReference GetIcon(IDotNetImageManager dnImgMgr) =>
			new ImageReference(GetType().Assembly, "TextFile");

		protected override void Write(IOutputColorWriter output, ILanguage language) {
			output.WriteFilename(Path.GetFileName(file.Filename));
		}

		public bool Decompile(IDecompileNodeContext context) {
			context.ContentTypeString = ContentTypes.PlainText;
			context.Output.Write(file.Text, BoxedOutputColor.Text);
			return true;
		}
	}
}
