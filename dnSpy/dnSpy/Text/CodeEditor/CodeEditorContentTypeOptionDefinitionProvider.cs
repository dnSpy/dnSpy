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
using Microsoft.VisualStudio.Text.Editor;

namespace dnSpy.Text.CodeEditor {
	[ExportContentTypeOptionDefinitionProvider(PredefinedTextViewGroupNames.CodeEditor)]
	sealed class CodeEditorContentTypeOptionDefinitionProvider : IContentTypeOptionDefinitionProvider {
		readonly Lazy<CodeEditorOptionsDefinition, ICodeEditorOptionsDefinitionMetadata>[] codeEditorOptionsDefinitions;

		[ImportingConstructor]
		CodeEditorContentTypeOptionDefinitionProvider([ImportMany] IEnumerable<Lazy<CodeEditorOptionsDefinition, ICodeEditorOptionsDefinitionMetadata>> codeEditorOptionsDefinitions) {
			this.codeEditorOptionsDefinitions = codeEditorOptionsDefinitions.ToArray();
		}

		public IEnumerable<ContentTypeOptionDefinition> GetOptions() {
			foreach (var lz in codeEditorOptionsDefinitions) {
				yield return new ContentTypeOptionDefinition<bool>(DefaultTextViewOptions.UseVirtualSpaceId) {
					ContentType = lz.Metadata.ContentType,
					DefaultValue = lz.Metadata.UseVirtualSpace,
				};
				yield return new ContentTypeOptionDefinition<WordWrapStyles>(DefaultTextViewOptions.WordWrapStyleId) {
					ContentType = lz.Metadata.ContentType,
					DefaultValue = lz.Metadata.WordWrapStyle,
				};
				yield return new ContentTypeOptionDefinition<bool>(DefaultTextViewHostOptions.LineNumberMarginId) {
					ContentType = lz.Metadata.ContentType,
					DefaultValue = lz.Metadata.ShowLineNumbers,
				};
				yield return new ContentTypeOptionDefinition<int>(DefaultOptions.TabSizeOptionId) {
					ContentType = lz.Metadata.ContentType,
					DefaultValue = lz.Metadata.TabSize,
				};
				yield return new ContentTypeOptionDefinition<int>(DefaultOptions.IndentSizeOptionId) {
					ContentType = lz.Metadata.ContentType,
					DefaultValue = lz.Metadata.IndentSize,
				};
				yield return new ContentTypeOptionDefinition<bool>(DefaultOptions.ConvertTabsToSpacesOptionId) {
					ContentType = lz.Metadata.ContentType,
					DefaultValue = lz.Metadata.ConvertTabsToSpaces,
				};
			}
		}
	}
}
