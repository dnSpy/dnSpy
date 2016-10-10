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
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using dnSpy.Contracts.BackgroundImage;
using dnSpy.Contracts.Images;
using dnSpy.Contracts.MVVM;
using dnSpy.Contracts.Settings.Dialog;
using dnSpy.Properties;

namespace dnSpy.BackgroundImage.Dialog {
	[Export(typeof(IAppSettingsPageProvider))]
	sealed class AppSettingsPageProvider : IAppSettingsPageProvider {
		readonly IBackgroundImageSettingsService backgroundImageSettingsService;
		readonly IPickFilename pickFilename;
		readonly IPickDirectory pickDirectory;

		[ImportingConstructor]
		AppSettingsPageProvider(IBackgroundImageSettingsService backgroundImageSettingsService, IPickFilename pickFilename, IPickDirectory pickDirectory) {
			this.backgroundImageSettingsService = backgroundImageSettingsService;
			this.pickFilename = pickFilename;
			this.pickDirectory = pickDirectory;
		}

		public IEnumerable<IAppSettingsPage> Create() {
			var rawSettings = backgroundImageSettingsService.GetRawSettings();
			if (rawSettings.Length != 0)
				yield return new AppSettingsPage(backgroundImageSettingsService, pickFilename, pickDirectory, rawSettings);
		}
	}

	sealed class AppSettingsPage : ViewModelBase, IAppSettingsPage {
		public Guid ParentGuid => Guid.Empty;
		public Guid Guid => new Guid("A36F0A79-E8D0-44C5-8F22-A50B28F6117E");
		public double Order => AppSettingsConstants.ORDER_BACKGROUNDIMAGE;
		public string Title => dnSpy_Resources.BackgroundImageOptDlgTab;
		public ImageReference Icon => ImageReference.None;
		public object UIObject => this;

		public ICommand ResetCommand => new RelayCommand(a => ResetSettings(), a => CanResetSettings);
		public ICommand PickFilenamesCommand => new RelayCommand(a => PickFilenames(), a => CanPickFilenames);
		public ICommand PickDirectoryCommand => new RelayCommand(a => PickDirectory(), a => CanPickDirectory);

		public Settings CurrentItem {
			get { return currentItem; }
			set {
				if (currentItem != value) {
					currentItem = value;
					OnPropertyChanged(nameof(CurrentItem));
					OnPropertyChanged(nameof(Images));
					OnPropertyChanged(nameof(IsRandom));
					OnPropertyChanged(nameof(IsEnabled));
					OpacityVM.Value = currentItem.RawSettings.Opacity;
					HorizontalOffsetVM.Value = currentItem.RawSettings.HorizontalOffset;
					VerticalOffsetVM.Value = currentItem.RawSettings.VerticalOffset;
					LeftMarginWidthPercentVM.Value = currentItem.RawSettings.LeftMarginWidthPercent;
					RightMarginWidthPercentVM.Value = currentItem.RawSettings.RightMarginWidthPercent;
					TopMarginHeightPercentVM.Value = currentItem.RawSettings.TopMarginHeightPercent;
					BottomMarginHeightPercentVM.Value = currentItem.RawSettings.BottomMarginHeightPercent;
					MaxHeightVM.Value = currentItem.RawSettings.MaxHeight;
					MaxWidthVM.Value = currentItem.RawSettings.MaxWidth;
					ZoomVM.Value = currentItem.RawSettings.Zoom;
					IntervalVM.Value = currentItem.RawSettings.Interval;
					StretchVM.SelectedItem = currentItem.RawSettings.Stretch;
					StretchDirectionVM.SelectedItem = currentItem.RawSettings.StretchDirection;
					ImagePlacementVM.SelectedItem = currentItem.RawSettings.ImagePlacement;
				}
			}
		}
		Settings currentItem;

		public string Images {
			get { return currentItem.Images; }
			set {
				if (currentItem.Images != value) {
					currentItem.Images = value;
					OnPropertyChanged(nameof(Images));
				}
			}
		}

		public bool IsRandom {
			get { return currentItem.RawSettings.IsRandom; }
			set {
				if (currentItem.RawSettings.IsRandom != value) {
					currentItem.RawSettings.IsRandom = value;
					OnPropertyChanged(nameof(IsRandom));
				}
			}
		}

		public bool IsEnabled {
			get { return currentItem.RawSettings.IsEnabled; }
			set {
				if (currentItem.RawSettings.IsEnabled != value) {
					currentItem.RawSettings.IsEnabled = value;
					OnPropertyChanged(nameof(IsEnabled));
				}
			}
		}

		public DoubleVM OpacityVM => opacityVM;
		readonly DoubleVM opacityVM;

		public DoubleVM HorizontalOffsetVM => horizontalOffsetVM;
		readonly DoubleVM horizontalOffsetVM;

		public DoubleVM VerticalOffsetVM => verticalOffsetVM;
		readonly DoubleVM verticalOffsetVM;

		public DoubleVM LeftMarginWidthPercentVM => leftMarginWidthPercentVM;
		readonly DoubleVM leftMarginWidthPercentVM;

		public DoubleVM RightMarginWidthPercentVM => rightMarginWidthPercentVM;
		readonly DoubleVM rightMarginWidthPercentVM;

		public DoubleVM TopMarginHeightPercentVM => topMarginHeightPercentVM;
		readonly DoubleVM topMarginHeightPercentVM;

		public DoubleVM BottomMarginHeightPercentVM => bottomMarginHeightPercentVM;
		readonly DoubleVM bottomMarginHeightPercentVM;

		public DoubleVM MaxHeightVM => maxHeightVM;
		readonly DoubleVM maxHeightVM;

		public DoubleVM MaxWidthVM => maxWidthVM;
		readonly DoubleVM maxWidthVM;

		public DoubleVM ZoomVM => zoomVM;
		readonly DoubleVM zoomVM;

		public DefaultConverterVM<TimeSpan> IntervalVM => intervalVM;
		readonly DefaultConverterVM<TimeSpan> intervalVM;

		public EnumListVM StretchVM => stretchVM;
		readonly EnumListVM stretchVM;

		public EnumListVM StretchDirectionVM => stretchDirectionVM;
		readonly EnumListVM stretchDirectionVM;
		static readonly EnumVM[] stretchDirectionList = new EnumVM[] {
			new EnumVM(StretchDirection.Both, dnSpy_Resources.StretchDirection_Both),
			new EnumVM(StretchDirection.UpOnly, dnSpy_Resources.StretchDirection_UpOnly),
			new EnumVM(StretchDirection.DownOnly, dnSpy_Resources.StretchDirection_DownOnly),
		};

		public EnumListVM ImagePlacementVM => imagePlacementVM;
		readonly EnumListVM imagePlacementVM;
		static readonly EnumVM[] imagePlacementList = new EnumVM[] {
			new EnumVM(ImagePlacement.TopLeft, dnSpy_Resources.ImagePlacement_TopLeft),
			new EnumVM(ImagePlacement.TopRight, dnSpy_Resources.ImagePlacement_TopRight),
			new EnumVM(ImagePlacement.BottomLeft, dnSpy_Resources.ImagePlacement_BottomLeft),
			new EnumVM(ImagePlacement.BottomRight, dnSpy_Resources.ImagePlacement_BottomRight),
			new EnumVM(ImagePlacement.Top, dnSpy_Resources.ImagePlacement_Top),
			new EnumVM(ImagePlacement.Left, dnSpy_Resources.ImagePlacement_Left),
			new EnumVM(ImagePlacement.Right, dnSpy_Resources.ImagePlacement_Right),
			new EnumVM(ImagePlacement.Bottom, dnSpy_Resources.ImagePlacement_Bottom),
			new EnumVM(ImagePlacement.Center, dnSpy_Resources.ImagePlacement_Center),
		};

		public ObservableCollection<Settings> Settings { get; }

		readonly IBackgroundImageSettingsService backgroundImageSettingsService;
		readonly IPickFilename pickFilename;
		readonly IPickDirectory pickDirectory;

		public AppSettingsPage(IBackgroundImageSettingsService backgroundImageSettingsService, IPickFilename pickFilename, IPickDirectory pickDirectory, ImageSettingsInfo[] settings) {
			if (backgroundImageSettingsService == null)
				throw new ArgumentNullException(nameof(backgroundImageSettingsService));
			if (pickFilename == null)
				throw new ArgumentNullException(nameof(pickFilename));
			if (pickDirectory == null)
				throw new ArgumentNullException(nameof(pickDirectory));
			if (settings == null)
				throw new ArgumentNullException(nameof(settings));
			if (settings.Length == 0)
				throw new ArgumentException();
			Debug.Assert(settings.All(a => a.Lazy.Value.UserVisible));
			this.backgroundImageSettingsService = backgroundImageSettingsService;
			this.pickFilename = pickFilename;
			this.pickDirectory = pickDirectory;
			this.Settings = new ObservableCollection<Settings>(settings.OrderBy(a => a.Lazy.Value.UIOrder).Select(a => new Settings(a)));
			this.stretchVM = new EnumListVM(EnumVM.Create(false, typeof(Stretch)), (a, b) => currentItem.RawSettings.Stretch = (Stretch)stretchVM.SelectedItem);
			this.stretchDirectionVM = new EnumListVM(stretchDirectionList, (a, b) => currentItem.RawSettings.StretchDirection = (StretchDirection)stretchDirectionVM.SelectedItem);
			this.imagePlacementVM = new EnumListVM(imagePlacementList, (a, b) => currentItem.RawSettings.ImagePlacement = (ImagePlacement)imagePlacementVM.SelectedItem);
			this.opacityVM = new DoubleVM(a => { if (!opacityVM.HasError) currentItem.RawSettings.Opacity = FilterOpacity(opacityVM.Value); });
			this.horizontalOffsetVM = new DoubleVM(a => { if (!horizontalOffsetVM.HasError) currentItem.RawSettings.HorizontalOffset = FilterOffset(horizontalOffsetVM.Value); });
			this.verticalOffsetVM = new DoubleVM(a => { if (!verticalOffsetVM.HasError) currentItem.RawSettings.VerticalOffset = FilterOffset(verticalOffsetVM.Value); });
			this.leftMarginWidthPercentVM = new DoubleVM(a => { if (!leftMarginWidthPercentVM.HasError) currentItem.RawSettings.LeftMarginWidthPercent = FilterMarginPercent(leftMarginWidthPercentVM.Value); });
			this.rightMarginWidthPercentVM = new DoubleVM(a => { if (!rightMarginWidthPercentVM.HasError) currentItem.RawSettings.RightMarginWidthPercent = FilterMarginPercent(rightMarginWidthPercentVM.Value); });
			this.topMarginHeightPercentVM = new DoubleVM(a => { if (!topMarginHeightPercentVM.HasError) currentItem.RawSettings.TopMarginHeightPercent = FilterMarginPercent(topMarginHeightPercentVM.Value); });
			this.bottomMarginHeightPercentVM = new DoubleVM(a => { if (!bottomMarginHeightPercentVM.HasError) currentItem.RawSettings.BottomMarginHeightPercent = FilterMarginPercent(bottomMarginHeightPercentVM.Value); });
			this.maxHeightVM = new DoubleVM(a => { if (!maxHeightVM.HasError) currentItem.RawSettings.MaxHeight = FilterLength(maxHeightVM.Value); });
			this.maxWidthVM = new DoubleVM(a => { if (!maxWidthVM.HasError) currentItem.RawSettings.MaxWidth = FilterLength(maxWidthVM.Value); });
			this.zoomVM = new DoubleVM(a => { if (!zoomVM.HasError) currentItem.RawSettings.Zoom = FilterZoom(zoomVM.Value); });
			this.intervalVM = new DefaultConverterVM<TimeSpan>(a => { if (!intervalVM.HasError) currentItem.RawSettings.Interval = intervalVM.Value; });
			CurrentItem = this.Settings.FirstOrDefault(a => a.Id == backgroundImageSettingsService.LastSelectedId) ?? this.Settings[0];
		}

		static double FilterMarginPercent(double value) {
			if (double.IsNaN(value))
				return 100;
			if (value < 0)
				return 0;
			if (value > 100)
				return 100;
			return value;
		}

		static double FilterZoom(double value) {
			if (double.IsNaN(value))
				return 1;
			if (value < 0)
				return 1;
			return value;
		}

		static double FilterLength(double value) {
			if (double.IsNaN(value))
				return 0;
			if (value < 0)
				return 0;
			return value;
		}

		static double FilterOffset(double value) {
			if (double.IsNaN(value))
				return 0;
			return value;
		}

		static double FilterOpacity(double value) {
			if (double.IsNaN(value))
				return 1;
			if (value < 0)
				return 0;
			if (value > 1)
				return 1;
			return value;
		}

		bool CanResetSettings => true;

		void ResetSettings() {
			IsRandom = DefaultRawSettings.IsRandom;
			IsEnabled = DefaultRawSettings.IsEnabled;
			OpacityVM.Value = DefaultRawSettings.Opacity;
			HorizontalOffsetVM.Value = DefaultRawSettings.HorizontalOffset;
			VerticalOffsetVM.Value = DefaultRawSettings.VerticalOffset;
			LeftMarginWidthPercentVM.Value = DefaultRawSettings.LeftMarginWidthPercent;
			RightMarginWidthPercentVM.Value = DefaultRawSettings.RightMarginWidthPercent;
			TopMarginHeightPercentVM.Value = DefaultRawSettings.TopMarginHeightPercent;
			BottomMarginHeightPercentVM.Value = DefaultRawSettings.BottomMarginHeightPercent;
			MaxHeightVM.Value = DefaultRawSettings.MaxHeight;
			MaxWidthVM.Value = DefaultRawSettings.MaxWidth;
			ZoomVM.Value = DefaultRawSettings.Zoom;
			IntervalVM.Value = DefaultRawSettings.Interval;
			StretchVM.SelectedItem = DefaultRawSettings.DefaultStretch;
			StretchDirectionVM.SelectedItem = DefaultRawSettings.DefaultStretchDirection;
			ImagePlacementVM.SelectedItem = DefaultRawSettings.DefaultImagePlacement;
		}

		static readonly string ImagesFilter = $"{dnSpy_Resources.Files_Images}|*.png;*.gif;*.bmp;*.jpg;*.jpeg|{dnSpy_Resources.AllFiles} (*.*)|*.*";
		bool CanPickFilenames => IsEnabled;
		void PickFilenames() => AddToImages(pickFilename.GetFilenames(null, null, ImagesFilter));

		bool CanPickDirectory => IsEnabled;
		void PickDirectory() => AddToImages(new[] { pickDirectory.GetDirectory(GetLastDirectory()) });

		string GetLastDirectory() {
			foreach (var t in Images.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries).Reverse()) {
				var f = t.Trim();
				if (Directory.Exists(f))
					return f;
				if (File.Exists(f)) {
					try {
						return Path.GetDirectoryName(f);
					}
					catch {
					}
				}
			}
			return null;
		}

		void AddToImages(string[] filenames) {
			var images = Images;
			foreach (var name in filenames) {
				if (string.IsNullOrWhiteSpace(name))
					return;
				if (images.Length != 0 && !images.EndsWith(Environment.NewLine))
					images += Environment.NewLine;
				images += name + Environment.NewLine;
			}
			Images = images;
		}

		public void OnClosed(bool saveSettings, IAppRefreshSettings appRefreshSettings) {
			backgroundImageSettingsService.LastSelectedId = currentItem.Id;
			if (!saveSettings)
				return;
			backgroundImageSettingsService.SetRawSettings(Settings.Select(a => a.GetUpdatedRawSettings()).ToArray());
		}
	}

	sealed class Settings {
		public RawSettings RawSettings { get; }

		public string Id { get; }
		public string Name { get; }
		public string Images { get; set; }

		public Settings(ImageSettingsInfo info) {
			this.RawSettings = info.RawSettings;
			this.Id = info.Lazy.Value.Id;
			this.Name = info.Lazy.Value.DisplayName;
			this.Images = string.Join(Environment.NewLine, RawSettings.Images);
			if (Images.Length != 0)
				Images += Environment.NewLine;
		}

		public RawSettings GetUpdatedRawSettings() {
			RawSettings.Images = Images.Split(newLineChars).Select(a => a.Trim()).Where(a => !string.IsNullOrEmpty(a)).ToArray();
			return RawSettings;
		}
		static readonly char[] newLineChars = new char[] { '\r', '\n', '\u0085', '\u2028', '\u2029' };
	}
}
