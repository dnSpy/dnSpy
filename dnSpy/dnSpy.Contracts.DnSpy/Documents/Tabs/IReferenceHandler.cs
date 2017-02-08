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

namespace dnSpy.Contracts.Documents.Tabs {
	/// <summary>
	/// Called when a reference is followed in a tab. Use <see cref="ExportReferenceHandlerAttribute"/>
	/// to export an instance.
	/// </summary>
	public interface IReferenceHandler {
		/// <summary>
		/// Called when a reference is followed. Returns true if it was handled, otherwise false.
		/// </summary>
		/// <param name="context">Context</param>
		/// <returns></returns>
		bool OnFollowReference(IReferenceHandlerContext context);
	}

	/// <summary>Metadata</summary>
	public interface IReferenceHandlerMetadata {
		/// <summary>See <see cref="ExportReferenceHandlerAttribute.Order"/></summary>
		double Order { get; }
	}

	/// <summary>
	/// Exports a <see cref="IReferenceHandler"/> instance
	/// </summary>
	[MetadataAttribute, AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
	public sealed class ExportReferenceHandlerAttribute : ExportAttribute, IReferenceHandlerMetadata {
		/// <summary>Constructor</summary>
		/// <param name="order">Order</param>
		public ExportReferenceHandlerAttribute(double order = double.MaxValue)
			: base(typeof(IReferenceHandler)) {
			Order = order;
		}

		/// <summary>
		/// Order of this instance
		/// </summary>
		public double Order { get; }
	}
}
