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
using dnSpy.Contracts.Debugger.Evaluation;

namespace dnSpy.Contracts.Debugger.DotNet.Code {
	/// <summary>
	/// Converts <see cref="DbgLanguage"/>s to decompiler GUIDs. Use <see cref="ExportDbgDotNetDecompilerGuidProviderAttribute"/>
	/// to export an instance
	/// </summary>
	public abstract class DbgDotNetDecompilerGuidProvider {
		/// <summary>
		/// Gets the decompiler GUID or null
		/// </summary>
		/// <param name="language">Language</param>
		/// <returns></returns>
		public abstract Guid? GetDecompilerGuid(DbgLanguage language);
	}

	/// <summary>Metadata</summary>
	public interface IDbgDotNetDecompilerGuidProviderMetadata {
		/// <summary>See <see cref="ExportDbgDotNetDecompilerGuidProviderAttribute.Order"/></summary>
		double Order { get; }
	}

	/// <summary>
	/// Exports a <see cref="DbgDotNetDecompilerGuidProvider"/> instance
	/// </summary>
	[MetadataAttribute, AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
	public sealed class ExportDbgDotNetDecompilerGuidProviderAttribute : ExportAttribute, IDbgDotNetDecompilerGuidProviderMetadata {
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="order">Order</param>
		public ExportDbgDotNetDecompilerGuidProviderAttribute(double order = double.MaxValue)
			: base(typeof(DbgDotNetDecompilerGuidProvider)) {
			Order = order;
		}

		/// <summary>
		/// Order
		/// </summary>
		public double Order { get; }
	}
}
