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
	class MasmDisassemblySettings : DisassemblySettings, IX86DisassemblySettings {
		public MasmDisassemblySettings() {
			HexSuffix = "h";
			OctalSuffix = "o";
			BinarySuffix = "b";
		}

		public MasmDisassemblySettings Clone() => CopyTo(new MasmDisassemblySettings());

		public MasmDisassemblySettings CopyTo(MasmDisassemblySettings other) {
			if (other is null)
				throw new ArgumentNullException(nameof(other));
			base.CopyTo(other);
			return other;
		}
	}

	[Export(typeof(MasmDisassemblySettingsImpl))]
	sealed class MasmDisassemblySettingsImpl : MasmDisassemblySettings {
		static readonly Guid SETTINGS_GUID = new Guid("F70D9AFD-0233-4630-A2A7-0C5A157158FF");

		readonly ISettingsService settingsService;

		[ImportingConstructor]
		MasmDisassemblySettingsImpl(ISettingsService settingsService) {
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
