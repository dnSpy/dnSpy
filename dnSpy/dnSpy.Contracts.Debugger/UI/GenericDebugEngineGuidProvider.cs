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

namespace dnSpy.Contracts.Debugger.UI {
	/// <summary>
	/// Detects the debug engine that should be shown by default when showing the options.
	/// Use <see cref="ExportGenericDebugEngineGuidProviderAttribute"/> to export an instance.
	/// </summary>
	public abstract class GenericDebugEngineGuidProvider {
		/// <summary>
		/// Gets the guid of an engine
		/// </summary>
		/// <param name="filename">Filename</param>
		/// <returns></returns>
		public abstract Guid? GetEngineGuid(string filename);
	}

	/// <summary>Metadata</summary>
	public interface IGenericDebugEngineGuidProviderMetadata {
		/// <summary>See <see cref="ExportGenericDebugEngineGuidProviderAttribute.Order"/></summary>
		double Order { get; }
	}

	/// <summary>
	/// Exports a <see cref="GenericDebugEngineGuidProvider"/> instance
	/// </summary>
	[MetadataAttribute, AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
	public sealed class ExportGenericDebugEngineGuidProviderAttribute : ExportAttribute, IGenericDebugEngineGuidProviderMetadata {
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="order">Order, see <see cref="PredefinedGenericDebugEngineGuidProviderOrders"/></param>
		public ExportGenericDebugEngineGuidProviderAttribute(double order)
			: base(typeof(GenericDebugEngineGuidProvider)) => Order = order;

		/// <summary>
		/// Order
		/// </summary>
		public double Order { get; }
	}

	/// <summary>
	/// Predefined <see cref="GenericDebugEngineGuidProvider"/> order constants
	/// </summary>
	public static class PredefinedGenericDebugEngineGuidProviderOrders {
		/// <summary>
		/// .NET Framework / .NET Core
		/// </summary>
		public const double DotNet = 1000000;
	}
}
