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
using dnSpy.Contracts.Decompiler;
using dnSpy.Contracts.Files.TreeView;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.Languages;
using dnSpy.Contracts.Text;
using dnSpy.Contracts.TreeView;

namespace dnSpy.Files.TreeView {
	sealed class TypeNode : FileTreeNodeData, ITypeNode {
		public override Guid Guid => new Guid(FileTVConstants.TYPE_NODE_GUID);
		public override NodePathName NodePathName => new NodePathName(Guid, TypeDef.Namespace + "." + TypeDef.Name);
		public TypeDef TypeDef { get; }
		IMDTokenProvider IMDTokenNode.Reference => TypeDef;
		protected override ImageReference GetIcon(IDotNetImageManager dnImgMgr) =>
			dnImgMgr.GetImageReference(TypeDef);
		public override ITreeNodeGroup TreeNodeGroup { get; }

		public TypeNode(ITreeNodeGroup treeNodeGroup, TypeDef type) {
			this.TreeNodeGroup = treeNodeGroup;
			this.TypeDef = type;
		}

		public override void Initialize() => TreeNode.LazyLoading = true;

		public override IEnumerable<ITreeNodeData> CreateChildren() {
			yield return new BaseTypeFolderNode(Context.FileTreeView.FileTreeNodeGroups.GetGroup(FileTreeNodeGroupType.BaseTypeFolderTreeNodeGroupType), TypeDef);
			yield return new DerivedTypesFolderNode(Context.FileTreeView.FileTreeNodeGroups.GetGroup(FileTreeNodeGroupType.DerivedTypesFolderTreeNodeGroupType), TypeDef);

			var hash = TypeDef.GetPropertyAndEventMethods();
			foreach (var m in TypeDef.Methods) {
				if (!hash.Contains(m))
					yield return new MethodNode(Context.FileTreeView.FileTreeNodeGroups.GetGroup(FileTreeNodeGroupType.MethodTreeNodeGroupType), m);
			}
			foreach (var p in TypeDef.Properties)
				yield return new PropertyNode(Context.FileTreeView.FileTreeNodeGroups.GetGroup(FileTreeNodeGroupType.PropertyTreeNodeGroupType), p);
			foreach (var e in TypeDef.Events)
				yield return new EventNode(Context.FileTreeView.FileTreeNodeGroups.GetGroup(FileTreeNodeGroupType.EventTreeNodeGroupType), e);
			foreach (var f in TypeDef.Fields)
				yield return new FieldNode(Context.FileTreeView.FileTreeNodeGroups.GetGroup(FileTreeNodeGroupType.FieldTreeNodeGroupType), f);
			foreach (var t in TypeDef.NestedTypes)
				yield return new TypeNode(Context.FileTreeView.FileTreeNodeGroups.GetGroup(FileTreeNodeGroupType.TypeTreeNodeGroupType), t);
		}

		protected override void Write(ITextColorWriter output, ILanguage language) =>
			new NodePrinter().Write(output, language, TypeDef, Context.ShowToken);
		public IMethodNode Create(MethodDef method) => Context.FileTreeView.Create(method);
		public IPropertyNode Create(PropertyDef property) => Context.FileTreeView.Create(property);
		public IEventNode Create(EventDef @event) => Context.FileTreeView.Create(@event);
		public IFieldNode Create(FieldDef field) => Context.FileTreeView.Create(field);
		public ITypeNode Create(TypeDef type) => Context.FileTreeView.CreateNested(type);

		public override FilterType GetFilterType(IFileTreeNodeFilter filter) {
			var res = filter.GetResult(TypeDef);
			if (res.FilterType != FilterType.Default)
				return res.FilterType;
			if (Context.Language.ShowMember(TypeDef))
				return FilterType.Visible;
			return FilterType.Hide;
		}
	}
}
