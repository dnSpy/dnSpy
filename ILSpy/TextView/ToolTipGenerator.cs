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
using System.Windows.Controls;
using System.Windows.Documents;
using System.Xml.Linq;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.Decompiler;
using ICSharpCode.ILSpy.dntheme;
using ICSharpCode.ILSpy.XmlDoc;
using ICSharpCode.NRefactory;

namespace ICSharpCode.ILSpy.TextView
{
	sealed class ToolTipGenerator : IXmlDocOutput
	{
		public ITextOutput TextOutput {
			get { return output; }
		}
		readonly AvalonEditTextOutput output = new AvalonEditTextOutput();

		bool needsNewLine = false;

		public void WriteNewLine()
		{
			output.WriteLine();
			needsNewLine = false;
		}

		public void WriteSpace()
		{
			if (needsNewLine)
				WriteNewLine();
			output.WriteSpace();
		}

		public void Write(string s, TextTokenType tokenType)
		{
			if (needsNewLine)
				WriteNewLine();
			output.Write(s, tokenType);
		}

		public void WriteXmlDoc(string xmlDoc)
		{
			needsNewLine = !output.Text.EndsWith(Environment.NewLine);
			XmlDocRenderer.WriteXmlDoc(this, xmlDoc);
			needsNewLine = false;
		}

		public void WriteXmlDocParameter(string xmlDoc, string paramName)
		{
			needsNewLine = !output.Text.EndsWith(Environment.NewLine);
			WriteXmlDocParameter(this, xmlDoc, paramName);
			needsNewLine = false;
		}

		static void WriteXmlDocParameter(IXmlDocOutput output, string xmlDoc, string paramName)
		{
			if (xmlDoc == null)
				return;
			try {
				var xml = XDocument.Load(new StringReader("<docroot>" + xmlDoc + "</docroot>"), LoadOptions.None);
				foreach (var pxml in xml.Root.Elements("param")) {
					if ((string)pxml.Attribute("name") == paramName) {
						WriteXmlDocParameter(output, pxml);
						break;
					}
				}
			}
			catch {
			}
		}

		static void WriteXmlDocParameter(IXmlDocOutput output, XElement xml)
		{
			foreach (var elem in xml.DescendantNodes()) {
				if (elem is XText)
					output.Write(((XText)elem).Value, TextTokenType.XmlDocSummary);
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
						output.Write(elem.ToString(), TextTokenType.XmlDocSummary);
						break;
					}
				}
				else
					output.Write(elem.ToString(), TextTokenType.XmlDocSummary);
			}
		}

		IEnumerable<Tuple<string, int>> GetLines(string s)
		{
			var sb = new StringBuilder();
			for (int offs = 0; offs < s.Length; ) {
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

		public object Create()
		{
			var toolTipText = output.Text;
			var tokens = output.LanguageTokens;
			tokens.Finish();

			var tooltip = new TextBlock();

			int offs = 0;
			foreach (var line in GetLines(toolTipText)) {
				if (offs != 0)
					tooltip.Inlines.Add(new LineBreak());
				int endOffs = offs + line.Item1.Length;
				Debug.Assert(offs <= toolTipText.Length);

				while (offs < endOffs) {
					int defaultTextLength, tokenLength;
					TextTokenType tokenType;
					if (!tokens.Find(offs, out defaultTextLength, out tokenType, out tokenLength)) {
						Debug.Fail("Could not find token info");
						break;
					}

					if (defaultTextLength != 0) {
						var text = toolTipText.Substring(offs, defaultTextLength);
						tooltip.Inlines.Add(text);
					}
					offs += defaultTextLength;

					if (tokenLength != 0) {
						var hlColor = GetColor(tokenType);
						var text = toolTipText.Substring(offs, tokenLength);
						var elem = new Run(text);
						if (hlColor.FontStyle != null)
							elem.FontStyle = hlColor.FontStyle.Value;
						if (hlColor.FontWeight != null)
							elem.FontWeight = hlColor.FontWeight.Value;
						if (hlColor.Foreground != null)
							elem.Foreground = hlColor.Foreground.GetBrush(null);
						if (hlColor.Background != null)
							elem.Background = hlColor.Background.GetBrush(null);
						tooltip.Inlines.Add(elem);
					}
					offs += tokenLength;
				}
				Debug.Assert(offs == endOffs);
				offs += line.Item2;
				Debug.Assert(offs <= toolTipText.Length);
			}

			return tooltip;
		}

		HighlightingColor GetColor(TextTokenType tokenType)
		{
			var color = Themes.Theme.GetColor(tokenType).TextInheritedColor;
			Debug.Assert(color != null);
			return color;
		}
	}
}
