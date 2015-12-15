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
using System.IO;
using System.Threading;
using dnlib.DotNet;
using dnSpy.Contracts.Files.Tabs.TextEditor;
using dnSpy.Contracts.Files.TreeView;
using dnSpy.Contracts.Files.TreeView.Resources;
using dnSpy.Contracts.Languages;
using dnSpy.Contracts.TreeView;
using dnSpy.Shared.UI.Decompiler;
using dnSpy.Shared.UI.Files.TreeView.Resources;
using ICSharpCode.Decompiler;

namespace dnSpy.Files.TreeView.Resources {
	sealed class UnknownResourceNode : ResourceNode, IUnknownResourceNode, IDecompileSelf {
		public override Guid Guid {
			get { return new Guid(FileTVConstants.UNKNOWN_RESOURCE_NODE_GUID); }
		}

		public UnknownResourceNode(ITreeNodeGroup treeNodeGroup, Resource resource)
			: base(treeNodeGroup, resource) {
		}

		public override void WriteShort(ITextOutput output, ILanguage language, bool showOffset) {
			base.WriteShort(output, language, showOffset);
			var so = output as ISmartTextOutput;
			if (so != null) {
				so.AddButton("Save", (s, e) => Save());
				so.WriteLine();
				so.WriteLine();
			}
		}

		public override string ToString(CancellationToken token, bool canDecompile) {
			var er = Resource as EmbeddedResource;
			if (er != null)
				return ResourceUtils.TryGetString(new MemoryStream(er.GetResourceData()));
			return null;
		}

		public bool Decompile(IDecompileNodeContext context) {
			var er = Resource as EmbeddedResource;
			if (er != null)
				return ResourceUtils.Decompile(context, new MemoryStream(er.GetResourceData()), er.Name);
			return false;
		}
	}
}
