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
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using dnlib.DotNet;
using dnSpy.Contracts.Decompiler;
using dnSpy.Contracts.Documents.TreeView;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.Text;
using dnSpy.Contracts.TreeView;
using dnSpy.Properties;

namespace dnSpy.Documents.TreeView {
	sealed class TypeReferencesFolderNodeImpl : TypeReferencesFolderNode {
		public override Guid Guid => new Guid(DocumentTreeViewConstants.TYPE_REFERENCES_FOLDER_NODE_GUID);
		protected override ImageReference GetIcon(IDotNetImageService dnImgMgr) => DsImages.Reference;
		public override NodePathName NodePathName => new NodePathName(Guid);
		public override void Initialize() => TreeNode.LazyLoading = true;
		public override ITreeNodeGroup? TreeNodeGroup { get; }

		readonly ModuleDocumentNode moduleNode;

		public TypeReferencesFolderNodeImpl(ITreeNodeGroup treeNodeGroup, ModuleDocumentNode moduleNode) {
			Debug2.Assert(!(moduleNode.Document.ModuleDef is null));
			TreeNodeGroup = treeNodeGroup;
			this.moduleNode = moduleNode;
		}

		protected override void WriteCore(ITextColorWriter output, IDecompiler decompiler, DocumentNodeWriteOptions options) {
			output.Write(BoxedTextColor.Text, dnSpy_Resources.TypeReferencesFolder);
			if ((options & DocumentNodeWriteOptions.ToolTip) != 0) {
				output.WriteLine();
				WriteFilename(output);
			}
		}

		public override IEnumerable<TreeNodeData> CreateChildren() {
			if (moduleNode.Document.ModuleDef is ModuleDefMD module) {
				var typeDict = new MemberReferenceFinder(module).Find();
				foreach (var kv in typeDict) {
					var type = kv.Key;
					var typeInfo = kv.Value;
					yield return new TypeReferenceNodeImpl(Context.DocumentTreeView.DocumentTreeNodeGroups.GetGroup(DocumentTreeNodeGroupType.TypeReferenceTreeNodeGroupTypeReferences), type, typeInfo);
				}
			}
		}
	}

	sealed class TypeSpecsFolderNodeImpl : TypeSpecsFolderNode {
		public override Guid Guid => new Guid(DocumentTreeViewConstants.TYPESPECS_FOLDER_NODE_GUID);
		public override NodePathName NodePathName => new NodePathName(Guid);
		public override void Initialize() => TreeNode.LazyLoading = true;
		public override ITreeNodeGroup? TreeNodeGroup { get; }

		protected override ImageReference GetIcon(IDotNetImageService dnImgMgr) => DsImages.Template;

		Dictionary<ITypeDefOrRef, TypeRefInfo>? typeDict;
		readonly IScope? scope;

		public TypeSpecsFolderNodeImpl(ITreeNodeGroup treeNodeGroup, Dictionary<ITypeDefOrRef, TypeRefInfo> typeDict) {
			TreeNodeGroup = treeNodeGroup;
			this.typeDict = typeDict;
			foreach (var kv in typeDict) {
				scope = kv.Key.Scope;
				if (!(scope is null))
					break;
			}
		}

		public override IEnumerable<TreeNodeData> CreateChildren() {
			Debug2.Assert(!(typeDict is null));

			foreach (var kv in typeDict) {
				var type = kv.Key;
				var typeInfo = kv.Value;
				Debug.Assert(typeInfo.TypeDict.Count == 0);
				yield return new TypeReferenceNodeImpl(Context.DocumentTreeView.DocumentTreeNodeGroups.GetGroup(DocumentTreeNodeGroupType.TypeSpecTreeNodeGroupTypeSpecsFolder), type, typeInfo);
			}

			typeDict = null;
		}

		protected override void WriteCore(ITextColorWriter output, IDecompiler decompiler, DocumentNodeWriteOptions options) {
			output.Write(BoxedTextColor.Text, dnSpy_Resources.TreeViewTypesFolder);
			if ((options & DocumentNodeWriteOptions.ToolTip) != 0) {
				output.WriteLine();
				WriteScope(output, scope);
			}
		}
	}

	sealed class MethodReferencesFolderNodeImpl : MethodReferencesFolderNode {
		public override Guid Guid => new Guid(DocumentTreeViewConstants.METHODREFS_FOLDER_NODE_GUID);
		public override NodePathName NodePathName => new NodePathName(Guid);
		public override void Initialize() => TreeNode.LazyLoading = true;
		public override ITreeNodeGroup? TreeNodeGroup { get; }

		protected override ImageReference GetIcon(IDotNetImageService dnImgMgr) => DsImages.MethodPublic;

		Dictionary<IMethod, HashSet<IMethod>>? methodDict;
		readonly IScope? scope;

		public MethodReferencesFolderNodeImpl(ITreeNodeGroup treeNodeGroup, Dictionary<IMethod, HashSet<IMethod>> methodDict) {
			TreeNodeGroup = treeNodeGroup;
			this.methodDict = methodDict;
			foreach (var kv in methodDict) {
				scope = kv.Key.DeclaringType?.Scope;
				if (!(scope is null))
					break;
			}
		}

		public override IEnumerable<TreeNodeData> CreateChildren() {
			Debug2.Assert(!(methodDict is null));

			foreach (var kv in methodDict) {
				var method = kv.Key;
				yield return new MethodReferenceNodeImpl(Context.DocumentTreeView.DocumentTreeNodeGroups.GetGroup(DocumentTreeNodeGroupType.MethodReferenceTreeNodeGroupMethodReferencesFolder), method);
			}

			methodDict = null;
		}

		protected override void WriteCore(ITextColorWriter output, IDecompiler decompiler, DocumentNodeWriteOptions options) {
			output.Write(BoxedTextColor.Text, dnSpy_Resources.TreeViewMethodsFolder);
			if ((options & DocumentNodeWriteOptions.ToolTip) != 0) {
				output.WriteLine();
				WriteScope(output, scope);
			}
		}
	}

	sealed class PropertyReferencesFolderNodeImpl : PropertyReferencesFolderNode {
		public override Guid Guid => new Guid(DocumentTreeViewConstants.PROPERTYREFS_FOLDER_NODE_GUID);
		public override NodePathName NodePathName => new NodePathName(Guid);
		public override void Initialize() => TreeNode.LazyLoading = true;
		public override ITreeNodeGroup? TreeNodeGroup { get; }

		protected override ImageReference GetIcon(IDotNetImageService dnImgMgr) => DsImages.Property;

		Dictionary<IMethod, HashSet<IMethod>>? propertyDict;
		readonly IScope? scope;

		public PropertyReferencesFolderNodeImpl(ITreeNodeGroup treeNodeGroup, Dictionary<IMethod, HashSet<IMethod>> propertyDict) {
			TreeNodeGroup = treeNodeGroup;
			this.propertyDict = propertyDict;
			foreach (var kv in propertyDict) {
				scope = kv.Key.DeclaringType?.Scope;
				if (!(scope is null))
					break;
			}
		}

		public override IEnumerable<TreeNodeData> CreateChildren() {
			Debug2.Assert(!(propertyDict is null));

			foreach (var kv in propertyDict) {
				var property = kv.Key;
				yield return new PropertyReferenceNodeImpl(Context.DocumentTreeView.DocumentTreeNodeGroups.GetGroup(DocumentTreeNodeGroupType.PropertyReferenceTreeNodeGroupPropertyReferencesFolder), property);
			}

			propertyDict = null;
		}

		protected override void WriteCore(ITextColorWriter output, IDecompiler decompiler, DocumentNodeWriteOptions options) {
			output.Write(BoxedTextColor.Text, dnSpy_Resources.TreeViewPropertiesFolder);
			if ((options & DocumentNodeWriteOptions.ToolTip) != 0) {
				output.WriteLine();
				WriteScope(output, scope);
			}
		}
	}

	sealed class EventReferencesFolderNodeImpl : EventReferencesFolderNode {
		public override Guid Guid => new Guid(DocumentTreeViewConstants.EVENTREFS_FOLDER_NODE_GUID);
		public override NodePathName NodePathName => new NodePathName(Guid);
		public override void Initialize() => TreeNode.LazyLoading = true;
		public override ITreeNodeGroup? TreeNodeGroup { get; }

		protected override ImageReference GetIcon(IDotNetImageService dnImgMgr) => DsImages.EventPublic;

		Dictionary<IMethod, HashSet<IMethod>>? eventDict;
		readonly IScope? scope;

		public EventReferencesFolderNodeImpl(ITreeNodeGroup treeNodeGroup, Dictionary<IMethod, HashSet<IMethod>> eventDict) {
			TreeNodeGroup = treeNodeGroup;
			this.eventDict = eventDict;
			foreach (var kv in eventDict) {
				scope = kv.Key.DeclaringType?.Scope;
				if (!(scope is null))
					break;
			}
		}

		public override IEnumerable<TreeNodeData> CreateChildren() {
			Debug2.Assert(!(eventDict is null));

			foreach (var kv in eventDict) {
				var @event = kv.Key;
				yield return new EventReferenceNodeImpl(Context.DocumentTreeView.DocumentTreeNodeGroups.GetGroup(DocumentTreeNodeGroupType.EventReferenceTreeNodeGroupEventReferencesFolder), @event);
			}

			eventDict = null;
		}

		protected override void WriteCore(ITextColorWriter output, IDecompiler decompiler, DocumentNodeWriteOptions options) {
			output.Write(BoxedTextColor.Text, dnSpy_Resources.TreeViewEventsFolder);
			if ((options & DocumentNodeWriteOptions.ToolTip) != 0) {
				output.WriteLine();
				WriteScope(output, scope);
			}
		}
	}

	sealed class FieldReferencesFolderNodeImpl : FieldReferencesFolderNode {
		public override Guid Guid => new Guid(DocumentTreeViewConstants.FIELDREFS_FOLDER_NODE_GUID);
		public override NodePathName NodePathName => new NodePathName(Guid);
		public override void Initialize() => TreeNode.LazyLoading = true;
		public override ITreeNodeGroup? TreeNodeGroup { get; }

		protected override ImageReference GetIcon(IDotNetImageService dnImgMgr) => DsImages.FieldPublic;

		Dictionary<MemberRef, MemberRef>? fieldDict;
		readonly IScope? scope;

		public FieldReferencesFolderNodeImpl(ITreeNodeGroup treeNodeGroup, Dictionary<MemberRef, MemberRef> fieldDict) {
			TreeNodeGroup = treeNodeGroup;
			this.fieldDict = fieldDict;
			foreach (var kv in fieldDict) {
				scope = kv.Key.DeclaringType?.Scope;
				if (!(scope is null))
					break;
			}
		}

		public override IEnumerable<TreeNodeData> CreateChildren() {
			Debug2.Assert(!(fieldDict is null));

			foreach (var kv in fieldDict) {
				var field = kv.Key;
				yield return new FieldReferenceNodeImpl(Context.DocumentTreeView.DocumentTreeNodeGroups.GetGroup(DocumentTreeNodeGroupType.FieldReferenceTreeNodeGroupFieldReferencesFolder), field);
			}

			fieldDict = null;
		}

		protected override void WriteCore(ITextColorWriter output, IDecompiler decompiler, DocumentNodeWriteOptions options) {
			output.Write(BoxedTextColor.Text, dnSpy_Resources.TreeViewFieldsFolder);
			if ((options & DocumentNodeWriteOptions.ToolTip) != 0) {
				output.WriteLine();
				WriteScope(output, scope);
			}
		}
	}

	sealed class TypeReferenceNodeImpl : TypeReferenceNode {
		public override Guid Guid => new Guid(DocumentTreeViewConstants.TYPE_REFERENCE_NODE_GUID);
		public override NodePathName NodePathName => new NodePathName(Guid, TypeRef.FullName);
		public override ITreeNodeGroup? TreeNodeGroup { get; }

		protected override ImageReference GetIcon(IDotNetImageService dnImgMgr) {
			if (imageReference is null) {
				if (TypeRef.ResolveTypeDef() is TypeDef td)
					imageReference = dnImgMgr.GetImageReference(td);
				else
					imageReference = DsImages.Type;
			}
			return imageReference.Value;
		}
		ImageReference? imageReference;

		TypeRefInfo? typeInfo;

		public TypeReferenceNodeImpl(ITreeNodeGroup treeNodeGroup, ITypeDefOrRef type, TypeRefInfo typeInfo)
			: base(type) {
			TreeNodeGroup = treeNodeGroup;
			this.typeInfo = typeInfo;
		}

		public override void Initialize() {
			Debug2.Assert(!(typeInfo is null));
			TreeNode.LazyLoading =
				typeInfo.TypeDict.Count != 0 ||
				typeInfo.MethodDict.Count != 0 ||
				typeInfo.FieldDict.Count != 0 ||
				typeInfo.PropertyDict.Count != 0 ||
				typeInfo.EventDict.Count != 0;
		}

		public override IEnumerable<TreeNodeData> CreateChildren() {
			Debug2.Assert(!(typeInfo is null));

			if (typeInfo.TypeDict.Count != 0)
				yield return new TypeSpecsFolderNodeImpl(Context.DocumentTreeView.DocumentTreeNodeGroups.GetGroup(DocumentTreeNodeGroupType.TypeSpecsFolderTreeNodeGroupTypeReference), typeInfo.TypeDict);
			if (typeInfo.MethodDict.Count != 0)
				yield return new MethodReferencesFolderNodeImpl(Context.DocumentTreeView.DocumentTreeNodeGroups.GetGroup(DocumentTreeNodeGroupType.MethodReferencesFolderTreeNodeGroupTypeReference), typeInfo.MethodDict);
			if (typeInfo.FieldDict.Count != 0)
				yield return new FieldReferencesFolderNodeImpl(Context.DocumentTreeView.DocumentTreeNodeGroups.GetGroup(DocumentTreeNodeGroupType.FieldReferencesFolderTreeNodeGroupTypeReference), typeInfo.FieldDict);
			if (typeInfo.PropertyDict.Count != 0)
				yield return new PropertyReferencesFolderNodeImpl(Context.DocumentTreeView.DocumentTreeNodeGroups.GetGroup(DocumentTreeNodeGroupType.PropertyReferencesFolderTreeNodeGroupTypeReference), typeInfo.PropertyDict);
			if (typeInfo.EventDict.Count != 0)
				yield return new EventReferencesFolderNodeImpl(Context.DocumentTreeView.DocumentTreeNodeGroups.GetGroup(DocumentTreeNodeGroupType.EventReferencesFolderTreeNodeGroupTypeReference), typeInfo.EventDict);

			typeInfo = null;
		}

		protected override void WriteCore(ITextColorWriter output, IDecompiler decompiler, DocumentNodeWriteOptions options) {
			if ((options & DocumentNodeWriteOptions.ToolTip) != 0) {
				WriteMemberRef(output, decompiler, TypeRef);
				output.WriteLine();
				WriteScope(output, TypeRef.Scope);
			}
			else
				new NodeFormatter().Write(output, decompiler, TypeRef, GetShowToken(options));
		}
	}

	sealed class MethodReferenceNodeImpl : MethodReferenceNode {
		public override Guid Guid => new Guid(DocumentTreeViewConstants.METHOD_REFERENCE_NODE_GUID);
		public override NodePathName NodePathName => new NodePathName(Guid, MethodRef.FullName);
		public override ITreeNodeGroup? TreeNodeGroup { get; }

		protected override ImageReference GetIcon(IDotNetImageService dnImgMgr) {
			if (imageReference is null) {
				if (MethodRef.ResolveMethodDef() is MethodDef md)
					imageReference = dnImgMgr.GetImageReference(md);
				else
					imageReference = DsImages.MethodPublic;
			}
			return imageReference.Value;
		}
		ImageReference? imageReference;

		public MethodReferenceNodeImpl(ITreeNodeGroup treeNodeGroup, IMethod method)
			: base(method) => TreeNodeGroup = treeNodeGroup;

		protected override void WriteCore(ITextColorWriter output, IDecompiler decompiler, DocumentNodeWriteOptions options) {
			if ((options & DocumentNodeWriteOptions.ToolTip) != 0) {
				WriteMemberRef(output, decompiler, MethodRef.ResolveMethodDef() ?? MethodRef);
				output.WriteLine();
				WriteScope(output, MethodRef.DeclaringType.Scope);
			}
			else
				new NodeFormatter().WriteMethod(output, decompiler, MethodRef, GetShowToken(options));
		}
	}

	sealed class PropertyReferenceNodeImpl : PropertyReferenceNode {
		public override Guid Guid => new Guid(DocumentTreeViewConstants.PROPERTY_REFERENCE_NODE_GUID);
		public override NodePathName NodePathName => new NodePathName(Guid, PropertyRef.FullName);
		public override ITreeNodeGroup? TreeNodeGroup { get; }

		// The defs aren't cached since they're in some other assembly that could get unloaded,
		// but its assembly won't get GC'd if this class has an indirect ref to it. A weak ref isn't
		// used since there would be too many weak refs (many member ref tree nodes), and it's
		// currently not needed (not on a hot path).
		bool TryResolveDef([NotNullWhen(true)] out PropertyDef? property, [NotNullWhen(true)] out MethodDef? method) {
			property = null;
			method = PropertyRef.ResolveMethodDef();
			if (method is null)
				return false;
			var props = method.DeclaringType.Properties;
			for (int i = 0; i < props.Count; i++) {
				var pd = props[i];
				if (pd.GetMethod == method || pd.SetMethod == method) {
					property = pd;
					return true;
				}
			}
			return false;
		}

		protected override ImageReference GetIcon(IDotNetImageService dnImgMgr) {
			if (imageReference is null) {
				if (TryResolveDef(out var property, out _))
					imageReference = dnImgMgr.GetImageReference(property);
				else
					imageReference = DsImages.Property;
			}
			return imageReference.Value;
		}
		ImageReference? imageReference;

		public PropertyReferenceNodeImpl(ITreeNodeGroup treeNodeGroup, IMethod method)
			: base(method) => TreeNodeGroup = treeNodeGroup;

		protected override void WriteCore(ITextColorWriter output, IDecompiler decompiler, DocumentNodeWriteOptions options) {
			if ((options & DocumentNodeWriteOptions.ToolTip) != 0) {
				WriteMemberRef(output, decompiler, TryResolveDef(out var property, out _) ? (IMemberRef)property : PropertyRef);
				output.WriteLine();
				WriteScope(output, PropertyRef.DeclaringType.Scope);
			}
			else
				new NodeFormatter().WriteMethod(output, decompiler, PropertyRef, GetShowToken(options));
		}
	}

	sealed class EventReferenceNodeImpl : EventReferenceNode {
		public override Guid Guid => new Guid(DocumentTreeViewConstants.EVENT_REFERENCE_NODE_GUID);
		public override NodePathName NodePathName => new NodePathName(Guid, EventRef.FullName);
		public override ITreeNodeGroup? TreeNodeGroup { get; }

		bool TryResolveDef([NotNullWhen(true)] out EventDef? @event, [NotNullWhen(true)] out MethodDef? method) {
			@event = null;
			method = EventRef.ResolveMethodDef();
			if (method is null)
				return false;
			var events = method.DeclaringType.Events;
			for (int i = 0; i < events.Count; i++) {
				var ed = events[i];
				if (ed.AddMethod == method || ed.RemoveMethod == method || ed.InvokeMethod == method) {
					@event = ed;
					return true;
				}
			}
			return false;
		}

		protected override ImageReference GetIcon(IDotNetImageService dnImgMgr) {
			if (imageReference is null) {
				if (TryResolveDef(out var @event, out _))
					imageReference = dnImgMgr.GetImageReference(@event);
				else
					imageReference = DsImages.EventPublic;
			}
			return imageReference.Value;
		}
		ImageReference? imageReference;

		public EventReferenceNodeImpl(ITreeNodeGroup treeNodeGroup, IMethod method)
			: base(method) => TreeNodeGroup = treeNodeGroup;

		protected override void WriteCore(ITextColorWriter output, IDecompiler decompiler, DocumentNodeWriteOptions options) {
			if ((options & DocumentNodeWriteOptions.ToolTip) != 0) {
				WriteMemberRef(output, decompiler, TryResolveDef(out var @event, out _) ? (IMemberRef)@event : EventRef);
				output.WriteLine();
				WriteScope(output, EventRef.DeclaringType.Scope);
			}
			else
				new NodeFormatter().WriteMethod(output, decompiler, EventRef, GetShowToken(options));
		}
	}

	sealed class FieldReferenceNodeImpl : FieldReferenceNode {
		public override Guid Guid => new Guid(DocumentTreeViewConstants.FIELD_REFERENCE_NODE_GUID);
		public override NodePathName NodePathName => new NodePathName(Guid, FieldRef.FullName);
		public override ITreeNodeGroup? TreeNodeGroup { get; }

		protected override ImageReference GetIcon(IDotNetImageService dnImgMgr) {
			if (imageReference is null) {
				if (FieldRef.ResolveFieldDef() is FieldDef fd)
					imageReference = dnImgMgr.GetImageReference(fd);
				else
					imageReference = DsImages.FieldPublic;
			}
			return imageReference.Value;
		}
		ImageReference? imageReference;

		public FieldReferenceNodeImpl(ITreeNodeGroup treeNodeGroup, MemberRef field)
			: base(field) => TreeNodeGroup = treeNodeGroup;

		protected override void WriteCore(ITextColorWriter output, IDecompiler decompiler, DocumentNodeWriteOptions options) {
			if ((options & DocumentNodeWriteOptions.ToolTip) != 0) {
				WriteMemberRef(output, decompiler, (IMemberRef)FieldRef.ResolveFieldDef() ?? FieldRef);
				output.WriteLine();
				WriteScope(output, FieldRef.DeclaringType.Scope);
			}
			else
				new NodeFormatter().WriteField(output, decompiler, FieldRef, GetShowToken(options));
		}
	}
}
