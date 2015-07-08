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
using System.Text;
using System.Xml.Linq;

namespace dnSpy.BamlDecompiler.Xaml {
	internal class XamlExtension {
		public XamlType ExtensionType { get; private set; }
		public object Initializer { get; set; }
		public IDictionary<string, object> NamedArguments { get; private set; }

		public XamlExtension(XamlType type) {
			ExtensionType = type;
			NamedArguments = new Dictionary<string, object>();
		}

		static void WriteObject(StringBuilder sb, XamlContext ctx, XElement ctxElement, object value) {
			if (value is XamlExtension)
				sb.Append(((XamlExtension)value).ToString(ctx, ctxElement));
			else
				sb.Append(value.ToString());
		}

		public string ToString(XamlContext ctx, XElement ctxElement) {
			var sb = new StringBuilder();
			sb.Append('{');

			var typeName = ctx.ToString(ctxElement, ExtensionType);
			if (typeName.EndsWith("Extension"))
				sb.Append(typeName.Substring(0, typeName.Length - 9));
			else
				sb.Append(typeName);

			if (Initializer != null) {
				sb.Append(' ');
				WriteObject(sb, ctx, ctxElement, Initializer);
			}

			if (NamedArguments.Count > 0) {
				bool comma = Initializer != null;
				foreach (var kvp in NamedArguments) {
					if (comma)
						sb.Append(',');
					else
						comma = true;
					sb.AppendFormat("{0}=", kvp.Key);
					WriteObject(sb, ctx, ctxElement, kvp.Value);
				}
			}

			sb.Append('}');
			return sb.ToString();
		}
	}
}