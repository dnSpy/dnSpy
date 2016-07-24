/*
    Copyright (C) 2014-2016 de4dot@gmail.com

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
using System.Xml;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using dnSpy.Contracts.Files.Tabs;
using dnSpy.Contracts.Files.Tabs.DocViewer.ToolTips;
using dnSpy.Languages.IL;
using System.Text;
using dnSpy.Contracts.Text;
using dnSpy.Contracts.Languages.XmlDoc;

namespace dnSpy.Files.Tabs.DocViewer.ToolTips {
	[ExportToolTipContentCreator(Order = TabConstants.ORDER_DNLIBREFTOOLTIPCONTENTCREATOR)]
	sealed class DnlibReferenceToolTipContentCreator : IToolTipContentCreator {
		public object Create(IToolTipContentCreatorContext context, object @ref) {
			if (@ref is GenericParam)
				return Create(context, (GenericParam)@ref);
			if (@ref is IMemberRef)
				return Create(context, (IMemberRef)@ref);
			if (@ref is Parameter)
				return Create(context, (Parameter)@ref);
			if (@ref is OpCode)
				return Create(context, (OpCode)@ref);
			return null;
		}

		string GetDocumentation(XmlDocumentationProvider docProvider, IMemberRef mr) {
			var sb = new StringBuilder();
			var doc = docProvider.GetDocumentation(XmlDocKeyProvider.GetKey(mr, sb));
			if (doc != null)
				return doc;
			var method = mr as IMethod;
			if (method == null)
				return null;
			string name = method.Name;
			if (name.StartsWith("set_") || name.StartsWith("get_")) {
				var md = Resolve(method) as MethodDef;
				if (md == null)
					return null;
				mr = md.DeclaringType.Properties.FirstOrDefault(p => p.GetMethod == md || p.SetMethod == md);
				return docProvider.GetDocumentation(XmlDocKeyProvider.GetKey(mr, sb));
			}
			else if (name.StartsWith("add_")) {
				var md = Resolve(method) as MethodDef;
				if (md == null)
					return null;
				mr = md.DeclaringType.Events.FirstOrDefault(p => p.AddMethod == md);
				return docProvider.GetDocumentation(XmlDocKeyProvider.GetKey(mr, sb));
			}
			else if (name.StartsWith("remove_")) {
				var md = Resolve(method) as MethodDef;
				if (md == null)
					return null;
				mr = md.DeclaringType.Events.FirstOrDefault(p => p.RemoveMethod == md);
				return docProvider.GetDocumentation(XmlDocKeyProvider.GetKey(mr, sb));
			}
			return null;
		}

		static IMemberRef Resolve(IMemberRef mr) {
			if (mr is ITypeDefOrRef)
				return ((ITypeDefOrRef)mr).ResolveTypeDef();
			if (mr is IMethod && ((IMethod)mr).IsMethod)
				return ((IMethod)mr).ResolveMethodDef();
			if (mr is IField)
				return ((IField)mr).ResolveFieldDef();
			Debug.Assert(mr is PropertyDef || mr is EventDef || mr is GenericParam, "Unknown IMemberRef");
			return null;
		}

		object Create(IToolTipContentCreatorContext context, GenericParam gp) {
			var creator = context.Create();
			creator.SetImage(gp);

			context.Language.WriteToolTip(creator.Output, gp, null);

			creator.CreateNewOutput();
			try {
				var docProvider = XmlDocLoader.LoadDocumentation(gp.Module);
				if (docProvider != null) {
					if (!creator.Output.WriteXmlDocGeneric(GetDocumentation(docProvider, gp.Owner), gp.Name) && gp.Owner is TypeDef) {
						// If there's no doc available, use the parent class' documentation if this
						// is a generic type parameter (and not a generic method parameter).
						var owner = ((TypeDef)gp.Owner).DeclaringType;
						while (owner != null) {
							if (creator.Output.WriteXmlDocGeneric(GetDocumentation(docProvider, owner), gp.Name))
								break;
							owner = owner.DeclaringType;
						}
					}
				}
			}
			catch (XmlException) {
			}

			return creator.Create();
		}

		object Create(IToolTipContentCreatorContext context, IMemberRef @ref) {
			var creator = context.Create();

			var resolvedRef = Resolve(@ref) ?? @ref;
			creator.SetImage(resolvedRef);
			context.Language.WriteToolTip(creator.Output, @ref, null);
			creator.CreateNewOutput();
			try {
				if (resolvedRef is IMemberDef) {
					var docProvider = XmlDocLoader.LoadDocumentation(resolvedRef.Module);
					if (docProvider != null)
						creator.Output.WriteXmlDoc(GetDocumentation(docProvider, resolvedRef));
				}
			}
			catch (XmlException) {
			}

			return creator.Create();
		}

		object Create(IToolTipContentCreatorContext context, Parameter p) => Create(context, p, null);

		object Create(IToolTipContentCreatorContext context, IVariable v, string name) {
			var creator = context.Create();
			creator.SetImage(v);

			if (v == null) {
				if (name == null)
					return null;
				creator.Output.Write(BoxedOutputColor.Text, string.Format("(local variable) {0}", name));
				return creator.Create();
			}

			context.Language.WriteToolTip(creator.Output, v, name);

			creator.CreateNewOutput();
			if (v is Parameter) {
				var method = ((Parameter)v).Method;
				try {
					var docProvider = XmlDocLoader.LoadDocumentation(method.Module);
					if (docProvider != null) {
						if (!creator.Output.WriteXmlDocParameter(GetDocumentation(docProvider, method), v.Name)) {
							var owner = method.DeclaringType;
							while (owner != null) {
								if (creator.Output.WriteXmlDocParameter(GetDocumentation(docProvider, owner), v.Name))
									break;
								owner = owner.DeclaringType;
							}
						}
					}
				}
				catch (XmlException) {
				}
			}

			return creator.Create();
		}

		object Create(IToolTipContentCreatorContext context, OpCode opCode) {
			var creator = context.Create();

			var s = ILLanguageHelper.GetOpCodeDocumentation(opCode);
			string opCodeHex = opCode.Size > 1 ? string.Format("0x{0:X4}", opCode.Value) : string.Format("0x{0:X2}", opCode.Value);
			creator.Output.Write(BoxedOutputColor.OpCode, opCode.Name);
			creator.Output.WriteSpace();
			creator.Output.Write(BoxedOutputColor.Punctuation, "(");
			creator.Output.Write(BoxedOutputColor.Number, opCodeHex);
			creator.Output.Write(BoxedOutputColor.Punctuation, ")");
			if (s != null) {
				creator.Output.Write(BoxedOutputColor.Text, " - ");
				creator.Output.Write(BoxedOutputColor.Text, s);
			}

			return creator.Create();
		}
	}
}
