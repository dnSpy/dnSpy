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
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using dnSpy.Contracts.Extension;
using dnSpy.Contracts.Settings;
using dnSpy.Contracts.Settings.Groups;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace dnSpy.Text.Groups {
	[ExportAutoLoaded(LoadType = AutoLoadedLoadType.BeforeExtensions)]
	sealed class TextViewOptionsGroupServiceLoader : IAutoLoaded {
		readonly Lazy<ITextViewOptionsGroupServiceImpl> textViewOptionsGroupService;

		[ImportingConstructor]
		TextViewOptionsGroupServiceLoader(Lazy<ITextViewOptionsGroupServiceImpl> textViewOptionsGroupService, ITextEditorFactoryService textEditorFactoryService) {
			this.textViewOptionsGroupService = textViewOptionsGroupService;
			textEditorFactoryService.TextViewCreated += TextEditorFactoryService_TextViewCreated;
		}

		void TextEditorFactoryService_TextViewCreated(object sender, TextViewCreatedEventArgs e) =>
			textViewOptionsGroupService.Value.TextViewCreated(e.TextView);
	}

	interface ITextViewOptionsGroupServiceImpl : ITextViewOptionsGroupService {
		void TextViewCreated(ITextView textView);
	}

	[Export(typeof(ITextViewOptionsGroupService)), Export(typeof(ITextViewOptionsGroupServiceImpl))]
	sealed class TextViewOptionsGroupService : ITextViewOptionsGroupServiceImpl {
		readonly IContentTypeRegistryService contentTypeRegistryService;
		readonly Lazy<ITextViewOptionsGroupNameProvider, ITextViewOptionsGroupNameProviderMetadata>[] textViewOptionsGroupNameProviders;
		readonly Lazy<IContentTypeOptionDefinitionProvider, IContentTypeOptionDefinitionProviderMetadata>[] contentTypeOptionDefinitionProviders;
		readonly Dictionary<string, TextViewOptionsGroup> nameToGroup;
		readonly OptionsStorage optionsStorage;

		[ImportingConstructor]
		TextViewOptionsGroupService(ISettingsService settingsService, IContentTypeRegistryService contentTypeRegistryService, [ImportMany] IEnumerable<Lazy<ITextViewOptionsGroupNameProvider, ITextViewOptionsGroupNameProviderMetadata>> textViewOptionsGroupNameProviders, [ImportMany] IEnumerable<Lazy<IContentTypeOptionDefinitionProvider, IContentTypeOptionDefinitionProviderMetadata>> contentTypeOptionDefinitionProviders) {
			nameToGroup = new Dictionary<string, TextViewOptionsGroup>(StringComparer.Ordinal);
			this.contentTypeRegistryService = contentTypeRegistryService;
			this.textViewOptionsGroupNameProviders = textViewOptionsGroupNameProviders.OrderBy(a => a.Metadata.Order).ToArray();
			this.contentTypeOptionDefinitionProviders = contentTypeOptionDefinitionProviders.OrderBy(a => a.Metadata.Order).ToArray();
			optionsStorage = new OptionsStorage(settingsService);
		}

		ITextViewOptionsGroup ITextViewOptionsGroupService.GetGroup(string name) => GetGroup(name);
		TextViewOptionsGroup GetGroup(string name) {
			if (name == null)
				throw new ArgumentNullException(nameof(name));
			TextViewOptionsGroup group;
			if (!nameToGroup.TryGetValue(name, out group)) {
				var defaultOptions = GetDefaultOptions(name);
				nameToGroup.Add(name, group = new TextViewOptionsGroup(name, contentTypeRegistryService, defaultOptions, optionsStorage));
			}
			return group;
		}

		ContentTypeOptionDefinition[] GetDefaultOptions(string groupName) {
			var options = new List<ContentTypeOptionDefinition>();
			foreach (var lz in contentTypeOptionDefinitionProviders) {
				if (lz.Metadata.Group != groupName)
					continue;
				options.AddRange(lz.Value.GetOptions());
			}
			return options.Where(a => a.ContentType != null && a.Name != null && a.Type != null).ToArray();
		}

		void ITextViewOptionsGroupServiceImpl.TextViewCreated(ITextView textView) {
			var wpfTextView = textView as IWpfTextView;
			Debug.Assert(wpfTextView != null);
			if (wpfTextView == null)
				return;

			Debug.Assert(!wpfTextView.IsClosed);
			if (wpfTextView.IsClosed)
				return;

			foreach (var lz in textViewOptionsGroupNameProviders) {
				var name = lz.Value.TryGetGroupName(wpfTextView);
				if (name != null) {
					var group = GetGroup(name);
					group.TextViewCreated(wpfTextView);
					break;
				}
			}
		}
	}
}
