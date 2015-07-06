/*
	Copyright (c) 2015 Ki

	Permission is hereby granted, free of charge, to any person obtaining a copy
	of this software and associated documentation files (the "Software"), to deal
	in the Software without restriction, including without limitation the rights
	to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
	copies of the Software, and to permit persons to whom the Software is
	furnished to do so, subject to the following conditions:

	The above copyright notice and this permission notice shall be included in
	all copies or substantial portions of the Software.

	THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
	IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
	FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
	AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
	LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
	OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
	THE SOFTWARE.
*/

using System.Collections.Generic;
using System.Diagnostics;
using System.Xml;
using System.Xml.Linq;
using dnlib.DotNet;
using dnSpy.BamlDecompiler.Baml;
using dnSpy.BamlDecompiler.Xaml;

namespace dnSpy.BamlDecompiler.Handlers {
	internal class XmlnsPropertyHandler : IHandler {
		public BamlRecordType Type {
			get { return BamlRecordType.XmlnsProperty; }
		}

		IEnumerable<string> ResolveCLRNamespaces(AssemblyDef assembly, string ns) {
			foreach (var attr in assembly.CustomAttributes.FindAll("System.Windows.Markup.XmlnsDefinitionAttribute")) {
				Debug.Assert(attr.ConstructorArguments.Count == 2);

				var xmlNs = attr.ConstructorArguments[0].Value;
				var clrNs = attr.ConstructorArguments[1].Value;
				Debug.Assert(xmlNs is UTF8String && clrNs is UTF8String);

				if ((UTF8String)xmlNs == ns)
					yield return (UTF8String)clrNs;
			}
		}

		public BamlElement Translate(XamlContext ctx, BamlNode node, BamlElement parent) {
			var record = (XmlnsPropertyRecord)((BamlRecordNode)node).Record;
			foreach (var asmId in record.AssemblyIds) {
				var assembly = ctx.Baml.ResolveAssembly(asmId);
				ctx.XmlNs.Add(new NamespaceMap(record.Prefix, assembly, record.XmlNamespace));

				if (assembly is AssemblyDef) {
					foreach (var clrNs in ResolveCLRNamespaces((AssemblyDef)assembly, record.XmlNamespace))
						ctx.XmlNs.Add(new NamespaceMap(record.Prefix, assembly, record.XmlNamespace, clrNs));
				}
			}

			XName xmlnsDef;
			if (string.IsNullOrEmpty(record.Prefix))
				xmlnsDef = "xmlns";
			else
				xmlnsDef = XNamespace.Xmlns + XmlConvert.EncodeLocalName(record.Prefix);
			parent.Xaml.Element.Add(new XAttribute(xmlnsDef, ctx.GetXmlNamespace(record.XmlNamespace)));

			return null;
		}
	}
}