/*
    Copyright (C) 2014-2019 de4dot@gmail.com

    This file is part of dnSpy

    dnSpy is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    dnSpy is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with dnSpy.  If not, see <http://www.gnu.org/licenses/>.
*/

using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Xml;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using dnSpy.Contracts.Decompiler;
using dnSpy.Contracts.Decompiler.XmlDoc;
using dnSpy.Contracts.Documents.Tabs;
using dnSpy.Contracts.Documents.Tabs.DocViewer.ToolTips;
using dnSpy.Contracts.Text;
using dnSpy.Decompiler.IL;

namespace dnSpy.Documents.Tabs.DocViewer.ToolTips {
	[ExportDocumentViewerToolTipProvider(TabConstants.ORDER_DNLIBREFTOOLTIPCONTENTPROVIDER)]
	sealed class DnlibReferenceDocumentViewerToolTipProvider : IDocumentViewerToolTipProvider {
		public object? Create(IDocumentViewerToolTipProviderContext context, object? @ref) {
			switch (@ref) {
			case GenericParam gp:
				return Create(context, gp);
			case IMemberRef mr:
				return Create(context, mr);
			case Parameter pd:
				return Create(context, new SourceParameter(pd, pd.Name, pd.Type, SourceVariableFlags.None));
			case SourceParameter p:
				return Create(context, p);
			case SourceLocal l:
				return Create(context, l);
			case OpCode opc:
				return Create(context, opc);
			case NamespaceReference nsr:
				return Create(context, nsr);
			}
			return null;
		}

		string? GetDocumentation(XmlDocumentationProvider docProvider, IMemberRef mr) {
			var sb = new StringBuilder();
			var doc = docProvider.GetDocumentation(XmlDocKeyProvider.GetKey(mr, sb));
			if (!(doc is null))
				return doc;
			var method = mr as IMethod;
			if (method is null)
				return null;
			string name = method.Name;
			if (name.StartsWith("set_") || name.StartsWith("get_")) {
				var md = Resolve(method) as MethodDef;
				if (md is null)
					return null;
				var mr2 = md.DeclaringType.Properties.FirstOrDefault(p => p.GetMethod == md || p.SetMethod == md);
				return docProvider.GetDocumentation(XmlDocKeyProvider.GetKey(mr2, sb));
			}
			else if (name.StartsWith("add_")) {
				var md = Resolve(method) as MethodDef;
				if (md is null)
					return null;
				var mr2 = md.DeclaringType.Events.FirstOrDefault(p => p.AddMethod == md);
				return docProvider.GetDocumentation(XmlDocKeyProvider.GetKey(mr2, sb));
			}
			else if (name.StartsWith("remove_")) {
				var md = Resolve(method) as MethodDef;
				if (md is null)
					return null;
				var mr2 = md.DeclaringType.Events.FirstOrDefault(p => p.RemoveMethod == md);
				return docProvider.GetDocumentation(XmlDocKeyProvider.GetKey(mr2, sb));
			}
			return null;
		}

		static IMemberRef? Resolve(IMemberRef mr) {
			if (mr is ITypeDefOrRef)
				return ((ITypeDefOrRef)mr).ResolveTypeDef();
			if (mr is IMethod && ((IMethod)mr).IsMethod)
				return ((IMethod)mr).ResolveMethodDef();
			if (mr is IField)
				return ((IField)mr).ResolveFieldDef();
			Debug.Assert(mr is PropertyDef || mr is EventDef || mr is GenericParam, "Unknown IMemberRef");
			return null;
		}

		object Create(IDocumentViewerToolTipProviderContext context, GenericParam gp) {
			var provider = context.Create();
			provider.SetImage(gp);

			context.Decompiler.WriteToolTip(provider.Output, gp, null);

			provider.CreateNewOutput();
			try {
				var docProvider = XmlDocLoader.LoadDocumentation(gp.Module);
				if (!(docProvider is null)) {
					if (!provider.Output.WriteXmlDocGeneric(GetDocumentation(docProvider, gp.Owner), gp.Name) && gp.Owner is TypeDef) {
						// If there's no doc available, use the parent class' documentation if this
						// is a generic type parameter (and not a generic method parameter).
						var owner = ((TypeDef)gp.Owner).DeclaringType;
						while (!(owner is null)) {
							if (provider.Output.WriteXmlDocGeneric(GetDocumentation(docProvider, owner), gp.Name))
								break;
							owner = owner.DeclaringType;
						}
					}
				}
			}
			catch (XmlException) {
			}

			return provider.Create();
		}

		object Create(IDocumentViewerToolTipProviderContext context, NamespaceReference nsRef) {
			var provider = context.Create();
			provider.SetImage(nsRef);
			context.Decompiler.WriteNamespaceToolTip(provider.Output, nsRef.Namespace);
			return provider.Create();
		}

		object Create(IDocumentViewerToolTipProviderContext context, IMemberRef @ref) {
			var provider = context.Create();

			var resolvedRef = Resolve(@ref) ?? @ref;
			provider.SetImage(resolvedRef);
			context.Decompiler.WriteToolTip(provider.Output, @ref, null);
			provider.CreateNewOutput();
			try {
				if (resolvedRef is IMemberDef) {
					var docProvider = XmlDocLoader.LoadDocumentation(resolvedRef.Module);
					if (!(docProvider is null))
						provider.Output.WriteXmlDoc(GetDocumentation(docProvider, resolvedRef));
				}
			}
			catch (XmlException) {
			}

			return provider.Create();
		}

		object Create(IDocumentViewerToolTipProviderContext context, SourceLocal local) {
			var provider = context.Create();
			provider.SetImage(local);
			context.Decompiler.WriteToolTip(provider.Output, local);
			return provider.Create();
		}

		object Create(IDocumentViewerToolTipProviderContext context, SourceParameter parameter) {
			var provider = context.Create();
			provider.SetImage(parameter);

			context.Decompiler.WriteToolTip(provider.Output, parameter);

			provider.CreateNewOutput();
			var method = parameter.Parameter.Method;
			try {
				var docProvider = XmlDocLoader.LoadDocumentation(method.Module);
				if (!(docProvider is null)) {
					if (!provider.Output.WriteXmlDocParameter(GetDocumentation(docProvider, method), parameter.Name)) {
						var owner = method.DeclaringType;
						while (!(owner is null)) {
							if (provider.Output.WriteXmlDocParameter(GetDocumentation(docProvider, owner), parameter.Name))
								break;
							owner = owner.DeclaringType;
						}
					}
				}
			}
			catch (XmlException) {
			}

			return provider.Create();
		}

		object Create(IDocumentViewerToolTipProviderContext context, OpCode opCode) {
			var provider = context.Create();

			var s = ILLanguageHelper.GetOpCodeDocumentation(opCode);
			string opCodeHex = opCode.Size > 1 ? $"0x{opCode.Value:X4}" : $"0x{opCode.Value:X2}";
			provider.Output.Write(BoxedTextColor.OpCode, opCode.Name);
			provider.Output.WriteSpace();
			provider.Output.Write(BoxedTextColor.Punctuation, "(");
			provider.Output.Write(BoxedTextColor.Number, opCodeHex);
			provider.Output.Write(BoxedTextColor.Punctuation, ")");
			if (!(s is null)) {
				provider.Output.Write(BoxedTextColor.Text, " - ");
				provider.Output.Write(BoxedTextColor.Text, s);
			}

			return provider.Create();
		}
	}
}
