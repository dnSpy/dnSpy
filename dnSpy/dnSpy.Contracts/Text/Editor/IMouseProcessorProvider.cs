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

namespace dnSpy.Contracts.Text.Editor {
	/// <summary>
	/// <see cref="IMouseProcessor"/> provider. Use <see cref="ExportMouseProcessorProviderAttribute"/>
	/// to export an instance.
	/// </summary>
	public interface IMouseProcessorProvider {
		/// <summary>
		/// Creates an <see cref="IMouseProcessor"/> for a <see cref="IWpfTextView"/> or returns null
		/// </summary>
		/// <param name="wpfTextView">Text view</param>
		/// <returns></returns>
		IMouseProcessor GetAssociatedProcessor(IWpfTextView wpfTextView);
	}

	/// <summary>Metadata</summary>
	public interface IMouseProcessorProviderMetadata {
		/// <summary>See <see cref="ExportMouseProcessorProviderAttribute.Order"/></summary>
		double Order { get; }
	}

	/// <summary>
	/// Exports a <see cref="IMouseProcessorProvider"/> instance
	/// </summary>
	[MetadataAttribute, AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
	public sealed class ExportMouseProcessorProviderAttribute : ExportAttribute, IMouseProcessorProviderMetadata {
		/// <summary>Constructor</summary>
		/// <param name="order">Order of this instance</param>
		public ExportMouseProcessorProviderAttribute(double order)
			: base(typeof(IMouseProcessorProvider)) {
			Order = order;
		}

		/// <summary>
		/// Order of this instance
		/// </summary>
		public double Order { get; set; }
	}
}
