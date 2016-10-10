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
using System.Linq;
using dnSpy.Contracts.Settings.CodeEditor;
using dnSpy.Contracts.Settings.Groups;
using dnSpy.Contracts.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace dnSpy.Text.CodeEditor {
	[ExportContentTypeOptionDefinitionProvider(PredefinedTextViewGroupNames.CodeEditor)]
	sealed class CodeEditorContentTypeOptionDefinitionProvider : IContentTypeOptionDefinitionProvider {
		readonly Lazy<CodeEditorOptionsDefinition, ICodeEditorOptionsDefinitionMetadata>[] codeEditorOptionsDefinitions;

		[ImportingConstructor]
		CodeEditorContentTypeOptionDefinitionProvider([ImportMany] IEnumerable<Lazy<CodeEditorOptionsDefinition, ICodeEditorOptionsDefinitionMetadata>> codeEditorOptionsDefinitions) {
			this.codeEditorOptionsDefinitions = codeEditorOptionsDefinitions.ToArray();
		}

		IEnumerable<ICodeEditorOptionsDefinitionMetadata> GetOptionsDefinitions() {
			foreach (var lz in codeEditorOptionsDefinitions)
				yield return lz.Metadata;

			const string DEFAULT_NAME = "";
			const string GUID_CODE_EDITOR_DEFAULT = "509B12BA-9F20-4963-B357-23F182ADE8FD";
			yield return new ExportCodeEditorOptionsDefinitionAttribute(DEFAULT_NAME, ContentTypes.Any, GUID_CODE_EDITOR_DEFAULT);
		}

		public IEnumerable<ContentTypeOptionDefinition> GetOptions() {
			foreach (var md in GetOptionsDefinitions()) {
				yield return new OptionDefinition<bool>(md.ContentType, DefaultTextViewOptions.UseVirtualSpaceId, md.UseVirtualSpace);
				yield return new OptionDefinition<WordWrapStyles>(md.ContentType, DefaultTextViewOptions.WordWrapStyleId, md.WordWrapStyle);
				yield return new OptionDefinition<bool>(md.ContentType, DefaultTextViewHostOptions.LineNumberMarginId, md.ShowLineNumbers);
				yield return new OptionDefinition<bool>(md.ContentType, DefaultTextViewHostOptions.HorizontalScrollBarId, md.HorizontalScrollBar);
				yield return new OptionDefinition<bool>(md.ContentType, DefaultTextViewHostOptions.VerticalScrollBarId, md.VerticalScrollBar);
				yield return new OptionDefinition<int>(md.ContentType, DefaultOptions.TabSizeOptionId, md.TabSize);
				yield return new OptionDefinition<int>(md.ContentType, DefaultOptions.IndentSizeOptionId, md.IndentSize);
				yield return new OptionDefinition<bool>(md.ContentType, DefaultOptions.ConvertTabsToSpacesOptionId, md.ConvertTabsToSpaces);
			}
		}

		sealed class OptionDefinition<T> : ContentTypeOptionDefinition<T> {
			public OptionDefinition(string contentType, EditorOptionKey<T> option, T defaultValue)
				: base(option) {
				ContentType = contentType;
				DefaultValue = defaultValue;
			}
		}
	}
}
