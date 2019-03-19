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
using dnSpy.BamlDecompiler.Xaml;

namespace dnSpy.BamlDecompiler.Rewrite {
	internal class AttributeRewritePass : IRewritePass {
		XName key;

		public void Run(XamlContext ctx, XDocument document) {
			key = ctx.GetXamlNsName("Key");

			bool doWork;
			do {
				doWork = false;
				foreach (var elem in document.Elements()) {
					doWork |= ProcessElement(ctx, elem);
				}
			} while (doWork);
		}

		bool ProcessElement(XamlContext ctx, XElement elem) {
			bool doWork = false;
			foreach (var child in elem.Elements()) {
				doWork |= RewriteElement(ctx, elem, child);
				doWork |= ProcessElement(ctx, child);
			}
			return doWork;
		}

		bool RewriteElement(XamlContext ctx, XElement parent, XElement elem) {
			var property = elem.Annotation<XamlProperty>();
			if (property == null && elem.Name != key)
				return false;

			if (elem.HasAttributes || elem.HasElements)
				return false;

			ctx.CancellationToken.ThrowIfCancellationRequested();

			var value = elem.Value;
			var attrName = elem.Name;
			if (attrName != key)
				attrName = property.ToXName(ctx, parent, property.IsAttachedTo(parent.Annotation<XamlType>()));
			var attr = new XAttribute(attrName, value);
			parent.Add(attr);
			elem.Remove();

			return true;
		}
	}
}
