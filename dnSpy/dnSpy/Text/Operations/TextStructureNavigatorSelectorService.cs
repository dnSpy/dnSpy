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
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Utilities;

namespace dnSpy.Text.Operations {
	[Export(typeof(ITextStructureNavigatorSelectorService))]
	sealed class TextStructureNavigatorSelectorService : ITextStructureNavigatorSelectorService {
		readonly IContentTypeRegistryService contentTypeRegistryService;
		readonly Lazy<ITextStructureNavigatorProvider, IContentTypeMetadata>[] textStructureNavigatorProviders;
		ProviderSelector<ITextStructureNavigatorProvider, IContentTypeMetadata>? providerSelector;

		[ImportingConstructor]
		TextStructureNavigatorSelectorService(IContentTypeRegistryService contentTypeRegistryService, [ImportMany] IEnumerable<Lazy<ITextStructureNavigatorProvider, IContentTypeMetadata>> textStructureNavigatorProviders) {
			this.contentTypeRegistryService = contentTypeRegistryService;
			this.textStructureNavigatorProviders = textStructureNavigatorProviders.ToArray();
		}

		public ITextStructureNavigator GetTextStructureNavigator(ITextBuffer textBuffer) {
			if (textBuffer is null)
				throw new ArgumentNullException(nameof(textBuffer));
			return textBuffer.Properties.GetOrCreateSingletonProperty(typeof(ITextStructureNavigator), () => {
				var nav = CreateTextStructureNavigator(textBuffer, textBuffer.ContentType);
				textBuffer.ContentTypeChanged += TextBuffer_ContentTypeChanged;
				return nav;
			});
		}

		static void TextBuffer_ContentTypeChanged(object? sender, ContentTypeChangedEventArgs e) {
			var textBuffer = (ITextBuffer)sender!;
			textBuffer.ContentTypeChanged -= TextBuffer_ContentTypeChanged;
			bool b = textBuffer.Properties.RemoveProperty(typeof(ITextStructureNavigator));
			Debug.Assert(b);
		}

		public ITextStructureNavigator CreateTextStructureNavigator(ITextBuffer textBuffer, IContentType contentType) {
			if (textBuffer is null)
				throw new ArgumentNullException(nameof(textBuffer));
			if (contentType is null)
				throw new ArgumentNullException(nameof(contentType));

			if (providerSelector is null)
				providerSelector = new ProviderSelector<ITextStructureNavigatorProvider, IContentTypeMetadata>(contentTypeRegistryService, textStructureNavigatorProviders);
			foreach (var p in providerSelector.GetProviders(contentType)) {
				var nav = p.Value.CreateTextStructureNavigator(textBuffer);
				if (nav is not null)
					return nav;
			}
			Debug.Fail($"Couldn't find a {nameof(ITextStructureNavigatorProvider)}");
			return new TextStructureNavigator(textBuffer, contentTypeRegistryService.UnknownContentType);
		}
	}
}
