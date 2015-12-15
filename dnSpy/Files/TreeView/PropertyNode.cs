/*
    Copyright (C) 2014-2015 de4dot@gmail.com

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
using dnSpy.Shared.UI.Files.TreeView;

namespace dnSpy.Files.TreeView {
	sealed class PropertyNode : FileTreeNodeData, IPropertyNode {
		public override Guid Guid {
			get { return new Guid(FileTVConstants.PROPERTY_NODE_GUID); }
		}

		public override NodePathName NodePathName {
			get { return new NodePathName(Guid, property.FullName); }
		}

		public PropertyDef PropertyDef {
			get { return property; }
		}
		readonly PropertyDef property;

		IMDTokenProvider IMDTokenNode.Reference {
			get { return property; }
		}

		protected override ImageReference GetIcon(IDotNetImageManager dnImgMgr) {
			return dnImgMgr.GetImageReference(property);
		}

		public override ITreeNodeGroup TreeNodeGroup {
			get { return treeNodeGroup; }
		}
		readonly ITreeNodeGroup treeNodeGroup;

		public PropertyNode(ITreeNodeGroup treeNodeGroup, PropertyDef property) {
			this.treeNodeGroup = treeNodeGroup;
			this.property = property;
		}

		protected override void Write(ISyntaxHighlightOutput output, ILanguage language) {
			new NodePrinter().Write(output, language, property, Context.ShowToken, null);
		}

		public override IEnumerable<ITreeNodeData> CreateChildren() {
			foreach (var m in property.GetMethods)
				yield return new MethodNode(Context.FileTreeView.FileTreeNodeGroups.GetGroup(FileTreeNodeGroupType.MethodTreeNodeGroupProperty), m);
			foreach (var m in property.SetMethods)
				yield return new MethodNode(Context.FileTreeView.FileTreeNodeGroups.GetGroup(FileTreeNodeGroupType.MethodTreeNodeGroupProperty), m);
			foreach (var m in property.OtherMethods)
				yield return new MethodNode(Context.FileTreeView.FileTreeNodeGroups.GetGroup(FileTreeNodeGroupType.MethodTreeNodeGroupProperty), m);
		}

		public IMethodNode Create(MethodDef method) {
			return Context.FileTreeView.CreateProperty(method);
		}

		public override FilterType GetFilterType(IFileTreeNodeFilter filter) {
			var res = filter.GetResult(PropertyDef);
			if (res.FilterType != FilterType.Default)
				return res.FilterType;
			if (Context.Language.ShowMember(PropertyDef, Context.DecompilerSettings))
				return FilterType.Visible;
			return FilterType.Hide;
		}
	}
}
