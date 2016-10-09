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
using dnSpy.Contracts.Settings.CodeEditor;
using dnSpy.Contracts.Settings.Groups;
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
			get { return group.GetOptionValue(ContentType.TypeName, DefaultTextViewOptions.UseVirtualSpaceId); }
			set { group.SetOptionValue(ContentType.TypeName, DefaultTextViewOptions.UseVirtualSpaceId, value); }
		}

		public WordWrapStyles WordWrapStyle {
			get { return group.GetOptionValue(ContentType.TypeName, DefaultTextViewOptions.WordWrapStyleId); }
			set { group.SetOptionValue(ContentType.TypeName, DefaultTextViewOptions.WordWrapStyleId, value); }
		}

		public bool ShowLineNumbers {
			get { return group.GetOptionValue(ContentType.TypeName, DefaultTextViewHostOptions.LineNumberMarginId); }
			set { group.SetOptionValue(ContentType.TypeName, DefaultTextViewHostOptions.LineNumberMarginId, value); }
		}

		public int TabSize {
			get { return group.GetOptionValue(ContentType.TypeName, DefaultOptions.TabSizeOptionId); }
			set {
				var newValue = value;
				if (newValue < MIN_TAB_SIZE)
					newValue = MIN_TAB_SIZE;
				else if (newValue > MAX_TAB_SIZE)
					newValue = MAX_TAB_SIZE;
				group.SetOptionValue(ContentType.TypeName, DefaultOptions.TabSizeOptionId, newValue);
			}
		}

		public int IndentSize {
			get { return group.GetOptionValue(ContentType.TypeName, DefaultOptions.IndentSizeOptionId); }
			set {
				var newValue = value;
				if (newValue < MIN_TAB_SIZE)
					newValue = MIN_TAB_SIZE;
				else if (newValue > MAX_TAB_SIZE)
					newValue = MAX_TAB_SIZE;
				group.SetOptionValue(ContentType.TypeName, DefaultOptions.IndentSizeOptionId, newValue);
			}
		}

		public bool ConvertTabsToSpaces {
			get { return group.GetOptionValue(ContentType.TypeName, DefaultOptions.ConvertTabsToSpacesOptionId); }
			set { group.SetOptionValue(ContentType.TypeName, DefaultOptions.ConvertTabsToSpacesOptionId, value); }
		}

		readonly ITextViewOptionsGroup group;

		CodeEditorOptions(ITextViewOptionsGroup group, IContentType contentType, Guid guid, string languageName) {
			this.group = group;
			ContentType = contentType;
			Guid = guid;
			LanguageName = languageName;
		}

		public static CodeEditorOptions TryCreate(ITextViewOptionsGroup group, IContentTypeRegistryService contentTypeRegistryService, ICodeEditorOptionsDefinitionMetadata md) {
			if (group == null)
				throw new ArgumentNullException(nameof(group));
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

			return new CodeEditorOptions(group, contentType, guid, md.LanguageName);
		}
	}
}
