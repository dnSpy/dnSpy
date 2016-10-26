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

namespace dnSpy.Contracts.TreeView {
	/// <summary>
	/// Creates <see cref="TreeNodeData"/>. Use <see cref="ExportTreeNodeDataProviderAttribute"/> to
	/// export an instance.
	/// </summary>
	public interface ITreeNodeDataProvider {
		/// <summary>
		/// Creates new <see cref="TreeNodeData"/>
		/// </summary>
		/// <param name="context">Context</param>
		/// <returns></returns>
		IEnumerable<TreeNodeData> Create(TreeNodeDataProviderContext context);
	}

	/// <summary>Metadata</summary>
	public interface ITreeNodeDataProviderMetadata {
		/// <summary>See <see cref="ExportTreeNodeDataProviderAttribute.Order"/></summary>
		double Order { get; }
		/// <summary>See <see cref="ExportTreeNodeDataProviderAttribute.Guid"/></summary>
		string Guid { get; }
	}

	/// <summary>
	/// Exports a <see cref="ITreeNodeDataProvider"/> instance
	/// </summary>
	[MetadataAttribute, AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
	public sealed class ExportTreeNodeDataProviderAttribute : ExportAttribute, ITreeNodeDataProviderMetadata {
		/// <summary>Constructor</summary>
		public ExportTreeNodeDataProviderAttribute()
			: base(typeof(ITreeNodeDataProvider)) {
			Order = double.MaxValue;
		}

		/// <summary>
		/// Order of this instance
		/// </summary>
		public double Order { get; set; }

		/// <summary>
		/// Guid of owner <see cref="TreeNodeData"/> that will receive the new
		/// <see cref="TreeNodeData"/> nodes
		/// </summary>
		public string Guid { get; set; }
	}
}
