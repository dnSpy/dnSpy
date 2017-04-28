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
using dnSpy.Contracts.Debugger.Breakpoints.Code;

namespace dnSpy.Contracts.Debugger.CallStack {
	/// <summary>
	/// Converts a stack frame location to a breakpoint location
	/// </summary>
	public abstract class DbgStackFrameBreakpointLocationProvider {
		/// <summary>
		/// Creates a <see cref="DbgBreakpointLocation"/> or null if it doesn't support <paramref name="location"/>
		/// </summary>
		/// <param name="location">Stack frame location</param>
		/// <returns></returns>
		public abstract DbgBreakpointLocation Create(DbgStackFrameLocation location);
	}

	/// <summary>Metadata</summary>
	public interface IDbgStackFrameBreakpointLocationProviderMetadata {
		/// <summary>See <see cref="ExportDbgStackFrameBreakpointLocationProviderAttribute.Order"/></summary>
		double Order { get; }
	}

	/// <summary>
	/// Exports a <see cref="DbgStackFrameBreakpointLocationProvider"/> instance
	/// </summary>
	[MetadataAttribute, AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
	public sealed class ExportDbgStackFrameBreakpointLocationProviderAttribute : ExportAttribute, IDbgStackFrameBreakpointLocationProviderMetadata {
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="order">Order</param>
		public ExportDbgStackFrameBreakpointLocationProviderAttribute(double order = double.MaxValue)
			: base(typeof(DbgStackFrameBreakpointLocationProvider)) => Order = order;

		/// <summary>
		/// Order
		/// </summary>
		public double Order { get; }
	}
}
