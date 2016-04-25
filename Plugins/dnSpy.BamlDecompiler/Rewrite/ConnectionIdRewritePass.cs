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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using dnlib.DotNet;
using dnSpy.BamlDecompiler.Properties;
using dnSpy.Decompiler.Shared;
using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.ILAst;

namespace dnSpy.BamlDecompiler.Rewrite {
	internal class ConnectionIdRewritePass : IRewritePass {
		static bool Impl(MethodDef method, MethodDef ifaceMethod) {
			if (method.HasOverrides) {
				var comparer = new SigComparer(SigComparerOptions.CompareDeclaringTypes | SigComparerOptions.PrivateScopeIsComparable);
				if (method.Overrides.Any(m => comparer.Equals(m.MethodDeclaration, ifaceMethod)))
					return true;
			}

			if (method.Name != ifaceMethod.Name)
				return false;
			return TypesHierarchyHelpers.MatchInterfaceMethod(method, ifaceMethod, ifaceMethod.DeclaringType);
		}

		public void Run(XamlContext ctx, XDocument document) {
			var xClass = document.Root.Elements().First().Attribute(ctx.GetXamlNsName("Class"));
			if (xClass == null)
				return;

			var type = ctx.Module.Find(xClass.Value, true);
			if (type == null)
				return;

			var wbAsm = new AssemblyNameInfo("WindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35").ToAssemblyRef();
			var ifaceRef = new TypeRefUser(ctx.Module, "System.Windows.Markup", "IComponentConnector", wbAsm);
			var iface = ctx.Module.Context.Resolver.ResolveThrow(ifaceRef);

			var connect = iface.FindMethod("Connect");

			foreach (MethodDef method in type.Methods) {
				if (Impl(method, connect)) {
					connect = method;
					iface = null;
					break;
				}
			}
			if (iface != null)
				return;

			Dictionary<int, Action<XamlContext, XElement>> connIds = null;
			try {
				connIds = ExtractConnectionId(ctx, connect);
			}
			catch {
			}

			if (connIds == null) {
				var msg = dnSpy_BamlDecompiler_Resources.Error_IComponentConnectorConnetCannotBeParsed;
				document.Root.AddBeforeSelf(new XComment(string.Format(msg, type.ReflectionFullName)));
				return;
			}

			foreach (var elem in document.Elements()) {
				ProcessElement(ctx, elem, connIds);
			}
		}

		void ProcessElement(XamlContext ctx, XElement elem, Dictionary<int, Action<XamlContext, XElement>> connIds) {
			CheckConnectionId(ctx, elem, connIds);
			foreach (var child in elem.Elements()) {
				ProcessElement(ctx, child, connIds);
			}
		}

		void CheckConnectionId(XamlContext ctx, XElement elem, Dictionary<int, Action<XamlContext, XElement>> connIds) {
			var connId = elem.Annotation<BamlConnectionId>();
			if (connId == null)
				return;

			Action<XamlContext, XElement> cb;
			if (!connIds.TryGetValue((int)connId.Id, out cb)) {
				elem.AddBeforeSelf(new XComment(string.Format(dnSpy_BamlDecompiler_Resources.Error_UnknownConnectionId, connId.Id)));
				return;
			}

			cb(ctx, elem);
		}

		struct FieldAssignment {
			public string FieldName;

			public void Callback(XamlContext ctx, XElement elem) {
				var xName = ctx.GetXamlNsName("Name");
				if (elem.Attribute("Name") == null && elem.Attribute(xName) == null)
					elem.Add(new XAttribute(xName, FieldName));
			}
		}

		struct EventAttachment {
			public TypeDef AttachedType;
			public string EventName;
			public string MethodName;

			public void Callback(XamlContext ctx, XElement elem) {
				XName name;
				if (AttachedType != null) {
					var clrNs = AttachedType.ReflectionNamespace;
					var xmlNs = ctx.XmlNs.LookupXmlns(AttachedType.DefinitionAssembly, clrNs);
					name = ctx.GetXmlNamespace(xmlNs)?.GetName(EventName) ?? AttachedType.Name + "." + EventName;
				}
				else
					name = EventName;

				elem.Add(new XAttribute(name, MethodName));
			}
		}

		struct Error {
			public string Msg;

			public void Callback(XamlContext ctx, XElement elem) {
				elem.AddBeforeSelf(new XComment(Msg));
			}
		}

		Dictionary<int, Action<XamlContext, XElement>> ExtractConnectionId(XamlContext ctx, MethodDef method) {
			var context = new DecompilerContext(method.Module) {
				CurrentType = method.DeclaringType,
				CurrentMethod = method,
				CancellationToken = ctx.CancellationToken
			};
			var body = new ILBlock(new ILAstBuilder().Build(method, true, context));
			new ILAstOptimizer().Optimize(context, body);

			var sw = body.GetSelfAndChildrenRecursive<ILSwitch>().FirstOrDefault();
			if (sw == null)
				return null;

			var connIds = new Dictionary<int, Action<XamlContext, XElement>>();
			foreach (var cas in sw.CaseBlocks) {
				if (cas.Values == null)
					continue;

				Action<XamlContext, XElement> cb = null;
				foreach (var node in cas.Body) {
					var expr = node as ILExpression;
					if (expr == null)
						continue;

					switch (expr.Code) {
						case ILCode.Stfld:
							cb += new FieldAssignment { FieldName = ((IField)expr.Operand).Name }.Callback;
							break;

						case ILCode.Call:
						case ILCode.Callvirt:
							var operand = (IMethod)expr.Operand;
							if (operand.Name == "AddHandler" && operand.DeclaringType.FullName == "System.Windows.UIElement") {
								// Attached event
								var re = expr.Arguments[1];
								var ctor = expr.Arguments[2];
								var reField = re.Operand as IField;

								if (re.Code != ILCode.Ldsfld || ctor.Code != ILCode.Newobj ||
								    ctor.Arguments.Count != 2 || ctor.Arguments[1].Code != ILCode.Ldftn) {
									cb += new Error { Msg = string.Format(dnSpy_BamlDecompiler_Resources.Error_AttachedEvent, reField.Name) }.Callback;
									break;
								}
								var handler = (IMethod)ctor.Arguments[1].Operand;
								string evName = reField.Name;
								if (evName.EndsWith("Event"))
									evName = evName.Substring(0, evName.Length - 5);

								cb += new EventAttachment {
									AttachedType = reField.DeclaringType.ResolveTypeDefThrow(),
									EventName = evName,
									MethodName = handler.Name
								}.Callback;
							}
							else {
								// CLR event
								var add = operand.ResolveMethodDefThrow();
								var ev = add.DeclaringType.Events.FirstOrDefault(e => e.AddMethod == add);

								var ctor = expr.Arguments[1];
								if (ev == null || ctor.Code != ILCode.Newobj ||
								    ctor.Arguments.Count != 2 || ctor.Arguments[1].Code != ILCode.Ldftn) {
									cb += new Error { Msg = string.Format(dnSpy_BamlDecompiler_Resources.Error_AttachedEvent, add.Name) }.Callback;
									break;
								}
								var handler = (IMethod)ctor.Arguments[1].Operand;

								cb += new EventAttachment {
									EventName = ev.Name,
									MethodName = handler.Name
								}.Callback;
							}
							break;
					}
				}

				if (cb != null) {
					foreach (var id in cas.Values)
						connIds[id] = cb;
				}
			}

			return connIds.Count == 0 ? null : connIds;
		}
	}
}