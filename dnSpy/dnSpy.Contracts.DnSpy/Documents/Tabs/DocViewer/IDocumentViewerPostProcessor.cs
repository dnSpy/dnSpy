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
using dnSpy.Contracts.Decompiler;

namespace dnSpy.Contracts.Documents.Tabs.DocViewer {
	/// <summary>
	/// Gets a chance to add custom data before creating the content. Use
	/// <see cref="ExportDocumentViewerPostProcessorAttribute"/> to export an instance.
	/// </summary>
	public interface IDocumentViewerPostProcessor {
		/// <summary>
		/// Gets called just before all <see cref="IDocumentViewerCustomDataProvider"/> instances
		/// get called. It's possible to call <see cref="IDecompilerOutput.AddCustomData{TData}(string, TData)"/>
		/// but not to add any text.
		/// </summary>
		/// <param name="context">Context</param>
		void PostProcess(IDocumentViewerPostProcessorContext context);
	}

	/// <summary>Metadata</summary>
	public interface IDocumentViewerPostProcessorMetadata {
		/// <summary>See <see cref="ExportDocumentViewerPostProcessorAttribute.Order"/></summary>
		double Order { get; }
	}

	/// <summary>
	/// Exports a <see cref="IDocumentViewerPostProcessor"/> instance
	/// </summary>
	[MetadataAttribute, AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
	public sealed class ExportDocumentViewerPostProcessorAttribute : ExportAttribute, IDocumentViewerPostProcessorMetadata {
		/// <summary>Constructor</summary>
		public ExportDocumentViewerPostProcessorAttribute()
			: this(double.MaxValue) {
		}

		/// <summary>Constructor</summary>
		/// <param name="order">Order of this instance</param>
		public ExportDocumentViewerPostProcessorAttribute(double order)
			: base(typeof(IDocumentViewerPostProcessor)) => Order = order;

		/// <summary>
		/// Order of this instance
		/// </summary>
		public double Order { get; set; }
	}
}
