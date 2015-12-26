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

using System.Linq;
using System.Diagnostics;
using dnlib.DotNet;
using dnSpy.Contracts.Menus;
using System;
using System.Collections.Generic;
using dnSpy.Shared.UI.Menus;
using dnSpy.Contracts.Files.Tabs.TextEditor;
using dnSpy.Contracts.TreeView;
using dnSpy.Contracts.Files.TreeView;

namespace dnSpy.Files.Tabs {
	static class SearchMsdnCtxMenuCommand {
		private static string msdnAddress = "http://msdn.microsoft.com/en-us/library/{0}";

		[ExportMenuItem(Header = "Search _MSDN", Icon = "Search", Group = MenuConstants.GROUP_CTX_CODE_OTHER, Order = 10)]
		sealed class CodeCommand : MenuItemBase {
			public override bool IsVisible(IMenuItemContext context) {
				return GetMemberRef(context) != null;
			}

			static IMemberRef GetMemberRef(IMenuItemContext context) {
				return GetMemberRef(context, MenuConstants.GUIDOBJ_TEXTEDITORCONTROL_GUID);
			}

			public override void Execute(IMenuItemContext context) {
				SearchMsdn(GetMemberRef(context));
			}

			internal static IMemberRef GetMemberRef(IMenuItemContext context, string guid) {
				if (context.CreatorObject.Guid != new Guid(guid))
					return null;
				var @ref = context.Find<CodeReference>();
				return @ref == null ? null : @ref.Reference as IMemberRef;
			}
		}

		[ExportMenuItem(Header = "Search _MSDN", Icon = "Search", Group = MenuConstants.GROUP_CTX_SEARCH_OTHER, Order = 10)]
		sealed class SearchCommand : MenuItemBase {
			public override bool IsVisible(IMenuItemContext context) {
				return GetMemberRef(context) != null;
			}

			static IMemberRef GetMemberRef(IMenuItemContext context) {
				return CodeCommand.GetMemberRef(context, MenuConstants.GUIDOBJ_SEARCH_GUID);
			}

			public override void Execute(IMenuItemContext context) {
				SearchMsdn(GetMemberRef(context));
			}
		}

		[ExportMenuItem(Header = "Search _MSDN", Icon = "Search", Group = MenuConstants.GROUP_CTX_FILES_OTHER, Order = 10)]
		sealed class FilesCommand : MenuItemBase {
			static IEnumerable<ITreeNodeData> GetNodes(IMenuItemContext context) {
				return GetNodes(context, MenuConstants.GUIDOBJ_FILES_TREEVIEW_GUID);
			}

			public override bool IsVisible(IMenuItemContext context) {
				return GetNodes(context).Any();
			}

			public override void Execute(IMenuItemContext context) {
				ExecuteInternal(GetNodes(context));
			}

			internal static IEnumerable<ITreeNodeData> GetNodes(IMenuItemContext context, string guid) {
				if (context.CreatorObject.Guid != new Guid(guid))
					yield break;
				var nodes = context.Find<ITreeNodeData[]>();
				if (nodes == null)
					yield break;
				foreach (var node in nodes) {
					var tokNode = node as IMDTokenNode;
					if (tokNode != null) {
						if (IsPublic(tokNode.Reference as IMemberRef))
							yield return node;
						continue;
					}

					var nsNode = node as INamespaceNode;
					if (nsNode != null) {
						if (!string.IsNullOrEmpty(nsNode.Name))
							yield return node;
						continue;
					}
				}
			}
		}

		[ExportMenuItem(Header = "Search _MSDN", Icon = "Search", Group = MenuConstants.GROUP_CTX_ANALYZER_OTHER, Order = 10)]
		sealed class AnalyzerCommand : MenuItemBase {
			static IEnumerable<ITreeNodeData> GetNodes(IMenuItemContext context) {
				return FilesCommand.GetNodes(context, MenuConstants.GUIDOBJ_ANALYZER_TREEVIEW_GUID);
			}

			public override bool IsVisible(IMenuItemContext context) {
				return GetNodes(context).Any();
			}

			public override void Execute(IMenuItemContext context) {
				ExecuteInternal(GetNodes(context));
			}
		}

		static IMemberDef ResolveDef(IMemberRef mr) {
			if (mr is ITypeDefOrRef)
				return ((ITypeDefOrRef)mr).ResolveTypeDef();
			if (mr is IMethod && ((IMethod)mr).IsMethod)
				return ((IMethod)mr).ResolveMethodDef();
			if (mr is IField)
				return ((IField)mr).ResolveFieldDef();
			return mr as IMemberDef;
		}

		static IMemberDef Resolve(IMemberRef memberRef) {
			var member = ResolveDef(memberRef);
			var md = member as MethodDef;
			if (md == null)
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

		static bool IsPublic(IMemberRef memberRef) {
			var def = Resolve(memberRef);
			if (def is TypeDef)
				return IsAccessible((TypeDef)def);

			var md = def as IMemberDef;
			if (md == null)
				return false;
			if (!IsAccessible(md.DeclaringType))
				return false;

			var method = def as MethodDef;
			if (method != null)
				return IsAccessible(method);

			var field = def as FieldDef;
			if (field != null)
				return IsAccessible(field);

			var prop = def as PropertyDef;
			if (prop != null)
				return IsAccessible(prop);

			var evt = def as EventDef;
			if (evt != null)
				return IsAccessible(evt);

			return false;
		}

		static bool IsAccessible(TypeDef type) {
			if (type == null)
				return false;
			while (true) {
				if (type.DeclaringType == null)
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

		static bool IsAccessible(MethodDef method) {
			return method != null && (method.IsPublic || method.IsFamily || method.IsFamilyOrAssembly);
		}

		static bool IsAccessible(FieldDef field) {
			return field != null && (field.IsPublic || field.IsFamily || field.IsFamilyOrAssembly);
		}

		static bool IsAccessible(PropertyDef prop) {
			return prop.GetMethods.Any(m => IsAccessible(m)) ||
				prop.SetMethods.Any(m => IsAccessible(m)) ||
				prop.OtherMethods.Any(m => IsAccessible(m));
		}

		static bool IsAccessible(EventDef evt) {
			return IsAccessible(evt.AddMethod) ||
				IsAccessible(evt.InvokeMethod) ||
				IsAccessible(evt.RemoveMethod) ||
				evt.OtherMethods.Any(m => IsAccessible(m));
		}

		static string GetAddress(IMemberRef memberRef) {
			var member = Resolve(memberRef);
			if (member == null)
				return string.Empty;

			//TODO: This code doesn't work with:
			//	- generic types, eg. IEnumerable<T>
			//	- constructors
			if (member is MethodDef && ((MethodDef)member).IsConstructor)
				member = member.DeclaringType;  //TODO: Use declaring type until we can search for constructors

			if (member.DeclaringType != null && member.DeclaringType.IsEnum && member is FieldDef && ((FieldDef)member).IsLiteral)
				member = member.DeclaringType;

			string memberName;
			if (member.DeclaringType == null)
				memberName = member.FullName;
			else
				memberName = string.Format("{0}.{1}", member.DeclaringType.FullName, member.Name);

			return string.Format(msdnAddress, memberName.Replace('/', '.'));
		}

		static void ExecuteInternal(IEnumerable<ITreeNodeData> nodes) {
			foreach (var node in nodes) {
				var nsNode = node as INamespaceNode;
				if (nsNode != null) {
					SearchMsdn(string.Format(msdnAddress, nsNode.Name));
					continue;
				}

				var mrNode = node as IMDTokenNode;
				if (mrNode != null) {
					SearchMsdn(mrNode.Reference as IMemberRef);
					continue;
				}
			}
		}

		public static void SearchMsdn(IMemberRef memberRef) {
			if (memberRef != null)
				SearchMsdn(GetAddress(memberRef));
		}

		static void SearchMsdn(string address) {
			address = address.ToLower();
			if (!string.IsNullOrEmpty(address))
				Process.Start(address);
		}
	}
}