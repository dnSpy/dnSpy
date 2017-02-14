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

using System.Windows;
using System.Windows.Controls;

namespace dnSpy.Contracts.Controls {
	/// <summary>
	/// Simple text control. Should be used in the options dialog box instead of <see cref="TextBlock"/>
	/// so the text can be highlighted.
	/// </summary>
	public class TextControl : ContentControl {
		/// <summary>
		/// Text wrapping dependency property
		/// </summary>
		public static readonly DependencyProperty TextWrappingProperty =
			TextBlock.TextWrappingProperty.AddOwner(typeof(TextControl),
			new FrameworkPropertyMetadata(TextWrapping.NoWrap, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender));

		/// <summary>
		/// Gets/sets the text wrapping
		/// </summary>
		public TextWrapping TextWrapping {
			get { return (TextWrapping)GetValue(TextWrappingProperty); }
			set { SetValue(TextWrappingProperty, value); }
		}

		/// <summary>
		/// Text trimming dependency property
		/// </summary>
		public static readonly DependencyProperty TextTrimmingProperty =
			TextBlock.TextTrimmingProperty.AddOwner(typeof(TextControl),
			new FrameworkPropertyMetadata(TextTrimming.None, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender));

		/// <summary>
		/// Gets/sets the text trimming
		/// </summary>
		public TextTrimming TextTrimming {
			get { return (TextTrimming)GetValue(TextTrimmingProperty); }
			set { SetValue(TextTrimmingProperty, value); }
		}

		static TextControl() => DefaultStyleKeyProperty.OverrideMetadata(typeof(TextControl), new FrameworkPropertyMetadata(typeof(TextControl)));
	}
}
