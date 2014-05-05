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

using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Controls;
using System.Xml;

namespace ICSharpCode.ILSpy.XmlDoc
{
	/// <summary>
	/// Renders XML documentation into a WPF <see cref="TextBlock"/>.
	/// </summary>
	public class XmlDocRenderer
	{
		readonly StringBuilder ret = new StringBuilder();
		
		public void AppendText(string text)
		{
			ret.Append(text);
		}
		
		public void AddXmlDocumentation(string xmlDocumentation)
		{
			if (xmlDocumentation == null)
				return;
			Debug.WriteLine(xmlDocumentation);
			try {
				XmlTextReader r = new XmlTextReader(new StringReader("<docroot>" + xmlDocumentation + "</docroot>"));
				r.XmlResolver = null;
				AddXmlDocumentation(r);
			} catch (XmlException) {
			}
		}
		
		static readonly Regex whitespace = new Regex(@"\s+");
		
		public void AddXmlDocumentation(XmlReader xml)
		{
			while (xml.Read()) {
				if (xml.NodeType == XmlNodeType.Element) {
					string elname = xml.Name.ToLowerInvariant();
					switch (elname) {
						case "filterpriority":
						case "remarks":
							xml.Skip();
							break;
						case "example":
							ret.Append(Environment.NewLine);
							ret.Append("Example:");
							ret.Append(Environment.NewLine);
							break;
						case "exception":
							ret.Append(Environment.NewLine);
							ret.Append(GetCref(xml["cref"]));
							ret.Append(": ");
							break;
						case "returns":
							ret.Append(Environment.NewLine);
							ret.Append("Returns: ");
							break;
						case "see":
							ret.Append(GetCref(xml["cref"]));
							ret.Append(xml["langword"]);
							break;
						case "seealso":
							ret.Append(Environment.NewLine);
							ret.Append("See also: ");
							ret.Append(GetCref(xml["cref"]));
							break;
						case "paramref":
							ret.Append(xml["name"]);
							break;
						case "param":
							ret.Append(Environment.NewLine);
							ret.Append(whitespace.Replace(xml["name"].Trim()," "));
							ret.Append(": ");
							break;
						case "typeparam":
							ret.Append(Environment.NewLine);
							ret.Append(whitespace.Replace(xml["name"].Trim()," "));
							ret.Append(": ");
							break;
						case "value":
							ret.Append(Environment.NewLine);
							ret.Append("Value: ");
							ret.Append(Environment.NewLine);
							break;
						case "br":
						case "para":
							ret.Append(Environment.NewLine);
							break;
					}
				} else if (xml.NodeType == XmlNodeType.Text) {
					ret.Append(whitespace.Replace(xml.Value, " "));
				}
			}
		}
		
		static string GetCref(string cref)
		{
			if (cref == null || cref.Trim().Length==0) {
				return "";
			}
			if (cref.Length < 2) {
				return cref;
			}
			if (cref.Substring(1, 1) == ":") {
				return cref.Substring(2, cref.Length - 2);
			}
			return cref;
		}
		
		public TextBlock CreateTextBlock()
		{
			return new TextBlock { Text = ret.ToString() };
		}
	}
}
