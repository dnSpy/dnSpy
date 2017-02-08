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

namespace dnSpy.Contracts.Metadata {
	/// <summary>
	/// Creates a <see cref="IModuleIdFactory"/>. Use <see cref="ExportModuleIdFactoryProviderAttribute"/>
	/// to export an instance.
	/// </summary>
	public interface IModuleIdFactoryProvider {
		/// <summary>
		/// Creates a <see cref="IModuleIdFactory"/> or returns null
		/// </summary>
		/// <returns></returns>
		IModuleIdFactory Create();
	}

	/// <summary>Metadata</summary>
	public interface IModuleIdFactoryProviderMetadata {
		/// <summary>See <see cref="ExportModuleIdFactoryProviderAttribute.Order"/></summary>
		double Order { get; }
	}

	/// <summary>
	/// Exports a <see cref="IModuleIdFactoryProvider"/> instance
	/// </summary>
	[MetadataAttribute, AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
	public sealed class ExportModuleIdFactoryProviderAttribute : ExportAttribute, IModuleIdFactoryProviderMetadata {
		/// <summary>Constructor</summary>
		public ExportModuleIdFactoryProviderAttribute()
			: this(ModuleIdFactoryProviderConstants.OrderDefault) {
		}

		/// <summary>Constructor</summary>
		/// <param name="order">Order of this instance, eg. <see cref="ModuleIdFactoryProviderConstants.OrderDebugger"/></param>
		public ExportModuleIdFactoryProviderAttribute(double order)
			: base(typeof(IModuleIdFactoryProvider)) {
			Order = order;
		}

		/// <summary>
		/// Order of this instance
		/// </summary>
		public double Order { get; set; }
	}
}
