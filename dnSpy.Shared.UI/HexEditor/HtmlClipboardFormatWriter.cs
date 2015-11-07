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
using System.Net;
using System.Text;

namespace dnSpy.Shared.UI.HexEditor {
	sealed class HtmlClipboardFormatWriter {
		const int TAB_SIZE = 4;

		readonly StringBuilder sb;

		public HtmlClipboardFormatWriter() {
			this.sb = new StringBuilder();
		}

		public void WriteBr() {
			sb.Append("<br />");
		}

		public void WriteSpan(string cssText, string spanText) {
			sb.Append("<span");
			if (cssText.Length > 0)
				sb.Append(string.Format(" style=\"{0}\"", WebUtility.HtmlEncode(cssText)));
			sb.Append('>');
			WriteString(spanText);
			sb.Append("</span>");
		}

		static readonly char[] whitespace = new char[] { '\r', '\n', '\t', ' ' };
		void WriteString(string s) {
			for (int i = 0; i < s.Length;) {
				int wsi = s.IndexOfAny(whitespace, i);
				if (wsi < 0) {
					sb.Append(WebUtility.HtmlEncode(s.Substring(i)));
					break;
				}
				if (wsi != i)
					sb.Append(WebUtility.HtmlEncode(s.Substring(i, wsi - i)));

				switch (s[wsi]) {
				case ' ':
					sb.Append("&nbsp;");
					break;
				case '\t':
					for (int j = 0; j < TAB_SIZE; j++)
						sb.Append("&nbsp;");
					break;
				case '\r':
					break;
				case '\n':
					WriteBr();
					break;
				default:
					throw new InvalidOperationException();
				}
				i = wsi + 1;
			}
		}

		static string GetHeader(int startHtml, int endHtml, int startFragment, int endFragment) {
			return string.Format(
				"Version:0.9" + Environment.NewLine +
				"StartHTML:{0:D10}" + Environment.NewLine +
				"EndHTML:{1:D10}" + Environment.NewLine +
				"StartFragment:{2:D10}" + Environment.NewLine +
				"EndFragment:{3:D10}" + Environment.NewLine,
				startHtml, endHtml, startFragment, endFragment);
		}

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
