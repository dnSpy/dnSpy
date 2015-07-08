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
	internal class PropertyWithExtensionHandler : IHandler {
		public BamlRecordType Type {
			get { return BamlRecordType.PropertyWithExtension; }
		}

		public BamlElement Translate(XamlContext ctx, BamlNode node, BamlElement parent) {
			var record = (PropertyWithExtensionRecord)((BamlRecordNode)node).Record;
			var extTypeId = ((short)record.Flags & 0xfff);
			bool valTypeExt = ((short)record.Flags & 0x4000) == 0x4000;
			bool valStaticExt = ((short)record.Flags & 0x2000) == 0x2000;

			var elemType = parent.Xaml.Element.Annotation<XamlType>();
			var xamlProp = ctx.ResolveProperty(record.AttributeId);
			var extType = ctx.ResolveType((ushort)-extTypeId);
			extType.ResolveNamespace(parent.Xaml, ctx);

			var ext = new XamlExtension(extType);
			if (valTypeExt || extTypeId == (short)KnownTypes.TypeExtension) {
				var value = ctx.ResolveType(record.ValueId);
				ext.Initializer = ctx.ToString(parent.Xaml, value);
			}
			else if (valStaticExt || extTypeId == (short)KnownTypes.TemplateBindingExtension ||
			         extTypeId == (short)KnownTypes.StaticExtension) {
				var value = ctx.ResolveProperty(record.ValueId);

				value.DeclaringType.ResolveNamespace(parent.Xaml, ctx);
				var xName = value.ToXName(ctx, parent.Xaml, extTypeId != (short)KnownTypes.TemplateBindingExtension);

				ext.Initializer = ctx.ToString(parent.Xaml, xName);
			}
			else {
				ext.Initializer = XamlUtils.Escape(ctx.Baml.StringIdMap[record.ValueId].Value);
			}

			var extValue = ext.ToString(ctx, parent.Xaml);
			var attr = new XAttribute(xamlProp.ToXName(ctx, parent.Xaml, xamlProp.IsAttachedTo(elemType)), extValue);
			parent.Xaml.Element.Add(attr);

			return null;
		}
	}
}