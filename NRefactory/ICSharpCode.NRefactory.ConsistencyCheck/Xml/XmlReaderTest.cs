// Copyright (c) AlphaSierraPapa for the SharpDevelop Team
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
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml;
using System.Xml.Linq;
using ICSharpCode.NRefactory.CSharp;
using ICSharpCode.NRefactory.Editor;
using ICSharpCode.NRefactory.Xml;

namespace ICSharpCode.NRefactory.ConsistencyCheck.Xml
{
	public static class XmlReaderTest
	{
		public static void Run(string fileName)
		{
			var textSource = new StringTextSource(File.ReadAllText(fileName));
			using (var textReader = textSource.CreateReader()) {
				using (var xmlReader = new XmlTextReader(textReader)) {
					Run(xmlReader);
				}
			}
			var doc = new AXmlParser().Parse(textSource);
			using (var xmlReader = doc.CreateReader()) {
				Run(xmlReader);
			}
			var xmlDocument = new XmlDocument();
			xmlDocument.Load(doc.CreateReader());
			xmlDocument.Save(Path.Combine(Program.TempPath, "savedXmlDocument.xml"));
			var xDocument = XDocument.Load(doc.CreateReader());
			xDocument.Save(Path.Combine(Program.TempPath, "savedXDocument.xml"));
		}
		
		static string CSV(IEnumerable<string> input)
		{
			return string.Join(",", input.Select(i => "\"" + i.Replace("\"", "\"\"") + "\""));
		}
		
		static readonly string[] ignoredProperties = {
			"NameTable", "CanResolveEntity", "CanReadBinaryContent", "CanReadValueChunk", "EOF", "ValueType",
			"SchemaInfo", "IsDefault", "BaseURI", "Settings"
		};
		
		public static void Run(XmlReader reader)
		{
			using (StreamWriter output = File.CreateText(Path.Combine(Program.TempPath, reader.GetType().Name + "-output.csv"))) {
				var properties = typeof(XmlReader).GetProperties(BindingFlags.Public | BindingFlags.Instance)
					.Where(p => p.GetIndexParameters().Length == 0 && !ignoredProperties.Contains(p.Name))
					.ToArray();
				output.WriteLine(CSV(properties.Select(p => p.Name)));
				do {
					output.WriteLine(CSV(properties.Select(p => ToString(p.GetValue(reader, null)))));
				} while (reader.Read());
				output.WriteLine(CSV(properties.Select(p => ToString(p.GetValue(reader, null)))));
			}
		}
		
		static string ToString(object val)
		{
			if (val == null)
				return "null";
			else if (val is string)
				return "\"" + CSharpOutputVisitor.ConvertString((string)val) + "\"";
			else if (val is char)
				return "'" + CSharpOutputVisitor.ConvertChar((char)val) + "'";
			else
				return val.ToString();
		}
	}
}
