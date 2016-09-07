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

namespace dnSpy.Contracts.Files.Tabs.DocViewer.ToolTips {
	/// <summary>
	/// Creates tooltips. Use <see cref="ExportDocumentViewerToolTipProviderAttribute"/> to export an
	/// instance.
	/// </summary>
	public interface IDocumentViewerToolTipProvider {
		/// <summary>
		/// Creates a tooltip or returns null
		/// </summary>
		/// <param name="context">Context</param>
		/// <param name="ref">Reference</param>
		/// <returns></returns>
		object Create(IDocumentViewerToolTipProviderContext context, object @ref);
	}

	/// <summary>Metadata</summary>
	public interface IDocumentViewerToolTipProviderMetadata {
		/// <summary>See <see cref="ExportDocumentViewerToolTipProviderAttribute.Order"/></summary>
		double Order { get; }
	}

	/// <summary>
	/// Exports a <see cref="IDocumentViewerToolTipProvider"/> instance
	/// </summary>
	[MetadataAttribute, AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
	public sealed class ExportDocumentViewerToolTipProviderAttribute : ExportAttribute, IDocumentViewerToolTipProviderMetadata {
		/// <summary>Constructor</summary>
		public ExportDocumentViewerToolTipProviderAttribute()
			: this(double.MaxValue) {
		}

		/// <summary>Constructor</summary>
		/// <param name="order">Order of this instance</param>
		public ExportDocumentViewerToolTipProviderAttribute(double order)
			: base(typeof(IDocumentViewerToolTipProvider)) {
		}

		/// <summary>
		/// Order of this instance
		/// </summary>
		public double Order { get; }
	}
}
