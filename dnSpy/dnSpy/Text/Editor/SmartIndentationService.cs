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
using System.Linq;
using dnSpy.Contracts.Text.Editor;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace dnSpy.Text.Editor {
	[Export(typeof(ISmartIndentationService))]
	sealed class SmartIndentationService : ISmartIndentationService {
		readonly IContentTypeRegistryService contentTypeRegistryService;
		readonly Lazy<ISmartIndentProvider, IContentTypeMetadata>[] smartIndentProviders;
		ProviderSelector<ISmartIndentProvider, IContentTypeMetadata> providerSelector;

		[ImportingConstructor]
		SmartIndentationService(IContentTypeRegistryService contentTypeRegistryService, [ImportMany] IEnumerable<Lazy<ISmartIndentProvider, IContentTypeMetadata>> smartIndentProviders) {
			this.contentTypeRegistryService = contentTypeRegistryService;
			this.smartIndentProviders = smartIndentProviders.ToArray();
		}

		public int? GetDesiredIndentation(ITextView textView, ITextSnapshotLine line) {
			if (textView == null)
				throw new ArgumentNullException(nameof(textView));
			if (line == null)
				throw new ArgumentNullException(nameof(line));

			var smartIndent = textView.Properties.GetOrCreateSingletonProperty(typeof(SmartIndentationService), () => new Helper(this, textView).SmartIndent);
			return smartIndent.GetDesiredIndentation(line);
		}

		ISmartIndent CreateSmartIndent(ITextView textView) {
			if (providerSelector == null)
				providerSelector = new ProviderSelector<ISmartIndentProvider, IContentTypeMetadata>(contentTypeRegistryService, smartIndentProviders);
			var contentType = textView.TextDataModel.ContentType;
			foreach (var p in providerSelector.GetProviders(contentType)) {
				var smartIndent = p.Value.CreateSmartIndent(textView);
				if (smartIndent != null)
					return smartIndent;
			}

			return new DefaultSmartIndent(textView);
		}

		sealed class Helper {
			readonly ITextView textView;

			public ISmartIndent SmartIndent { get; }

			public Helper(SmartIndentationService smartIndentationService, ITextView textView) {
				this.textView = textView;

				textView.Closed += TextView_Closed;
				textView.Options.OptionChanged += Options_OptionChanged;
				textView.TextDataModel.ContentTypeChanged += TextDataModel_ContentTypeChanged;
				SmartIndent = smartIndentationService.CreateSmartIndent(textView);
			}

			void Options_OptionChanged(object sender, EditorOptionChangedEventArgs e) {
				if (e.OptionId == DefaultDsOptions.IndentStyleOptionName)
					CleanUp();
			}

			void TextDataModel_ContentTypeChanged(object sender, TextDataModelContentTypeChangedEventArgs e) => CleanUp();
			void TextView_Closed(object sender, EventArgs e) => CleanUp();

			void CleanUp() {
				textView.Closed -= TextView_Closed;
				textView.Options.OptionChanged -= Options_OptionChanged;
				textView.TextDataModel.ContentTypeChanged -= TextDataModel_ContentTypeChanged;
				textView.Properties.RemoveProperty(typeof(SmartIndentationService));
			}
		}
	}
}
