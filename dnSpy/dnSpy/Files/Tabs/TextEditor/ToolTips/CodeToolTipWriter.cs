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
using System.IO;
using System.Windows;
using System.Xml.Linq;
using dnSpy.Contracts.Files.Tabs.TextEditor.ToolTips;
using dnSpy.Decompiler.Shared;
using dnSpy.Shared.Highlighting;
using dnSpy.Shared.Languages.XmlDoc;

namespace dnSpy.Files.Tabs.TextEditor.ToolTips {
	sealed class CodeToolTipWriter : ICodeToolTipWriter, IXmlDocOutput {
		readonly UISyntaxHighlighter uiSyntaxHighlighter;

		public bool IsEmpty {
			get { return uiSyntaxHighlighter.IsEmpty; }
		}

		public CodeToolTipWriter(bool syntaxHighlight) {
			this.uiSyntaxHighlighter = UISyntaxHighlighter.Create(syntaxHighlight);
		}

		public UIElement Create() {
			return uiSyntaxHighlighter.CreateResult(false, false);
		}

		public void Write(string s, object data) => uiSyntaxHighlighter.Output.Write(s, data);

		bool needsNewLine = false;

		void IXmlDocOutput.Write(string s, object data) {
			if (needsNewLine)
				((IXmlDocOutput)this).WriteNewLine();
			uiSyntaxHighlighter.Output.Write(s, data);
		}

		void IXmlDocOutput.WriteNewLine() {
			uiSyntaxHighlighter.Output.WriteLine();
			needsNewLine = false;
		}

		void IXmlDocOutput.WriteSpace() {
			((IXmlDocOutput)this).Write(" ", BoxedTextTokenKind.Text);
		}

		void InitializeNeedsNewLine() {
			var text = uiSyntaxHighlighter.Text;
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
					output.Write(XmlDocRenderer.WhitespaceRegex.Replace(((XText)elem).Value, " "), BoxedTextTokenKind.XmlDocToolTipSummary);
				else if (elem is XElement) {
					var xelem = (XElement)elem;
					switch (xelem.Name.ToString().ToUpperInvariant()) {
					case "SEE":
						var cref = xelem.Attribute("cref");
						if (cref != null)
							output.Write(XmlDocRenderer.GetCref((string)cref), BoxedTextTokenKind.XmlDocToolTipSeeCref);
						var langword = xelem.Attribute("langword");
						if (langword != null)
							output.Write(((string)langword).Trim(), BoxedTextTokenKind.XmlDocToolTipSeeLangword);
						break;
					case "PARAMREF":
						var nameAttr = xml.Attribute("name");
						if (nameAttr != null)
							output.Write(((string)nameAttr).Trim(), BoxedTextTokenKind.XmlDocToolTipParamRefName);
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
					output.Write(elem.ToString(), BoxedTextTokenKind.XmlDocToolTipSummary);
			}
		}
	}
}
