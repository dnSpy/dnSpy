// Copyright (c) 2011 AlphaSierraPapa for the SharpDevelop Team
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this
// software and associated documentation files (the "Software"), to deal in the Software
// without restriction, including without limitation the rights to use, copy, modify, merge,
// publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons
// to whom the Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or
// substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
// PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE
// FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
// OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using dnlib.DotNet;
using dnSpy.Contracts.Documents.Tabs.DocViewer;
using dnSpy.Contracts.Documents.TreeView;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.Menus;
using dnSpy.Contracts.TreeView;

namespace dnSpy.Documents.Tabs {
	static class SearchMsdnCtxMenuCommand {
		const string searchUrl = "https://docs.microsoft.com/dotnet/api/{0}";

		[ExportMenuItem(Header = "res:SearchMsdnCommand", Icon = DsImagesAttribute.Search, Group = MenuConstants.GROUP_CTX_DOCVIEWER_OTHER, Order = 10)]
		sealed class CodeCommand : MenuItemBase {
			public override bool IsVisible(IMenuItemContext context) => !(GetMemberRef(context) is null);
			static IMemberRef? GetMemberRef(IMenuItemContext context) => GetMemberRef(context, MenuConstants.GUIDOBJ_DOCUMENTVIEWERCONTROL_GUID);
			public override void Execute(IMenuItemContext context) => SearchMsdn(GetMemberRef(context));

			internal static IMemberRef? GetMemberRef(IMenuItemContext context, string guid) {
				if (context.CreatorObject.Guid != new Guid(guid))
					return null;
				return context.Find<TextReference>()?.Reference as IMemberRef;
			}
		}

		[ExportMenuItem(Header = "res:SearchMsdnCommand", Icon = DsImagesAttribute.Search, Group = MenuConstants.GROUP_CTX_SEARCH_OTHER, Order = 10)]
		sealed class SearchCommand : MenuItemBase {
			public override bool IsVisible(IMenuItemContext context) => !(GetMemberRef(context) is null);
			static IMemberRef? GetMemberRef(IMenuItemContext context) => CodeCommand.GetMemberRef(context, MenuConstants.GUIDOBJ_SEARCH_GUID);
			public override void Execute(IMenuItemContext context) => SearchMsdn(GetMemberRef(context));
		}

		[ExportMenuItem(Header = "res:SearchMsdnCommand", Icon = DsImagesAttribute.Search, Group = MenuConstants.GROUP_CTX_DOCUMENTS_OTHER, Order = 10)]
		sealed class DocumentsCommand : MenuItemBase {
			static IEnumerable<TreeNodeData> GetNodes(IMenuItemContext context) => GetNodes(context, MenuConstants.GUIDOBJ_DOCUMENTS_TREEVIEW_GUID);
			public override bool IsVisible(IMenuItemContext context) => GetNodes(context).Any();
			public override void Execute(IMenuItemContext context) => ExecuteInternal(GetNodes(context));

			internal static IEnumerable<TreeNodeData> GetNodes(IMenuItemContext context, string guid) {
				if (context.CreatorObject.Guid != new Guid(guid))
					yield break;
				var nodes = context.Find<TreeNodeData[]>();
				if (nodes is null)
					yield break;
				foreach (var node in nodes) {
					if (node is IMDTokenNode tokNode) {
						if (IsPublic(tokNode.Reference as IMemberRef))
							yield return node;
						continue;
					}

					if (node is NamespaceNode nsNode) {
						if (!string.IsNullOrEmpty(nsNode.Name))
							yield return node;
						continue;
					}
				}
			}
		}

		[ExportMenuItem(Header = "res:SearchMsdnCommand", Icon = DsImagesAttribute.Search, Group = MenuConstants.GROUP_CTX_ANALYZER_OTHER, Order = 10)]
		sealed class AnalyzerCommand : MenuItemBase {
			static IEnumerable<TreeNodeData> GetNodes(IMenuItemContext context) => DocumentsCommand.GetNodes(context, MenuConstants.GUIDOBJ_ANALYZER_TREEVIEW_GUID);
			public override bool IsVisible(IMenuItemContext context) => GetNodes(context).Any();
			public override void Execute(IMenuItemContext context) => ExecuteInternal(GetNodes(context));
		}

		static IMemberDef? ResolveDef(IMemberRef? mr) {
			if (mr is ITypeDefOrRef)
				return ((ITypeDefOrRef)mr).ResolveTypeDef();
			if (mr is IMethod && ((IMethod)mr).IsMethod)
				return ((IMethod)mr).ResolveMethodDef();
			if (mr is IField)
				return ((IField)mr).ResolveFieldDef();
			return mr as IMemberDef;
		}

		static IMemberDef? Resolve(IMemberRef? memberRef) {
			var member = ResolveDef(memberRef);
			var md = member as MethodDef;
			if (md is null)
				return member;

			if (md.SemanticsAttributes == 0)
				return member;

			// Find the property or event and return it instead

			foreach (var prop in md.DeclaringType.Properties) {
				foreach (var md2 in prop.GetMethods) {
					if (md2 == md)
						return prop;
				}
				foreach (var md2 in prop.SetMethods) {
					if (md2 == md)
						return prop;
				}
				foreach (var md2 in prop.OtherMethods) {
					if (md2 == md)
						return prop;
				}
			}

			foreach (var evt in md.DeclaringType.Events) {
				if (evt.AddMethod == md)
					return evt;
				if (evt.InvokeMethod == md)
					return evt;
				if (evt.RemoveMethod == md)
					return evt;
				foreach (var md2 in evt.OtherMethods) {
					if (md2 == md)
						return evt;
				}
			}

			// Shouldn't be here
			return member;
		}

		static bool IsPublic(IMemberRef? memberRef) {
			var def = Resolve(memberRef);
			if (def is TypeDef)
				return IsAccessible((TypeDef)def);

			var md = def as IMemberDef;
			if (md is null)
				return false;
			if (!IsAccessible(md.DeclaringType))
				return false;

			if (def is MethodDef method)
				return IsAccessible(method);

			if (def is FieldDef field)
				return IsAccessible(field);

			if (def is PropertyDef prop)
				return IsAccessible(prop);

			if (def is EventDef evt)
				return IsAccessible(evt);

			return false;
		}

		static bool IsAccessible(TypeDef type) {
			if (type is null)
				return false;
			while (true) {
				if (type.DeclaringType is null)
					break;
				switch (type.Visibility) {
				case TypeAttributes.NotPublic:
				case TypeAttributes.NestedPrivate:
				case TypeAttributes.NestedAssembly:
				case TypeAttributes.NestedFamANDAssem:
					return false;

				case TypeAttributes.Public:
				case TypeAttributes.NestedPublic:
				case TypeAttributes.NestedFamily:
				case TypeAttributes.NestedFamORAssem:
				default:// never reached
					break;
				}

				type = type.DeclaringType;
			}

			return type.IsPublic;
		}

		static bool IsAccessible(MethodDef method) =>
			!(method is null) && (method.IsPublic || method.IsFamily || method.IsFamilyOrAssembly);

		static bool IsAccessible(FieldDef field) =>
			!(field is null) && (field.IsPublic || field.IsFamily || field.IsFamilyOrAssembly);

		static bool IsAccessible(PropertyDef prop) =>
			prop.GetMethods.Any(m => IsAccessible(m)) ||
			prop.SetMethods.Any(m => IsAccessible(m)) ||
			prop.OtherMethods.Any(m => IsAccessible(m));

		static bool IsAccessible(EventDef evt) =>
			IsAccessible(evt.AddMethod) ||
			IsAccessible(evt.InvokeMethod) ||
			IsAccessible(evt.RemoveMethod) ||
			evt.OtherMethods.Any(m => IsAccessible(m));

		static string GetAddress(IMemberRef memberRef) {
			var member = Resolve(memberRef);
			if (member is null)
				return string.Empty;

			if (!(member.DeclaringType is null) && member.DeclaringType.IsEnum && member is FieldDef && ((FieldDef)member).IsLiteral)
				member = member.DeclaringType;

			string memberName;
			if (member.DeclaringType is null)
				memberName = member.FullName;
			else
				memberName = $"{member.DeclaringType.FullName}.{member.Name.Replace('.', '-')}";

			return string.Format(searchUrl, memberName.Replace('/', '.').Replace('`', '-'));
		}

		static void ExecuteInternal(IEnumerable<TreeNodeData> nodes) {
			foreach (var node in nodes) {
				if (node is NamespaceNode nsNode) {
					SearchMsdn(string.Format(searchUrl, nsNode.Name));
					continue;
				}

				if (node is IMDTokenNode mrNode) {
					SearchMsdn(mrNode.Reference as IMemberRef);
					continue;
				}
			}
		}

		public static void SearchMsdn(IMemberRef? memberRef) {
			if (!(memberRef is null))
				SearchMsdn(GetAddress(memberRef));
		}

		static void SearchMsdn(string address) {
			if (!string.IsNullOrEmpty(address)) {
				try {
					Process.Start(new ProcessStartInfo(address) { UseShellExecute = true });
				}
				catch { }
			}
		}
	}
}
