/*
    Copyright (C) 2014-2015 de4dot@gmail.com

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
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Threading;
using dnSpy.Contracts;
using dnSpy.Options;
using dnSpy.Shared.UI.HexEditor;
using dnSpy.Shared.UI.MVVM;
using ICSharpCode.ILSpy;
using ICSharpCode.ILSpy.Options;

namespace dnSpy.Hex {
	[ExportOptionPage(Title = "Hex Editor", Order = 3)]
	sealed class HexSettingsCreator : IOptionPageCreator {
		public OptionPage Create() {
			return new HexSettingsVM();
		}
	}

	class HexSettings : OptionPage {
		public static int MAX_BYTES_PER_LINE = 128;

		public static HexSettings Instance {
			get {
				if (settings != null)
					return settings;
				var s = new HexSettings();
				s.Load();
				Interlocked.CompareExchange(ref settings, s, null);
				return settings;
			}
		}
		static HexSettings settings;

		public HexSettings() {
			this.bytesGroupCountVM = new Int32VM(a => { HasErrorUpdated(); OnPropertyChanged("BytesGroupCount"); });
			this.bytesPerLineVM = new Int32VM(a => { HasErrorUpdated(); OnPropertyChanged("BytesPerLine"); }) {
				Min = 0,
				Max = MAX_BYTES_PER_LINE,
			};
		}

		public Int32VM BytesGroupCountVM {
			get { return bytesGroupCountVM; }
		}
		readonly Int32VM bytesGroupCountVM;

		public Int32VM BytesPerLineVM {
			get { return bytesPerLineVM; }
		}
		readonly Int32VM bytesPerLineVM;

		public int BytesGroupCount {
			get { return BytesGroupCountVM.Value; }
			set { BytesGroupCountVM.Value = value; }
		}

		public int BytesPerLine {
			get { return BytesPerLineVM.Value; }
			set { BytesPerLineVM.Value = value; }
		}

		public bool UseHexPrefix {
			get { return useHexPrefix; }
			set {
				if (useHexPrefix != value) {
					useHexPrefix = value;
					OnPropertyChanged("UseHexPrefix");
				}
			}
		}
		bool useHexPrefix;

		public bool ShowAscii {
			get { return showAscii; }
			set {
				if (showAscii != value) {
					showAscii = value;
					OnPropertyChanged("ShowAscii");
				}
			}
		}
		bool showAscii;

		public bool LowerCaseHex {
			get { return lowerCaseHex; }
			set {
				if (lowerCaseHex != value) {
					lowerCaseHex = value;
					OnPropertyChanged("LowerCaseHex");
				}
			}
		}
		bool lowerCaseHex;

		public FontFamily FontFamily {
			get { return fontFamily; }
			set {
				if (fontFamily != value) {
					fontFamily = value;
					OnPropertyChanged("FontFamily");
				}
			}
		}
		FontFamily fontFamily;

		public double FontSize {
			get { return fontSize; }
			set {
				if (fontSize != value) {
					fontSize = FontUtils.FilterFontSize(value);
					OnPropertyChanged("FontSize");
				}
			}
		}
		double fontSize;

		public virtual AsciiEncoding AsciiEncoding {
			get { return asciiEncoding; }
			set {
				if (asciiEncoding != value) {
					asciiEncoding = value;
					OnPropertyChanged("AsciiEncoding");
				}
			}
		}
		AsciiEncoding asciiEncoding;

		const string SETTINGS_NAME = "4EFA9642-600F-42AD-9FC0-7B4B9D792225";
		public override void Load() {
			var section = DnSpy.App.SettingsManager.GetOrCreateSection(SETTINGS_NAME);
			this.BytesGroupCountVM.Value = section.Attribute<int?>("BytesGroupCount") ?? 8;
			this.BytesPerLineVM.Value = section.Attribute<int?>("BytesPerLine") ?? 0;
			this.UseHexPrefix = section.Attribute<bool?>("UseHexPrefix") ?? false;
			this.ShowAscii = section.Attribute<bool?>("ShowAscii") ?? true;
			this.LowerCaseHex = section.Attribute<bool?>("LowerCaseHex") ?? false;
			this.FontFamily = new FontFamily(section.Attribute<string>("FontFamily") ?? FontUtils.GetDefaultFont());
			this.FontSize = section.Attribute<double?>("FontSize") ?? FontUtils.DEFAULT_FONT_SIZE;
			this.AsciiEncoding = section.Attribute<AsciiEncoding?>("AsciiEncoding") ?? AsciiEncoding.UTF8;
		}

		public override RefreshFlags Save() {
			var section = DnSpy.App.SettingsManager.CreateSection(SETTINGS_NAME);

			section.Attribute("BytesGroupCount", this.BytesGroupCountVM.Value);
			section.Attribute("BytesPerLine", this.BytesPerLineVM.Value);
			section.Attribute("UseHexPrefix", this.UseHexPrefix);
			section.Attribute("ShowAscii", this.ShowAscii);
			section.Attribute("LowerCaseHex", this.LowerCaseHex);
			section.Attribute("FontFamily", this.FontFamily.Source);
			section.Attribute("FontSize", this.FontSize);
			section.Attribute("AsciiEncoding", this.AsciiEncoding);

			WriteTo(Instance);

			return RefreshFlags.None;
		}

		void WriteTo(HexSettings other) {
			other.BytesGroupCountVM.Value = this.BytesGroupCountVM.Value;
			other.BytesPerLineVM.Value = this.BytesPerLineVM.Value;
			other.UseHexPrefix = this.UseHexPrefix;
			other.ShowAscii = this.ShowAscii;
			other.LowerCaseHex = this.LowerCaseHex;
			other.FontFamily = this.FontFamily;
			other.FontSize = this.FontSize;
			other.AsciiEncoding = this.AsciiEncoding;
		}

		public override bool HasError {
			get {
				return BytesGroupCountVM.HasError ||
					BytesPerLineVM.HasError;
			}
		}
	}

	sealed class HexSettingsVM : HexSettings {
		public FontFamily[] Fonts {
			get { return fonts; }
			set {
				if (fonts != value) {
					fonts = value;
					OnPropertyChanged("Fonts");
				}
			}
		}
		FontFamily[] fonts;

		public EnumListVM AsciiEncodingVM {
			get { return asciiEncodingVM; }
		}
		readonly EnumListVM asciiEncodingVM;
		readonly EnumVM[] asciiEncodingList = new EnumVM[] {
			new EnumVM(AsciiEncoding.ASCII, "ASCII"),
			new EnumVM(AsciiEncoding.ANSI, "ANSI"),
			new EnumVM(AsciiEncoding.UTF7, "UTF-7"),
			new EnumVM(AsciiEncoding.UTF8, "UTF-8"),
			new EnumVM(AsciiEncoding.UTF32, "UTF-32"),
			new EnumVM(AsciiEncoding.Unicode, "Unicode"),
			new EnumVM(AsciiEncoding.BigEndianUnicode, "BE Unicode"),
		};

		public override AsciiEncoding AsciiEncoding {
			get { return (AsciiEncoding)AsciiEncodingVM.SelectedItem; }
			set { AsciiEncodingVM.SelectedItem = value; }
		}

		public HexSettingsVM() {
			var task = new Task<FontFamily[]>(FontUtils.GetMonospacedFonts);
			task.Start();
			task.ContinueWith(continuation => {
				App.Current.Dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() => {
					this.Fonts = task.Result;
					if (continuation.Exception != null) {
						foreach (var ex in continuation.Exception.InnerExceptions)
							MainWindow.Instance.ShowMessageBox(ex.ToString());
					}
				}));
			});
			this.asciiEncodingVM = new EnumListVM(asciiEncodingList);
		}
	}
}
