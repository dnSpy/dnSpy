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
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Xml.Linq;
using dnSpy.DnTheme;
using dnSpy.NRefactory;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.Decompiler;
using ICSharpCode.ILSpy.TextView;
using ICSharpCode.ILSpy.XmlDoc;

namespace dnSpy.TextView {
	public sealed class SimpleHighlighter : IXmlDocOutput {
		public ITextOutput TextOutput {
			get { return output; }
		}
		readonly AvalonEditTextOutput output = new AvalonEditTextOutput();

		public bool IsEmpty {
			get { return output.TextLength == 0; }
		}

		bool needsNewLine = false;

		public void WriteNewLine() {
			output.WriteLine();
			needsNewLine = false;
		}

		public void WriteSpace() {
			if (needsNewLine)
				WriteNewLine();
			output.WriteSpace();
		}

		public void Write(string s, TextTokenType tokenType) {
			if (needsNewLine)
				WriteNewLine();
			output.Write(s, tokenType);
		}

		void InitializeNeedsNewLine() {
			var text = output.Text;
			needsNewLine = text.Length > 0 && !text.EndsWith(Environment.NewLine);
		}

		public bool WriteXmlDoc(string xmlDoc) {
			InitializeNeedsNewLine();
			bool res = XmlDocRenderer.WriteXmlDoc(this, xmlDoc);
			needsNewLine = false;
			return res;
		}

		public bool WriteXmlDocParameter(string xmlDoc, string paramName) {
			InitializeNeedsNewLine();
			bool res = WriteXmlDoc(this, xmlDoc, paramName, "param");
			needsNewLine = false;
			return res;
		}

		public bool WriteXmlDocGeneric(string xmlDoc, string gpName) {
			InitializeNeedsNewLine();
			bool res = WriteXmlDoc(this, xmlDoc, gpName, "typeparam");
			needsNewLine = false;
			return res;
		}

		static bool WriteXmlDoc(IXmlDocOutput output, string xmlDoc, string name, string xmlElemName) {
			if (xmlDoc == null || name == null)
				return false;
			try {
				var xml = XDocument.Load(new StringReader("<docroot>" + xmlDoc + "</docroot>"), LoadOptions.None);
				foreach (var pxml in xml.Root.Elements(xmlElemName)) {
					if ((string)pxml.Attribute("name") == name) {
						WriteXmlDocParameter(output, pxml);
						return true;
					}
				}
			}
			catch {
			}
			return false;
		}

		static void WriteXmlDocParameter(IXmlDocOutput output, XElement xml) {
			foreach (var elem in xml.DescendantNodes()) {
				if (elem is XText)
					output.Write(XmlDocRenderer.whitespace.Replace(((XText)elem).Value, " "), TextTokenType.XmlDocSummary);
				else if (elem is XElement) {
					var xelem = (XElement)elem;
					switch (xelem.Name.ToString().ToUpperInvariant()) {
					case "SEE":
						var cref = xelem.Attribute("cref");
						if (cref != null)
							output.Write(XmlDocRenderer.GetCref((string)cref), TextTokenType.XmlDocToolTipSeeCref);
						var langword = xelem.Attribute("langword");
						if (langword != null)
							output.Write(((string)langword).Trim(), TextTokenType.XmlDocToolTipSeeLangword);
						break;
					case "PARAMREF":
						var nameAttr = xml.Attribute("name");
						if (nameAttr != null)
							output.Write(((string)nameAttr).Trim(), TextTokenType.XmlDocToolTipParamRefName);
						break;
					case "BR":
					case "PARA":
						output.WriteNewLine();
						break;
					default:
						break;
					}
				}
				else
					output.Write(elem.ToString(), TextTokenType.XmlDocSummary);
			}
		}

		IEnumerable<Tuple<string, int>> GetLines(string s) {
			var sb = new StringBuilder();
			for (int offs = 0; offs < s.Length;) {
				sb.Clear();
				while (offs < s.Length && s[offs] != '\r' && s[offs] != '\n')
					sb.Append(s[offs++]);
				int nlLen;
				if (offs >= s.Length)
					nlLen = 0;
				else if (s[offs] == '\n')
					nlLen = 1;
				else if (offs + 1 < s.Length && s[offs + 1] == '\n')
					nlLen = 2;
				else
					nlLen = 1;
				yield return Tuple.Create(sb.ToString(), nlLen);
				offs += nlLen;
			}
		}

		public TextBlock Create(bool useEllipsis = false, bool filterOutNewLines = false) {
			var textBlockText = output.Text;
			var tokens = output.LanguageTokens;
			tokens.Finish();

			var textBlock = new TextBlock();

			int offs = 0;
			foreach (var line in GetLines(textBlockText)) {
				if (offs != 0 && !filterOutNewLines)
					textBlock.Inlines.Add(new LineBreak());
				int endOffs = offs + line.Item1.Length;
				Debug.Assert(offs <= textBlockText.Length);

				while (offs < endOffs) {
					int defaultTextLength, tokenLength;
					TextTokenType tokenType;
					if (!tokens.Find(offs, out defaultTextLength, out tokenType, out tokenLength)) {
						Debug.Fail("Could not find token info");
						break;
					}

					if (defaultTextLength != 0) {
						var text = textBlockText.Substring(offs, defaultTextLength);
						textBlock.Inlines.Add(text);
					}
					offs += defaultTextLength;

					if (tokenLength != 0) {
						var hlColor = GetColor(tokenType);
						var text = textBlockText.Substring(offs, tokenLength);
						var elem = new Run(text);
						if (hlColor.FontStyle != null)
							elem.FontStyle = hlColor.FontStyle.Value;
						if (hlColor.FontWeight != null)
							elem.FontWeight = hlColor.FontWeight.Value;
						if (hlColor.Foreground != null)
							elem.Foreground = hlColor.Foreground.GetBrush(null);
						if (hlColor.Background != null)
							elem.Background = hlColor.Background.GetBrush(null);
						textBlock.Inlines.Add(elem);
					}
					offs += tokenLength;
				}
				Debug.Assert(offs == endOffs);
				offs += line.Item2;
				Debug.Assert(offs <= textBlockText.Length);
			}

			if (useEllipsis)
				textBlock.TextTrimming = TextTrimming.CharacterEllipsis;
			return textBlock;
		}

		HighlightingColor GetColor(TextTokenType tokenType) {
			var color = Themes.Theme.GetColor(tokenType).TextInheritedColor;
			Debug.Assert(color != null);
			return color;
		}
	}
}
