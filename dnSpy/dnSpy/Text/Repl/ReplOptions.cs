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
using dnSpy.Contracts.Settings.Groups;
using dnSpy.Contracts.Settings.Repl;
using dnSpy.Text.Settings;
using Microsoft.VisualStudio.Utilities;

namespace dnSpy.Text.Repl {
	sealed class ReplOptions : CommonEditorOptions, IReplOptions {
		public Guid Guid { get; }
		public string LanguageName { get; }

		ReplOptions(ITextViewOptionsGroup group, IContentType contentType, Guid guid, string languageName)
			: base(group, contentType) {
			Guid = guid;
			LanguageName = languageName;
		}

		public static ReplOptions? TryCreate(ITextViewOptionsGroup group, IContentTypeRegistryService contentTypeRegistryService, IReplOptionsDefinitionMetadata md) {
			if (group is null)
				throw new ArgumentNullException(nameof(group));
			if (contentTypeRegistryService is null)
				throw new ArgumentNullException(nameof(contentTypeRegistryService));
			if (md is null)
				throw new ArgumentNullException(nameof(md));

			if (md.ContentType is null)
				return null;
			var contentType = contentTypeRegistryService.GetContentType(md.ContentType);
			if (contentType is null)
				return null;

			if (md.Guid is null)
				return null;
			if (!Guid.TryParse(md.Guid, out var guid))
				return null;

			if (md.LanguageName is null)
				return null;

			return new ReplOptions(group, contentType, guid, md.LanguageName);
		}
	}
}
