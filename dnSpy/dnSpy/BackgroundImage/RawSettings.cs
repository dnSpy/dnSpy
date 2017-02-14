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
using System.Linq;
using System.Windows.Controls;
using System.Windows.Media;
using dnSpy.Contracts.BackgroundImage;
using dnSpy.Contracts.Settings;

namespace dnSpy.BackgroundImage {
	sealed class RawSettings : IEquatable<RawSettings> {
		public bool IsValid => Id != null;
		public string Id { get; private set; }
		public Stretch Stretch { get; set; }
		public StretchDirection StretchDirection { get; set; }
		public double Opacity { get; set; }
		public double HorizontalOffset { get; set; }
		public double VerticalOffset { get; set; }
		public double LeftMarginWidthPercent { get; set; }
		public double RightMarginWidthPercent { get; set; }
		public double TopMarginHeightPercent { get; set; }
		public double BottomMarginHeightPercent { get; set; }
		public double MaxHeight { get; set; }
		public double MaxWidth { get; set; }
		public double Zoom { get; set; }
		public ImagePlacement ImagePlacement { get; set; }
		public bool IsRandom { get; set; }
		public bool IsEnabled { get; set; }
		public TimeSpan Interval { get; set; }

		public string[] Images {
			get { return images; }
			set {
				images = value ?? throw new ArgumentNullException(nameof(value));
			}
		}
		string[] images;

		RawSettings() {
			Id = null;
			Images = Array.Empty<string>();
			Stretch = DefaultRawSettings.DefaultStretch;
			StretchDirection = DefaultRawSettings.DefaultStretchDirection;
			Opacity = DefaultRawSettings.Opacity;
			HorizontalOffset = DefaultRawSettings.HorizontalOffset;
			VerticalOffset = DefaultRawSettings.VerticalOffset;
			LeftMarginWidthPercent = DefaultRawSettings.LeftMarginWidthPercent;
			RightMarginWidthPercent = DefaultRawSettings.RightMarginWidthPercent;
			TopMarginHeightPercent = DefaultRawSettings.TopMarginHeightPercent;
			BottomMarginHeightPercent = DefaultRawSettings.BottomMarginHeightPercent;
			MaxHeight = DefaultRawSettings.MaxHeight;
			MaxWidth = DefaultRawSettings.MaxWidth;
			Zoom = DefaultRawSettings.Zoom;
			ImagePlacement = DefaultRawSettings.DefaultImagePlacement;
			IsRandom = DefaultRawSettings.IsRandom;
			IsEnabled = DefaultRawSettings.IsEnabled;
			Interval = DefaultRawSettings.Interval;
		}

		public RawSettings(RawSettings other) {
			Id = other.Id;
			CopyFrom(other);
		}

		public RawSettings(string id, DefaultImageSettings defaultSettings) {
			Id = id ?? throw new ArgumentNullException(nameof(id));
			Images = defaultSettings.Images ?? Array.Empty<string>();
			Stretch = defaultSettings.Stretch ?? DefaultRawSettings.DefaultStretch;
			StretchDirection = defaultSettings.StretchDirection ?? DefaultRawSettings.DefaultStretchDirection;
			Opacity = defaultSettings.Opacity ?? DefaultRawSettings.Opacity;
			HorizontalOffset = defaultSettings.HorizontalOffset ?? DefaultRawSettings.HorizontalOffset;
			VerticalOffset = defaultSettings.VerticalOffset ?? DefaultRawSettings.VerticalOffset;
			LeftMarginWidthPercent = defaultSettings.LeftMarginWidthPercent ?? DefaultRawSettings.LeftMarginWidthPercent;
			RightMarginWidthPercent = defaultSettings.RightMarginWidthPercent ?? DefaultRawSettings.RightMarginWidthPercent;
			TopMarginHeightPercent = defaultSettings.TopMarginHeightPercent ?? DefaultRawSettings.TopMarginHeightPercent;
			BottomMarginHeightPercent = defaultSettings.BottomMarginHeightPercent ?? DefaultRawSettings.BottomMarginHeightPercent;
			MaxHeight = defaultSettings.MaxHeight ?? DefaultRawSettings.MaxHeight;
			MaxWidth = defaultSettings.MaxWidth ?? DefaultRawSettings.MaxWidth;
			Zoom = defaultSettings.Zoom ?? DefaultRawSettings.Zoom;
			ImagePlacement = defaultSettings.ImagePlacement ?? DefaultRawSettings.DefaultImagePlacement;
			IsRandom = defaultSettings.IsRandom ?? DefaultRawSettings.IsRandom;
			IsEnabled = defaultSettings.IsEnabled ?? DefaultRawSettings.IsEnabled;
			Interval = defaultSettings.Interval ?? DefaultRawSettings.Interval;
		}

		public RawSettings(string id)
			: this() => Id = id ?? throw new ArgumentNullException(nameof(id));

		public RawSettings(ISettingsSection section) => ReadSettings(section);

		void ReadSettings(ISettingsSection section) {
			Id = section.Attribute<string>(nameof(Id));
			Images = FilterOutImages(DeserializeImages(section.Attribute<string>(nameof(Images))) ?? Array.Empty<string>()).ToArray();
			Stretch = section.Attribute<Stretch?>(nameof(Stretch)) ?? DefaultRawSettings.DefaultStretch;
			StretchDirection = section.Attribute<StretchDirection?>(nameof(StretchDirection)) ?? DefaultRawSettings.DefaultStretchDirection;
			Opacity = section.Attribute<double?>(nameof(Opacity)) ?? DefaultRawSettings.Opacity;
			HorizontalOffset = section.Attribute<double?>(nameof(HorizontalOffset)) ?? DefaultRawSettings.HorizontalOffset;
			VerticalOffset = section.Attribute<double?>(nameof(VerticalOffset)) ?? DefaultRawSettings.VerticalOffset;
			LeftMarginWidthPercent = section.Attribute<double?>(nameof(LeftMarginWidthPercent)) ?? DefaultRawSettings.LeftMarginWidthPercent;
			RightMarginWidthPercent = section.Attribute<double?>(nameof(RightMarginWidthPercent)) ?? DefaultRawSettings.RightMarginWidthPercent;
			TopMarginHeightPercent = section.Attribute<double?>(nameof(TopMarginHeightPercent)) ?? DefaultRawSettings.TopMarginHeightPercent;
			BottomMarginHeightPercent = section.Attribute<double?>(nameof(BottomMarginHeightPercent)) ?? DefaultRawSettings.BottomMarginHeightPercent;
			MaxHeight = section.Attribute<double?>(nameof(MaxHeight)) ?? DefaultRawSettings.MaxHeight;
			MaxWidth = section.Attribute<double?>(nameof(MaxWidth)) ?? DefaultRawSettings.MaxWidth;
			Zoom = section.Attribute<double?>(nameof(Zoom)) ?? DefaultRawSettings.Zoom;
			ImagePlacement = section.Attribute<ImagePlacement?>(nameof(ImagePlacement)) ?? DefaultRawSettings.DefaultImagePlacement;
			IsRandom = section.Attribute<bool?>(nameof(IsRandom)) ?? DefaultRawSettings.IsRandom;
			IsEnabled = section.Attribute<bool?>(nameof(IsEnabled)) ?? DefaultRawSettings.IsEnabled;
			Interval = section.Attribute<TimeSpan?>(nameof(Interval)) ?? DefaultRawSettings.Interval;
		}

		static IEnumerable<string> FilterOutImages(string[] images) =>
			images.Where(a => !IsBetaImage(a));

		static bool IsBetaImage(string image) =>
			image.EndsWith("pack://application:,,,/dnSpy;component/Images/DefaultWatermarkLight.png", StringComparison.CurrentCultureIgnoreCase) ||
			image.EndsWith("pack://application:,,,/dnSpy;component/Images/DefaultWatermarkDark.png", StringComparison.CurrentCultureIgnoreCase);

		const string SEP_STRING = "<{[]}>";
		static string SerializeImages(string[] s) => string.Join(SEP_STRING, s);
		static string[] DeserializeImages(string s) {
			if (s == null)
				return Array.Empty<string>();
			return s.Split(new string[] { SEP_STRING }, StringSplitOptions.None).Where(a => !string.IsNullOrEmpty(a)).Select(a => a.Trim()).ToArray();
		}

		public void SaveSettings(ISettingsSection section) {
			section.Attribute(nameof(Id), Id);
			section.Attribute(nameof(Images), SerializeImages(Images));
			section.Attribute(nameof(Stretch), Stretch);
			section.Attribute(nameof(StretchDirection), StretchDirection);
			section.Attribute(nameof(Opacity), Opacity);
			section.Attribute(nameof(HorizontalOffset), HorizontalOffset);
			section.Attribute(nameof(VerticalOffset), VerticalOffset);
			section.Attribute(nameof(LeftMarginWidthPercent), LeftMarginWidthPercent);
			section.Attribute(nameof(RightMarginWidthPercent), RightMarginWidthPercent);
			section.Attribute(nameof(TopMarginHeightPercent), TopMarginHeightPercent);
			section.Attribute(nameof(BottomMarginHeightPercent), BottomMarginHeightPercent);
			section.Attribute(nameof(MaxHeight), MaxHeight);
			section.Attribute(nameof(MaxWidth), MaxWidth);
			section.Attribute(nameof(Zoom), Zoom);
			section.Attribute(nameof(ImagePlacement), ImagePlacement);
			section.Attribute(nameof(IsRandom), IsRandom);
			section.Attribute(nameof(IsEnabled), IsEnabled);
			section.Attribute(nameof(Interval), Interval);
		}

		public void CopyFrom(RawSettings other) {
			Images = other.Images;
			Stretch = other.Stretch;
			StretchDirection = other.StretchDirection;
			Opacity = other.Opacity;
			HorizontalOffset = other.HorizontalOffset;
			VerticalOffset = other.VerticalOffset;
			LeftMarginWidthPercent = other.LeftMarginWidthPercent;
			RightMarginWidthPercent = other.RightMarginWidthPercent;
			TopMarginHeightPercent = other.TopMarginHeightPercent;
			BottomMarginHeightPercent = other.BottomMarginHeightPercent;
			MaxHeight = other.MaxHeight;
			MaxWidth = other.MaxWidth;
			Zoom = other.Zoom;
			ImagePlacement = other.ImagePlacement;
			IsRandom = other.IsRandom;
			IsEnabled = other.IsEnabled;
			Interval = other.Interval;
		}

		public RawSettings Clone() => new RawSettings(this);

		public bool EqualsImages(RawSettings other) => EqualsImagesInternal(Images, other.Images);

		public bool EqualsSettingsNoImages(RawSettings other) {
			if (!StringComparer.Ordinal.Equals(Id, other.Id))
				return false;
			if (Stretch != other.Stretch)
				return false;
			if (StretchDirection != other.StretchDirection)
				return false;
			if (Opacity != other.Opacity)
				return false;
			if (HorizontalOffset != other.HorizontalOffset)
				return false;
			if (VerticalOffset != other.VerticalOffset)
				return false;
			if (LeftMarginWidthPercent != other.LeftMarginWidthPercent)
				return false;
			if (RightMarginWidthPercent != other.RightMarginWidthPercent)
				return false;
			if (TopMarginHeightPercent != other.TopMarginHeightPercent)
				return false;
			if (BottomMarginHeightPercent != other.BottomMarginHeightPercent)
				return false;
			if (MaxHeight != other.MaxHeight)
				return false;
			if (MaxWidth != other.MaxWidth)
				return false;
			if (Zoom != other.Zoom)
				return false;
			if (ImagePlacement != other.ImagePlacement)
				return false;
			if (IsRandom != other.IsRandom)
				return false;
			if (IsEnabled != other.IsEnabled)
				return false;
			if (Interval != other.Interval)
				return false;
			return true;
		}

		public bool Equals(RawSettings other) =>
			EqualsSettingsNoImages(other) && EqualsImages(other);

		bool EqualsImagesInternal(string[] a, string[] b) {
			if (a == b)
				return true;
			if (a == null || b == null)
				return false;
			if (a.Length != b.Length)
				return false;
			for (int i = 0; i < a.Length; i++) {
				if (!StringComparer.InvariantCultureIgnoreCase.Equals(a[i], b[i]))
					return false;
			}
			return true;
		}

		public override bool Equals(object obj) => Equals(obj as RawSettings);

		public override int GetHashCode() {
			int hc = Id.GetHashCode();
			foreach (var i in Images)
				hc ^= i.GetHashCode();
			hc ^= (int)Stretch << 14;
			hc ^= (int)StretchDirection << 17;
			hc ^= Opacity.GetHashCode();
			hc ^= HorizontalOffset.GetHashCode();
			hc ^= VerticalOffset.GetHashCode();
			hc ^= LeftMarginWidthPercent.GetHashCode();
			hc ^= RightMarginWidthPercent.GetHashCode();
			hc ^= TopMarginHeightPercent.GetHashCode();
			hc ^= BottomMarginHeightPercent.GetHashCode();
			hc ^= MaxHeight.GetHashCode();
			hc ^= MaxWidth.GetHashCode();
			hc ^= Zoom.GetHashCode();
			hc ^= (int)ImagePlacement;
			hc ^= IsRandom ? int.MinValue : 0;
			hc ^= IsEnabled ? 0x40000000 : 0;
			hc ^= Interval.GetHashCode();
			return hc;
		}
	}
}
