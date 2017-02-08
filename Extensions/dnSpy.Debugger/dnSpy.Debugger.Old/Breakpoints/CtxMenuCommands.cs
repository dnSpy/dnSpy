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

using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using dnlib.DotNet;
using dnSpy.Contracts.Documents.Tabs.DocViewer;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.Menus;
using dnSpy.Contracts.TreeView;

namespace dnSpy.Debugger.Breakpoints {
	static class AddClassBreakpointCtxMenuCommands {
		static IMDTokenProvider GetReference(IMenuItemContext context, Guid guid) =>
			AddMethodBreakpointCtxMenuCommands.GetReference(context, guid);

		static ITypeDefOrRef GetTypeRef(IMenuItemContext context, Guid guid) =>
			GetReference(context, guid) as ITypeDefOrRef;

		abstract class MenuItemCommon : MenuItemBase {
			readonly Lazy<IBreakpointService> breakpointService;
			readonly Guid guid;

			protected MenuItemCommon(Lazy<IBreakpointService> breakpointService, string guid) {
				this.breakpointService = breakpointService;
				this.guid = Guid.Parse(guid);
			}

			public override bool IsVisible(IMenuItemContext context) => GetTypeRef(context, guid) != null;
			public override bool IsEnabled(IMenuItemContext context) => GetTypeRef(context, guid) != null;

			public override void Execute(IMenuItemContext context) {
				var type = GetTypeRef(context, guid)?.ResolveTypeDef();
				if (type == null)
					return;
				foreach (var method in type.Methods)
					breakpointService.Value.Add(method);
			}
		}

		[ExportMenuItem(Header = "res:AddClassBreakpointCommand", Icon = DsImagesAttribute.CheckDot, Group = MenuConstants.GROUP_CTX_DOCVIEWER_DEBUG, Order = 100)]
		sealed class CodeCommand : MenuItemCommon {
			[ImportingConstructor]
			CodeCommand(Lazy<IBreakpointService> breakpointService) : base(breakpointService, MenuConstants.GUIDOBJ_DOCUMENTVIEWERCONTROL_GUID) { }
		}

		[ExportMenuItem(Header = "res:AddClassBreakpointCommand", Icon = DsImagesAttribute.CheckDot, Group = MenuConstants.GROUP_CTX_DOCUMENTS_DEBUG, Order = 100)]
		sealed class AssemblyExplorerCommand : MenuItemCommon {
			[ImportingConstructor]
			AssemblyExplorerCommand(Lazy<IBreakpointService> breakpointService) : base(breakpointService, MenuConstants.GUIDOBJ_DOCUMENTS_TREEVIEW_GUID) { }
		}

		[ExportMenuItem(Header = "res:AddClassBreakpointCommand", Icon = DsImagesAttribute.CheckDot, Group = MenuConstants.GROUP_CTX_SEARCH_DEBUG, Order = 100)]
		sealed class SearchCommand : MenuItemCommon {
			[ImportingConstructor]
			SearchCommand(Lazy<IBreakpointService> breakpointService) : base(breakpointService, MenuConstants.GUIDOBJ_SEARCH_GUID) { }
		}

		[ExportMenuItem(Header = "res:AddClassBreakpointCommand", Icon = DsImagesAttribute.CheckDot, Group = MenuConstants.GROUP_CTX_ANALYZER_DEBUG, Order = 100)]
		sealed class AnalyzerCommand : MenuItemCommon {
			[ImportingConstructor]
			AnalyzerCommand(Lazy<IBreakpointService> breakpointService) : base(breakpointService, MenuConstants.GUIDOBJ_ANALYZER_TREEVIEW_GUID) { }
		}
	}

	static class AddMethodBreakpointCtxMenuCommands {
		internal static IMDTokenProvider GetReference(IMenuItemContext context, Guid guid) {
			if (context.CreatorObject.Guid != guid)
				return null;

			var @ref = context.Find<TextReference>();
			if (@ref != null) {
				var realRef = @ref.Reference;
				if (realRef is Parameter)
					realRef = ((Parameter)realRef).ParamDef;
				if (realRef is IMDTokenProvider)
					return (IMDTokenProvider)realRef;
			}

			var nodes = context.Find<TreeNodeData[]>();
			if (nodes != null && nodes.Length != 0) {
				var node = nodes[0] as IMDTokenNode;
				if (node != null)
					return node.Reference;
			}

			return null;
		}

		static IMethod[] GetMethodReferences(IMenuItemContext context, Guid guid) {
			var @ref = GetReference(context, guid);

			var methodRef = @ref as IMethod;
			if (methodRef != null)
				return methodRef.IsMethod ? new[] { methodRef } : null;

			var prop = @ref as PropertyDef;
			if (prop != null) {
				var list = new List<IMethod>();
				list.AddRange(prop.GetMethods);
				list.AddRange(prop.SetMethods);
				list.AddRange(prop.OtherMethods);
				return list.Count == 0 ? null : list.ToArray();
			}

			var evt = @ref as EventDef;
			if (evt != null) {
				var list = new List<IMethod>();
				if (evt.AddMethod != null)
					list.Add(evt.AddMethod);
				if (evt.RemoveMethod != null)
					list.Add(evt.RemoveMethod);
				if (evt.InvokeMethod != null)
					list.Add(evt.InvokeMethod);
				list.AddRange(evt.OtherMethods);
				return list.Count == 0 ? null : list.ToArray();
			}

			return null;
		}

		abstract class MenuItemCommon : MenuItemBase {
			readonly Lazy<IBreakpointService> breakpointService;
			readonly Guid guid;

			protected MenuItemCommon(Lazy<IBreakpointService> breakpointService, string guid) {
				this.breakpointService = breakpointService;
				this.guid = Guid.Parse(guid);
			}

			public override bool IsVisible(IMenuItemContext context) => GetMethodReferences(context, guid) != null;
			public override bool IsEnabled(IMenuItemContext context) => GetMethodReferences(context, guid) != null;

			public override void Execute(IMenuItemContext context) {
				var methodRefs = GetMethodReferences(context, guid);
				if (methodRefs == null)
					return;
				foreach (var methodRef in methodRefs) {
					var method = methodRef.ResolveMethodDef();
					if (method != null)
						breakpointService.Value.Add(method);
				}
			}
		}

		[ExportMenuItem(Header = "res:AddMethodBreakpointCommand", Icon = DsImagesAttribute.CheckDot, Group = MenuConstants.GROUP_CTX_DOCVIEWER_DEBUG, Order = 150)]
		sealed class CodeCommand : MenuItemCommon {
			[ImportingConstructor]
			CodeCommand(Lazy<IBreakpointService> breakpointService) : base(breakpointService, MenuConstants.GUIDOBJ_DOCUMENTVIEWERCONTROL_GUID) { }
		}

		[ExportMenuItem(Header = "res:AddMethodBreakpointCommand", Icon = DsImagesAttribute.CheckDot, Group = MenuConstants.GROUP_CTX_DOCUMENTS_DEBUG, Order = 150)]
		sealed class AssemblyExplorerCommand : MenuItemCommon {
			[ImportingConstructor]
			AssemblyExplorerCommand(Lazy<IBreakpointService> breakpointService) : base(breakpointService, MenuConstants.GUIDOBJ_DOCUMENTS_TREEVIEW_GUID) { }
		}

		[ExportMenuItem(Header = "res:AddMethodBreakpointCommand", Icon = DsImagesAttribute.CheckDot, Group = MenuConstants.GROUP_CTX_SEARCH_DEBUG, Order = 150)]
		sealed class SearchCommand : MenuItemCommon {
			[ImportingConstructor]
			SearchCommand(Lazy<IBreakpointService> breakpointService) : base(breakpointService, MenuConstants.GUIDOBJ_SEARCH_GUID) { }
		}

		[ExportMenuItem(Header = "res:AddMethodBreakpointCommand", Icon = DsImagesAttribute.CheckDot, Group = MenuConstants.GROUP_CTX_ANALYZER_DEBUG, Order = 150)]
		sealed class AnalyzerCommand : MenuItemCommon {
			[ImportingConstructor]
			AnalyzerCommand(Lazy<IBreakpointService> breakpointService) : base(breakpointService, MenuConstants.GUIDOBJ_ANALYZER_TREEVIEW_GUID) { }
		}
	}
}
