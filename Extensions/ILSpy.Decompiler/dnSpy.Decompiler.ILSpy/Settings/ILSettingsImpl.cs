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
using dnSpy.Contracts.Settings;
using dnSpy.Decompiler.ILSpy.Core.Settings;

namespace dnSpy.Decompiler.ILSpy.Settings {
	[Export]
	sealed class ILSettingsImpl : ILSettings {
		static readonly Guid SETTINGS_GUID = new Guid("DD6752B1-5336-4601-A9B2-0879E18AE9F3");

		readonly ISettingsService settingsService;

		[ImportingConstructor]
		ILSettingsImpl(ISettingsService settingsService) {
			this.settingsService = settingsService;

			disableSave = true;
			var sect = settingsService.GetOrCreateSection(SETTINGS_GUID);
			ShowILComments = sect.Attribute<bool?>(nameof(ShowILComments)) ?? ShowILComments;
			ShowXmlDocumentation = sect.Attribute<bool?>(nameof(ShowXmlDocumentation)) ?? ShowXmlDocumentation;
			ShowTokenAndRvaComments = sect.Attribute<bool?>(nameof(ShowTokenAndRvaComments)) ?? ShowTokenAndRvaComments;
			ShowILBytes = sect.Attribute<bool?>(nameof(ShowILBytes)) ?? ShowILBytes;
			SortMembers = sect.Attribute<bool?>(nameof(SortMembers)) ?? SortMembers;
			ShowPdbInfo = sect.Attribute<bool?>(nameof(ShowPdbInfo)) ?? ShowPdbInfo;
			MaxStringLength = sect.Attribute<int?>(nameof(MaxStringLength)) ?? MaxStringLength;
			HexadecimalNumbers = sect.Attribute<bool?>(nameof(HexadecimalNumbers)) ?? HexadecimalNumbers;
			disableSave = false;
		}
		readonly bool disableSave;

		protected override void OnModified() {
			if (disableSave)
				return;

			var sect = settingsService.RecreateSection(SETTINGS_GUID);
			sect.Attribute(nameof(ShowILComments), ShowILComments);
			sect.Attribute(nameof(ShowXmlDocumentation), ShowXmlDocumentation);
			sect.Attribute(nameof(ShowTokenAndRvaComments), ShowTokenAndRvaComments);
			sect.Attribute(nameof(ShowILBytes), ShowILBytes);
			sect.Attribute(nameof(SortMembers), SortMembers);
			sect.Attribute(nameof(ShowPdbInfo), ShowPdbInfo);
			sect.Attribute(nameof(MaxStringLength), MaxStringLength);
			sect.Attribute(nameof(HexadecimalNumbers), HexadecimalNumbers);
		}
	}
}
