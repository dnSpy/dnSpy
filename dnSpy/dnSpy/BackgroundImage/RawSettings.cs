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
		public int TotalGridRows { get; set; }
		public int TotalGridColumns { get; set; }
		public int GridRow { get; set; }
		public int GridColumn { get; set; }
		public int GridRowSpan { get; set; }
		public int GridColumnSpan { get; set; }
		public double MaxHeight { get; set; }
		public double MaxWidth { get; set; }
		public double Scale { get; set; }
		public ImagePlacement ImagePlacement { get; set; }
		public bool IsRandom { get; set; }
		public bool IsEnabled { get; set; }
		public TimeSpan Interval { get; set; }

		public string[] Images {
			get { return images; }
			set {
				if (value == null)
					throw new ArgumentNullException(nameof(value));
				images = value;
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
			TotalGridRows = DefaultRawSettings.TotalGridRows;
			TotalGridColumns = DefaultRawSettings.TotalGridColumns;
			GridRow = DefaultRawSettings.GridRow;
			GridColumn = DefaultRawSettings.GridColumn;
			GridRowSpan = DefaultRawSettings.GridRowSpan;
			GridColumnSpan = DefaultRawSettings.GridColumnSpan;
			MaxHeight = DefaultRawSettings.MaxHeight;
			MaxWidth = DefaultRawSettings.MaxWidth;
			Scale = DefaultRawSettings.Scale;
			ImagePlacement = DefaultRawSettings.DefaultImagePlacement;
			IsRandom = DefaultRawSettings.IsRandom;
			IsEnabled = DefaultRawSettings.IsEnabled;
			Interval = DefaultRawSettings.Interval;
		}

		public RawSettings(RawSettings other) {
			this.Id = other.Id;
			CopyFrom(other);
		}

		public RawSettings(string id, DefaultImageSettings defaultSettings) {
			if (id == null)
				throw new ArgumentNullException(nameof(id));
			this.Id = id;
			Images = defaultSettings.Images ?? Array.Empty<string>();
			Stretch = defaultSettings.Stretch ?? DefaultRawSettings.DefaultStretch;
			StretchDirection = defaultSettings.StretchDirection ?? DefaultRawSettings.DefaultStretchDirection;
			Opacity = defaultSettings.Opacity ?? DefaultRawSettings.Opacity;
			HorizontalOffset = defaultSettings.HorizontalOffset ?? DefaultRawSettings.HorizontalOffset;
			VerticalOffset = defaultSettings.VerticalOffset ?? DefaultRawSettings.VerticalOffset;
			TotalGridRows = defaultSettings.TotalGridRows ?? DefaultRawSettings.TotalGridRows;
			TotalGridColumns = defaultSettings.TotalGridColumns ?? DefaultRawSettings.TotalGridColumns;
			GridRow = defaultSettings.GridRow ?? DefaultRawSettings.GridRow;
			GridColumn = defaultSettings.GridColumn ?? DefaultRawSettings.GridColumn;
			GridRowSpan = defaultSettings.GridRowSpan ?? DefaultRawSettings.GridRowSpan;
			GridColumnSpan = defaultSettings.GridColumnSpan ?? DefaultRawSettings.GridColumnSpan;
			MaxHeight = defaultSettings.MaxHeight ?? DefaultRawSettings.MaxHeight;
			MaxWidth = defaultSettings.MaxWidth ?? DefaultRawSettings.MaxWidth;
			Scale = defaultSettings.Scale ?? DefaultRawSettings.Scale;
			ImagePlacement = defaultSettings.ImagePlacement ?? DefaultRawSettings.DefaultImagePlacement;
			IsRandom = defaultSettings.IsRandom ?? DefaultRawSettings.IsRandom;
			IsEnabled = defaultSettings.IsEnabled ?? DefaultRawSettings.IsEnabled;
			Interval = defaultSettings.Interval ?? DefaultRawSettings.Interval;
		}

		public RawSettings(string id)
			: this() {
			if (id == null)
				throw new ArgumentNullException(nameof(id));
			Id = id;
		}

		public RawSettings(ISettingsSection section) {
			ReadSettings(section);
		}

		void ReadSettings(ISettingsSection section) {
			Id = section.Attribute<string>(nameof(Id));
			Images = DeserializeImages(section.Attribute<string>(nameof(Images))) ?? Array.Empty<string>();
			Stretch = section.Attribute<Stretch?>(nameof(Stretch)) ?? DefaultRawSettings.DefaultStretch;
			StretchDirection = section.Attribute<StretchDirection?>(nameof(StretchDirection)) ?? DefaultRawSettings.DefaultStretchDirection;
			Opacity = section.Attribute<double?>(nameof(Opacity)) ?? DefaultRawSettings.Opacity;
			HorizontalOffset = section.Attribute<double?>(nameof(HorizontalOffset)) ?? DefaultRawSettings.HorizontalOffset;
			VerticalOffset = section.Attribute<double?>(nameof(VerticalOffset)) ?? DefaultRawSettings.VerticalOffset;
			TotalGridRows = section.Attribute<int?>(nameof(TotalGridRows)) ?? DefaultRawSettings.TotalGridRows;
			TotalGridColumns = section.Attribute<int?>(nameof(TotalGridColumns)) ?? DefaultRawSettings.TotalGridColumns;
			GridRow = section.Attribute<int?>(nameof(GridRow)) ?? DefaultRawSettings.GridRow;
			GridColumn = section.Attribute<int?>(nameof(GridColumn)) ?? DefaultRawSettings.GridColumn;
			GridRowSpan = section.Attribute<int?>(nameof(GridRowSpan)) ?? DefaultRawSettings.GridRowSpan;
			GridColumnSpan = section.Attribute<int?>(nameof(GridColumnSpan)) ?? DefaultRawSettings.GridColumnSpan;
			MaxHeight = section.Attribute<double?>(nameof(MaxHeight)) ?? DefaultRawSettings.MaxHeight;
			MaxWidth = section.Attribute<double?>(nameof(MaxWidth)) ?? DefaultRawSettings.MaxWidth;
			Scale = section.Attribute<double?>(nameof(Scale)) ?? DefaultRawSettings.Scale;
			ImagePlacement = section.Attribute<ImagePlacement?>(nameof(ImagePlacement)) ?? DefaultRawSettings.DefaultImagePlacement;
			IsRandom = section.Attribute<bool?>(nameof(IsRandom)) ?? DefaultRawSettings.IsRandom;
			IsEnabled = section.Attribute<bool?>(nameof(IsEnabled)) ?? DefaultRawSettings.IsEnabled;
			Interval = section.Attribute<TimeSpan?>(nameof(Interval)) ?? DefaultRawSettings.Interval;
		}

		static string SerializeImages(string[] s) => string.Join(";", s);
		static string[] DeserializeImages(string s) {
			if (s == null)
				return Array.Empty<string>();
			return s.Split(';').Where(a => !string.IsNullOrEmpty(a)).Select(a => a.Trim()).ToArray();
		}

		public void SaveSettings(ISettingsSection section) {
			section.Attribute(nameof(Id), Id);
			section.Attribute(nameof(Images), SerializeImages(Images));
			section.Attribute(nameof(Stretch), Stretch);
			section.Attribute(nameof(StretchDirection), StretchDirection);
			section.Attribute(nameof(Opacity), Opacity);
			section.Attribute(nameof(HorizontalOffset), HorizontalOffset);
			section.Attribute(nameof(VerticalOffset), VerticalOffset);
			section.Attribute(nameof(TotalGridRows), TotalGridRows);
			section.Attribute(nameof(TotalGridColumns), TotalGridColumns);
			section.Attribute(nameof(GridRow), GridRow);
			section.Attribute(nameof(GridColumn), GridColumn);
			section.Attribute(nameof(GridRowSpan), GridRowSpan);
			section.Attribute(nameof(GridColumnSpan), GridColumnSpan);
			section.Attribute(nameof(MaxHeight), MaxHeight);
			section.Attribute(nameof(MaxWidth), MaxWidth);
			section.Attribute(nameof(Scale), Scale);
			section.Attribute(nameof(ImagePlacement), ImagePlacement);
			section.Attribute(nameof(IsRandom), IsRandom);
			section.Attribute(nameof(IsEnabled), IsEnabled);
			section.Attribute(nameof(Interval), Interval);
		}

		public void CopyFrom(RawSettings other) {
			this.Images = other.Images;
			this.Stretch = other.Stretch;
			this.StretchDirection = other.StretchDirection;
			this.Opacity = other.Opacity;
			this.HorizontalOffset = other.HorizontalOffset;
			this.VerticalOffset = other.VerticalOffset;
			this.TotalGridRows = other.TotalGridRows;
			this.TotalGridColumns = other.TotalGridColumns;
			this.GridRow = other.GridRow;
			this.GridColumn = other.GridColumn;
			this.GridRowSpan = other.GridRowSpan;
			this.GridColumnSpan = other.GridColumnSpan;
			this.MaxHeight = other.MaxHeight;
			this.MaxWidth = other.MaxWidth;
			this.Scale = other.Scale;
			this.ImagePlacement = other.ImagePlacement;
			this.IsRandom = other.IsRandom;
			this.IsEnabled = other.IsEnabled;
			this.Interval = other.Interval;
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
			if (TotalGridRows != other.TotalGridRows)
				return false;
			if (TotalGridColumns != other.TotalGridColumns)
				return false;
			if (GridRow != other.GridRow)
				return false;
			if (GridColumn != other.GridColumn)
				return false;
			if (GridRowSpan != other.GridRowSpan)
				return false;
			if (GridColumnSpan != other.GridColumnSpan)
				return false;
			if (MaxHeight != other.MaxHeight)
				return false;
			if (MaxWidth != other.MaxWidth)
				return false;
			if (Scale != other.Scale)
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
			hc ^= TotalGridRows << 20;
			hc ^= TotalGridColumns << 23;
			hc ^= GridRow << 26;
			hc ^= GridColumn << 29;
			hc ^= GridRowSpan << 10;
			hc ^= GridColumnSpan << 12;
			hc ^= MaxHeight.GetHashCode();
			hc ^= MaxWidth.GetHashCode();
			hc ^= Scale.GetHashCode();
			hc ^= (int)ImagePlacement;
			hc ^= IsRandom ? int.MinValue : 0;
			hc ^= IsEnabled ? 0x40000000 : 0;
			hc ^= Interval.GetHashCode();
			return hc;
		}
	}
}
