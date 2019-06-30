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

using System;
using System.ComponentModel.Composition;
using dnlib.DotNet;
using dnSpy.Contracts.Debugger.Exceptions;
using dnSpy.Contracts.Documents.Tabs.DocViewer;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.Menus;
using dnSpy.Contracts.TreeView;

namespace dnSpy.Debugger.DotNet.Exceptions {
	static class BreakWhenThrownExceptionCommand {
		abstract class CommandBase : MenuItemBase<string> {
			protected readonly Lazy<DbgExceptionSettingsService> dbgExceptionSettingsService;

			protected CommandBase(Lazy<DbgExceptionSettingsService> dbgExceptionSettingsService) =>
				this.dbgExceptionSettingsService = dbgExceptionSettingsService;

			protected sealed override string? CreateContext(IMenuItemContext context) => GetExceptionTypeName(context);

			public override void Execute(string context) {
				if (context is null)
					return;
				var id = new DbgExceptionId(PredefinedExceptionCategories.DotNet, context);
				if (dbgExceptionSettingsService.Value.TryGetSettings(id, out var settings)) {
					settings = new DbgExceptionSettings(settings.Flags | DbgExceptionDefinitionFlags.StopFirstChance, settings.Conditions);
					dbgExceptionSettingsService.Value.Modify(id, settings);
				}
				else {
					var def = new DbgExceptionDefinition(id, DbgExceptionDefinitionFlags.StopFirstChance | DbgExceptionDefinitionFlags.StopSecondChance);
					settings = new DbgExceptionSettings(def.Flags);
					var info = new DbgExceptionSettingsInfo(def, settings);
					dbgExceptionSettingsService.Value.Add(info);
				}
			}

			string? GetExceptionTypeName(IMenuItemContext context) {
				var td = GetTypeDef(context);
				if (td is null)
					return null;
				if (!IsException(td))
					return null;
				return GetExceptionString(td);
			}

			static bool IsException(TypeDef type) {
				TypeDef? td = type;
				if (IsSystemException(td))
					return true;
				for (int i = 0; i < 1000 && !(td is null); i++) {
					if (IsSystemException(td.BaseType))
						return true;
					var bt = td.BaseType;
					td = bt?.ScopeType.ResolveTypeDef();
				}
				return false;
			}

			static bool IsSystemException(ITypeDefOrRef type) =>
				!(type is null) &&
				type.DeclaringType is null &&
				type.Namespace == "System" &&
				type.Name == "Exception" &&
				type.DefinitionAssembly.IsCorLib();

			static string GetExceptionString(TypeDef td) => td.ReflectionFullName;

			protected abstract TypeDef? GetTypeDef(IMenuItemContext context);

			protected TypeDef? GetTypeDefFromTreeNodes(IMenuItemContext context, string guid) {
				if (context.CreatorObject.Guid != new Guid(guid))
					return null;
				var nodes = context.Find<TreeNodeData[]>();
				if (nodes is null || nodes.Length != 1)
					return null;
				var node = nodes[0] as IMDTokenNode;
				if (node is null)
					return null;
				return (node.Reference as ITypeDefOrRef).ResolveTypeDef();
			}

			protected TypeDef? GetTypeDefFromReference(IMenuItemContext context, string guid) {
				if (context.CreatorObject.Guid != new Guid(guid))
					return null;

				var @ref = context.Find<TextReference>();
				if (@ref is null || @ref.Reference is null)
					return null;

				return (@ref.Reference as ITypeDefOrRef).ResolveTypeDef();
			}
		}

		[ExportMenuItem(Header = "res:BreakWhenExceptionThrownCommand", Icon = DsImagesAttribute.Add, Group = MenuConstants.GROUP_CTX_DOCUMENTS_DEBUG, Order = 0)]
		sealed class FilesCommand : CommandBase {
			protected sealed override object CachedContextKey => ContextKey;
			static readonly object ContextKey = new object();

			[ImportingConstructor]
			FilesCommand(Lazy<DbgExceptionSettingsService> dbgExceptionSettingsService)
				: base(dbgExceptionSettingsService) {
			}

			protected override TypeDef? GetTypeDef(IMenuItemContext context) => GetTypeDefFromTreeNodes(context, MenuConstants.GUIDOBJ_DOCUMENTS_TREEVIEW_GUID);
		}

		[ExportMenuItem(Header = "res:BreakWhenExceptionThrownCommand", Icon = DsImagesAttribute.Add, Group = MenuConstants.GROUP_CTX_DOCVIEWER_DEBUG, Order = 1000)]
		sealed class CodeCommand : CommandBase {
			protected sealed override object CachedContextKey => ContextKey;
			static readonly object ContextKey = new object();

			[ImportingConstructor]
			CodeCommand(Lazy<DbgExceptionSettingsService> dbgExceptionSettingsService)
				: base(dbgExceptionSettingsService) {
			}

			protected override TypeDef? GetTypeDef(IMenuItemContext context) => GetTypeDefFromReference(context, MenuConstants.GUIDOBJ_DOCUMENTVIEWERCONTROL_GUID);
		}
	}
}
