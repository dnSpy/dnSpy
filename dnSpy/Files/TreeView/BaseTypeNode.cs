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
using dnSpy.NRefactory;
using dnSpy.Shared.UI.Files.TreeView;

namespace dnSpy.Files.TreeView {
	sealed class BaseTypeNode : FileTreeNodeData, IBaseTypeNode {
		public override Guid Guid {
			get { return new Guid(FileTVConstants.DNSPY_BASETYPE_NODE_GUID); }
		}

		public override NodePathName NodePathName {
			get { return new NodePathName(Guid, TypeDefOrRef.FullName); }
		}

		IMDTokenProvider IMDTokenNode.Reference {
			get { return TypeDefOrRef; }
		}

		protected override ImageReference GetIcon(IDotNetImageManager dnImgMgr) {
			var td = TryGetTypeDef();
			if (td != null)
				return dnImgMgr.GetImageReference(td);
			return new ImageReference(GetType().Assembly, isBaseType ? "Class" : "Interface");
		}

		ITypeDefOrRef TryGetTypeDefOrRef() {
			return (ITypeDefOrRef)weakRefTypeDefOrRef.Target;
		}

		public ITypeDefOrRef TypeDefOrRef {
			get { return TryGetTypeDefOrRef() ?? new TypeRefUser(new ModuleDefUser("???"), "???"); }
		}

		TypeDef TryGetTypeDef() {
			var td = (TypeDef)weakRefResolvedTypeDef.Target;
			if (td != null)
				return td;
			td = TryGetTypeDefOrRef().ResolveTypeDef();
			if (td != null)
				weakRefResolvedTypeDef = new WeakReference(td);
			return td;
		}

		public override ITreeNodeGroup TreeNodeGroup {
			get { return treeNodeGroup; }
		}
		readonly ITreeNodeGroup treeNodeGroup;

		readonly bool isBaseType;
		WeakReference weakRefResolvedTypeDef;
		readonly WeakReference weakRefTypeDefOrRef;

		public BaseTypeNode(ITreeNodeGroup treeNodeGroup, ITypeDefOrRef typeDefOrRef, bool isBaseType) {
			this.treeNodeGroup = treeNodeGroup;
			this.isBaseType = isBaseType;
			// Keep weak refs to them so we won't prevent removed modules from being GC'd.
			this.weakRefTypeDefOrRef = new WeakReference(typeDefOrRef);
			this.weakRefResolvedTypeDef = new WeakReference(null);
		}

		public override void Initialize() {
			TreeNode.LazyLoading = true;
		}

		protected override void Write(ISyntaxHighlightOutput output, ILanguage language) {
			var tdr = TryGetTypeDefOrRef();
			if (tdr == null)
				output.Write("???", TextTokenType.Error);
			else
				new NodePrinter().Write(output, language, tdr, Context.ShowToken);
		}

		public override IEnumerable<ITreeNodeData> CreateChildren() {
			var td = TryGetTypeDef();
			if (td == null)
				yield break;

			if (td.BaseType != null)
				yield return new BaseTypeNode(Context.FileTreeView.FileTreeNodeGroups.GetGroup(FileTreeNodeGroupType.BaseTypeTreeNodeGroupBaseType), td.BaseType, true);
			foreach (var iface in td.Interfaces)
				yield return new BaseTypeNode(Context.FileTreeView.FileTreeNodeGroups.GetGroup(FileTreeNodeGroupType.InterfaceBaseTypeTreeNodeGroupBaseType), iface.Interface, false);
		}
	}
}
