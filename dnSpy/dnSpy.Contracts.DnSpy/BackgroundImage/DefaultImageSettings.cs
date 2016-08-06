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
using System.Windows.Controls;
using System.Windows.Media;

namespace dnSpy.Contracts.BackgroundImage {
	/// <summary>
	/// Default image settings
	/// </summary>
	public sealed class DefaultImageSettings {
		/// <summary>
		/// All images or null to use the default value. This can be filenames, folders, or pack:// URIs
		/// </summary>
		public string[] Images { get; set; }

		/// <summary>
		/// Stretch value or null to use the default value
		/// </summary>
		public Stretch? Stretch { get; set; }

		/// <summary>
		/// Stretch direction value or null to use the default value
		/// </summary>
		public StretchDirection? StretchDirection { get; set; }

		/// <summary>
		/// Opacity or null to use the default value
		/// </summary>
		public double? Opacity { get; set; }

		/// <summary>
		/// Horizontal offset or null to use the default value
		/// </summary>
		public double? HorizontalOffset { get; set; }

		/// <summary>
		/// Vertical offset or null to use the default value
		/// </summary>
		public double? VerticalOffset { get; set; }

		/// <summary>
		/// Number of grid rows or null to use the default value
		/// </summary>
		public int? TotalGridRows { get; set; }

		/// <summary>
		/// Number of grid columns or null to use the default value
		/// </summary>
		public int? TotalGridColumns { get; set; }

		/// <summary>
		/// Grid row or null to use the default value
		/// </summary>
		public int? GridRow { get; set; }

		/// <summary>
		/// Grid column or null to use the default value
		/// </summary>
		public int? GridColumn { get; set; }

		/// <summary>
		/// Grid row span or null to use the default value
		/// </summary>
		public int? GridRowSpan { get; set; }

		/// <summary>
		/// Grid column span or null to use the default value
		/// </summary>
		public int? GridColumnSpan { get; set; }

		/// <summary>
		/// Max height or null to use the default value
		/// </summary>
		public double? MaxHeight { get; set; }

		/// <summary>
		/// Max width or null to use the default value
		/// </summary>
		public double? MaxWidth { get; set; }

		/// <summary>
		/// Scale or null to use the default value
		/// </summary>
		public double? Scale { get; set; }

		/// <summary>
		/// Image placement or null to use the default value
		/// </summary>
		public ImagePlacement? ImagePlacement { get; set; }

		/// <summary>
		/// True if images are picked in random order
		/// </summary>
		public bool? IsRandom { get; set; }

		/// <summary>
		/// true if it's enabled
		/// </summary>
		public bool? IsEnabled { get; set; }

		/// <summary>
		/// Time interval until next image is shown or null to use the default value
		/// </summary>
		public TimeSpan? Interval { get; set; }
	}
}
