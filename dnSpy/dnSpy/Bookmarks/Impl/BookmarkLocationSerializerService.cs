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
using dnSpy.Contracts.Bookmarks;
using dnSpy.Contracts.Settings;

namespace dnSpy.Bookmarks.Impl {
	abstract class BookmarkLocationSerializerService {
		public abstract void Serialize(ISettingsSection section, BookmarkLocation location);
		public abstract BookmarkLocation Deserialize(ISettingsSection section);
	}

	[Export(typeof(BookmarkLocationSerializerService))]
	sealed class BookmarkLocationSerializerServiceImpl : BookmarkLocationSerializerService {
		readonly Lazy<BookmarkLocationSerializer, IBookmarkLocationSerializerMetadata>[] bookmarkLocationSerializers;

		[ImportingConstructor]
		BookmarkLocationSerializerServiceImpl([ImportMany] IEnumerable<Lazy<BookmarkLocationSerializer, IBookmarkLocationSerializerMetadata>> bookmarkLocationSerializers) =>
			this.bookmarkLocationSerializers = bookmarkLocationSerializers.ToArray();

		Lazy<BookmarkLocationSerializer, IBookmarkLocationSerializerMetadata> TryGetSerializer(string type) {
			foreach (var lz in bookmarkLocationSerializers) {
				if (Array.IndexOf(lz.Metadata.Types, type) >= 0)
					return lz;
			}
			return null;
		}

		public override void Serialize(ISettingsSection section, BookmarkLocation location) {
			if (section == null)
				throw new ArgumentNullException(nameof(section));
			if (location == null)
				throw new ArgumentNullException(nameof(location));

			var bmType = location.Type;
			var serializer = TryGetSerializer(bmType);
			Debug.Assert(serializer != null);
			if (serializer == null)
				return;

			section.Attribute("__BMT", bmType);
			serializer.Value.Serialize(section, location);
		}

		public override BookmarkLocation Deserialize(ISettingsSection section) {
			if (section == null)
				return null;

			var typeFullName = section.Attribute<string>("__BMT");
			Debug.Assert(typeFullName != null);
			if (typeFullName == null)
				return null;
			var serializer = TryGetSerializer(typeFullName);
			Debug.Assert(serializer != null);
			if (serializer == null)
				return null;

			return serializer.Value.Deserialize(section);
		}
	}
}
