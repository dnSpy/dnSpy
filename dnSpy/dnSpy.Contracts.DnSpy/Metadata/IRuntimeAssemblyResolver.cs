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
using System.ComponentModel.Composition;
using dnlib.DotNet;

namespace dnSpy.Contracts.DnSpy.Metadata {
	/// <summary>
	/// Resolves assemblies. Use <see cref="ExportRuntimeAssemblyResolverAttribute"/>
	/// to export an instance.
	/// </summary>
	public interface IRuntimeAssemblyResolver {
		/// <summary>
		/// Tries to resolve an assembly or returns 'default' if it couldn't resolve it
		/// </summary>
		/// <param name="assembly">Assembly that should be resolved</param>
		/// <param name="sourceModule">Module that needs to resolve an assembly</param>
		/// <returns></returns>
		RuntimeAssemblyResolverResult Resolve(IAssembly assembly, ModuleDef? sourceModule);
	}

	/// <summary>Metadata</summary>
	public interface IRuntimeAssemblyResolverMetadata {
		/// <summary>See <see cref="ExportRuntimeAssemblyResolverAttribute.Order"/></summary>
		double Order { get; }
	}

	/// <summary>
	/// Exports a <see cref="IRuntimeAssemblyResolver"/> instance
	/// </summary>
	[MetadataAttribute, AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
	public sealed class ExportRuntimeAssemblyResolverAttribute : ExportAttribute, IRuntimeAssemblyResolverMetadata {
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="order">Order of this instance</param>
		public ExportRuntimeAssemblyResolverAttribute(double order = double.MaxValue)
			: base(typeof(IRuntimeAssemblyResolver)) => Order = order;

		/// <summary>
		/// Order of this instance
		/// </summary>
		public double Order { get; }
	}

	/// <summary>
	/// Resolved assembly result, see <see cref="IRuntimeAssemblyResolver"/>
	/// </summary>
	public readonly struct RuntimeAssemblyResolverResult {
		/// <summary>
		/// Checks if this is the 'default' instance
		/// </summary>
		public bool IsDefault => Filename is null && GetFileData is null;

		/// <summary>
		/// Filename of module or null/empty string if it's unknown
		/// </summary>
		public string? Filename { get; }

		/// <summary>
		/// A delegate that creates the assembly data or null
		/// </summary>
		public Func<(byte[]? filedata, bool isFileLayout)>? GetFileData { get; }

		RuntimeAssemblyResolverResult(string? filename, Func<(byte[]? filedata, bool isFileLayout)>? getFileData) {
			Filename = filename;
			GetFileData = getFileData;
		}

		/// <summary>
		/// Creates an instance
		/// </summary>
		/// <param name="filename">Filename</param>
		/// <returns></returns>
		public static RuntimeAssemblyResolverResult Create(string filename) {
			if (filename is null)
				throw new ArgumentNullException(nameof(filename));
			return new RuntimeAssemblyResolverResult(filename, getFileData: null);
		}

		/// <summary>
		/// Creates an instance
		/// </summary>
		/// <param name="getFileData">A delegate that creates the assembly data</param>
		/// <param name="filename">Optional filename</param>
		/// <returns></returns>
		public static RuntimeAssemblyResolverResult Create(Func<(byte[]? filedata, bool isFileLayout)> getFileData, string? filename) {
			if (getFileData is null)
				throw new ArgumentNullException(nameof(getFileData));
			return new RuntimeAssemblyResolverResult(filename, getFileData);
		}
	}
}
