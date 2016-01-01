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
using dnSpy.Contracts.Settings;
using dnSpy.Shared.UI.Controls;
using dnSpy.Shared.UI.HexEditor;
using dnSpy.Shared.UI.MVVM;

namespace dnSpy.Shared.UI.Hex {
	public interface IHexEditorSettings : INotifyPropertyChanged {
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

		protected virtual void OnModified() {
		}

		public int BytesGroupCount {
			get { return bytesGroupCount; }
			set {
				if (bytesGroupCount != value) {
					bytesGroupCount = value;
					OnPropertyChanged("BytesGroupCount");
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
					OnPropertyChanged("BytesPerLine");
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
					OnPropertyChanged("UseHexPrefix");
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
					OnPropertyChanged("ShowAscii");
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
					OnPropertyChanged("LowerCaseHex");
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
					OnPropertyChanged("FontFamily");
					OnModified();
				}
			}
		}
		FontFamily fontFamily = new FontFamily(FontUtils.GetDefaultMonospacedFont());

		public double FontSize {
			get { return fontSize; }
			set {
				if (fontSize != value) {
					fontSize = FontUtils.FilterFontSize(value);
					OnPropertyChanged("FontSize");
					OnModified();
				}
			}
		}
		double fontSize = FontUtils.DEFAULT_FONT_SIZE;

		public AsciiEncoding AsciiEncoding {
			get { return asciiEncoding; }
			set {
				if (asciiEncoding != value) {
					asciiEncoding = value;
					OnPropertyChanged("AsciiEncoding");
					OnModified();
				}
			}
		}
		AsciiEncoding asciiEncoding = AsciiEncoding.UTF8;

		public HexEditorSettings Clone() {
			return CopyTo(new HexEditorSettings());
		}

		public HexEditorSettings CopyTo(HexEditorSettings other) {
			other.BytesGroupCount = this.BytesGroupCount;
			other.BytesPerLine = this.BytesPerLine;
			other.UseHexPrefix = this.UseHexPrefix;
			other.ShowAscii = this.ShowAscii;
			other.LowerCaseHex = this.LowerCaseHex;
			other.FontFamily = this.FontFamily;
			other.FontSize = this.FontSize;
			other.AsciiEncoding = this.AsciiEncoding;
			return other;
		}
	}

	[Export, Export(typeof(IHexEditorSettings)), PartCreationPolicy(CreationPolicy.Shared)]
	sealed class HexEditorSettingsImpl : HexEditorSettings {
		static readonly Guid SETTINGS_GUID = new Guid("4EFA9642-600F-42AD-9FC0-7B4B9D792225");

		readonly ISettingsManager settingsManager;

		[ImportingConstructor]
		HexEditorSettingsImpl(ISettingsManager settingsManager) {
			this.settingsManager = settingsManager;

			this.disableSave = true;
			var sect = settingsManager.GetOrCreateSection(SETTINGS_GUID);
			BytesGroupCount = sect.Attribute<int?>("BytesGroupCount") ?? BytesGroupCount;
			BytesPerLine = sect.Attribute<int?>("BytesPerLine") ?? BytesPerLine;
			UseHexPrefix = sect.Attribute<bool?>("UseHexPrefix") ?? UseHexPrefix;
			ShowAscii = sect.Attribute<bool?>("ShowAscii") ?? ShowAscii;
			LowerCaseHex = sect.Attribute<bool?>("LowerCaseHex") ?? LowerCaseHex;
			FontFamily = new FontFamily(sect.Attribute<string>("FontFamily") ?? FontUtils.GetDefaultMonospacedFont());
			FontSize = sect.Attribute<double?>("FontSize") ?? FontSize;
			AsciiEncoding = sect.Attribute<AsciiEncoding?>("AsciiEncoding") ?? AsciiEncoding;
			this.disableSave = false;
		}
		readonly bool disableSave;

		protected override void OnModified() {
			if (disableSave)
				return;
			var sect = settingsManager.RecreateSection(SETTINGS_GUID);
			sect.Attribute("BytesGroupCount", this.BytesGroupCount);
			sect.Attribute("BytesPerLine", this.BytesPerLine);
			sect.Attribute("UseHexPrefix", this.UseHexPrefix);
			sect.Attribute("ShowAscii", this.ShowAscii);
			sect.Attribute("LowerCaseHex", this.LowerCaseHex);
			sect.Attribute("FontFamily", this.FontFamily.Source);
			sect.Attribute("FontSize", this.FontSize);
			sect.Attribute("AsciiEncoding", this.AsciiEncoding);
		}
	}
}
