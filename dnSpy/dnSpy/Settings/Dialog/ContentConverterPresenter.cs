/*
    Copyright (C) 2014-2019 de4dot@gmail.com

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

namespace dnSpy.Settings.Dialog {
	sealed class ContentConverterPresenter : ContentPresenter {
		public static readonly DependencyProperty OwnerControlProperty =
			DependencyProperty.Register(nameof(OwnerControl), typeof(object), typeof(ContentConverterPresenter),
			new UIPropertyMetadata(null, OwnerControlPropertyChangedCallback));

		public object OwnerControl {
			get => GetValue(OwnerControlProperty);
			set => SetValue(OwnerControlProperty, value);
		}

		static void OwnerControlPropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
			((ContentConverterPresenter)d).UpdateContent();

		public static readonly DependencyProperty ContentConverterProperty =
			DependencyProperty.Register(nameof(ContentConverter), typeof(IContentConverter), typeof(ContentConverterPresenter),
			new UIPropertyMetadata(null, ContentConverterPropertyChangedCallback));

		public IContentConverter ContentConverter {
			get => (IContentConverter)GetValue(ContentConverterProperty);
			set => SetValue(ContentConverterProperty, value);
		}

		static void ContentConverterPropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
			((ContentConverterPresenter)d).UpdateContent();

		public static readonly DependencyProperty ContentConverterVersionProperty =
			DependencyProperty.Register(nameof(ContentConverterVersion), typeof(int), typeof(ContentConverterPresenter),
			new UIPropertyMetadata(ContentConverterProperties.DefaultContentConverterVersion, ContentConverterVersionPropertyChangedCallback));

		public int ContentConverterVersion {
			get => (int)GetValue(ContentConverterVersionProperty);
			set => SetValue(ContentConverterVersionProperty, value);
		}

		static void ContentConverterVersionPropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
			((ContentConverterPresenter)d).UpdateContent();

		public static readonly DependencyProperty TextProperty =
			DependencyProperty.Register(nameof(Text), typeof(object), typeof(ContentConverterPresenter),
			new UIPropertyMetadata(null, TextPropertyChangedCallback));

		public object Text {
			get => GetValue(TextProperty);
			set => SetValue(TextProperty, value);
		}

		static void TextPropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
			((ContentConverterPresenter)d).UpdateContent(e.NewValue);

		void UpdateContent() => UpdateContent(Text);
		void UpdateContent(object value) =>
			Content = CreateContent(value);

		object CreateContent(object value) {
			var ownerControl = OwnerControl;
			if (ownerControl is null)
				return value;
			var converter = ContentConverter;
			if (converter is null)
				return value;
			return converter.Convert(value, ownerControl);
		}
	}
}
