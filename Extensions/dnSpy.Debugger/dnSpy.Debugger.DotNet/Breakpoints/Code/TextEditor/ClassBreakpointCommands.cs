/*
    Copyright (C) 2014-2017 de4dot@gmail.com

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
using System.Linq;
using dnlib.DotNet;
using dnSpy.Contracts.Debugger.Breakpoints.Code;
using dnSpy.Contracts.Debugger.DotNet.Breakpoints.Code;
using dnSpy.Contracts.Documents.Tabs.DocViewer;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.Menus;
using dnSpy.Contracts.Metadata;
using dnSpy.Contracts.TreeView;

namespace dnSpy.Debugger.DotNet.Breakpoints.Code.TextEditor {
	[Export(typeof(MethodBreakpointsService))]
	sealed class MethodBreakpointsService {
		readonly Lazy<IModuleIdProvider> moduleIdProvider;
		readonly Lazy<DbgCodeBreakpointsService> dbgCodeBreakpointsService;
		readonly Lazy<DbgDotNetBreakpointLocationFactory2> dbgDotNetBreakpointLocationFactory;

		[ImportingConstructor]
		MethodBreakpointsService(Lazy<IModuleIdProvider> moduleIdProvider, Lazy<DbgCodeBreakpointsService> dbgCodeBreakpointsService, Lazy<DbgDotNetBreakpointLocationFactory2> dbgDotNetBreakpointLocationFactory) {
			this.moduleIdProvider = moduleIdProvider;
			this.dbgCodeBreakpointsService = dbgCodeBreakpointsService;
			this.dbgDotNetBreakpointLocationFactory = dbgDotNetBreakpointLocationFactory;
		}

		public void Add(MethodDef[] methods) {
			var list = new List<DbgCodeBreakpointInfo>(methods.Length);
			var existing = new HashSet<DbgDotNetBreakpointLocation>(dbgCodeBreakpointsService.Value.Breakpoints.Select(a => a.Location).OfType<DbgDotNetBreakpointLocation>());
			foreach (var method in methods) {
				var moduleId = moduleIdProvider.Value.Create(method.Module);
				var location = dbgDotNetBreakpointLocationFactory.Value.CreateLocation(moduleId, method.MDToken.Raw, 0);
				if (existing.Contains(location))
					continue;
				existing.Add(location);
				list.Add(new DbgCodeBreakpointInfo(location, new DbgCodeBreakpointSettings { IsEnabled = true }));
			}
			dbgCodeBreakpointsService.Value.Add(list.ToArray());
		}
	}

	static class AddClassBreakpointCtxMenuCommands {
		static IMDTokenProvider GetReference(IMenuItemContext context, Guid guid) =>
			AddMethodBreakpointCtxMenuCommands.GetReference(context, guid);

		static ITypeDefOrRef GetTypeRef(IMenuItemContext context, Guid guid) =>
			GetReference(context, guid) as ITypeDefOrRef;

		abstract class MenuItemCommon : MenuItemBase {
			readonly Lazy<MethodBreakpointsService> methodBreakpointsService;
			readonly Guid guid;

			protected MenuItemCommon(Lazy<MethodBreakpointsService> methodBreakpointsService, string guid) {
				this.methodBreakpointsService = methodBreakpointsService;
				this.guid = Guid.Parse(guid);
			}

			public override bool IsVisible(IMenuItemContext context) => GetTypeRef(context, guid) != null;
			public override bool IsEnabled(IMenuItemContext context) => GetTypeRef(context, guid) != null;

			public override void Execute(IMenuItemContext context) {
				var type = GetTypeRef(context, guid)?.ResolveTypeDef();
				if (type == null)
					return;
				methodBreakpointsService.Value.Add(type.Methods.ToArray());
			}
		}

		[ExportMenuItem(Header = "res:AddClassBreakpointCommand", Icon = DsImagesAttribute.CheckDot, Group = MenuConstants.GROUP_CTX_DOCVIEWER_DEBUG, Order = 100)]
		sealed class CodeCommand : MenuItemCommon {
			[ImportingConstructor]
			CodeCommand(Lazy<MethodBreakpointsService> methodBreakpointsService) : base(methodBreakpointsService, MenuConstants.GUIDOBJ_DOCUMENTVIEWERCONTROL_GUID) { }
		}

		[ExportMenuItem(Header = "res:AddClassBreakpointCommand", Icon = DsImagesAttribute.CheckDot, Group = MenuConstants.GROUP_CTX_DOCUMENTS_DEBUG, Order = 100)]
		sealed class AssemblyExplorerCommand : MenuItemCommon {
			[ImportingConstructor]
			AssemblyExplorerCommand(Lazy<MethodBreakpointsService> methodBreakpointsService) : base(methodBreakpointsService, MenuConstants.GUIDOBJ_DOCUMENTS_TREEVIEW_GUID) { }
		}

		[ExportMenuItem(Header = "res:AddClassBreakpointCommand", Icon = DsImagesAttribute.CheckDot, Group = MenuConstants.GROUP_CTX_SEARCH_DEBUG, Order = 100)]
		sealed class SearchCommand : MenuItemCommon {
			[ImportingConstructor]
			SearchCommand(Lazy<MethodBreakpointsService> methodBreakpointsService) : base(methodBreakpointsService, MenuConstants.GUIDOBJ_SEARCH_GUID) { }
		}

		[ExportMenuItem(Header = "res:AddClassBreakpointCommand", Icon = DsImagesAttribute.CheckDot, Group = MenuConstants.GROUP_CTX_ANALYZER_DEBUG, Order = 100)]
		sealed class AnalyzerCommand : MenuItemCommon {
			[ImportingConstructor]
			AnalyzerCommand(Lazy<MethodBreakpointsService> methodBreakpointsService) : base(methodBreakpointsService, MenuConstants.GUIDOBJ_ANALYZER_TREEVIEW_GUID) { }
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
				if (nodes[0] is IMDTokenNode node)
					return node.Reference;
			}

			return null;
		}

		static IMethod[] GetMethodReferences(IMenuItemContext context, Guid guid) {
			var @ref = GetReference(context, guid);

			if (@ref is IMethod methodRef)
				return methodRef.IsMethod ? new[] { methodRef } : null;

			if (@ref is PropertyDef prop) {
				var list = new List<IMethod>();
				list.AddRange(prop.GetMethods);
				list.AddRange(prop.SetMethods);
				list.AddRange(prop.OtherMethods);
				return list.Count == 0 ? null : list.ToArray();
			}

			if (@ref is EventDef evt) {
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
			readonly Lazy<MethodBreakpointsService> methodBreakpointsService;
			readonly Guid guid;

			protected MenuItemCommon(Lazy<MethodBreakpointsService> methodBreakpointsService, string guid) {
				this.methodBreakpointsService = methodBreakpointsService;
				this.guid = Guid.Parse(guid);
			}

			public override bool IsVisible(IMenuItemContext context) => GetMethodReferences(context, guid) != null;
			public override bool IsEnabled(IMenuItemContext context) => GetMethodReferences(context, guid) != null;

			public override void Execute(IMenuItemContext context) {
				var methodRefs = GetMethodReferences(context, guid);
				if (methodRefs == null)
					return;
				methodBreakpointsService.Value.Add(methodRefs.Select(a => a.ResolveMethodDef()).Where(a => a != null).ToArray());
			}
		}

		[ExportMenuItem(Header = "res:AddMethodBreakpointCommand", Icon = DsImagesAttribute.CheckDot, Group = MenuConstants.GROUP_CTX_DOCVIEWER_DEBUG, Order = 150)]
		sealed class CodeCommand : MenuItemCommon {
			[ImportingConstructor]
			CodeCommand(Lazy<MethodBreakpointsService> methodBreakpointsService) : base(methodBreakpointsService, MenuConstants.GUIDOBJ_DOCUMENTVIEWERCONTROL_GUID) { }
		}

		[ExportMenuItem(Header = "res:AddMethodBreakpointCommand", Icon = DsImagesAttribute.CheckDot, Group = MenuConstants.GROUP_CTX_DOCUMENTS_DEBUG, Order = 150)]
		sealed class AssemblyExplorerCommand : MenuItemCommon {
			[ImportingConstructor]
			AssemblyExplorerCommand(Lazy<MethodBreakpointsService> methodBreakpointsService) : base(methodBreakpointsService, MenuConstants.GUIDOBJ_DOCUMENTS_TREEVIEW_GUID) { }
		}

		[ExportMenuItem(Header = "res:AddMethodBreakpointCommand", Icon = DsImagesAttribute.CheckDot, Group = MenuConstants.GROUP_CTX_SEARCH_DEBUG, Order = 150)]
		sealed class SearchCommand : MenuItemCommon {
			[ImportingConstructor]
			SearchCommand(Lazy<MethodBreakpointsService> methodBreakpointsService) : base(methodBreakpointsService, MenuConstants.GUIDOBJ_SEARCH_GUID) { }
		}

		[ExportMenuItem(Header = "res:AddMethodBreakpointCommand", Icon = DsImagesAttribute.CheckDot, Group = MenuConstants.GROUP_CTX_ANALYZER_DEBUG, Order = 150)]
		sealed class AnalyzerCommand : MenuItemCommon {
			[ImportingConstructor]
			AnalyzerCommand(Lazy<MethodBreakpointsService> methodBreakpointsService) : base(methodBreakpointsService, MenuConstants.GUIDOBJ_ANALYZER_TREEVIEW_GUID) { }
		}
	}
}
