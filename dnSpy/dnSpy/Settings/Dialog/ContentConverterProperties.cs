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

namespace dnSpy.Settings.Dialog {
	/// <summary>
	/// Attached properties bound to eg. <see cref="ContentConverterPresenter"/>'s corresponding properties
	/// to make highlighting of searched text visible in checkboxes, labels, etc.
	/// </summary>
	static class ContentConverterProperties {
		// This is the interface that converts the input text to highlighted text
		public static readonly DependencyProperty ContentConverterProperty =
			DependencyProperty.RegisterAttached("ContentConverter", typeof(IContentConverter), typeof(ContentConverterProperties),
			new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.Inherits));
		public static IContentConverter GetContentConverter(DependencyObject depo) => (IContentConverter)depo.GetValue(ContentConverterProperty);
		public static void SetContentConverter(DependencyObject depo, IContentConverter value) => depo.SetValue(ContentConverterProperty, value);

		// This value gets changed (eg. incremented) whenever we want Convert() to be called again
		public static readonly DependencyProperty ContentConverterVersionProperty =
			DependencyProperty.RegisterAttached("ContentConverterVersion", typeof(int), typeof(ContentConverterProperties),
			new FrameworkPropertyMetadata(DefaultContentConverterVersion, FrameworkPropertyMetadataOptions.Inherits));
		public static int GetContentConverterVersion(DependencyObject depo) => (int)depo.GetValue(ContentConverterVersionProperty);
		public static void SetContentConverterVersion(DependencyObject depo, int value) => depo.SetValue(ContentConverterVersionProperty, value);

		public const int DefaultContentConverterVersion = -1;
	}
}
