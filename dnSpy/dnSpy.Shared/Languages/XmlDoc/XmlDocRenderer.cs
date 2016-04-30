// Copyright (c) 2011 AlphaSierraPapa for the SharpDevelop Team
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this
// software and associated documentation files (the "Software"), to deal in the Software
// without restriction, including without limitation the rights to use, copy, modify, merge,
// publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons
// to whom the Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or
// substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
// PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE
// FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
// OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.

using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using dnSpy.Decompiler.Shared;

namespace dnSpy.Shared.Languages.XmlDoc {
	public interface IXmlDocOutput {
		void WriteNewLine();
		void WriteSpace();
		void Write(string s, TextTokenKind tokenKind);
	}

	/// <summary>
	/// Renders XML documentation into a WPF <see cref="TextBlock"/>.
	/// </summary>
	public class XmlDocRenderer : IXmlDocOutput {
		readonly StringBuilder ret = new StringBuilder();

		void IXmlDocOutput.WriteNewLine() {
			ret.AppendLine();
		}

		void IXmlDocOutput.WriteSpace() {
			ret.Append(' ');
		}

		void IXmlDocOutput.Write(string s, TextTokenKind tokenKind) {
			ret.Append(s);
		}

		public void AppendText(string text) {
			ret.Append(text);
		}

		public void AddXmlDocumentation(string xmlDocumentation) {
			WriteXmlDoc(this, xmlDocumentation);
		}

		public static bool WriteXmlDoc(IXmlDocOutput output, string xmlDocumentation) {
			if (xmlDocumentation == null)
				return false;
			try {
				XmlTextReader r = new XmlTextReader(new StringReader("<docroot>" + xmlDocumentation + "</docroot>"));
				r.XmlResolver = null;
				AddXmlDocumentation(output, r);
			}
			catch (XmlException) {
			}
			return true;
		}

		public static Regex WhitespaceRegex {
			get { return whitespace; }
		}
		static readonly Regex whitespace = new Regex(@"\s+");

		static void AddXmlDocumentation(IXmlDocOutput output, XmlReader xml) {
			string lastElemName = string.Empty;
			bool isNewLine = true;
			while (xml.Read()) {
				if (xml.NodeType == XmlNodeType.Element) {
					string elname = xml.Name.ToLowerInvariant();
					if (!xml.IsEmptyElement)
						lastElemName = elname;
					switch (elname) {
					case "filterpriority":
					case "remarks":
						xml.Skip();
						break;
					case "example":
						output.WriteNewLine();
						output.Write("Example", TextTokenKind.XmlDocToolTipExample);
						output.Write(":", TextTokenKind.XmlDocToolTipColon);
						output.WriteNewLine();
						isNewLine = true;
						break;
					case "exception":
						output.WriteNewLine();
						output.Write(GetCref(xml["cref"]), TextTokenKind.XmlDocToolTipExceptionCref);
						output.Write(":", TextTokenKind.XmlDocToolTipColon);
						output.WriteSpace();
						isNewLine = false;
						break;
					case "returns":
						output.WriteNewLine();
						output.Write("Returns", TextTokenKind.XmlDocToolTipReturns);
						output.Write(":", TextTokenKind.XmlDocToolTipColon);
						output.WriteSpace();
						isNewLine = false;
						break;
					case "see":
						output.Write(GetCref(xml["cref"]), TextTokenKind.XmlDocToolTipSeeCref);
						output.Write((xml["langword"] ?? string.Empty).Trim(), TextTokenKind.XmlDocToolTipSeeLangword);
						isNewLine = false;
						break;
					case "seealso":
						output.WriteNewLine();
						output.Write("See also", TextTokenKind.XmlDocToolTipSeeAlso);
						output.Write(":", TextTokenKind.XmlDocToolTipColon);
						output.WriteSpace();
						output.Write(GetCref(xml["cref"]), TextTokenKind.XmlDocToolTipSeeAlsoCref);
						isNewLine = false;
						break;
					case "paramref":
						output.Write((xml["name"] ?? string.Empty).Trim(), TextTokenKind.XmlDocToolTipParamRefName);
						isNewLine = false;
						break;
					case "param":
						output.WriteNewLine();
						output.Write(whitespace.Replace((xml["name"] ?? string.Empty).Trim(), " "), TextTokenKind.XmlDocToolTipParamName);
						output.Write(":", TextTokenKind.XmlDocToolTipColon);
						output.WriteSpace();
						isNewLine = false;
						break;
					case "typeparam":
						output.WriteNewLine();
						output.Write(whitespace.Replace((xml["name"] ?? string.Empty).Trim(), " "), TextTokenKind.XmlDocToolTipTypeParamName);
						output.Write(":", TextTokenKind.XmlDocToolTipColon);
						output.WriteSpace();
						isNewLine = false;
						break;
					case "value":
						output.WriteNewLine();
						output.Write("Value", TextTokenKind.XmlDocToolTipValue);
						output.Write(":", TextTokenKind.XmlDocToolTipColon);
						output.WriteNewLine();
						isNewLine = true;
						break;
					case "br":
					case "para":
						output.WriteNewLine();
						isNewLine = true;
						break;
					default:
						break;
					}
				}
				else if (xml.NodeType == XmlNodeType.Text) {
					var s = whitespace.Replace(xml.Value, " ");
					if (isNewLine)
						s = s.TrimStart();
					output.Write(s, lastElemName == "summary" ? TextTokenKind.XmlDocToolTipSummary : TextTokenKind.XmlDocToolTipText);
					isNewLine = false;
				}
			}
		}

		public static string GetCref(string cref) {
			if (string.IsNullOrWhiteSpace(cref))
				return string.Empty;
			if (cref.Length < 2) {
				return cref.Trim();
			}
			if (cref.Substring(1, 1) == ":") {
				return cref.Substring(2, cref.Length - 2).Trim();
			}
			return cref.Trim();
		}

		public override string ToString() {
			return ret.ToString();
		}
	}
}
