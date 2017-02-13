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
using dnlib.DotNet;
using dnSpy.BamlDecompiler.Xaml;

namespace dnSpy.BamlDecompiler {
	internal class XmlnsScope : List<NamespaceMap> {
		public BamlElement Element { get; }
		public XmlnsScope PreviousScope { get; }

		public XmlnsScope(XmlnsScope prev, BamlElement elem) {
			PreviousScope = prev;
			Element = elem;
		}


		public string LookupXmlns(IAssembly asm, string clrNs) {
			var comparer = new AssemblyNameComparer(AssemblyNameComparerFlags.All);
			foreach (var ns in this) {
				if (comparer.Equals(ns.Assembly, asm) && ns.CLRNamespace == clrNs)
					return ns.XMLNamespace;
			}

			return null;
		}
	}

	internal class XmlnsDictionary {
		Dictionary<string, NamespaceMap> piMappings = new Dictionary<string, NamespaceMap>();

		public XmlnsDictionary() {
			CurrentScope = null;
		}

		public XmlnsScope CurrentScope { get; set; }

		public void PushScope(BamlElement element) {
			CurrentScope = new XmlnsScope(CurrentScope, element);
		}

		public void PopScope() {
			CurrentScope = CurrentScope.PreviousScope;
		}

		public void Add(NamespaceMap map) {
			CurrentScope.Add(map);
		}

		public void SetPIMapping(string xmlNs, string clrNs, IAssembly assembly) {
			if (!piMappings.ContainsKey(xmlNs)) {
				var map = new NamespaceMap(null, assembly, xmlNs, clrNs);
				piMappings[xmlNs] = map;
			}
		}

		NamespaceMap PIFixup(NamespaceMap map) {
			if (piMappings.TryGetValue(map.XMLNamespace, out var piMap)) {
				map.Assembly = piMap.Assembly;
				map.CLRNamespace = piMap.CLRNamespace;
			}
			return map;
		}

		public NamespaceMap LookupNamespaceFromPrefix(string prefix) {
			var scope = CurrentScope;
			while (scope != null) {
				foreach (var ns in scope) {
					if (ns.XmlnsPrefix == prefix)
						return PIFixup(ns);
				}

				scope = scope.PreviousScope;
			}

			return null;
		}

		public NamespaceMap LookupNamespaceFromXmlns(string xmlNs) {
			var scope = CurrentScope;
			while (scope != null) {
				foreach (var ns in scope) {
					if (ns.XMLNamespace == xmlNs)
						return ns;
				}

				scope = scope.PreviousScope;
			}

			return null;
		}

		public string LookupXmlns(IAssembly asm, string clrNs) {
			var comparer = new AssemblyNameComparer(AssemblyNameComparerFlags.All);
			foreach (var map in piMappings) {
				if (comparer.Equals(map.Value.Assembly, asm) && map.Value.CLRNamespace == clrNs)
					return map.Key;
			}

			var scope = CurrentScope;
			while (scope != null) {
				foreach (var ns in scope) {
					if (comparer.Equals(ns.Assembly, asm) && ns.CLRNamespace == clrNs)
						return ns.XMLNamespace;
				}

				scope = scope.PreviousScope;
			}

			return null;
		}
	}
}