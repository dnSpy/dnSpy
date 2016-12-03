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
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Windows.Media;
using dnSpy.Contracts.Controls;
using dnSpy.Contracts.HexEditor;
using dnSpy.Contracts.MVVM;
using dnSpy.Contracts.Settings;

namespace dnSpy.Contracts.Hex {
	interface IHexEditorSettings : INotifyPropertyChanged {
		int BytesGroupCount { get; }
		int BytesPerLine { get; }
		bool UseHexPrefix { get; }
		bool ShowAscii { get; }
		bool LowerCaseHex { get; }
		FontFamily FontFamily { get; }
		double FontSize { get; }
		AsciiEncoding AsciiEncoding { get; }
	}

	class HexEditorSettings : ViewModelBase, IHexEditorSettings {
		public static readonly int MAX_BYTES_PER_LINE = 128;

		protected virtual void OnModified() { }

		public int BytesGroupCount {
			get { return bytesGroupCount; }
			set {
				if (bytesGroupCount != value) {
					bytesGroupCount = value;
					OnPropertyChanged(nameof(BytesGroupCount));
					OnModified();
				}
			}
		}
		int bytesGroupCount = 8;

		public int BytesPerLine {
			get { return bytesPerLine; }
			set {
				if (bytesPerLine != value) {
					bytesPerLine = value;
					OnPropertyChanged(nameof(BytesPerLine));
					OnModified();
				}
			}
		}
		int bytesPerLine = 0;

		public bool UseHexPrefix {
			get { return useHexPrefix; }
			set {
				if (useHexPrefix != value) {
					useHexPrefix = value;
					OnPropertyChanged(nameof(UseHexPrefix));
					OnModified();
				}
			}
		}
		bool useHexPrefix = false;

		public bool ShowAscii {
			get { return showAscii; }
			set {
				if (showAscii != value) {
					showAscii = value;
					OnPropertyChanged(nameof(ShowAscii));
					OnModified();
				}
			}
		}
		bool showAscii = true;

		public bool LowerCaseHex {
			get { return lowerCaseHex; }
			set {
				if (lowerCaseHex != value) {
					lowerCaseHex = value;
					OnPropertyChanged(nameof(LowerCaseHex));
					OnModified();
				}
			}
		}
		bool lowerCaseHex = false;

		public FontFamily FontFamily {
			get { return fontFamily; }
			set {
				if (fontFamily.Source != value.Source) {
					fontFamily = value;
					OnPropertyChanged(nameof(FontFamily));
					OnModified();
				}
			}
		}
		FontFamily fontFamily = new FontFamily(FontUtilities.GetDefaultMonospacedFont());

		public double FontSize {
			get { return fontSize; }
			set {
				if (fontSize != value) {
					fontSize = FontUtilities.FilterFontSize(value);
					OnPropertyChanged(nameof(FontSize));
					OnModified();
				}
			}
		}
		double fontSize = FontUtilities.DEFAULT_FONT_SIZE;

		public AsciiEncoding AsciiEncoding {
			get { return asciiEncoding; }
			set {
				if (asciiEncoding != value) {
					asciiEncoding = value;
					OnPropertyChanged(nameof(AsciiEncoding));
					OnModified();
				}
			}
		}
		AsciiEncoding asciiEncoding = AsciiEncoding.UTF8;

		public HexEditorSettings Clone() => CopyTo(new HexEditorSettings());

		public HexEditorSettings CopyTo(HexEditorSettings other) {
			other.BytesGroupCount = BytesGroupCount;
			other.BytesPerLine = BytesPerLine;
			other.UseHexPrefix = UseHexPrefix;
			other.ShowAscii = ShowAscii;
			other.LowerCaseHex = LowerCaseHex;
			other.FontFamily = FontFamily;
			other.FontSize = FontSize;
			other.AsciiEncoding = AsciiEncoding;
			return other;
		}
	}

	[Export, Export(typeof(IHexEditorSettings))]
	sealed class HexEditorSettingsImpl : HexEditorSettings {
		static readonly Guid SETTINGS_GUID = new Guid("4EFA9642-600F-42AD-9FC0-7B4B9D792225");

		readonly ISettingsService settingsService;

		[ImportingConstructor]
		HexEditorSettingsImpl(ISettingsService settingsService) {
			this.settingsService = settingsService;

			disableSave = true;
			var sect = settingsService.GetOrCreateSection(SETTINGS_GUID);
			BytesGroupCount = sect.Attribute<int?>(nameof(BytesGroupCount)) ?? BytesGroupCount;
			BytesPerLine = sect.Attribute<int?>(nameof(BytesPerLine)) ?? BytesPerLine;
			UseHexPrefix = sect.Attribute<bool?>(nameof(UseHexPrefix)) ?? UseHexPrefix;
			ShowAscii = sect.Attribute<bool?>(nameof(ShowAscii)) ?? ShowAscii;
			LowerCaseHex = sect.Attribute<bool?>(nameof(LowerCaseHex)) ?? LowerCaseHex;
			FontFamily = new FontFamily(sect.Attribute<string>(nameof(FontFamily)) ?? FontUtilities.GetDefaultMonospacedFont());
			FontSize = sect.Attribute<double?>(nameof(FontSize)) ?? FontSize;
			AsciiEncoding = sect.Attribute<AsciiEncoding?>(nameof(AsciiEncoding)) ?? AsciiEncoding;
			disableSave = false;
		}
		readonly bool disableSave;

		protected override void OnModified() {
			if (disableSave)
				return;
			var sect = settingsService.RecreateSection(SETTINGS_GUID);
			sect.Attribute(nameof(BytesGroupCount), BytesGroupCount);
			sect.Attribute(nameof(BytesPerLine), BytesPerLine);
			sect.Attribute(nameof(UseHexPrefix), UseHexPrefix);
			sect.Attribute(nameof(ShowAscii), ShowAscii);
			sect.Attribute(nameof(LowerCaseHex), LowerCaseHex);
			sect.Attribute(nameof(FontFamily), FontFamily.Source);
			sect.Attribute(nameof(FontSize), FontSize);
			sect.Attribute(nameof(AsciiEncoding), AsciiEncoding);
		}
	}
}
