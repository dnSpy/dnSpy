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

namespace dnSpy.BamlDecompiler.Xaml {
	internal class XamlProperty {
		public XamlType DeclaringType { get; private set; }
		public string PropertyName { get; private set; }

		public IMemberDef ResolvedMember { get; set; }

		public XamlProperty(XamlType type, string name) {
			DeclaringType = type;
			PropertyName = name;
		}

		public void TryResolve() {
			if (ResolvedMember != null)
				return;

			var typeDef = DeclaringType.ResolvedType.ResolveTypeDef();
			if (typeDef == null)
				return;

			ResolvedMember = typeDef.FindProperty(PropertyName);
			if (ResolvedMember != null)
				return;

			ResolvedMember = typeDef.FindField(PropertyName + "Property");
			if (ResolvedMember != null)
				return;

			ResolvedMember = typeDef.FindEvent(PropertyName);
			if (ResolvedMember != null)
				return;

			ResolvedMember = typeDef.FindField(PropertyName + "Event");
		}

		public bool IsAttachedTo(XamlType type) {
			if (ResolvedMember == null || type.ResolvedType == null)
				return true;

			var declType = ResolvedMember.DeclaringType;
			var t = type.ResolvedType;
			var comparer = new SigComparer();
			do {
				if (comparer.Equals(t, declType))
					return false;
				t = t.GetBaseType();
			} while (t != null);
			return true;
		}

		public XName ToXName(XamlContext ctx, XElement parent, bool isFullName = true) {
			var typeName = DeclaringType.ToXName(ctx);
			XName name;
			if (!isFullName)
				name = XmlConvert.EncodeLocalName(PropertyName);
			else
				name = typeName.LocalName + "." + XmlConvert.EncodeLocalName(PropertyName);

			if (parent == null || parent.GetDefaultNamespace() != typeName.Namespace)
				name = typeName.Namespace + name.LocalName;

			return name;
		}

		public override string ToString() {
			return PropertyName;
		}
	}
}