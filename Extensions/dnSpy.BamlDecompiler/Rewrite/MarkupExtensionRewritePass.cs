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
using System.Linq;
using System.Xml.Linq;
using dnlib.DotNet;
using dnSpy.BamlDecompiler.Xaml;

namespace dnSpy.BamlDecompiler.Rewrite {
	internal class MarkupExtensionRewritePass : IRewritePass {
		XName key;
		XName ctor;

		public void Run(XamlContext ctx, XDocument document) {
			key = ctx.GetXamlNsName("Key");
			ctor = ctx.GetPseudoName("Ctor");

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
			var type = parent.Annotation<XamlType>();
			var property = elem.Annotation<XamlProperty>();
			if ((property == null || type == null) && elem.Name != key)
				return false;

			if (elem.Elements().Count() != 1 || elem.Attributes().Any(t => t.Name.Namespace != XNamespace.Xmlns))
				return false;

			var value = elem.Elements().Single();

			if (!CanInlineExt(ctx, value))
				return false;

			var ext = InlineExtension(ctx, value);
			if (ext == null)
				return false;

			ctx.CancellationToken.ThrowIfCancellationRequested();

			var extValue = ext.ToString(ctx, parent);

			var attrName = elem.Name;
			if (attrName != key)
				attrName = property.ToXName(ctx, parent, property.IsAttachedTo(type));
			var attr = new XAttribute(attrName, extValue);
			parent.Add(attr);
			elem.Remove();

			return true;
		}

		bool CanInlineExt(XamlContext ctx, XElement ctxElement) {
			var type = ctxElement.Annotation<XamlType>();
			if (type != null && type.ResolvedType != null) {
				var typeDef = type.ResolvedType.GetBaseType();
				bool isExt = false;
				while (typeDef != null) {
					if (typeDef.FullName == "System.Windows.Markup.MarkupExtension") {
						isExt = true;
						break;
					}
					typeDef = typeDef.GetBaseType();
				}
				if (!isExt)
					return false;
			}
			else if (ctxElement.Annotation<XamlProperty>() == null &&
			         ctxElement.Name != ctor)
				return false;

			foreach (var child in ctxElement.Elements()) {
				if (!CanInlineExt(ctx, child))
					return false;
			}
			return true;
		}

		object InlineObject(XamlContext ctx, XNode obj) {
			if (obj is XText)
				return ((XText)obj).Value;
			else if (obj is XElement)
				return InlineExtension(ctx, (XElement)obj);
			else
				return null;
		}

		object[] InlineCtor(XamlContext ctx, XElement ctor) {
			if (ctor.HasAttributes)
				return null;
			var args = new List<object>();
			foreach (var child in ctor.Nodes()) {
				var arg = InlineObject(ctx, child);
				if (arg == null)
					return null;
				args.Add(arg);
			}
			return args.ToArray();
		}

		XamlExtension InlineExtension(XamlContext ctx, XElement ctxElement) {
			var type = ctxElement.Annotation<XamlType>();
			if (type == null)
				return null;

			var ext = new XamlExtension(type);

			foreach (var attr in ctxElement.Attributes().Where(attr => attr.Name.Namespace != XNamespace.Xmlns))
				ext.NamedArguments[attr.Name.LocalName] = attr.Value;

			foreach (var child in ctxElement.Nodes()) {
				var elem = child as XElement;
				if (elem == null)
					return null;

				if (elem.Name == ctor) {
					if (ext.Initializer != null)
						return null;

					var args = InlineCtor(ctx, elem);
					if (args == null)
						return null;

					ext.Initializer = args;
					continue;
				}

				var property = elem.Annotation<XamlProperty>();
				if (property == null || elem.Nodes().Count() != 1 ||
				    elem.Attributes().Any(attr => attr.Name.Namespace != XNamespace.Xmlns))
					return null;

				var name = property.PropertyName;
				var value = InlineObject(ctx, elem.Nodes().Single());
				ext.NamedArguments[name] = value;
			}
			return ext;
		}
	}
}