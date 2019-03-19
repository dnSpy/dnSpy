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

using System;
using System.Net;
using System.Text;

namespace dnSpy.Contracts.Text.Formatting {
	sealed class HtmlClipboardFormatWriter {
		readonly StringBuilder sb;

		public int TabSize { get; set; }

		public HtmlClipboardFormatWriter() {
			sb = new StringBuilder();
			TabSize = 4;
		}

		public void WriteRaw(string s) => sb.Append(s);
		public void WriteBr() => sb.Append("<br/>");

		public void WriteSpan(string cssText, string spanText) =>
			WriteSpan(cssText, spanText, 0, spanText.Length);
		public void WriteSpan(string cssText, string spanText, int index, int length) {
			sb.Append("<span");
			if (cssText.Length > 0)
				sb.Append($" style=\"{WebUtility.HtmlEncode(cssText)}\"");
			sb.Append('>');
			WriteString(spanText, index, length);
			sb.Append("</span>");
		}

		static readonly char[] whitespace = new char[] { '\r', '\n', '\u0085', '\u2028', '\u2029', '\t', ' ' };
		void WriteString(string text, int index, int length) {
			for (int i = 0; i < length;) {
				int wsi = text.IndexOfAny(whitespace, index + i, length - i);
				if (wsi < 0) {
					sb.Append(WebUtility.HtmlEncode(text.Substring(index + i, length - i)));
					break;
				}
				if (wsi != index + i)
					sb.Append(WebUtility.HtmlEncode(text.Substring(index + i, wsi - (index + i))));

				switch (text[wsi]) {
				case ' ':
					sb.Append("&nbsp;");
					break;
				case '\t':
					for (int j = 0; j < TabSize; j++)
						sb.Append("&nbsp;");
					break;
				case '\r':
					break;
				case '\n':
				case '\u0085':
				case '\u2028':
				case '\u2029':
					WriteBr();
					break;
				default:
					throw new InvalidOperationException();
				}
				i = wsi + 1 - index;
			}
		}

		static string GetHeader(int startHtml, int endHtml, int startFragment, int endFragment) => string.Format(
				"Version:0.9" + Environment.NewLine +
				"StartHTML:{0:D10}" + Environment.NewLine +
				"EndHTML:{1:D10}" + Environment.NewLine +
				"StartFragment:{2:D10}" + Environment.NewLine +
				"EndFragment:{3:D10}" + Environment.NewLine,
				startHtml, endHtml, startFragment, endFragment);

		public override string ToString() {
			var header = GetHeader(0, 0, 0, 0);
			var prefix = "<html>" + Environment.NewLine + "<body>" + Environment.NewLine + "<!--StartFragment-->";
			var html = sb.ToString();
			var suffix = "<!--EndFragment-->" + Environment.NewLine + "</body>" + Environment.NewLine + "</html>";

			int startHtml = header.Length;
			int startFragment = startHtml + prefix.Length;
			int endFragment = startFragment + Encoding.UTF8.GetByteCount(html);
			int endHtml = endFragment + suffix.Length;

			return GetHeader(startHtml, endHtml, startFragment, endFragment) + prefix + html + suffix;
		}
	}
}
