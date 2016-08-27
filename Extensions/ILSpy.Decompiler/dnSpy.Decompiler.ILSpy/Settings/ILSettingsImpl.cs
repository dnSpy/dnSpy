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
using System.ComponentModel.Composition;
using dnSpy.Contracts.Settings;
using dnSpy.Decompiler.ILSpy.Core.Settings;

namespace dnSpy.Decompiler.ILSpy.Settings {
	[Export]
	sealed class ILSettingsImpl : ILSettings {
		static readonly Guid SETTINGS_GUID = new Guid("DD6752B1-5336-4601-A9B2-0879E18AE9F3");

		readonly ISettingsManager settingsManager;

		[ImportingConstructor]
		ILSettingsImpl(ISettingsManager settingsManager) {
			this.settingsManager = settingsManager;

			this.disableSave = true;
			var sect = settingsManager.GetOrCreateSection(SETTINGS_GUID);
			this.ShowILComments = sect.Attribute<bool?>(nameof(ShowILComments)) ?? this.ShowILComments;
			this.ShowXmlDocumentation = sect.Attribute<bool?>(nameof(ShowXmlDocumentation)) ?? this.ShowXmlDocumentation;
			this.ShowTokenAndRvaComments = sect.Attribute<bool?>(nameof(ShowTokenAndRvaComments)) ?? this.ShowTokenAndRvaComments;
			this.ShowILBytes = sect.Attribute<bool?>(nameof(ShowILBytes)) ?? this.ShowILBytes;
			this.SortMembers = sect.Attribute<bool?>(nameof(SortMembers)) ?? this.SortMembers;
			this.disableSave = false;
		}
		readonly bool disableSave;

		protected override void OnModified() {
			if (disableSave)
				return;

			var sect = settingsManager.RecreateSection(SETTINGS_GUID);
			sect.Attribute(nameof(ShowILComments), ShowILComments);
			sect.Attribute(nameof(ShowXmlDocumentation), ShowXmlDocumentation);
			sect.Attribute(nameof(ShowTokenAndRvaComments), ShowTokenAndRvaComments);
			sect.Attribute(nameof(ShowILBytes), ShowILBytes);
			sect.Attribute(nameof(SortMembers), SortMembers);
		}
	}
}
