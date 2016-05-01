/*
    Copyright (C) 2014-2016 de4dot@gmail.com

    This file is part of dnSpy

    dnSpy is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    dnSpy is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with dnSpy.  If not, see <http://www.gnu.org/licenses/>.
*/

using System;
using System.IO;
using System.Reflection;
using System.Text;
using dnlib.DotNet.Resources;
using dnSpy.Contracts.Files.Tabs.TextEditor;
using dnSpy.Contracts.Files.TreeView;
using dnSpy.Contracts.Files.TreeView.Resources;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.TextEditor;
using dnSpy.Decompiler.Shared;
using dnSpy.Shared.Highlighting;
using ICSharpCode.AvalonEdit.Utils;

namespace dnSpy.Shared.Files.TreeView.Resources {
	public static class ResourceUtils {
		public static ImageReference? TryGetImageReference(Assembly dnSpyAsm, string name) {
			// Don't use Path.GetExtension() since it can throw
			int index = name.LastIndexOf('.');
			string rsrcName = null;
			if (index >= 0) {
				switch (name.Substring(index + 1).ToUpperInvariant()) {
				case "CS":
					rsrcName = "CSharpFile";
					break;
				case "VB":
					rsrcName = "VisualBasicFile";
					break;
				case "TXT":
					rsrcName = "TextFile";
					break;
				case "XAML":
				case "BAML":
					rsrcName = "XamlFile";
					break;
				case "XML":
					rsrcName = "XmlFile";
					break;
				case "XSD":
					rsrcName = "XsdFile";
					break;
				case "XSLT":
					rsrcName = "XsltFile";
					break;
				}
			}
			if (rsrcName != null)
				return new ImageReference(dnSpyAsm, rsrcName);
			return null;
		}

		public static ResourceTypeCode FixUserType(this ResourceTypeCode code) {
			if (code < ResourceTypeCode.UserTypes)
				return code;
			return ResourceTypeCode.UserTypes;
		}

		public static MemoryStream StringToStream(string s) {
			var outStream = new MemoryStream();
			var writer = new StreamWriter(outStream, Encoding.UTF8);
			writer.Write(s);
			writer.Close();
			return new MemoryStream(outStream.ToArray());
		}

		public static void WriteOffsetComment(this ITextOutput output, IResourceDataProvider node, bool showOffsetComment) {
			if (!showOffsetComment)
				return;

			ulong fo = node.FileOffset;
			if (fo == 0)
				return;

			var mod = (node as IFileTreeNodeData).GetModule();
			var filename = mod == null ? null : mod.Location;
			output.WriteReference(string.Format("0x{0:X8}", fo), new AddressReference(filename, false, fo, node.Length), BoxedTextTokenKind.Comment);
			output.Write(": ", BoxedTextTokenKind.Comment);
		}

		public static string TryGetString(Stream stream) {
			if (stream == null)
				return null;

			stream.Position = 0;
			if (GuessFileType.DetectFileType(stream) == FileType.Binary)
				return null;

			stream.Position = 0;
			return FileReader.OpenStream(stream, Encoding.UTF8).ReadToEnd();
		}

		public static bool Decompile(IDecompileNodeContext context, Stream stream, string name) {
			if (stream == null || stream.Length > 500 * 1024)
				return false;

			stream.Position = 0;
			FileType type = GuessFileType.DetectFileType(stream);
			if (type == FileType.Binary)
				return false;

			stream.Position = 0;
			context.Output.Write(FileReader.OpenStream(stream, Encoding.UTF8).ReadToEnd(), BoxedTextTokenKind.Text);
			string ext;
			if (type == FileType.Xml)
				ext = ".xml";
			else {
				try {
					ext = Path.GetExtension(NameUtils.CleanName(name));
				}
				catch (ArgumentException) {
					ext = ".txt";
				}
			}
			context.HighlightingExtension = ext;
			context.ContentTypeGuid = ContentTypes.TryGetContentTypeGuidByExtension(ext) ?? new Guid(ContentTypes.PLAIN_TEXT);
			return true;
		}
	}
}
