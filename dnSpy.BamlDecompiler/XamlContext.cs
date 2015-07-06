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
using System.Threading;
using dnlib.DotNet;
using dnSpy.BamlDecompiler.Baml;
using dnSpy.BamlDecompiler.Xaml;

namespace dnSpy.BamlDecompiler {
	internal class XamlContext {
		XamlContext(ModuleDef module) {
			Module = module;
			NodeMap = new Dictionary<BamlRecord, BamlBlockNode>();
			XmlNs = new XmlnsDictionary();
		}

		Dictionary<ushort, XamlType> typeMap = new Dictionary<ushort, XamlType>();
		Dictionary<ushort, XamlProperty> propertyMap = new Dictionary<ushort, XamlProperty>();

		public ModuleDef Module { get; private set; }

		public BamlContext Baml { get; private set; }
		public BamlNode RootNode { get; private set; }
		public IDictionary<BamlRecord, BamlBlockNode> NodeMap { get; private set; }

		public XmlnsDictionary XmlNs { get; private set; }

		public static XamlContext Construct(ModuleDef module, BamlDocument document, CancellationToken token) {
			var ctx = new XamlContext(module);

			ctx.Baml = BamlContext.ConstructContext(module, document, token);
			ctx.RootNode = BamlNode.Parse(document, token);

			ctx.BuildNodeMap(ctx.RootNode as BamlBlockNode, new RecursionCounter());

			return ctx;
		}

		void BuildNodeMap(BamlBlockNode node, RecursionCounter counter) {
			if (node == null || !counter.Increment())
				return;

			NodeMap[node.Header] = node;

			foreach (var child in node.Children) {
				var childBlock = child as BamlBlockNode;
				if (childBlock != null)
					BuildNodeMap(childBlock, counter);
			}

			counter.Decrement();
		}

		void BuildPIMappings(BamlDocument document) {
			foreach (var record in document) {
				var piMap = record as PIMappingRecord;
				if (piMap == null)
					continue;

				XmlNs.SetPIMapping(piMap.XmlNamespace, piMap.ClrNamespace, null);
			}
		}
	}
}