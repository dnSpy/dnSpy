/*
    Copyright (C) 2014-2017 de4dot@gmail.com

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
using dnSpy.Contracts.Hex.Editor;

namespace dnSpy.Contracts.Settings.HexGroups {
	/// <summary>
	/// Provides group names. Use <see cref="ExportHexViewOptionsGroupNameProviderAttribute"/> to
	/// export an instance.
	/// </summary>
	public abstract class HexViewOptionsGroupNameProvider {
		/// <summary>
		/// Constructor
		/// </summary>
		protected HexViewOptionsGroupNameProvider() { }

		/// <summary>
		/// Returns a group name, eg. <see cref="PredefinedHexViewGroupNames.HexEditor"/>, or null
		/// </summary>
		/// <param name="hexView">Hex view</param>
		/// <returns></returns>
		public abstract string TryGetGroupName(WpfHexView hexView);
	}

	/// <summary>Metadata</summary>
	public interface IHexViewOptionsGroupNameProviderMetadata {
		/// <summary>See <see cref="ExportHexViewOptionsGroupNameProviderAttribute.Order"/></summary>
		double Order { get; }
	}

	/// <summary>
	/// Exports a <see cref="HexViewOptionsGroupNameProvider"/> instance
	/// </summary>
	[MetadataAttribute, AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
	public sealed class ExportHexViewOptionsGroupNameProviderAttribute : ExportAttribute, IHexViewOptionsGroupNameProviderMetadata {
		/// <summary>Constructor</summary>
		/// <param name="order">Order of this instanec</param>
		public ExportHexViewOptionsGroupNameProviderAttribute(double order = double.MaxValue)
			: base(typeof(HexViewOptionsGroupNameProvider)) => Order = order;

		/// <summary>
		/// Order of this instance
		/// </summary>
		public double Order { get; }
	}
}
