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

using System.Xml.Linq;
using dnSpy.BamlDecompiler.Baml;
using dnSpy.BamlDecompiler.Xaml;

namespace dnSpy.BamlDecompiler.Handlers {
	internal class ElementHandler : IHandler {
		public BamlRecordType Type => BamlRecordType.ElementStart;

		public BamlElement Translate(XamlContext ctx, BamlNode node, BamlElement parent) {
			var record = (ElementStartRecord)((BamlBlockNode)node).Header;
			var doc = new BamlElement(node);

			var elemType = ctx.ResolveType(record.TypeId);
			doc.Xaml = new XElement(elemType.ToXName(ctx));

			doc.Xaml.Element.AddAnnotation(elemType);
			parent.Xaml.Element.Add(doc.Xaml.Element);

			HandlerMap.ProcessChildren(ctx, (BamlBlockNode)node, doc);
			var key = node.Annotation as XamlResourceKey;
			if (key != null && key.KeyNode.Record != node.Record) {
				var handler = (IDeferHandler)HandlerMap.LookupHandler(key.KeyNode.Record.Type);
				var keyElem = handler.TranslateDefer(ctx, key.KeyNode, doc);

				doc.Children.Add(keyElem);
				keyElem.Parent = doc;
			}

			elemType.ResolveNamespace(doc.Xaml, ctx);
			doc.Xaml.Element.Name = elemType.ToXName(ctx);

			return doc;
		}
	}
}