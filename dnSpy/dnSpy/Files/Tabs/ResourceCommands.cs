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
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using dnSpy.Contracts.Files.Tabs;
using dnSpy.Contracts.Files.Tabs.DocViewer;
using dnSpy.Contracts.Files.TreeView;
using dnSpy.Contracts.Files.TreeView.Resources;
using dnSpy.Contracts.Menus;
using dnSpy.Contracts.TreeView;

namespace dnSpy.Files.Tabs {
	sealed class ResourceRef {
		public ModuleDef Module { get; }
		public string Filename { get; }
		public string ResourceName { get; }

		ResourceRef(ModuleDef module, string resourcesFilename, string resourceName) {
			this.Module = module;
			this.Filename = resourcesFilename;
			this.ResourceName = resourceName;
		}

		public static ResourceRef TryCreate(object o) {
			if (o is PropertyDef) {
				var pd = (PropertyDef)o;
				if (pd.SetMethod != null)
					return null;
				o = pd.GetMethod;
			}
			var md = o as MethodDef;
			var type = md?.DeclaringType;
			if (type == null)
				return null;
			var resourceName = GetResourceName(md);
			if (resourceName == null)
				return null;
			var resourcesFilename = GetResourcesFilename(type);
			if (resourcesFilename == null)
				return null;
			var module = type.Module;
			if (module == null)
				return null;

			return new ResourceRef(module, resourcesFilename, resourceName);
		}

		static string GetResourcesFilename(TypeDef type) {
			foreach (var m in type.Methods) {
				if (!m.IsStatic)
					continue;
				if (m.MethodSig.GetParamCount() != 0)
					continue;
				var ret = m.MethodSig.GetRetType();
				if (ret == null || ret.FullName != "System.Resources.ResourceManager")
					continue;
				var body = m.Body;
				if (body == null)
					continue;

				ITypeDefOrRef resourceType = null;
				string resourceName = null;
				foreach (var instr in body.Instructions) {
					if (instr.OpCode.Code == Code.Ldstr) {
						resourceName = instr.Operand as string;
						continue;
					}
					if (instr.OpCode.Code == Code.Newobj) {
						var ctor = instr.Operand as IMethod;
						if (ctor == null || ctor.DeclaringType == null || ctor.DeclaringType.FullName != "System.Resources.ResourceManager")
							continue;
						var ctorFullName = ctor.FullName;
						if (ctorFullName == "System.Void System.Resources.ResourceManager::.ctor(System.Type)")
							return resourceType == null ? null : resourceType.ReflectionFullName + ".resources";
						if (ctorFullName == "System.Void System.Resources.ResourceManager::.ctor(System.String,System.Reflection.Assembly)" ||
							ctorFullName == "System.Void System.Resources.ResourceManager::.ctor(System.String,System.Reflection.Assembly,System.Type)")
							return resourceName == null ? null : resourceName + ".resources";
					}
				}
			}

			return null;
		}

		static string GetResourceName(MethodDef method) {
			if (!IsResourcesClass(method.DeclaringType))
				return null;

			var body = method.Body;
			if (body == null)
				return null;

			bool foundGetMethod = false;
			string resourceName = null;
			foreach (var instr in body.Instructions) {
				if (instr.OpCode.Code == Code.Ldstr) {
					resourceName = instr.Operand as string;
					continue;
				}
				if (instr.OpCode.Code == Code.Callvirt) {
					var getStringMethod = instr.Operand as IMethod;
					if (getStringMethod == null)
						continue;
					if (getStringMethod.Name != "GetObject" && getStringMethod.Name != "GetStream" && getStringMethod.Name != "GetString")
						continue;
					var getStringDeclType = getStringMethod.DeclaringType;
					if (getStringDeclType == null || getStringDeclType.FullName != "System.Resources.ResourceManager")
						continue;
					foundGetMethod = true;
					break;
				}
			}
			return foundGetMethod ? resourceName : null;
		}

		static bool IsResourcesClass(TypeDef type) {
			if (type.BaseType == null || type.BaseType.FullName != "System.Object")
				return false;
			if (type.Fields.Count != 2)
				return false;
			bool hasCultureInfo = false;
			bool hasResourceManager = false;
			foreach (var fd in type.Fields) {
				if (!fd.IsStatic)
					continue;
				var ftypeName = fd.FieldType?.FullName ?? string.Empty;
				if (ftypeName == "System.Globalization.CultureInfo")
					hasCultureInfo = true;
				else if (ftypeName == "System.Resources.ResourceManager")
					hasResourceManager = true;
			}
			return hasCultureInfo && hasResourceManager;
		}
	}

	static class GoToResourceCommand {
		[ExportMenuItem(Header = "res:GoToResourceCommand", Group = MenuConstants.GROUP_CTX_DOCVIEWER_OTHER, Order = 20)]
		sealed class TextEditorCommand : MenuItemBase {
			readonly IFileTabManager fileTabManager;

			[ImportingConstructor]
			TextEditorCommand(IFileTabManager fileTabManager) {
				this.fileTabManager = fileTabManager;
			}

			public override void Execute(IMenuItemContext context) => GoToResourceCommand.Execute(fileTabManager, TryCreate(context));

			static ResourceRef TryCreate(TextReference @ref) {
				if (@ref == null)
					return null;
				return ResourceRef.TryCreate(@ref.Reference);
			}

			static ResourceRef TryCreate(IMenuItemContext context) {
				if (context.CreatorObject.Guid != new Guid(MenuConstants.GUIDOBJ_DOCUMENTVIEWERCONTROL_GUID))
					return null;
				return TryCreate(context.Find<TextReference>());
			}

			public override bool IsVisible(IMenuItemContext context) => GoToResourceCommand.IsVisible(TryCreate(context));
		}

		[ExportMenuItem(Header = "res:GoToResourceCommand", Group = MenuConstants.GROUP_CTX_FILES_OTHER, Order = 20)]
		sealed class FileTreeViewCommand : MenuItemBase {
			readonly IFileTabManager fileTabManager;

			[ImportingConstructor]
			FileTreeViewCommand(IFileTabManager fileTabManager) {
				this.fileTabManager = fileTabManager;
			}

			public override void Execute(IMenuItemContext context) => GoToResourceCommand.Execute(fileTabManager, TryCreate(context));

			static ResourceRef TryCreate(ITreeNodeData[] nodes) {
				if (nodes == null || nodes.Length != 1)
					return null;
				var tokNode = nodes[0] as IMDTokenNode;
				if (tokNode != null)
					return ResourceRef.TryCreate(tokNode.Reference);
				return null;
			}

			static ResourceRef TryCreate(IMenuItemContext context) {
				if (context.CreatorObject.Guid != new Guid(MenuConstants.GUIDOBJ_FILES_TREEVIEW_GUID))
					return null;
				return TryCreate(context.Find<ITreeNodeData[]>());
			}

			public override bool IsVisible(IMenuItemContext context) => GoToResourceCommand.IsVisible(TryCreate(context));
		}

		static bool IsVisible(ResourceRef resRef) => resRef != null;

		static void Execute(IFileTabManager fileTabManager, ResourceRef resRef) {
			if (resRef == null)
				return;
			var modNode = fileTabManager.FileTreeView.FindNode(resRef.Module);
			Debug.Assert(modNode != null);
			if (modNode == null)
				return;
			modNode.TreeNode.EnsureChildrenLoaded();
			var resDirNode = modNode.TreeNode.DataChildren.FirstOrDefault(a => a is IResourcesFolderNode);
			Debug.Assert(resDirNode != null);
			if (resDirNode == null)
				return;
			resDirNode.TreeNode.EnsureChildrenLoaded();
			var resSetNode = resDirNode.TreeNode.DataChildren.FirstOrDefault(a => a is IResourceElementSetNode && ((IResourceElementSetNode)a).Name == resRef.Filename);
			Debug.Assert(resSetNode != null);
			if (resSetNode == null)
				return;
			resSetNode.TreeNode.EnsureChildrenLoaded();
			var resNode = resSetNode.TreeNode.DataChildren.FirstOrDefault(a => a is IResourceElementNode && ((IResourceElementNode)a).Name == resRef.ResourceName);
			Debug.Assert(resNode != null);
			if (resNode == null)
				return;
			fileTabManager.FollowReference(resNode);
		}
	}
}
