/*
    Copyright (C) 2014-2019 de4dot@gmail.com

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
	/// Creates <see cref="IDocumentViewerReferenceEnabler"/>s. Use <see cref="ExportDocumentViewerReferenceEnablerProviderAttribute"/>
	/// to export an instance.
	/// </summary>
	public interface IDocumentViewerReferenceEnablerProvider {
		/// <summary>
		/// Creates a <see cref="IDocumentViewerReferenceEnabler"/> or returns null
		/// </summary>
		/// <param name="documentViewer">Document viewer</param>
		/// <returns></returns>
		IDocumentViewerReferenceEnabler? Create(IDocumentViewer documentViewer);
	}

	/// <summary>Metadata</summary>
	public interface IDocumentViewerReferenceEnablerProviderMetadata {
		/// <summary>See <see cref="ExportDocumentViewerReferenceEnablerProviderAttribute.Id"/></summary>
		string Id { get; }
	}

	/// <summary>
	/// Exports a <see cref="IDocumentViewerReferenceEnablerProvider"/> instance
	/// </summary>
	[MetadataAttribute, AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
	public sealed class ExportDocumentViewerReferenceEnablerProviderAttribute : ExportAttribute, IDocumentViewerReferenceEnablerProviderMetadata {
		/// <summary>Constructor</summary>
		/// <param name="id">Reference id, eg. <see cref="PredefinedSpanReferenceIds.HighlightRelatedKeywords"/>. This id
		/// must equal an id stored in <see cref="SpanReference.Id"/></param>
		public ExportDocumentViewerReferenceEnablerProviderAttribute(string id)
			: base(typeof(IDocumentViewerReferenceEnablerProvider)) => Id = id ?? throw new ArgumentNullException(nameof(id));

		/// <summary>
		/// Reference id, eg. <see cref="PredefinedSpanReferenceIds.HighlightRelatedKeywords"/>
		/// </summary>
		public string Id { get; }
	}

	/// <summary>
	/// Enables or disables highlighting of <see cref="SpanReference"/> references
	/// </summary>
	public interface IDocumentViewerReferenceEnabler : IDisposable {
		/// <summary>
		/// Raised whenever <see cref="IsEnabled"/> has changed
		/// </summary>
		event EventHandler? IsEnabledChanged;

		/// <summary>
		/// true if the reference is enabled and can be highlighted, false if the reference
		/// is disabled and can't be highlighted.
		/// </summary>
		bool IsEnabled { get; }
	}
}
