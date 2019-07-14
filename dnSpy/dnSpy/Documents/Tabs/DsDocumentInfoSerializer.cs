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
using dnSpy.Contracts.Documents;
using dnSpy.Contracts.Settings;

namespace dnSpy.Documents.Tabs {
	static class DsDocumentInfoSerializer {
		const string DOCUMENTINFO_NAME_ATTR = "name";
		const string DOCUMENTINFO_TYPE_ATTR = "type";

		public static DsDocumentInfo? TryLoad(ISettingsSection section) {
			var name = section.Attribute<string>(DOCUMENTINFO_NAME_ATTR);
			var type = section.Attribute<Guid?>(DOCUMENTINFO_TYPE_ATTR) ?? DocumentConstants.DOCUMENTTYPE_FILE;
			if (string.IsNullOrEmpty(name))
				return null;
			return new DsDocumentInfo(name, type);
		}

		public static void Save(ISettingsSection section, DsDocumentInfo info) {
			// Assume that instances with a non-null Data property can't be serialized
			if (!(info.Data is null))
				return;

			section.Attribute(DOCUMENTINFO_NAME_ATTR, info.Name);
			if (info.Type != DocumentConstants.DOCUMENTTYPE_FILE)
				section.Attribute(DOCUMENTINFO_TYPE_ATTR, info.Type);
		}
	}
}
