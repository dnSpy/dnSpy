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
	internal class OptimizedStaticResourceHandler : IHandler, IDeferHandler {
		public BamlRecordType Type => BamlRecordType.OptimizedStaticResource;

		public BamlElement Translate(XamlContext ctx, BamlNode node, BamlElement parent) {
			var record = (OptimizedStaticResourceRecord)((BamlRecordNode)node).Record;
			var key = XamlResourceKey.FindKeyInSiblings(node);

			key.StaticResources.Add(node);
			return null;
		}

		public BamlElement TranslateDefer(XamlContext ctx, BamlNode node, BamlElement parent) {
			var record = (OptimizedStaticResourceRecord)((BamlRecordNode)node).Record;
			var bamlElem = new BamlElement(node);
			object key;
			if (record.IsType) {
				var value = ctx.ResolveType(record.ValueId);

				var typeElem = new XElement(ctx.GetXamlNsName("TypeExtension", parent.Xaml));
				typeElem.AddAnnotation(ctx.ResolveType(0xfd4d)); // Known type - TypeExtension
				typeElem.Add(new XElement(ctx.GetPseudoName("Ctor"), ctx.ToString(parent.Xaml, value)));
				key = typeElem;
			}
			else if (record.IsStatic) {
				string attrName;
				if (record.ValueId > 0x7fff) {
					bool isKey = true;
					short bamlId = (short)-record.ValueId;
					if (bamlId > 232 && bamlId < 464) {
						bamlId -= 232;
						isKey = false;
					}
					else if (bamlId > 464 && bamlId < 467) {
						bamlId -= 231;
					}
					else if (bamlId > 467 && bamlId < 470) {
						bamlId -= 234;
						isKey = false;
					}
					var res = ctx.Baml.KnownThings.Resources(bamlId);
					string name;
					if (isKey)
						name = res.Item1 + "." + res.Item2;
					else
						name = res.Item1 + "." + res.Item3;
					var xmlns = ctx.GetXmlNamespace("http://schemas.microsoft.com/winfx/2006/xaml/presentation");
					attrName = ctx.ToString(parent.Xaml, xmlns.GetName(name));
				}
				else {
					var value = ctx.ResolveProperty(record.ValueId);

					value.DeclaringType.ResolveNamespace(parent.Xaml, ctx);
					var xName = value.ToXName(ctx, parent.Xaml);

					attrName = ctx.ToString(parent.Xaml, xName);
				}

				var staticElem = new XElement(ctx.GetXamlNsName("StaticExtension", parent.Xaml));
				staticElem.AddAnnotation(ctx.ResolveType(0xfda6)); // Known type - StaticExtension
				staticElem.Add(new XElement(ctx.GetPseudoName("Ctor"), attrName));
				key = staticElem;
			}
			else
				key = ctx.ResolveString(record.ValueId);

			var extType = ctx.ResolveType(0xfda5);
			var resElem = new XElement(extType.ToXName(ctx));
			resElem.AddAnnotation(extType); // Known type - StaticResourceExtension
			bamlElem.Xaml = resElem;
			parent.Xaml.Element.Add(resElem);

			var attrElem = new XElement(ctx.GetPseudoName("Ctor"));
			attrElem.Add(key);
			resElem.Add(attrElem);

			return bamlElem;
		}
	}
}