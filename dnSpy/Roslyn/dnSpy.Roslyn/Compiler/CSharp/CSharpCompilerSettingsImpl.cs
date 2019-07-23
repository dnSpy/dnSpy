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
using dnSpy.Contracts.Settings;

namespace dnSpy.Roslyn.Compiler.CSharp {
	class CSharpCompilerSettingsBase : CSharpCompilerSettings {
		public override string PreprocessorSymbols {
			get => preprocessorSymbols;
			set {
				if (preprocessorSymbols != value) {
					preprocessorSymbols = value ?? string.Empty;
					OnPropertyChanged(nameof(PreprocessorSymbols));
				}
			}
		}
		string preprocessorSymbols = "TRACE";

		public override bool Optimize {
			get => optimize;
			set {
				if (optimize != value) {
					optimize = value;
					OnPropertyChanged(nameof(Optimize));
				}
			}
		}
		bool optimize = true;

		public override bool CheckOverflow {
			get => checkOverflow;
			set {
				if (checkOverflow != value) {
					checkOverflow = value;
					OnPropertyChanged(nameof(CheckOverflow));
				}
			}
		}
		bool checkOverflow = false;

		public override bool AllowUnsafe {
			get => allowUnsafe;
			set {
				if (allowUnsafe != value) {
					allowUnsafe = value;
					OnPropertyChanged(nameof(AllowUnsafe));
				}
			}
		}
		bool allowUnsafe = true;

		/// <summary>
		/// Clones this instance
		/// </summary>
		/// <returns></returns>
		public CSharpCompilerSettingsBase Clone() => CopyTo(new CSharpCompilerSettingsBase());

		/// <summary>
		/// Copies this to <paramref name="other"/> and returns <paramref name="other"/>
		/// </summary>
		/// <param name="other">Other instance</param>
		/// <returns></returns>
		public CSharpCompilerSettingsBase CopyTo(CSharpCompilerSettingsBase other) {
			if (other is null)
				throw new ArgumentNullException(nameof(other));
			other.PreprocessorSymbols = PreprocessorSymbols;
			other.Optimize = Optimize;
			other.CheckOverflow = CheckOverflow;
			other.AllowUnsafe = AllowUnsafe;
			return other;
		}
	}

	[Export(typeof(CSharpCompilerSettings))]
	[Export(typeof(CSharpCompilerSettingsImpl))]
	sealed class CSharpCompilerSettingsImpl : CSharpCompilerSettingsBase {
		static readonly Guid SETTINGS_GUID = new Guid("F1634589-21AD-42DC-A729-E23CBD7072D2");

		readonly ISettingsService settingsService;

		[ImportingConstructor]
		CSharpCompilerSettingsImpl(ISettingsService settingsService) {
			this.settingsService = settingsService;

			var sect = settingsService.GetOrCreateSection(SETTINGS_GUID);
			PreprocessorSymbols = sect.Attribute<string>(nameof(PreprocessorSymbols)) ?? PreprocessorSymbols;
			Optimize = sect.Attribute<bool?>(nameof(Optimize)) ?? Optimize;
			CheckOverflow = sect.Attribute<bool?>(nameof(CheckOverflow)) ?? CheckOverflow;
			AllowUnsafe = sect.Attribute<bool?>(nameof(AllowUnsafe)) ?? AllowUnsafe;
			PropertyChanged += CSharpCompilerSettingsImpl_PropertyChanged;
		}

		void CSharpCompilerSettingsImpl_PropertyChanged(object? sender, PropertyChangedEventArgs e) {
			var sect = settingsService.RecreateSection(SETTINGS_GUID);
			sect.Attribute(nameof(PreprocessorSymbols), PreprocessorSymbols);
			sect.Attribute(nameof(Optimize), Optimize);
			sect.Attribute(nameof(CheckOverflow), CheckOverflow);
			sect.Attribute(nameof(AllowUnsafe), AllowUnsafe);
		}
	}
}
