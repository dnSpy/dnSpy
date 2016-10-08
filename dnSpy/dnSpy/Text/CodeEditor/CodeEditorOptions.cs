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
using dnSpy.Contracts.Settings.Dialog;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Utilities;

namespace dnSpy.Text.CodeEditor {
	sealed class CodeEditorOptions : ICodeEditorOptions {
		const int MIN_TAB_SIZE = 1;
		const int MAX_TAB_SIZE = 60;

		public IContentType ContentType { get; }
		public Guid Guid { get; }
		public string LanguageName { get; }

		public bool UseVirtualSpace {
			get { return useVirtualSpace; }
			set {
				if (useVirtualSpace != value) {
					useVirtualSpace = value;
					owner.OptionChanged(this, nameof(UseVirtualSpace));
				}
			}
		}
		bool useVirtualSpace;

		public WordWrapStyles WordWrapStyle {
			get { return wordWrapStyle; }
			set {
				if (wordWrapStyle != value) {
					wordWrapStyle = value;
					owner.OptionChanged(this, nameof(WordWrapStyle));
				}
			}
		}
		WordWrapStyles wordWrapStyle;

		public bool ShowLineNumbers {
			get { return showLineNumbers; }
			set {
				if (showLineNumbers != value) {
					showLineNumbers = value;
					owner.OptionChanged(this, nameof(ShowLineNumbers));
				}
			}
		}
		bool showLineNumbers;

		public int TabSize {
			get { return tabSize; }
			set {
				var newValue = value;
				if (newValue < MIN_TAB_SIZE)
					newValue = MIN_TAB_SIZE;
				else if (newValue > MAX_TAB_SIZE)
					newValue = MAX_TAB_SIZE;
				if (tabSize != newValue) {
					tabSize = newValue;
					owner.OptionChanged(this, nameof(TabSize));
				}
			}
		}
		int tabSize;

		public int IndentSize {
			get { return indentSize; }
			set {
				var newValue = value;
				if (newValue < MIN_TAB_SIZE)
					newValue = MIN_TAB_SIZE;
				else if (newValue > MAX_TAB_SIZE)
					newValue = MAX_TAB_SIZE;
				if (indentSize != newValue) {
					indentSize = newValue;
					owner.OptionChanged(this, nameof(IndentSize));
				}
			}
		}
		int indentSize;

		public bool ConvertTabsToSpaces {
			get { return convertTabsToSpaces; }
			set {
				if (convertTabsToSpaces != value) {
					convertTabsToSpaces = value;
					owner.OptionChanged(this, nameof(ConvertTabsToSpaces));
				}
			}
		}
		bool convertTabsToSpaces;

		readonly CodeEditorOptionsService owner;
		readonly ICodeEditorOptionsDefinitionMetadata md;

		CodeEditorOptions(CodeEditorOptionsService owner, IContentType contentType, Guid guid, string languageName, ICodeEditorOptionsDefinitionMetadata md) {
			this.owner = owner;
			ContentType = contentType;
			Guid = guid;
			LanguageName = languageName;
			this.md = md;

			Reset();
		}

		void Reset() {
			UseVirtualSpace = md.UseVirtualSpace;
			WordWrapStyle = md.WordWrapStyle;
			ShowLineNumbers = md.ShowLineNumbers;
			TabSize = md.TabSize;
			IndentSize = md.IndentSize;
			ConvertTabsToSpaces = md.ConvertTabsToSpaces;
		}

		public static CodeEditorOptions TryCreate(CodeEditorOptionsService owner, IContentTypeRegistryService contentTypeRegistryService, ICodeEditorOptionsDefinitionMetadata md) {
			if (owner == null)
				throw new ArgumentNullException(nameof(owner));
			if (contentTypeRegistryService == null)
				throw new ArgumentNullException(nameof(contentTypeRegistryService));
			if (md == null)
				throw new ArgumentNullException(nameof(md));

			if (md.ContentType == null)
				return null;
			var contentType = contentTypeRegistryService.GetContentType(md.ContentType);
			if (contentType == null)
				return null;

			if (md.Guid == null)
				return null;
			Guid guid;
			if (!Guid.TryParse(md.Guid, out guid))
				return null;

			if (md.LanguageName == null)
				return null;

			return new CodeEditorOptions(owner, contentType, guid, md.LanguageName, md);
		}
	}
}
