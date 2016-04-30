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

using System.Collections.Generic;

namespace dnSpy.Decompiler.Shared {
	// We have to parse it ourselves since we'd get all sorts of exceptions if we let
	// the standard XML reader try to parse it, even if we set the data to Fragment.
	// Since it only operates on one line at a time (no extra context), it won't be
	// able to handle eg. attributes spanning more than one line, but this rarely happens.
	static class SimpleXmlParser
	{
		static readonly char[] specialChars = new char[] { '<' };
		static readonly char[] specialCharsTag = new char[] { '<', '>', '"' };

		public static IEnumerable<KeyValuePair<string, object>> Parse(string text)
		{
			bool inTag = true;
			int index = 0;
			while (index < text.Length) {
				int specialIndex = text.IndexOfAny(inTag ? specialCharsTag : specialChars, index);
				if (specialIndex < 0) {
					yield return new KeyValuePair<string, object>(text.Substring(index), BoxedTextTokenKind.XmlDocCommentText);
					break;
				}

				var c = text[specialIndex];
				if (c == '>') {
					yield return new KeyValuePair<string, object>(text.Substring(index, specialIndex - index + 1), BoxedTextTokenKind.XmlDocCommentText);
					index = specialIndex + 1;
				}
				else {
					if (specialIndex - index > 0) {
						if (c == '<')
							yield return new KeyValuePair<string, object>(text.Substring(index, specialIndex - index), BoxedTextTokenKind.XmlDocCommentText);
						else // c == '"'
							yield return new KeyValuePair<string, object>(text.Substring(index, specialIndex - index), inTag ? BoxedTextTokenKind.XmlDocCommentName : BoxedTextTokenKind.XmlDocCommentText);
					}

					index = specialIndex;
					int endIndex = text.IndexOf('>', index);
					endIndex = endIndex < 0 ? text.Length : endIndex + 1;

					while (index < endIndex) {
						int attrIndex = text.IndexOf('"', index, endIndex - index);
						if (attrIndex < 0) {
							yield return new KeyValuePair<string, object>(text.Substring(index, endIndex - index), BoxedTextTokenKind.XmlDocCommentName);
							break;
						}

						if (attrIndex - index > 0)
							yield return new KeyValuePair<string, object>(text.Substring(index, attrIndex - index), BoxedTextTokenKind.XmlDocCommentName);

						int endAttrIndex = text.IndexOf('"', attrIndex + 1, endIndex - attrIndex - 1);
						if (endAttrIndex < 0) {
							yield return new KeyValuePair<string, object>(text.Substring(attrIndex, endIndex - attrIndex), BoxedTextTokenKind.XmlDocCommentAttributeValue);
							break;
						}

						yield return new KeyValuePair<string, object>(text.Substring(attrIndex, endAttrIndex - attrIndex + 1), BoxedTextTokenKind.XmlDocCommentAttributeValue);
						index = endAttrIndex + 1;
					}

					index = endIndex;
				}
				inTag = false;
			}
		}
	}
}
