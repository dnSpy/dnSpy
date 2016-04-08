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
using System.Diagnostics;
using dnlib.DotNet;
using dnlib.PE;
using dnSpy.Contracts.Files;
using dnSpy.Contracts.Files.TreeView;
using dnSpy.Contracts.Highlighting;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.Languages;
using dnSpy.Contracts.TreeView;
using dnSpy.Decompiler.Shared;
using dnSpy.Languages;
using dnSpy.Shared.Files.TreeView;
using dnSpy.Shared.Highlighting;

namespace dnSpy.Files.TreeView {
	sealed class AssemblyFileNode : DnSpyFileNode, IAssemblyFileNode {
		public AssemblyFileNode(IDnSpyDotNetFile dnSpyFile)
			: base(dnSpyFile) {
			Debug.Assert(dnSpyFile.AssemblyDef != null);
		}

		public new IDnSpyDotNetFile DnSpyFile {
			get { return (IDnSpyDotNetFile)base.DnSpyFile; }
		}

		public override Guid Guid {
			get { return new Guid(FileTVConstants.ASSEMBLY_NODE_GUID); }
		}

		protected override ImageReference GetIcon(IDotNetImageManager dnImgMgr) {
			return dnImgMgr.GetImageReference(DnSpyFile.AssemblyDef);
		}

		public bool IsExe {
			get { return (DnSpyFile.ModuleDef.Characteristics & Characteristics.Dll) == 0; }
		}

		IMDTokenProvider IMDTokenNode.Reference {
			get { return DnSpyFile.AssemblyDef; }
		}

		public override void Initialize() {
			TreeNode.LazyLoading = true;
		}

		public override IEnumerable<ITreeNodeData> CreateChildren() {
			foreach (var file in DnSpyFile.Children)
				yield return Context.FileTreeView.CreateNode(this, file);
		}

		protected override void Write(ISyntaxHighlightOutput output, ILanguage language) {
			new NodePrinter().Write(output, language, DnSpyFile.AssemblyDef, false, Context.ShowAssemblyVersion, Context.ShowAssemblyPublicKeyToken);
		}

		protected override void WriteToolTip(ISyntaxHighlightOutput output, ILanguage language) {
			output.Write(DnSpyFile.AssemblyDef);

			output.WriteLine();
			output.Write(TargetFrameworkInfo.Create(DnSpyFile.AssemblyDef.ManifestModule).ToString(), TextTokenKind.EnumField);

			output.WriteLine();
			output.Write(TargetFrameworkUtils.GetArchString(DnSpyFile.AssemblyDef.ManifestModule), TextTokenKind.EnumField);

			output.WriteLine();
			output.WriteFilename(DnSpyFile.Filename);
		}

		public override FilterType GetFilterType(IFileTreeNodeFilter filter) {
			return filter.GetResult(DnSpyFile.AssemblyDef).FilterType;
		}
	}
}
