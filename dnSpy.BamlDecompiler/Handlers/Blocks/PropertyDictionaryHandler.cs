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

namespace dnSpy.BamlDecompiler.Handlers {
	internal class PropertyDictionaryHandler : IHandler {
		public BamlRecordType Type {
			get { return BamlRecordType.PropertyDictionaryStart; }
		}

		public BamlElement Translate(XamlContext ctx, BamlNode node, BamlElement parent) {
			var record = (PropertyDictionaryStartRecord)((BamlBlockNode)node).Header;
			var doc = new BamlElement(node);

			var elemAttr = ctx.ResolveProperty(record.AttributeId);
			doc.Xaml = new XElement(elemAttr.ToXName(ctx, null));

			doc.Xaml.Element.AddAnnotation(elemAttr);
			parent.Xaml.Element.Add(doc.Xaml.Element);

			HandlerMap.ProcessChildren(ctx, (BamlBlockNode)node, doc);
			elemAttr.DeclaringType.ResolveNamespace(ctx);
			doc.Xaml.Element.Name = elemAttr.ToXName(ctx, null);

			return doc;
		}
	}
}