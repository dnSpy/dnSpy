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
using dnSpy.Contracts.Documents.TreeView;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.Text;
using dnSpy.Contracts.TreeView;

namespace dnSpy.Documents.TreeView {
	sealed class BaseTypeNodeImpl : BaseTypeNode {
		public override Guid Guid => new Guid(DocumentTreeViewConstants.BASETYPE_NODE_GUID);
		public override NodePathName NodePathName => new NodePathName(Guid, TypeDefOrRef.FullName);
		public override ITreeNodeGroup TreeNodeGroup { get; }

		protected override ImageReference GetIcon(IDotNetImageService dnImgMgr) {
			var td = TryGetTypeDef();
			if (td != null)
				return dnImgMgr.GetImageReference(td);
			return isBaseType ? DsImages.ClassPublic : DsImages.InterfacePublic;
		}

		ITypeDefOrRef TryGetTypeDefOrRef() => (ITypeDefOrRef)weakRefTypeDefOrRef.Target;
		public override ITypeDefOrRef TypeDefOrRef => TryGetTypeDefOrRef() ?? new TypeRefUser(new ModuleDefUser("???"), "???");

		TypeDef TryGetTypeDef() {
			var td = (TypeDef)weakRefResolvedTypeDef.Target;
			if (td != null)
				return td;
			td = TryGetTypeDefOrRef().ResolveTypeDef();
			if (td != null)
				weakRefResolvedTypeDef = new WeakReference(td);
			return td;
		}

		readonly bool isBaseType;
		WeakReference weakRefResolvedTypeDef;
		readonly WeakReference weakRefTypeDefOrRef;

		public BaseTypeNodeImpl(ITreeNodeGroup treeNodeGroup, ITypeDefOrRef typeDefOrRef, bool isBaseType) {
			this.TreeNodeGroup = treeNodeGroup;
			this.isBaseType = isBaseType;
			// Keep weak refs to them so we won't prevent removed modules from being GC'd.
			this.weakRefTypeDefOrRef = new WeakReference(typeDefOrRef);
			this.weakRefResolvedTypeDef = new WeakReference(null);
		}

		public override void Initialize() => TreeNode.LazyLoading = true;

		protected override void WriteCore(ITextColorWriter output, IDecompiler decompiler, DocumentNodeWriteOptions options) {
			var tdr = TryGetTypeDefOrRef();
			if (tdr == null)
				output.Write(BoxedTextColor.Error, "???");
			else
				new NodePrinter().Write(output, decompiler, tdr, GetShowToken(options));
		}

		public override IEnumerable<TreeNodeData> CreateChildren() {
			var td = TryGetTypeDef();
			if (td == null)
				yield break;

			if (td.BaseType != null)
				yield return new BaseTypeNodeImpl(Context.DocumentTreeView.DocumentTreeNodeGroups.GetGroup(DocumentTreeNodeGroupType.BaseTypeTreeNodeGroupBaseType), td.BaseType, true);
			foreach (var iface in td.Interfaces)
				yield return new BaseTypeNodeImpl(Context.DocumentTreeView.DocumentTreeNodeGroups.GetGroup(DocumentTreeNodeGroupType.InterfaceBaseTypeTreeNodeGroupBaseType), iface.Interface, false);
		}

		public override FilterType GetFilterType(IDocumentTreeNodeFilter filter) => filter.GetResult(this).FilterType;
	}
}
