/*
    Copyright (C) 2014-2015 de4dot@gmail.com

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
using dnSpy.Contracts.Files;
using dnSpy.Contracts.Settings;

namespace dnSpy.Files.Tabs {
	static class DnSpyFileInfoSerializer {
		const string FILEINFO_NAME_ATTR = "name";
		const string FILEINFO_TYPE_ATTR = "type";

		public static DnSpyFileInfo? TryLoad(ISettingsSection section) {
			var name = section.Attribute<string>(FILEINFO_NAME_ATTR);
			var type = section.Attribute<Guid?>(FILEINFO_TYPE_ATTR) ?? FileConstants.FILETYPE_FILE;
			if (string.IsNullOrEmpty(name))
				return null;
			return new DnSpyFileInfo(name, type);
		}

		public static void Save(ISettingsSection section, DnSpyFileInfo info) {
			section.Attribute(FILEINFO_NAME_ATTR, info.Name);
			if (info.Type != FileConstants.FILETYPE_FILE)
				section.Attribute(FILEINFO_TYPE_ATTR, info.Type);
		}
	}
}
