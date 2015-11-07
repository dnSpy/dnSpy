using System;
using System.Collections.Generic;
using System.Diagnostics;
using dnSpy.BamlDecompiler.Baml;

namespace dnSpy.BamlDecompiler {
	internal interface IHandler {
		BamlRecordType Type { get; }
		BamlElement Translate(XamlContext ctx, BamlNode node, BamlElement parent);
	}

	internal interface IDeferHandler {
		BamlElement TranslateDefer(XamlContext ctx, BamlNode node, BamlElement parent);
	}

	internal static class HandlerMap {
		static readonly Dictionary<BamlRecordType, IHandler> handlers;

		static HandlerMap() {
			handlers = new Dictionary<BamlRecordType, IHandler>();

			foreach (var type in typeof(IHandler).Assembly.GetTypes()) {
				if (typeof(IHandler).IsAssignableFrom(type) &&
				    !type.IsInterface && !type.IsAbstract) {
					var handler = (IHandler)Activator.CreateInstance(type);
					handlers.Add(handler.Type, handler);
				}
			}
		}

		public static IHandler LookupHandler(BamlRecordType type) {
#if DEBUG
			switch (type) {
				case BamlRecordType.AssemblyInfo:
				case BamlRecordType.TypeInfo:
				case BamlRecordType.AttributeInfo:
				case BamlRecordType.StringInfo:
					break;
				default:
					if (!handlers.ContainsKey(type))
						throw new NotSupportedException(type.ToString());
					break;
			}
#endif
			return handlers.ContainsKey(type) ? handlers[type] : null;
		}

		public static void ProcessChildren(XamlContext ctx, BamlBlockNode node, BamlElement nodeElem) {
			ctx.XmlNs.PushScope(nodeElem);
			if (nodeElem.Xaml.Element != null)
				nodeElem.Xaml.Element.AddAnnotation(ctx.XmlNs.CurrentScope);
			foreach (var child in node.Children) {
				var handler = LookupHandler(child.Type);
				if (handler == null) {
					Debug.WriteLine("BAML Handler {0} not implemented.", child.Type);
					continue;
				}
				var elem = handler.Translate(ctx, (BamlNode)child, nodeElem);
				if (elem != null) {
					nodeElem.Children.Add(elem);
					elem.Parent = nodeElem;
				}

				ctx.CancellationToken.ThrowIfCancellationRequested();
			}
			ctx.XmlNs.PopScope();
		}
	}
}