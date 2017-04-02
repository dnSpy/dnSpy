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
using dnSpy.Contracts.Text.Editor;

namespace dnSpy.Contracts.Debugger.Breakpoints.Code.TextEditor {
	/// <summary>
	/// Creates <see cref="GlyphTextMarkerLocationInfo"/>s. Use <see cref="ExportBreakpointModuleLocationProviderAttribute"/>
	/// to export an instance.
	/// </summary>
	public abstract class BreakpointModuleLocationProvider {
		/// <summary>
		/// Gets the location of the breakpoint or null
		/// </summary>
		/// <param name="breakpoint">Breakpoint</param>
		/// <returns></returns>
		public abstract GlyphTextMarkerLocationInfo GetLocation(DbgCodeBreakpoint breakpoint);
	}

	/// <summary>Metadata</summary>
	public interface IBreakpointModuleLocationProviderMetadata {
		/// <summary>See <see cref="ExportBreakpointModuleLocationProviderAttribute.Order"/></summary>
		double Order { get; }
	}

	/// <summary>
	/// Exports a <see cref="BreakpointModuleLocationProvider"/> instance
	/// </summary>
	[MetadataAttribute, AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
	public sealed class ExportBreakpointModuleLocationProviderAttribute : ExportAttribute, IBreakpointModuleLocationProviderMetadata {
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="order">Order</param>
		public ExportBreakpointModuleLocationProviderAttribute(double order = double.MaxValue)
			: base(typeof(BreakpointModuleLocationProvider)) => Order = order;

		/// <summary>
		/// Order
		/// </summary>
		public double Order { get; }
	}
}
