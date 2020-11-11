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
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using dnSpy.Contracts.Documents.Tabs.DocViewer;
using Microsoft.VisualStudio.Utilities;

namespace dnSpy.Documents.Tabs.DocViewer {
	interface IDocumentViewerContentFactoryProvider {
		IDocumentViewerContentFactory Create();
	}

	/// <summary>
	/// Provides a <see cref="IDocumentViewerOutput"/> which is used to create a <see cref="DocumentViewerContent"/>
	/// </summary>
	interface IDocumentViewerContentFactory {
		/// <summary>
		/// Gets the output
		/// </summary>
		IDocumentViewerOutput Output { get; }

		/// <summary>
		/// Creates the content. This method can only be called once.
		/// </summary>
		/// <param name="documentViewer">Document viewer</param>
		/// <param name="contentType">Content type</param>
		/// <returns></returns>
		DocumentViewerContent CreateContent(IDocumentViewer documentViewer, IContentType contentType);
	}

	[Export(typeof(IDocumentViewerContentFactoryProvider))]
	sealed class DocumentViewerContentFactoryProvider : IDocumentViewerContentFactoryProvider {
		readonly Lazy<IDocumentViewerPostProcessor, IDocumentViewerPostProcessorMetadata>[] documentViewerPostProcessors;
		readonly Lazy<IDocumentViewerCustomDataProvider, IDocumentViewerCustomDataProviderMetadata>[] documentViewerCustomDataProviders;

		[ImportingConstructor]
		DocumentViewerContentFactoryProvider([ImportMany] IEnumerable<Lazy<IDocumentViewerPostProcessor, IDocumentViewerPostProcessorMetadata>> documentViewerPostProcessors, [ImportMany] IEnumerable<Lazy<IDocumentViewerCustomDataProvider, IDocumentViewerCustomDataProviderMetadata>> documentViewerCustomDataProviders) {
			this.documentViewerPostProcessors = documentViewerPostProcessors.OrderBy(a => a.Metadata.Order).ToArray();
			this.documentViewerCustomDataProviders = documentViewerCustomDataProviders.OrderBy(a => a.Metadata.Order).ToArray();
		}

		public IDocumentViewerContentFactory Create() => new DocumentViewerContentFactory(documentViewerPostProcessors, documentViewerCustomDataProviders);
	}

	sealed class DocumentViewerContentFactory : IDocumentViewerContentFactory {
		public IDocumentViewerOutput Output {
			get {
				if (documentViewerOutput is null)
					throw new InvalidOperationException();
				return documentViewerOutput;
			}
		}

		readonly Lazy<IDocumentViewerPostProcessor, IDocumentViewerPostProcessorMetadata>[] documentViewerPostProcessors;
		readonly Lazy<IDocumentViewerCustomDataProvider, IDocumentViewerCustomDataProviderMetadata>[] documentViewerCustomDataProviders;
		DocumentViewerOutput? documentViewerOutput;

		public DocumentViewerContentFactory(Lazy<IDocumentViewerPostProcessor, IDocumentViewerPostProcessorMetadata>[] documentViewerPostProcessors, Lazy<IDocumentViewerCustomDataProvider, IDocumentViewerCustomDataProviderMetadata>[] documentViewerCustomDataProviders) {
			this.documentViewerPostProcessors = documentViewerPostProcessors ?? throw new ArgumentNullException(nameof(documentViewerPostProcessors));
			this.documentViewerCustomDataProviders = documentViewerCustomDataProviders ?? throw new ArgumentNullException(nameof(documentViewerCustomDataProviders));
			documentViewerOutput = DocumentViewerOutput.Create();
		}

		sealed class DocumentViewerCustomDataContext : IDocumentViewerCustomDataContext, IDisposable {
			public string Text => text!;
			public IDocumentViewer DocumentViewer => documentViewer!;
			public IContentType ContentType { get; }
			string? text;
			IDocumentViewer? documentViewer;
			Dictionary<string, object>? customDataDict;
			Dictionary<string, object>? resultDict;

			public DocumentViewerCustomDataContext(IDocumentViewer documentViewer, string text, IContentType contentType, Dictionary<string, object> customDataDict) {
				this.documentViewer = documentViewer;
				this.text = text;
				ContentType = contentType;
				this.customDataDict = customDataDict;
				resultDict = new Dictionary<string, object>(StringComparer.Ordinal);
			}

			internal Dictionary<string, object>? GetResultDictionary() => resultDict;

			public void AddCustomData(string id, object data) {
				if (customDataDict is null)
					throw new ObjectDisposedException(nameof(IDocumentViewerCustomDataContext));
				Debug2.Assert(resultDict is not null);
				if (id is null)
					throw new ArgumentNullException(nameof(id));
				if (resultDict.ContainsKey(id))
					throw new InvalidOperationException(nameof(AddCustomData) + "() can only be called once with the same " + nameof(id));
				resultDict.Add(id, data);
			}

			public TData[] GetData<TData>(string id) {
				if (customDataDict is null)
					throw new ObjectDisposedException(nameof(IDocumentViewerCustomDataContext));
				if (id is null)
					throw new ArgumentNullException(nameof(id));

				if (!customDataDict.TryGetValue(id, out var listObj))
					return Array.Empty<TData>();
				var list = (List<TData>)listObj;
				return list.ToArray();
			}

			public void Dispose() {
				text = null;
				documentViewer = null;
				customDataDict = null;
				resultDict = null;
			}
		}

		sealed class DocumentViewerPostProcessorContext : IDocumentViewerPostProcessorContext, IDisposable {
			public string Text => text!;
			public IDocumentViewerOutput DocumentViewerOutput => documentViewerOutput!;
			public IDocumentViewer DocumentViewer => documentViewer!;
			public IContentType ContentType { get; }
			string? text;
			IDocumentViewerOutput? documentViewerOutput;
			IDocumentViewer? documentViewer;

			public DocumentViewerPostProcessorContext(IDocumentViewerOutput documentViewerOutput, IDocumentViewer documentViewer, string text, IContentType contentType) {
				this.documentViewerOutput = documentViewerOutput;
				this.documentViewer = documentViewer;
				this.text = text;
				ContentType = contentType;
			}

			public void Dispose() {
				text = null;
				documentViewer = null;
				documentViewerOutput = null;
			}
		}

		public DocumentViewerContent CreateContent(IDocumentViewer documentViewer, IContentType contentType) {
			if (documentViewerOutput is null)
				throw new InvalidOperationException();
			if (documentViewer is null)
				throw new ArgumentNullException(nameof(documentViewer));
			if (contentType is null)
				throw new ArgumentNullException(nameof(contentType));

			documentViewerOutput.SetStatePostProcessing();
			using (var context = new DocumentViewerPostProcessorContext(documentViewerOutput, documentViewer, documentViewerOutput.GetCachedText(), contentType)) {
				foreach (var lz in documentViewerPostProcessors)
					lz.Value.PostProcess(context);
			}

			documentViewerOutput.SetStateCustomDataProviders();
			using (var context = new DocumentViewerCustomDataContext(documentViewer, documentViewerOutput.GetCachedText(), contentType, documentViewerOutput.GetCustomDataDictionary())) {
				foreach (var lz in documentViewerCustomDataProviders)
					lz.Value.OnCustomData(context);
				var output = documentViewerOutput;
				documentViewerOutput = null;
				return output.CreateContent(context.GetResultDictionary()!);
			}
		}
	}
}
