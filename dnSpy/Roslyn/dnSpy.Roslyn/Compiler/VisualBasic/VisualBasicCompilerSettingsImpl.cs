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

namespace dnSpy.Roslyn.Compiler.VisualBasic {
	class VisualBasicCompilerSettingsBase : VisualBasicCompilerSettings {
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

		public override bool OptionExplicit {
			get => optionExplicit;
			set {
				if (optionExplicit != value) {
					optionExplicit = value;
					OnPropertyChanged(nameof(OptionExplicit));
				}
			}
		}
		bool optionExplicit = true;

		public override bool OptionInfer {
			get => optionInfer;
			set {
				if (optionInfer != value) {
					optionInfer = value;
					OnPropertyChanged(nameof(OptionInfer));
				}
			}
		}
		bool optionInfer = true;

		public override bool OptionStrict {
			get => optionStrict;
			set {
				if (optionStrict != value) {
					optionStrict = value;
					OnPropertyChanged(nameof(OptionStrict));
				}
			}
		}
		bool optionStrict = false;

		public override bool OptionCompareBinary {
			get => optionCompareBinary;
			set {
				if (optionCompareBinary != value) {
					optionCompareBinary = value;
					OnPropertyChanged(nameof(OptionCompareBinary));
				}
			}
		}
		bool optionCompareBinary = true;

		public override bool EmbedVBRuntime {
			get => embedVBRuntime;
			set {
				if (embedVBRuntime != value) {
					embedVBRuntime = value;
					OnPropertyChanged(nameof(EmbedVBRuntime));
				}
			}
		}
		bool embedVBRuntime = false;

		/// <summary>
		/// Clones this instance
		/// </summary>
		/// <returns></returns>
		public VisualBasicCompilerSettingsBase Clone() => CopyTo(new VisualBasicCompilerSettingsBase());

		/// <summary>
		/// Copies this to <paramref name="other"/> and returns <paramref name="other"/>
		/// </summary>
		/// <param name="other">Other instance</param>
		/// <returns></returns>
		public VisualBasicCompilerSettingsBase CopyTo(VisualBasicCompilerSettingsBase other) {
			if (other is null)
				throw new ArgumentNullException(nameof(other));
			other.PreprocessorSymbols = PreprocessorSymbols;
			other.Optimize = Optimize;
			other.OptionExplicit = OptionExplicit;
			other.OptionInfer = OptionInfer;
			other.OptionStrict = OptionStrict;
			other.OptionCompareBinary = OptionCompareBinary;
			other.EmbedVBRuntime = EmbedVBRuntime;
			return other;
		}
	}

	[Export(typeof(VisualBasicCompilerSettings))]
	[Export(typeof(VisualBasicCompilerSettingsImpl))]
	sealed class VisualBasicCompilerSettingsImpl : VisualBasicCompilerSettingsBase {
		static readonly Guid SETTINGS_GUID = new Guid("2366A90D-C708-4A10-81AF-7887976BC3FB");

		readonly ISettingsService settingsService;

		[ImportingConstructor]
		VisualBasicCompilerSettingsImpl(ISettingsService settingsService) {
			this.settingsService = settingsService;

			var sect = settingsService.GetOrCreateSection(SETTINGS_GUID);
			PreprocessorSymbols = sect.Attribute<string>(nameof(PreprocessorSymbols)) ?? PreprocessorSymbols;
			Optimize = sect.Attribute<bool?>(nameof(Optimize)) ?? Optimize;
			OptionExplicit = sect.Attribute<bool?>(nameof(OptionExplicit)) ?? OptionExplicit;
			OptionInfer = sect.Attribute<bool?>(nameof(OptionInfer)) ?? OptionInfer;
			OptionStrict = sect.Attribute<bool?>(nameof(OptionStrict)) ?? OptionStrict;
			OptionCompareBinary = sect.Attribute<bool?>(nameof(OptionCompareBinary)) ?? OptionCompareBinary;
			EmbedVBRuntime = sect.Attribute<bool?>(nameof(EmbedVBRuntime)) ?? EmbedVBRuntime;
			PropertyChanged += VisualBasicCompilerSettingsImpl_PropertyChanged;
		}

		void VisualBasicCompilerSettingsImpl_PropertyChanged(object? sender, PropertyChangedEventArgs e) {
			var sect = settingsService.RecreateSection(SETTINGS_GUID);
			sect.Attribute(nameof(PreprocessorSymbols), PreprocessorSymbols);
			sect.Attribute(nameof(Optimize), Optimize);
			sect.Attribute(nameof(OptionExplicit), OptionExplicit);
			sect.Attribute(nameof(OptionInfer), OptionInfer);
			sect.Attribute(nameof(OptionStrict), OptionStrict);
			sect.Attribute(nameof(OptionCompareBinary), OptionCompareBinary);
			sect.Attribute(nameof(EmbedVBRuntime), EmbedVBRuntime);
		}
	}
}
