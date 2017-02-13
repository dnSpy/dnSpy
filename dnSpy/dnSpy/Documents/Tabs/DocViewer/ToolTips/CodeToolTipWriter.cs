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
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows;
using System.Xml.Linq;
using dnSpy.Contracts.Decompiler.XmlDoc;
using dnSpy.Contracts.Documents.Tabs.DocViewer.ToolTips;
using dnSpy.Contracts.Text;
using dnSpy.Contracts.Text.Classification;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Formatting;

namespace dnSpy.Documents.Tabs.DocViewer.ToolTips {
	sealed class CodeToolTipWriter : ICodeToolTipWriter, IXmlDocOutput {
		readonly List<ColorAndText> result;
		readonly StringBuilder sb;
		readonly IClassificationFormatMap classificationFormatMap;
		readonly IThemeClassificationTypeService themeClassificationTypeService;
		readonly bool syntaxHighlight;

		public bool IsEmpty => sb.Length == 0;

		public CodeToolTipWriter(IClassificationFormatMap classificationFormatMap, IThemeClassificationTypeService themeClassificationTypeService, bool syntaxHighlight) {
			this.classificationFormatMap = classificationFormatMap ?? throw new ArgumentNullException(nameof(classificationFormatMap));
			this.themeClassificationTypeService = themeClassificationTypeService ?? throw new ArgumentNullException(nameof(themeClassificationTypeService));
			this.syntaxHighlight = syntaxHighlight;
			result = new List<ColorAndText>();
			sb = new StringBuilder();
		}

		public UIElement Create() {
			var text = sb.ToString();
			var propsSpans = CreateTextRunPropertiesAndSpans();
			return TextBlockFactory.Create(text, classificationFormatMap.DefaultTextProperties, propsSpans, TextBlockFactory.Flags.DisableSetTextBlockFontFamily | TextBlockFactory.Flags.DisableFontSize);
		}

		IEnumerable<TextRunPropertiesAndSpan> CreateTextRunPropertiesAndSpans() {
			int pos = 0;
			foreach (var res in result) {
				var props = GetTextFormattingRunProperties(res.Color);
				yield return new TextRunPropertiesAndSpan(new Span(pos, res.Text.Length), props);
				pos += res.Text.Length;
			}
		}

		TextFormattingRunProperties GetTextFormattingRunProperties(object color) {
			if (!syntaxHighlight)
				color = BoxedTextColor.Text;
			var classificationType = color as IClassificationType;
			if (classificationType == null) {
				var textColor = color as TextColor? ?? TextColor.Text;
				classificationType = themeClassificationTypeService.GetClassificationType(textColor);
			}
			return classificationFormatMap.GetTextProperties(classificationType);
		}

		void Add(object color, string text) {
			result.Add(new ColorAndText(color, text));
			sb.Append(text);
		}

		public void Write(IClassificationType classificationType, string text) => Add(classificationType, text);
		public void Write(object color, string text) => Add(color, text);
		public void Write(TextColor color, string text) => Add(color.Box(), text);

		bool needsNewLine = false;

		void IXmlDocOutput.Write(string s, object data) {
			if (needsNewLine)
				((IXmlDocOutput)this).WriteNewLine();
			Add(data, s);
		}

		void IXmlDocOutput.WriteNewLine() {
			Add(BoxedTextColor.Text, Environment.NewLine);
			needsNewLine = false;
		}

		void IXmlDocOutput.WriteSpace() => ((IXmlDocOutput)this).Write(" ", BoxedTextColor.Text);

		void InitializeNeedsNewLine() =>
			needsNewLine = sb.Length == 1 || (sb.Length >= 2 && (sb[sb.Length - 2] != '\r' || sb[sb.Length - 1] != '\n'));

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
					output.Write(XmlDocRenderer.WhitespaceRegex.Replace(((XText)elem).Value, " "), BoxedTextColor.Text);
				else if (elem is XElement) {
					var xelem = (XElement)elem;
					switch (xelem.Name.ToString().ToUpperInvariant()) {
					case "SEE":
						var cref = xelem.Attribute("cref");
						if (cref != null)
							output.Write(XmlDocRenderer.GetCref((string)cref), BoxedTextColor.Text);
						var langword = xelem.Attribute("langword");
						if (langword != null)
							output.Write(((string)langword).Trim(), BoxedTextColor.Keyword);
						break;
					case "PARAMREF":
						var nameAttr = xml.Attribute("name");
						if (nameAttr != null)
							output.Write(((string)nameAttr).Trim(), BoxedTextColor.Parameter);
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
					output.Write(elem.ToString(), BoxedTextColor.Text);
			}
		}
	}
}
