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
using System.ComponentModel.Composition;
using dnSpy.Contracts.App;
using dnSpy.Contracts.Bookmarks;
using dnSpy.Contracts.MVVM;
using dnSpy.Contracts.Settings;

namespace dnSpy.Bookmarks.Impl {
	abstract class BookmarkSerializerService {
		public abstract void Save(Bookmark[] bookmarks);
	}

	[Export(typeof(BookmarkSerializerService))]
	sealed class BookmarkSerializerServiceImpl : BookmarkSerializerService {
		readonly Lazy<ISettingsServiceFactory> settingsServiceFactory;
		readonly Lazy<BookmarkLocationSerializerService> bookmarkLocationSerializerService;
		readonly IPickSaveFilename pickSaveFilename;
		readonly IMessageBoxService messageBoxService;

		[ImportingConstructor]
		BookmarkSerializerServiceImpl(Lazy<ISettingsServiceFactory> settingsServiceFactory, Lazy<BookmarkLocationSerializerService> bookmarkLocationSerializerService, IPickSaveFilename pickSaveFilename, IMessageBoxService messageBoxService) {
			this.settingsServiceFactory = settingsServiceFactory;
			this.bookmarkLocationSerializerService = bookmarkLocationSerializerService;
			this.pickSaveFilename = pickSaveFilename;
			this.messageBoxService = messageBoxService;
		}

		public override void Save(Bookmark[] bookmarks) {
			if (bookmarks is null)
				throw new ArgumentNullException(nameof(bookmarks));
			if (bookmarks.Length == 0)
				return;
			var filename = pickSaveFilename.GetFilename(null, "xml", PickFilenameConstants.XmlFilenameFilter);
			if (filename is null)
				return;
			var settingsService = settingsServiceFactory.Value.Create();
			new BookmarksSerializer(settingsService, bookmarkLocationSerializerService.Value).Save(bookmarks);
			try {
				settingsService.Save(filename);
			}
			catch (Exception ex) {
				messageBoxService.Show(ex);
			}
		}
	}
}
