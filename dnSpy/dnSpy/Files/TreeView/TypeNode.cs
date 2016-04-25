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
using dnlib.DotNet;
using dnSpy.Contracts.Files.TreeView;
using dnSpy.Contracts.Highlighting;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.Languages;
using dnSpy.Contracts.TreeView;
using dnSpy.Decompiler.Shared;
using dnSpy.Shared.Files.TreeView;

namespace dnSpy.Files.TreeView {
	sealed class TypeNode : FileTreeNodeData, ITypeNode {
		public override Guid Guid {
			get { return new Guid(FileTVConstants.TYPE_NODE_GUID); }
		}

		public override NodePathName NodePathName {
			get { return new NodePathName(Guid, type.Namespace + "." + type.Name); }
		}

		public TypeDef TypeDef {
			get { return type; }
		}
		readonly TypeDef type;

		IMDTokenProvider IMDTokenNode.Reference {
			get { return type; }
		}

		protected override ImageReference GetIcon(IDotNetImageManager dnImgMgr) {
			return dnImgMgr.GetImageReference(type);
		}

		public override ITreeNodeGroup TreeNodeGroup {
			get { return treeNodeGroup; }
		}
		readonly ITreeNodeGroup treeNodeGroup;

		public TypeNode(ITreeNodeGroup treeNodeGroup, TypeDef type) {
			this.treeNodeGroup = treeNodeGroup;
			this.type = type;
		}

		public override void Initialize() {
			TreeNode.LazyLoading = true;
		}

		public override IEnumerable<ITreeNodeData> CreateChildren() {
			yield return new BaseTypeFolderNode(Context.FileTreeView.FileTreeNodeGroups.GetGroup(FileTreeNodeGroupType.BaseTypeFolderTreeNodeGroupType), type);
			yield return new DerivedTypesFolderNode(Context.FileTreeView.FileTreeNodeGroups.GetGroup(FileTreeNodeGroupType.DerivedTypesFolderTreeNodeGroupType), type);

			var hash = type.GetPropEventMethods();
			foreach (var m in type.Methods) {
				if (!hash.Contains(m))
					yield return new MethodNode(Context.FileTreeView.FileTreeNodeGroups.GetGroup(FileTreeNodeGroupType.MethodTreeNodeGroupType), m);
			}
			foreach (var p in type.Properties)
				yield return new PropertyNode(Context.FileTreeView.FileTreeNodeGroups.GetGroup(FileTreeNodeGroupType.PropertyTreeNodeGroupType), p);
			foreach (var e in type.Events)
				yield return new EventNode(Context.FileTreeView.FileTreeNodeGroups.GetGroup(FileTreeNodeGroupType.EventTreeNodeGroupType), e);
			foreach (var f in type.Fields)
				yield return new FieldNode(Context.FileTreeView.FileTreeNodeGroups.GetGroup(FileTreeNodeGroupType.FieldTreeNodeGroupType), f);
			foreach (var t in type.NestedTypes)
				yield return new TypeNode(Context.FileTreeView.FileTreeNodeGroups.GetGroup(FileTreeNodeGroupType.TypeTreeNodeGroupType), t);
		}

		protected override void Write(ISyntaxHighlightOutput output, ILanguage language) {
			new NodePrinter().Write(output, language, type, Context.ShowToken);
		}

		public IMethodNode Create(MethodDef method) {
			return Context.FileTreeView.Create(method);
		}

		public IPropertyNode Create(PropertyDef property) {
			return Context.FileTreeView.Create(property);
		}

		public IEventNode Create(EventDef @event) {
			return Context.FileTreeView.Create(@event);
		}

		public IFieldNode Create(FieldDef field) {
			return Context.FileTreeView.Create(field);
		}

		public ITypeNode Create(TypeDef type) {
			return Context.FileTreeView.CreateNested(type);
		}

		public override FilterType GetFilterType(IFileTreeNodeFilter filter) {
			var res = filter.GetResult(TypeDef);
			if (res.FilterType != FilterType.Default)
				return res.FilterType;
			if (Context.Language.ShowMember(type))
				return FilterType.Visible;
			return FilterType.Hide;
		}
	}
}
