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
using dnSpy.Contracts.Images;
using dnSpy.Contracts.Text;
using dnSpy.Decompiler.Shared;
using ICSharpCode.AvalonEdit.Utils;

namespace dnSpy.Contracts.Files.TreeView.Resources {
	/// <summary>
	/// Resource utilities
	/// </summary>
	public static class ResourceUtilities {
		/// <summary>
		/// Creates an image reference
		/// </summary>
		/// <param name="dnSpyAsm">dnSpy assembly</param>
		/// <param name="name">Name of resource element</param>
		/// <returns></returns>
		public static ImageReference? TryGetImageReference(Assembly dnSpyAsm, string name) {
			// Don't use Path.GetExtension() since it can throw
			int index = name.LastIndexOf('.');
			string rsrcName = null;
			if (index >= 0) {
				switch (name.Substring(index + 1).ToUpperInvariant()) {
				case "CS":
				case "CSX":
					rsrcName = "CSharpFile";
					break;
				case "VB":
				case "VBX":
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

		/// <summary>
		/// Returns <see cref="ResourceTypeCode.UserTypes"/> if it's a user type, else <paramref name="code"/> is returned
		/// </summary>
		/// <param name="code">Resource type code</param>
		/// <returns></returns>
		public static ResourceTypeCode FixUserType(this ResourceTypeCode code) {
			if (code < ResourceTypeCode.UserTypes)
				return code;
			return ResourceTypeCode.UserTypes;
		}

		/// <summary>
		/// Converts a string to a stream
		/// </summary>
		/// <param name="s">String</param>
		/// <returns></returns>
		public static MemoryStream StringToStream(string s) {
			var outStream = new MemoryStream();
			var writer = new StreamWriter(outStream, Encoding.UTF8);
			writer.Write(s);
			writer.Close();
			return new MemoryStream(outStream.ToArray());
		}

		/// <summary>
		/// Writes the offset
		/// </summary>
		/// <param name="output">Output</param>
		/// <param name="node">Node</param>
		/// <param name="showOffsetComment">true if the offset and comment should be written</param>
		public static void WriteOffsetComment(this ITextOutput output, IResourceDataProvider node, bool showOffsetComment) {
			if (!showOffsetComment)
				return;

			ulong fo = node.FileOffset;
			if (fo == 0)
				return;

			var mod = (node as IFileTreeNodeData).GetModule();
			var filename = mod == null ? null : mod.Location;
			output.WriteReference($"0x{fo:X8}", new AddressReference(filename, false, fo, node.Length), BoxedOutputColor.Comment);
			output.Write(": ", BoxedOutputColor.Comment);
		}

		/// <summary>
		/// Returns the string contents of <paramref name="stream"/> if it's text, else null is returned
		/// </summary>
		/// <param name="stream">Stream</param>
		/// <returns></returns>
		public static string TryGetString(Stream stream) {
			if (stream == null)
				return null;

			stream.Position = 0;
			if (GuessFileType.DetectFileType(stream) == FileType.Binary)
				return null;

			stream.Position = 0;
			return FileReader.OpenStream(stream, Encoding.UTF8).ReadToEnd();
		}

		/// <summary>
		/// "Decompiles" the data
		/// </summary>
		/// <param name="context">Context</param>
		/// <param name="stream">Stream</param>
		/// <param name="name">Name</param>
		/// <returns></returns>
		public static bool Decompile(IDecompileNodeContext context, Stream stream, string name) {
			if (stream == null || stream.Length > 500 * 1024)
				return false;

			stream.Position = 0;
			FileType type = GuessFileType.DetectFileType(stream);
			if (type == FileType.Binary)
				return false;

			stream.Position = 0;
			context.Output.Write(FileReader.OpenStream(stream, Encoding.UTF8).ReadToEnd(), BoxedOutputColor.Text);
			string ext;
			if (type == FileType.Xml)
				ext = ".xml";
			else {
				try {
					ext = Path.GetExtension(NameUtilities.CleanName(name));
				}
				catch (ArgumentException) {
					ext = ".txt";
				}
			}
			context.ContentTypeString = ContentTypes.TryGetContentTypeStringByExtension(ext) ?? ContentTypes.PLAIN_TEXT;
			return true;
		}
	}
}
