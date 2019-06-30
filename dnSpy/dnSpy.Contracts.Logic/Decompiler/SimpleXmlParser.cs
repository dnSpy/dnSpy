/*
    Copyright (C) 2014-2019 de4dot@gmail.com

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

using System.Collections.Generic;
using dnSpy.Contracts.Text;

namespace dnSpy.Contracts.Decompiler {
	// We have to parse it ourselves since we'd get all sorts of exceptions if we let
	// the standard XML reader try to parse it, even if we set the data to Fragment.
	// Since it only operates on one line at a time (no extra context), it won't be
	// able to handle eg. attributes spanning more than one line, but this rarely happens.
	static class SimpleXmlParser {
		static readonly char[] specialChars = new char[] { '<' };
		static readonly char[] specialCharsTag = new char[] { '<', '>', '"' };

		public static IEnumerable<(string text, object color)> Parse(string text) {
			bool inTag = true;
			int index = 0;
			while (index < text.Length) {
				int specialIndex = text.IndexOfAny(inTag ? specialCharsTag : specialChars, index);
				if (specialIndex < 0) {
					yield return (text.Substring(index), BoxedTextColor.XmlDocCommentText);
					break;
				}

				var c = text[specialIndex];
				if (c == '>') {
					yield return (text.Substring(index, specialIndex - index + 1), BoxedTextColor.XmlDocCommentText);
					index = specialIndex + 1;
				}
				else {
					if (specialIndex - index > 0) {
						if (c == '<')
							yield return (text.Substring(index, specialIndex - index), BoxedTextColor.XmlDocCommentText);
						else // c == '"'
							yield return (text.Substring(index, specialIndex - index), (inTag ? BoxedTextColor.XmlDocCommentName : BoxedTextColor.XmlDocCommentText));
					}

					index = specialIndex;
					int endIndex = text.IndexOf('>', index);
					endIndex = endIndex < 0 ? text.Length : endIndex + 1;

					while (index < endIndex) {
						int attrIndex = text.IndexOf('"', index, endIndex - index);
						if (attrIndex < 0) {
							yield return (text.Substring(index, endIndex - index), BoxedTextColor.XmlDocCommentName);
							break;
						}

						if (attrIndex - index > 0)
							yield return (text.Substring(index, attrIndex - index), BoxedTextColor.XmlDocCommentName);

						int endAttrIndex = text.IndexOf('"', attrIndex + 1, endIndex - attrIndex - 1);
						if (endAttrIndex < 0) {
							yield return (text.Substring(attrIndex, endIndex - attrIndex), BoxedTextColor.XmlDocCommentAttributeValue);
							break;
						}

						yield return (text.Substring(attrIndex, endAttrIndex - attrIndex + 1), BoxedTextColor.XmlDocCommentAttributeValue);
						index = endAttrIndex + 1;
					}

					index = endIndex;
				}
				inTag = false;
			}
		}
	}
}
