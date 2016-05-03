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
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using dnSpy.Contracts.Files.Tabs.TextEditor;
using dnSpy.Contracts.Files.TreeView;

namespace dnSpy.Files.Tabs {
	interface IFileTreeNodeDecompiler {
		void Decompile(IDecompileNodeContext decompileNodeContext, IFileTreeNodeData[] nodes);
	}

	[Export, Export(typeof(IFileTreeNodeDecompiler)), PartCreationPolicy(CreationPolicy.Shared)]
	sealed class FileTreeNodeDecompiler : IFileTreeNodeDecompiler {
		readonly IDecompileNode[] decompileNodes;
		readonly IDecompileNodeCollection[] decompileNodeCollections;

		[ImportingConstructor]
		FileTreeNodeDecompiler([ImportMany] IEnumerable<Lazy<IDecompileNode, IDecompileNodeMetadata>> mefDecompileNodes, [ImportMany] IEnumerable<Lazy<IDecompileNodeCollection, IDecompileNodeCollectionMetadata>> mefDecompileNodeCollections) {
			this.decompileNodes = mefDecompileNodes.OrderBy(a => a.Metadata.Order).Select(a => a.Value).ToArray();
			this.decompileNodeCollections = mefDecompileNodeCollections.OrderBy(a => a.Metadata.Order).Select(a => a.Value).ToArray();
			Debug.Assert(this.decompileNodes.Length > 0);
		}

		public void Decompile(IDecompileNodeContext decompileNodeContext, IFileTreeNodeData[] nodes) {
			foreach (var dc in decompileNodeCollections) {
				if (dc.Decompile(decompileNodeContext, nodes))
					return;
			}

			if (nodes.Length == 1) {
				var ds = nodes[0] as IDecompileSelf;
				if (ds?.Decompile(decompileNodeContext) == true)
					return;
			}

			for (int i = 0; i < nodes.Length; i++) {
				decompileNodeContext.DecompilationContext.CancellationToken.ThrowIfCancellationRequested();
				if (i > 0)
					decompileNodeContext.Output.WriteLine();
				DecompileNode(decompileNodeContext, nodes[i]);
			}
		}

		void DecompileNode(IDecompileNodeContext context, IFileTreeNodeData node) {
			foreach (var d in decompileNodes) {
				if (d.Decompile(context, node))
					return;
			}
			Debug.Fail("Missing decompiler");
		}
	}
}
