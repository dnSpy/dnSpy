/*
    Copyright (C) 2014-2015 de4dot@gmail.com

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

using System.IO;
using System.Reflection;
using System.Text;
using System.Windows.Media.Imaging;
using dnSpy.Contracts;
using dnSpy.Contracts.Images;
using dnSpy.Decompiler;
using dnSpy.NRefactory;
using ICSharpCode.Decompiler;
using ICSharpCode.ILSpy.Options;
using ICSharpCode.ILSpy.TreeNodes;
using ICSharpCode.TreeView;

namespace dnSpy.TreeNodes {
	static class ResourceUtils {
		public static string GetIconName(string name) {
			return GetIconName(name, "Resource");
		}

		public static string GetIconName(string name, string rsrcName) {
			// Don't use Path.GetExtension() since it can throw
			int index = name.LastIndexOf('.');
			if (index >= 0) {
				var ext = name.Substring(index + 1).ToLowerInvariant();
				if (ext == "cs")
					rsrcName = "CSharpFile";
				else if (ext == "vb")
					rsrcName = "VisualBasicFile";
				else if (ext == "txt")
					rsrcName = "TextFile";
				else if (ext == "xaml" || ext == "baml")
					rsrcName = "XamlFile";
				else if (ext == "xml")
					rsrcName = "XmlFile";
				else if (ext == "xsd")
					rsrcName = "XsdFile";
				else if (ext == "xslt")
					rsrcName = "XsltFile";
			}
			return rsrcName;
		}

		public static BitmapSource GetIcon(Assembly asm, string name, BackgroundType bgType) {
			return DnSpy.App.ImageManager.GetImage(asm, name, bgType);
		}

		public static MemoryStream StringToStream(string s) {
			var outStream = new MemoryStream();
			var writer = new StreamWriter(outStream, Encoding.UTF8);
			writer.Write(s);
			writer.Close();
			return new MemoryStream(outStream.ToArray());
		}

		public static void WriteOffsetComment(this ITextOutput output, IResourceNode node) {
			if (!DecompilerSettingsPanel.CurrentDecompilerSettings.ShowTokenAndRvaComments)
				return;

			ulong fo = node.FileOffset;
			if (fo == 0)
				return;

			var mod = ILSpyTreeNode.GetModule((SharpTreeNode)node);
			var filename = mod == null ? null : mod.Location;
			output.WriteReference(string.Format("0x{0:X8}", fo), new AddressReference(filename, false, fo, node.Length), TextTokenType.Comment);
			output.Write(": ", TextTokenType.Comment);
		}
	}
}
