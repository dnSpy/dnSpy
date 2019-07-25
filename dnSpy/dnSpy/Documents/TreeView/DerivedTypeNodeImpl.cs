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
using dnlib.DotNet;
using dnSpy.Contracts.Decompiler;
using dnSpy.Contracts.Documents.TreeView;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.Text;
using dnSpy.Contracts.TreeView;

namespace dnSpy.Documents.TreeView {
	sealed class DerivedTypeNodeImpl : DerivedTypeNode {
		public override Guid Guid => new Guid(DocumentTreeViewConstants.DERIVEDTYPE_NODE_GUID);
		public override NodePathName NodePathName => new NodePathName(Guid, TypeDef.FullName);
		public override ITreeNodeGroup? TreeNodeGroup { get; }
		public override TypeDef TypeDef => TryGetTypeDef() ?? new TypeDefUser("???");
		TypeDef? TryGetTypeDef() => (TypeDef?)weakRefTypeDef.Target;
		readonly WeakReference weakRefTypeDef;

		protected override ImageReference GetIcon(IDotNetImageService dnImgMgr) {
			var td = TryGetTypeDef();
			if (!(td is null))
				return dnImgMgr.GetImageReference(td);
			return DsImages.ClassPublic;
		}

		public DerivedTypeNodeImpl(ITreeNodeGroup treeNodeGroup, TypeDef type) {
			TreeNodeGroup = treeNodeGroup;
			weakRefTypeDef = new WeakReference(type);
		}

		public override void Initialize() =>
			TreeNode.LazyLoading = createChildren = DerivedTypesFinder.QuickCheck(TryGetTypeDef());
		bool createChildren;

		public override IEnumerable<TreeNodeData> CreateChildren() {
			if (!createChildren)
				yield break;
			if (!(derivedTypesFinder is null)) {
				derivedTypesFinder.Cancel();
				derivedTypesFinder = null;
			}
			var td = TryGetTypeDef();
			if (!(td is null))
				derivedTypesFinder = new DerivedTypesFinder(this, td);
		}
		DerivedTypesFinder? derivedTypesFinder;

		protected override void WriteCore(ITextColorWriter output, IDecompiler decompiler, DocumentNodeWriteOptions options) {
			var td = TryGetTypeDef();
			if ((options & DocumentNodeWriteOptions.ToolTip) != 0) {
				if (td is null)
					output.Write(BoxedTextColor.Error, "???");
				else
					WriteMemberRef(output, decompiler, td);
				output.WriteLine();
				WriteFilename(output);
			}
			else {
				if (td is null)
					output.Write(BoxedTextColor.Error, "???");
				else
					new NodeFormatter().Write(output, decompiler, td, GetShowToken(options));
			}
		}

		public override FilterType GetFilterType(IDocumentTreeNodeFilter filter) {
			var res = filter.GetResult(this);
			if (res.FilterType != FilterType.Default)
				return res.FilterType;
			var type = TypeDef;
			if (type.IsNested && !Context.Decompiler.ShowMember(type))
				return FilterType.Hide;
			return FilterType.Visible;
		}
	}
}
