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
using System.ComponentModel;
using System.ComponentModel.Composition;
using dnSpy.Contracts.Disassembly;
using dnSpy.Contracts.Settings;

namespace dnSpy.Disassembly.X86 {
	class GasDisassemblySettings : DisassemblySettings, IX86DisassemblySettings {
		public GasDisassemblySettings() {
			HexPrefix = "0x";
			OctalPrefix = "0";
			BinaryPrefix = "0b";
		}

		public GasDisassemblySettings Clone() => CopyTo(new GasDisassemblySettings());

		public GasDisassemblySettings CopyTo(GasDisassemblySettings other) {
			if (other is null)
				throw new ArgumentNullException(nameof(other));
			base.CopyTo(other);
			return other;
		}
	}

	[Export(typeof(GasDisassemblySettingsImpl))]
	sealed class GasDisassemblySettingsImpl : GasDisassemblySettings {
		static readonly Guid SETTINGS_GUID = new Guid("40476B68-2E8A-4507-8F59-727FE87A04EE");

		readonly ISettingsService settingsService;

		[ImportingConstructor]
		GasDisassemblySettingsImpl(ISettingsService settingsService) {
			this.settingsService = settingsService;

			var sect = settingsService.GetOrCreateSection(SETTINGS_GUID);
			ReadSettings(sect);

			PropertyChanged += OnPropertyChanged;
		}

		void OnPropertyChanged(object? sender, PropertyChangedEventArgs e) => Save();

		void Save() {
			var sect = settingsService.RecreateSection(SETTINGS_GUID);
			WriteSettings(sect);
		}
	}
}
