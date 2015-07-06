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

using System;
using System.Collections.Generic;
using dnlib.DotNet;
using dnSpy.BamlDecompiler.Xaml;

namespace dnSpy.BamlDecompiler {
	internal class XmlnsDictionary {
		Dictionary<string, NamespaceMap> piMappings = new Dictionary<string, NamespaceMap>();
		List<Tuple<BamlElement, List<NamespaceMap>>> xmlnsScopes = new List<Tuple<BamlElement, List<NamespaceMap>>>();
		List<NamespaceMap> lastScope;

		public void PushScope(BamlElement element) {
			xmlnsScopes.Add(Tuple.Create(element, new List<NamespaceMap>()));
		}

		public void PopScope() {
			lastScope = xmlnsScopes[xmlnsScopes.Count - 1].Item2;
			xmlnsScopes.RemoveAt(xmlnsScopes.Count - 1);
		}

		public BamlElement GetCurrentElement(bool xamlElement = false) {
			for (int i = xmlnsScopes.Count - 1; i >= 0; i--) {
				if (xmlnsScopes[i].Item1 != null && (!xamlElement || xmlnsScopes[i].Item1.Xaml.Element != null))
					return xmlnsScopes[i].Item1;
			}
			return null;
		}

		public void Add(NamespaceMap map) {
			xmlnsScopes[xmlnsScopes.Count - 1].Item2.Add(map);
		}

		public void SetPIMapping(string xmlNs, string clrNs, IAssembly assembly) {
			if (!piMappings.ContainsKey(xmlNs)) {
				var map = new NamespaceMap(null, assembly, xmlNs, clrNs);
				piMappings[xmlNs] = map;
			}
		}

		NamespaceMap PIFixup(NamespaceMap map) {
			NamespaceMap piMap;
			if (piMappings.TryGetValue(map.XMLNamespace, out piMap)) {
				map.Assembly = piMap.Assembly;
				map.CLRNamespace = piMap.CLRNamespace;
			}
			return map;
		}

		public NamespaceMap LookupNamespaceFromPrefix(string prefix) {
			for (int i = xmlnsScopes.Count - 1; i >= 0; i--) {
				foreach (var ns in xmlnsScopes[i].Item2) {
					if (ns.XmlnsPrefix == prefix)
						return PIFixup(ns);
				}
			}
			return null;
		}

		public NamespaceMap LookupNamespaceFromXmlns(string xmlNs) {
			for (int i = xmlnsScopes.Count - 1; i >= 0; i--) {
				foreach (var ns in xmlnsScopes[i].Item2) {
					if (ns.XMLNamespace == xmlNs)
						return ns;
				}
			}
			return null;
		}

		public string LookupXmlns(IAssembly asm, string clrNs, bool doLastScope = false) {
			foreach (var map in piMappings) {
				if (map.Value.Assembly == asm && map.Value.CLRNamespace == clrNs)
					return map.Key;
			}
			if (doLastScope) {
				foreach (var ns in lastScope) {
					if (ns.Assembly == asm && ns.CLRNamespace == clrNs)
						return ns.XMLNamespace;
				}
			}
			for (int i = xmlnsScopes.Count - 1; i >= 0; i--) {
				foreach (var ns in xmlnsScopes[i].Item2) {
					if (ns.Assembly == asm && ns.CLRNamespace == clrNs)
						return ns.XMLNamespace;
				}
			}
			return null;
		}
	}
}