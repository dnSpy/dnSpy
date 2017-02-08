/*
    Copyright (C) 2014-2017 de4dot@gmail.com

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

namespace dnSpy.Contracts.Text {
	static class ContentTypesHelper {
		/// <summary>
		/// Returns a content type or null if it's unknown
		/// </summary>
		/// <param name="extension">File extension, with or without the period</param>
		/// <returns></returns>
		internal static string TryGetContentTypeStringByExtension(string extension) {
			var comparer = StringComparer.InvariantCultureIgnoreCase;
			if (comparer.Equals(extension, ".txt") || comparer.Equals(extension, "txt"))
				return ContentTypes.PlainText;
			if (comparer.Equals(extension, ".xml") || comparer.Equals(extension, "xml"))
				return ContentTypes.Xml;
			if (comparer.Equals(extension, ".xaml") || comparer.Equals(extension, "xaml"))
				return ContentTypes.Xaml;
			if (comparer.Equals(extension, ".cs") || comparer.Equals(extension, "cs"))
				return ContentTypes.CSharp;
			if (comparer.Equals(extension, ".csx") || comparer.Equals(extension, "csx"))
				return ContentTypes.CSharp;
			if (comparer.Equals(extension, ".vb") || comparer.Equals(extension, "vb"))
				return ContentTypes.VisualBasic;
			if (comparer.Equals(extension, ".vbx") || comparer.Equals(extension, "vbx"))
				return ContentTypes.VisualBasic;
			if (comparer.Equals(extension, ".il") || comparer.Equals(extension, "il"))
				return ContentTypes.IL;

			return null;
		}
	}
}
