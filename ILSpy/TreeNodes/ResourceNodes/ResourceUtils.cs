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

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows.Media.Imaging;

namespace ICSharpCode.ILSpy.TreeNodes
{
	static class ResourceUtils
	{
		static readonly HashSet<char> invalidFileNameChar = new HashSet<char>();
		static ResourceUtils()
		{
			invalidFileNameChar.AddRange(Path.GetInvalidFileNameChars());
			invalidFileNameChar.AddRange(Path.GetInvalidPathChars());
		}

		public static string GetIconName(string name)
		{
			return GetIconName(name, "Resource");
		}

		public static string GetIconName(string name, string rsrcName)
		{
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

		public static BitmapSource GetIcon(string name, BackgroundType bgType)
		{
			return ImageCache.Instance.GetImage(name, bgType);
		}

		public static MemoryStream StringToStream(string s)
		{
			var outStream = new MemoryStream();
			var writer = new StreamWriter(outStream, Encoding.UTF8);
			writer.Write(s);
			writer.Close();
			return new MemoryStream(outStream.ToArray());
		}

		public static string GetFileName(string s)
		{
			int index = Math.Max(s.LastIndexOf('/'), s.LastIndexOf('\\'));
			if (index < 0)
				return s;
			return s.Substring(index + 1);
		}

		public static string FixFileNamePart(string s)
		{
			var sb = new StringBuilder(s.Length);

			foreach (var c in s) {
				if (invalidFileNameChar.Contains(c))
					sb.Append('_');
				else
					sb.Append(c);
			}

			return sb.ToString();
		}

		public static string GetCleanedPath(string s, bool useSubDirs)
		{
			if (!useSubDirs)
				return FixFileNamePart(GetFileName(s));

			string res = string.Empty;
			foreach (var part in s.Replace('/', '\\').Split('\\'))
				res = Path.Combine(res, FixFileNamePart(part));
			return res;
		}

		public static Exception SaveFile(string path, Stream data)
		{
			bool deleteFile = false;
			try {
				using (data) {
					deleteFile = !File.Exists(path);
					Directory.CreateDirectory(Path.GetDirectoryName(path));
					using (var outputStream = File.Create(path))
						data.CopyTo(outputStream);
				}
			}
			catch (Exception ex) {
				if (deleteFile) {
					try {
						File.Delete(path);
					}
					catch {
					}
				}
				return ex;
			}

			return null;
		}
	}
}
