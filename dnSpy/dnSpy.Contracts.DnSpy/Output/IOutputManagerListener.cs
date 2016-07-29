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

namespace dnSpy.Contracts.Output {
	/// <summary>
	/// Gets created when <see cref="IOutputManager"/> gets created. Use
	/// <see cref="ExportOutputManagerListenerAttribute"/> to export an instance.
	/// </summary>
	public interface IOutputManagerListener {
	}

	/// <summary>Metadata</summary>
	public interface IOutputManagerListenerMetadata {
		/// <summary>See <see cref="ExportOutputManagerListenerAttribute.Order"/></summary>
		double Order { get; }
	}

	/// <summary>
	/// Exports a <see cref="IOutputManagerListener"/> instance
	/// </summary>
	[MetadataAttribute, AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
	public sealed class ExportOutputManagerListenerAttribute : ExportAttribute, IOutputManagerListenerMetadata {
		/// <summary>Constructor</summary>
		public ExportOutputManagerListenerAttribute()
			: this(double.MaxValue) {
		}

		/// <summary>Constructor</summary>
		/// <param name="order">Order of this instance</param>
		public ExportOutputManagerListenerAttribute(double order)
			: base(typeof(IOutputManagerListener)) {
			Order = order;
		}

		/// <summary>
		/// Order of this instance
		/// </summary>
		public double Order { get; set; }
	}
}
