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

using dnlib.DotNet;

namespace dnSpy.BamlDecompiler.Xaml {
	internal class NamespaceMap {
		public string XmlnsPrefix { get; set; }
		public IAssembly Assembly { get; set; }
		public string XMLNamespace { get; set; }
		public string CLRNamespace { get; set; }

		public NamespaceMap(string prefix, IAssembly asm, string xmlNs)
			: this(prefix, asm, xmlNs, null) {
		}

		public NamespaceMap(string prefix, IAssembly asm, string xmlNs, string clrNs) {
			XmlnsPrefix = prefix;
			Assembly = asm;
			XMLNamespace = xmlNs;
			CLRNamespace = clrNs;
		}

		public override string ToString() {
			return string.Format("{0}:[{1}|{2}]", XmlnsPrefix, Assembly.Name, CLRNamespace ?? XMLNamespace);
		}
	}
}