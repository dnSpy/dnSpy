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

namespace dnSpy.Debugger.UI.Wpf {
	/// <summary>
	/// Recreates text blocks when the data changes. Most of the time the data is identical to the old data
	/// and we save CPU time.
	/// </summary>
	sealed class SmartTextBlock : ContentControl {
		public static readonly DependencyProperty ContentInfoProperty =
			DependencyProperty.Register(nameof(ContentInfo), typeof(TextBlockContentInfo), typeof(SmartTextBlock),
			new FrameworkPropertyMetadata(null, ContentInfoProperty_PropertyChangedCallback));

		static void ContentInfoProperty_PropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e) {
			var oldValue = (TextBlockContentInfo)e.OldValue;
			var newValue = (TextBlockContentInfo)e.NewValue;
			if (oldValue == newValue)
				return;
			((SmartTextBlock)d).Content = newValue?.TextElementFactory.Create(newValue.ClassificationFormatMap, newValue.Text, newValue.Tags, newValue.TextElementFlags);
		}

		public TextBlockContentInfo ContentInfo {
			get => (TextBlockContentInfo)GetValue(ContentInfoProperty);
			set => SetValue(ContentInfoProperty, value);
		}
	}
}
