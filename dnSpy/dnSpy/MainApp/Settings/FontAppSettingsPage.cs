/*
    Copyright (C) 2014-2017 de4dot@gmail.com

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
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Media;
using dnSpy.Contracts.Controls;
using dnSpy.Contracts.MVVM;
using dnSpy.Contracts.Settings.AppearanceCategory;
using dnSpy.Contracts.Settings.Dialog;
using dnSpy.Contracts.Settings.Fonts;
using dnSpy.Contracts.Settings.FontsAndColors;
using dnSpy.Properties;

namespace dnSpy.MainApp.Settings {
	[Export(typeof(IAppSettingsPageProvider))]
	sealed class FontAppSettingsPageProvider : IAppSettingsPageProvider {
		readonly Lazy<FontAndColorOptionsProvider>[] fontAndColorOptionsProviders;
		readonly FontAppSettingsPageOptions fontAppSettingsPageOptions;

		[ImportingConstructor]
		FontAppSettingsPageProvider([ImportMany] IEnumerable<Lazy<FontAndColorOptionsProvider>> fontAndColorOptionsProviders) {
			this.fontAndColorOptionsProviders = fontAndColorOptionsProviders.ToArray();
			fontAppSettingsPageOptions = new FontAppSettingsPageOptions();
		}

		public IEnumerable<AppSettingsPage> Create() {
			var options = fontAndColorOptionsProviders.SelectMany(a => a.Value.GetFontAndColors()).ToArray();
			if (options.Length != 0)
				yield return new FontAppSettingsPage(options, fontAppSettingsPageOptions);
		}
	}

	sealed class FontAppSettingsPageOptions {
		public int SelectedIndex { get; set; } = -1;
	}

	sealed class FontCollection : ViewModelBase {
		public FontFamilyVM[] FontFamilies {
			get { return fontFamilies; }
			set {
				if (fontFamilies != value) {
					fontFamilies = value;
					OnPropertyChanged(nameof(FontFamilies));
				}
			}
		}
		FontFamilyVM[] fontFamilies;
	}

	sealed class FontAppSettingsPage : AppSettingsPage, INotifyPropertyChanged {
		public override Guid ParentGuid => new Guid(AppSettingsConstants.GUID_ENVIRONMENT);
		public override Guid Guid => new Guid("915F7258-1441-4F80-9DB2-6DF6948C2E09");
		public override double Order => AppSettingsConstants.ORDER_ENVIRONMENT_FONT;
		public override string Title => dnSpy_Resources.FontSettings;
		public override object UIObject => this;

		public event PropertyChangedEventHandler PropertyChanged;
		void OnPropertyChanged(string propName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propName));

		public ObservableCollection<FontAndColorOptionsVM> FontAndColorOptions { get; }
		public FontAndColorOptionsVM SelectedFontAndColorOptions {
			get { return selectedFontAndColorOptions; }
			set {
				if (selectedFontAndColorOptions != value) {
					selectedFontAndColorOptions = value;
					OnPropertyChanged(nameof(SelectedFontAndColorOptions));
				}
			}
		}
		FontAndColorOptionsVM selectedFontAndColorOptions;

		readonly FontAppSettingsPageOptions fontAppSettingsPageOptions;
		readonly FontCollection allFonts;
		readonly FontCollection monospacedFonts;

		public FontAppSettingsPage(FontAndColorOptions[] options, FontAppSettingsPageOptions fontAppSettingsPageOptions) {
			if (options == null)
				throw new ArgumentNullException(nameof(options));
			this.fontAppSettingsPageOptions = fontAppSettingsPageOptions;
			allFonts = new FontCollection();
			monospacedFonts = new FontCollection();
			FontAndColorOptions = new ObservableCollection<FontAndColorOptionsVM>(options.OrderBy(a => a.DisplayName, StringComparer.CurrentCultureIgnoreCase).Select(a => new FontAndColorOptionsVM(a, GetFontCollection(a.FontOption.FontType))));
			SelectedFontAndColorOptions = (uint)fontAppSettingsPageOptions.SelectedIndex < (uint)FontAndColorOptions.Count ? FontAndColorOptions[fontAppSettingsPageOptions.SelectedIndex] : GetBestFontAndColorOptions();

			Task.Factory.StartNew(() =>
				Fonts.SystemFontFamilies.Where(a => !FontUtilities.IsSymbol(a)).OrderBy(a => a.Source, StringComparer.CurrentCultureIgnoreCase).Select(a => new FontFamilyVM(a)).ToArray()
			)
			.ContinueWith(t => {
				var ex = t.Exception;
				if (!t.IsCanceled && !t.IsFaulted) {
					allFonts.FontFamilies = t.Result;
					monospacedFonts.FontFamilies = t.Result.Where(a => FontUtilities.IsMonospacedFont(a.FontFamily)).ToArray();
				}
			}, CancellationToken.None, TaskContinuationOptions.None, TaskScheduler.FromCurrentSynchronizationContext());
		}

		FontAndColorOptionsVM GetBestFontAndColorOptions() =>
			FontAndColorOptions.FirstOrDefault(a => a.Name == AppearanceCategoryConstants.TextEditor) ??
			FontAndColorOptions.FirstOrDefault();

		FontCollection GetFontCollection(FontType fontType) {
			switch (fontType) {
			case FontType.TextEditor:
			case FontType.UI:
				return allFonts;

			case FontType.Monospaced:
			case FontType.HexEditor:
				return monospacedFonts;

			default:
				Debug.Fail($"Unknown font type: {fontType}");
				goto case FontType.UI;
			}
		}

		public override void OnApply() {
			foreach (var options in FontAndColorOptions)
				options.OnApply();
		}

		public override void OnClosed() {
			fontAppSettingsPageOptions.SelectedIndex = FontAndColorOptions.IndexOf(SelectedFontAndColorOptions);
			foreach (var options in FontAndColorOptions)
				options.OnClosed();
		}
	}

	sealed class FontAndColorOptionsVM : ViewModelBase {
		public string DisplayName => options.DisplayName;
		public string Name => options.Name;

		public FontFamilyVM FontFamilyVM {
			get { return fontFamilyVM; }
			set {
				if (fontFamilyVM != value) {
					fontFamilyVM = value;
					FontFamily = fontFamilyVM.FontFamily;
					OnPropertyChanged(nameof(FontFamilyVM));
				}
			}
		}
		FontFamilyVM fontFamilyVM;

		public FontFamily FontFamily {
			get { return options.FontOption.FontFamily; }
			set {
				if (options.FontOption.FontFamily == null || options.FontOption.FontFamily.Source != value.Source) {
					options.FontOption.FontFamily = value;
					OnPropertyChanged(nameof(FontFamily));
				}
			}
		}

		public double FontSize {
			get { return options.FontOption.FontSize; }
			set {
				if (options.FontOption.FontSize != value) {
					options.FontOption.FontSize = FontUtilities.FilterFontSize(value);
					OnPropertyChanged(nameof(FontSize));
				}
			}
		}

		public FontCollection FontCollection { get; }

		readonly FontAndColorOptions options;

		public FontAndColorOptionsVM(FontAndColorOptions options, FontCollection fontCollection) {
			this.options = options ?? throw new ArgumentNullException(nameof(options));
			FontCollection = fontCollection ?? throw new ArgumentNullException(nameof(fontCollection));
			fontFamilyVM = new FontFamilyVM(FontFamily);
		}

		public void OnApply() => options.OnApply();
		public void OnClosed() => options.OnClosed();
	}

	sealed class FontFamilyVM : ViewModelBase {
		public FontFamily FontFamily { get; }
		public bool IsMonospaced { get; }

		public FontFamilyVM(FontFamily ff) {
			FontFamily = ff;
			IsMonospaced = FontUtilities.IsMonospacedFont(ff);
		}

		public override bool Equals(object obj) {
			var other = obj as FontFamilyVM;
			return other != null &&
				FontFamily.Equals(other.FontFamily);
		}

		public override int GetHashCode() => FontFamily.GetHashCode();
	}

	sealed class FontFamilyVMConverter : IValueConverter {
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture) {
			var vm = (FontFamilyVM)value;
			if (!vm.IsMonospaced)
				return new TextBlock { Text = vm.FontFamily.Source };
			var tb = new TextBlock();
			tb.Inlines.Add(new Bold(new Run(vm.FontFamily.Source)));
			return tb;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
	}
}
