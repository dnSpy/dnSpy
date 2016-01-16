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
using dnSpy.Shared.UI.Files.TreeView;

namespace dnSpy.Files.TreeView {
	sealed class DerivedTypeNode : FileTreeNodeData, IDerivedTypeNode {
		public override Guid Guid {
			get { return new Guid(FileTVConstants.DERIVEDTYPE_NODE_GUID); }
		}

		public override NodePathName NodePathName {
			get { return new NodePathName(Guid, TypeDef.FullName); }
		}

		IMDTokenProvider IMDTokenNode.Reference {
			get { return TypeDef; }
		}

		public TypeDef TypeDef {
			get {return TryGetTypeDef() ?? new TypeDefUser("???"); }
		}

		TypeDef TryGetTypeDef() {
			return (TypeDef)weakRefTypeDef.Target;
		}
		readonly WeakReference weakRefTypeDef;

		protected override ImageReference GetIcon(IDotNetImageManager dnImgMgr) {
			var td = TryGetTypeDef();
			if (td != null)
				return dnImgMgr.GetImageReference(td);
			return new ImageReference(GetType().Assembly, "Class");
		}

		public override ITreeNodeGroup TreeNodeGroup {
			get { return treeNodeGroup; }
		}
		readonly ITreeNodeGroup treeNodeGroup;

		public DerivedTypeNode(ITreeNodeGroup treeNodeGroup, TypeDef type) {
			this.treeNodeGroup = treeNodeGroup;
			this.weakRefTypeDef = new WeakReference(type);
		}

		public override void Initialize() {
			TreeNode.LazyLoading = createChildren = DerivedTypesFinder.QuickCheck(TryGetTypeDef());
		}
		bool createChildren;

		public override IEnumerable<ITreeNodeData> CreateChildren() {
			if (!createChildren)
				yield break;
			if (derivedTypesFinder != null)
				derivedTypesFinder.Cancel();
			var td = TryGetTypeDef();
			if (td != null)
				derivedTypesFinder = new DerivedTypesFinder(this, td);
		}
		DerivedTypesFinder derivedTypesFinder;

		protected override void Write(ISyntaxHighlightOutput output, ILanguage language) {
			var td = TryGetTypeDef();
			if (td == null)
				output.Write("???", TextTokenKind.Error);
			else
				new NodePrinter().Write(output, language, td, Context.ShowToken);
		}

		public override FilterType GetFilterType(IFileTreeNodeFilter filter) {
			var res = filter.GetResult(this);
			if (res.FilterType != FilterType.Default)
				return res.FilterType;
			var type = TypeDef;
			if (type.IsNested && !Context.Language.ShowMember(type))
				return FilterType.Hide;
			return FilterType.Visible;
		}
	}
}
