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
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.Text;

namespace dnSpy.Text.Editor {
	struct UriFinder {
		readonly string line;
		int lineIndex;

		public UriFinder(string line) {
			if (line == null)
				throw new ArgumentNullException(nameof(line));
			this.line = line;
			this.lineIndex = 0;
		}

		public Span? GetNext() {
			for (;;) {
				if (lineIndex >= line.Length)
					return null;

				var match = regex.Match(line, lineIndex);
				if (!match.Success) {
					lineIndex = line.Length;
					return null;
				}

				var index = match.Index;
				var length = match.Length;

				// Ignore periods at the end, could be end of sentence
				while (length > 0) {
					var c = line[index + length - 1];
					if (c == '.' || c == ';')
						length--;
					else
						break;
				}

				// Allow URIs inside brackets without including the closing bracket
				//	Example: "If not, see <http://www.gnu.org/licenses/>."
				if (index > 0 && length > 1) {
					if (IsClosingBracket(line[index - 1], line[index + length - 1]))
						length--;
				}

				if (length == 0) {
					lineIndex++;
					continue;
				}

				var res = new Span(index, length);
				lineIndex = res.End;
				return res;
			}
		}

		bool IsClosingBracket(char c, char other) {
			switch (c) {
			case '(': return other == ')';
			case '<': return other == '>';
			case '{': return other == '}';
			case '[': return other == ']';
			case '"': return other == '"';
			default: return false;
			}
		}

		// https://gist.github.com/dperini/729294 (slightly modified)
		static readonly Regex regex = new Regex(
			// protocol identifier
			"(?:(?:https?|ftp)://)" +
			// user:pass authentication
			"(?:\\S+(?::\\S*)?@)?" +
			"(?:" +
			  "(?:\\d{1,3}(?:\\.\\d{1,3}){3})" +
			"|" +
			  // host name
			  "(?:(?:[a-z\\u00a1-\\uffff0-9]-*)*[a-z\\u00a1-\\uffff0-9]+)" +
			  // domain name
			  "(?:\\.(?:[a-z\\u00a1-\\uffff0-9]-*)*[a-z\\u00a1-\\uffff0-9]+)*" +
			  // TLD identifier
			  "(?:\\.(?:[a-z\\u00a1-\\uffff]{2,}))" +
			  // TLD may end with dot
			  "\\.?" +
			")" +
			// port number
			"(?::\\d{2,5})?" +
			// resource path
			"(?:[/?#]\\S*)?"
		, RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline);
	}
}
