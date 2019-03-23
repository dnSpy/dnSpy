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
	class MasmDisassemblySettings : DisassemblySettings, IMasmDisassemblySettings {
		public MasmDisassemblySettings() {
			HexSuffix = "h";
			OctalSuffix = "o";
			BinarySuffix = "b";
		}

		public bool AddDsPrefix32 {
			get => addDsPrefix32;
			set {
				if (value != addDsPrefix32) {
					addDsPrefix32 = value;
					OnPropertyChanged(nameof(AddDsPrefix32));
				}
			}
		}
		bool addDsPrefix32 = true;

		public bool SymbolDisplInBrackets {
			get => symbolDisplInBrackets;
			set {
				if (value != symbolDisplInBrackets) {
					symbolDisplInBrackets = value;
					OnPropertyChanged(nameof(SymbolDisplInBrackets));
				}
			}
		}
		bool symbolDisplInBrackets = true;

		public bool DisplInBrackets {
			get => displInBrackets;
			set {
				if (value != displInBrackets) {
					displInBrackets = value;
					OnPropertyChanged(nameof(DisplInBrackets));
				}
			}
		}
		bool displInBrackets = true;

		public MasmDisassemblySettings Clone() => CopyTo(new MasmDisassemblySettings());

		public MasmDisassemblySettings CopyTo(MasmDisassemblySettings other) {
			if (other == null)
				throw new ArgumentNullException(nameof(other));
			base.CopyTo(other);
			other.AddDsPrefix32 = AddDsPrefix32;
			other.SymbolDisplInBrackets = SymbolDisplInBrackets;
			other.DisplInBrackets = DisplInBrackets;
			return other;
		}
	}

	[Export(typeof(IMasmDisassemblySettings))]
	[Export(typeof(MasmDisassemblySettingsImpl))]
	sealed class MasmDisassemblySettingsImpl : MasmDisassemblySettings {
		static readonly Guid SETTINGS_GUID = new Guid("F70D9AFD-0233-4630-A2A7-0C5A157158FF");

		readonly ISettingsService settingsService;

		[ImportingConstructor]
		MasmDisassemblySettingsImpl(ISettingsService settingsService) {
			this.settingsService = settingsService;

			var sect = settingsService.GetOrCreateSection(SETTINGS_GUID);
			ReadSettings(sect);
			AddDsPrefix32 = sect.Attribute<bool?>(nameof(AddDsPrefix32)) ?? AddDsPrefix32;
			SymbolDisplInBrackets = sect.Attribute<bool?>(nameof(SymbolDisplInBrackets)) ?? SymbolDisplInBrackets;
			DisplInBrackets = sect.Attribute<bool?>(nameof(DisplInBrackets)) ?? DisplInBrackets;

			PropertyChanged += OnPropertyChanged;
		}

		void OnPropertyChanged(object sender, PropertyChangedEventArgs e) => Save();

		void Save() {
			var sect = settingsService.RecreateSection(SETTINGS_GUID);
			WriteSettings(sect);
			sect.Attribute(nameof(AddDsPrefix32), AddDsPrefix32);
			sect.Attribute(nameof(SymbolDisplInBrackets), SymbolDisplInBrackets);
			sect.Attribute(nameof(DisplInBrackets), DisplInBrackets);
		}
	}
}
