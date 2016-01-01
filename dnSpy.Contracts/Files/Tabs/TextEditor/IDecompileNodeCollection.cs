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
using System.ComponentModel.Composition;
using dnSpy.Contracts.Files.TreeView;

namespace dnSpy.Contracts.Files.Tabs.TextEditor {
	/// <summary>
	/// Decompiles <see cref="IFileTreeNodeData"/> instances. Use <see cref="ExportDecompileNodeCollectionAttribute"/>
	/// to export an instance.
	/// </summary>
	public interface IDecompileNodeCollection {
		/// <summary>
		/// Decompiles <paramref name="nodes"/> or returns false if someone else should have a try.
		/// This method can be called in any thread
		/// </summary>
		/// <param name="context">Context</param>
		/// <param name="nodes">Nodes to decompile</param>
		/// <returns></returns>
		bool Decompile(IDecompileNodeContext context, IFileTreeNodeData[] nodes);
	}

	/// <summary>Metadata</summary>
	public interface IDecompileNodeCollectionMetadata {
		/// <summary>See <see cref="ExportDecompileNodeCollectionAttribute.Order"/></summary>
		double Order { get; }
	}

	/// <summary>
	/// Exports a <see cref="IDecompileNodeCollection"/> instance
	/// </summary>
	[MetadataAttribute, AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
	public sealed class ExportDecompileNodeCollectionAttribute : ExportAttribute, IDecompileNodeCollectionMetadata {
		/// <summary>Constructor</summary>
		public ExportDecompileNodeCollectionAttribute()
			: base(typeof(IDecompileNodeCollection)) {
		}

		/// <summary>
		/// Order of this instance
		/// </summary>
		public double Order { get; set; }
	}
}
