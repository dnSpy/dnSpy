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
using System.Xml.Linq;
using dnSpy.Options;
using dnSpy.Shared.UI.HexEditor;
using dnSpy.Shared.UI.MVVM;
using ICSharpCode.ILSpy;
using ICSharpCode.ILSpy.Options;

namespace dnSpy.Hex {
	[ExportOptionPage(Title = "Hex Editor", Order = 2)]
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
				s.Load(DNSpySettings.Load());
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

		const string SETTINGS_SECTION_NAME = "HexSettings";
		public override void Load(DNSpySettings settings) {
			var xelem = settings[SETTINGS_SECTION_NAME];
			this.BytesGroupCountVM.Value = (int?)xelem.Attribute("BytesGroupCount") ?? 8;
			this.BytesPerLineVM.Value = (int?)xelem.Attribute("BytesPerLine") ?? 0;
			this.UseHexPrefix = (bool?)xelem.Attribute("UseHexPrefix") ?? false;
			this.ShowAscii = (bool?)xelem.Attribute("ShowAscii") ?? true;
			this.LowerCaseHex = (bool?)xelem.Attribute("LowerCaseHex") ?? false;
			this.FontFamily = new FontFamily(SessionSettings.Unescape((string)xelem.Attribute("FontFamily")) ?? FontUtils.GetDefaultFont());
			this.FontSize = (double?)xelem.Attribute("FontSize") ?? FontUtils.DEFAULT_FONT_SIZE;
			this.AsciiEncoding = (AsciiEncoding)((int?)xelem.Attribute("AsciiEncoding") ?? (int)AsciiEncoding.UTF8);
		}

		public override RefreshFlags Save(XElement root) {
			var xelem = new XElement(SETTINGS_SECTION_NAME);

			xelem.SetAttributeValue("BytesGroupCount", this.BytesGroupCountVM.Value);
			xelem.SetAttributeValue("BytesPerLine", this.BytesPerLineVM.Value);
			xelem.SetAttributeValue("UseHexPrefix", this.UseHexPrefix);
			xelem.SetAttributeValue("ShowAscii", this.ShowAscii);
			xelem.SetAttributeValue("LowerCaseHex", this.LowerCaseHex);
			xelem.SetAttributeValue("FontFamily", SessionSettings.Escape(this.FontFamily.Source));
			xelem.SetAttributeValue("FontSize", this.FontSize);
			xelem.SetAttributeValue("AsciiEncoding", (int)this.AsciiEncoding);

			var currElem = root.Element(SETTINGS_SECTION_NAME);
			if (currElem != null)
				currElem.ReplaceWith(xelem);
			else
				root.Add(xelem);

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
