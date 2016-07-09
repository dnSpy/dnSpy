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
using dnlib.DotNet;
using dnlib.DotNet.Resources;
using dnSpy.Contracts.TreeView;

namespace dnSpy.Contracts.Files.TreeView.Resources {
	/// <summary>
	/// Creates resource nodes. Use <see cref="ExportResourceNodeCreatorAttribute"/> to export an
	/// instance.
	/// </summary>
	public interface IResourceNodeCreator {
		/// <summary>
		/// Creates a <see cref="IResourceNode"/> instance or returns null
		/// </summary>
		/// <param name="module">Owner module</param>
		/// <param name="resource">Resource</param>
		/// <param name="treeNodeGroup">Group</param>
		/// <returns></returns>
		IResourceNode Create(ModuleDef module, Resource resource, ITreeNodeGroup treeNodeGroup);

		/// <summary>
		/// Creates a <see cref="IResourceElementNode"/> instance or returns null
		/// </summary>
		/// <param name="module">Owner module</param>
		/// <param name="resourceElement">Resource</param>
		/// <param name="treeNodeGroup">Group</param>
		/// <returns></returns>
		IResourceElementNode Create(ModuleDef module, ResourceElement resourceElement, ITreeNodeGroup treeNodeGroup);
	}

	/// <summary>Metadata</summary>
	public interface IResourceNodeCreatorMetadata {
		/// <summary>See <see cref="ExportResourceNodeCreatorAttribute.Order"/></summary>
		double Order { get; }
	}

	/// <summary>
	/// Exports a <see cref="IResourceNodeCreator"/> instance
	/// </summary>
	[MetadataAttribute, AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
	public sealed class ExportResourceNodeCreatorAttribute : ExportAttribute, IResourceNodeCreatorMetadata {
		/// <summary>Constructor</summary>
		public ExportResourceNodeCreatorAttribute()
			: base(typeof(IResourceNodeCreator)) {
			Order = double.MaxValue;
		}

		/// <summary>
		/// Order of this instance
		/// </summary>
		public double Order { get; set; }
	}
}
