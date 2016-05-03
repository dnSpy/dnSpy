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
using dnSpy.Shared.Files.TreeView;

namespace dnSpy.Files.TreeView {
	sealed class PropertyNode : FileTreeNodeData, IPropertyNode {
		public override Guid Guid => new Guid(FileTVConstants.PROPERTY_NODE_GUID);
		public override NodePathName NodePathName => new NodePathName(Guid, PropertyDef.FullName);
		public PropertyDef PropertyDef { get; }
		IMDTokenProvider IMDTokenNode.Reference => PropertyDef;
		protected override ImageReference GetIcon(IDotNetImageManager dnImgMgr) => dnImgMgr.GetImageReference(PropertyDef);
		public override ITreeNodeGroup TreeNodeGroup { get; }

		public PropertyNode(ITreeNodeGroup treeNodeGroup, PropertyDef property) {
			this.TreeNodeGroup = treeNodeGroup;
			this.PropertyDef = property;
		}

		protected override void Write(ISyntaxHighlightOutput output, ILanguage language) =>
			new NodePrinter().Write(output, language, PropertyDef, Context.ShowToken, null);

		public override IEnumerable<ITreeNodeData> CreateChildren() {
			foreach (var m in PropertyDef.GetMethods)
				yield return new MethodNode(Context.FileTreeView.FileTreeNodeGroups.GetGroup(FileTreeNodeGroupType.MethodTreeNodeGroupProperty), m);
			foreach (var m in PropertyDef.SetMethods)
				yield return new MethodNode(Context.FileTreeView.FileTreeNodeGroups.GetGroup(FileTreeNodeGroupType.MethodTreeNodeGroupProperty), m);
			foreach (var m in PropertyDef.OtherMethods)
				yield return new MethodNode(Context.FileTreeView.FileTreeNodeGroups.GetGroup(FileTreeNodeGroupType.MethodTreeNodeGroupProperty), m);
		}

		public IMethodNode Create(MethodDef method) => Context.FileTreeView.CreateProperty(method);

		public override FilterType GetFilterType(IFileTreeNodeFilter filter) {
			var res = filter.GetResult(PropertyDef);
			if (res.FilterType != FilterType.Default)
				return res.FilterType;
			if (Context.Language.ShowMember(PropertyDef))
				return FilterType.Visible;
			return FilterType.Hide;
		}
	}
}
