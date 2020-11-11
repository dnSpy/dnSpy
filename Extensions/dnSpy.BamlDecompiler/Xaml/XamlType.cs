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

using System.Xml;
using System.Xml.Linq;
using dnlib.DotNet;
using dnSpy.BamlDecompiler.Properties;

namespace dnSpy.BamlDecompiler.Xaml {
	internal class XamlType {
		public IAssembly Assembly { get; }
		public string TypeNamespace { get; }
		public string TypeName { get; }

		public XNamespace Namespace { get; private set; }
		public ITypeDefOrRef ResolvedType { get; set; }

		public XamlType(IAssembly assembly, string ns, string name)
			: this(assembly, ns, name, null) {
		}

		public XamlType(IAssembly assembly, string ns, string name, XNamespace xmlns) {
			Assembly = assembly;
			TypeNamespace = ns;
			TypeName = name;
			Namespace = xmlns;
		}

		public void ResolveNamespace(XElement elem, XamlContext ctx) {
			if (Namespace is not null)
				return;

			// Since XmlnsProperty records are inside the element,
			// the namespace is resolved after processing the element body.

			string xmlNs = null;
			if (elem.Annotation<XmlnsScope>() is not null)
				xmlNs = elem.Annotation<XmlnsScope>().LookupXmlns(Assembly, TypeNamespace);
			if (xmlNs is null)
				xmlNs = ctx.XmlNs.LookupXmlns(Assembly, TypeNamespace);
			// Sometimes there's no reference to System.Xaml even if x:Type is used
			if (xmlNs is null)
				xmlNs = ctx.TryGetXmlNamespace(Assembly, TypeNamespace);

			if (xmlNs is null) {
				if (AssemblyNameComparer.CompareAll.Equals(Assembly, ctx.Module.Assembly))
					xmlNs = $"clr-namespace:{TypeNamespace}";
				else
					xmlNs = $"clr-namespace:{TypeNamespace};assembly={Assembly.Name}";

				var nsSeg = TypeNamespace.Split('.');	
				var prefix = nsSeg[nsSeg.Length - 1].ToLowerInvariant();
				if (string.IsNullOrEmpty(prefix)) {
					if (string.IsNullOrEmpty(TypeNamespace))
						prefix = "global";
					else
						prefix = "empty";
				}
				int count = 0;
				var truePrefix = prefix;
				XNamespace prefixNs, ns = ctx.GetXmlNamespace(xmlNs);
				while ((prefixNs = elem.GetNamespaceOfPrefix(truePrefix)) is not null && prefixNs != ns) {
					count++;
					truePrefix = prefix + count;
				}

				if (prefixNs is null) {
					elem.Add(new XAttribute(XNamespace.Xmlns + XmlConvert.EncodeLocalName(truePrefix), ns));
					if (string.IsNullOrEmpty(TypeNamespace))
						elem.AddBeforeSelf(new XComment(string.Format(dnSpy_BamlDecompiler_Resources.Msg_GlobalNamespace, truePrefix)));
				}
			}
			Namespace = ctx.GetXmlNamespace(xmlNs);
		}

		public XName ToXName(XamlContext ctx) {
			if (Namespace is null)
				return XmlConvert.EncodeLocalName(TypeName);
			return Namespace + XmlConvert.EncodeLocalName(TypeName);
		}

		public override string ToString() => TypeName;
	}
}
