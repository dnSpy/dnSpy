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

using dnlib.DotNet;
using dnSpy.Contracts.Highlighting;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.Languages;
using dnSpy.Decompiler.Shared;
using dnSpy.Shared.Highlighting;

namespace dnSpy.Analyzer.TreeNodes {
	sealed class ModuleNode : EntityNode {
		readonly ModuleDef module;

		public override IMemberRef Member {
			get { return null; }
		}

		public override IMDTokenProvider Reference {
			get { return module; }
		}

		public ModuleNode(ModuleDef module) {
			this.module = module;
		}

		protected override ImageReference GetIcon(IDotNetImageManager dnImgMgr) {
			return dnImgMgr.GetImageReference(module);
		}

		protected override void Write(ISyntaxHighlightOutput output, ILanguage language) {
			output.Write(NameUtils.CleanIdentifier(module.Name), TextTokenKind.Module);
		}
	}
}
