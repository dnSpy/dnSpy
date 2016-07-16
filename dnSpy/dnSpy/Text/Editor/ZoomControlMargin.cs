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
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using dnSpy.Contracts.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.OptionsExtensionMethods;
using Microsoft.VisualStudio.Utilities;

namespace dnSpy.Text.Editor {
	[Export(typeof(IWpfTextViewMarginProvider))]
	[MarginContainer(PredefinedMarginNames.BottomControl)]
	[Name(PredefinedMarginNames.ZoomControl)]
	[ContentType(ContentTypes.TEXT)]
	[TextViewRole(PredefinedTextViewRoles.Zoomable)]
	[Order(Before = PredefinedMarginNames.HorizontalScrollBarContainer)]
	sealed class ZoomControlMarginProvider : IWpfTextViewMarginProvider {
		public IWpfTextViewMargin CreateMargin(IWpfTextViewHost wpfTextViewHost, IWpfTextViewMargin marginContainer) =>
			new ZoomControlMargin(wpfTextViewHost);
	}

	sealed class ZoomControlMargin : ZoomControl, IWpfTextViewMargin {
		public bool Enabled => wpfTextViewHost.TextView.Options.IsZoomControlEnabled() && wpfTextViewHost.TextView.Options.IsHorizontalScrollBarEnabled();
		public double MarginSize => ActualHeight;
		public FrameworkElement VisualElement => this;

		readonly IWpfTextViewHost wpfTextViewHost;

		public ZoomControlMargin(IWpfTextViewHost wpfTextViewHost) {
			if (wpfTextViewHost == null)
				throw new ArgumentNullException(nameof(wpfTextViewHost));
			this.wpfTextViewHost = wpfTextViewHost;

			IsVisibleChanged += ZoomControlMargin_IsVisibleChanged;
			wpfTextViewHost.TextView.Options.OptionChanged += Options_OptionChanged;

			// Need to set these explicitly so our themed styles are used
			SetResourceReference(StyleProperty, typeof(ComboBox));
			SetResourceReference(ItemContainerStyleProperty, typeof(ComboBoxItem));
			MinHeight = 0;
			Margin = new Thickness(0);
			Width = 60;
		}

		public ITextViewMargin GetTextViewMargin(string marginName) =>
			StringComparer.OrdinalIgnoreCase.Equals(PredefinedMarginNames.ZoomControl, marginName) ? this : null;

		void Options_OptionChanged(object sender, EditorOptionChangedEventArgs e) {
			if (e.OptionId == DefaultTextViewHostOptions.ZoomControlId.Name || e.OptionId == DefaultTextViewHostOptions.HorizontalScrollBarId.Name)
				Visibility = Enabled ? Visibility.Visible : Visibility.Collapsed;
			else if (!Enabled) {
				// Ignore all other options
			}
			else if (e.OptionId == DefaultWpfViewOptions.ZoomLevelId.Name)
				UpdateTextWithZoomLevel();
		}

		double TextViewZoomLevel {
			get { return wpfTextViewHost.TextView.Options.GlobalOptions.ZoomLevel(); }
			set { wpfTextViewHost.TextView.Options.GlobalOptions.SetOptionValue(DefaultWpfViewOptions.ZoomLevelId, value); }
		}

		protected override void OnGotKeyboardFocus(KeyboardFocusChangedEventArgs e) {
			base.OnGotKeyboardFocus(e);
			originalZoomLevel = TextViewZoomLevel;
		}
		double? originalZoomLevel;

		protected override void OnLostKeyboardFocus(KeyboardFocusChangedEventArgs e) {
			base.OnLostKeyboardFocus(e);
			originalZoomLevel = null;
			UpdateTextWithZoomLevel();
		}

		protected override void OnKeyDown(KeyEventArgs e) {
			if (Enabled) {
				if (Keyboard.Modifiers == ModifierKeys.None && e.Key == Key.Enter) {
					TryUpdateZoomLevel();
					wpfTextViewHost.TextView.VisualElement.Focus();
					e.Handled = true;
					return;
				}
				if (Keyboard.Modifiers == ModifierKeys.None && e.Key == Key.Escape) {
					Debug.Assert(originalZoomLevel != null);
					if (originalZoomLevel != null)
						TextViewZoomLevel = originalZoomLevel.Value;
					UpdateTextWithZoomLevel();
					wpfTextViewHost.TextView.VisualElement.Focus();
					e.Handled = true;
					return;
				}
			}

			base.OnKeyDown(e);
		}

		protected override void OnSelectionChanged(SelectionChangedEventArgs e) {
			base.OnSelectionChanged(e);
			TryUpdateZoomLevel();
		}
		static readonly ZoomLevelConverter zoomLevelConverter = new ZoomLevelConverter();

		void ZoomControlMargin_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e) {
			if (Visibility == Visibility.Visible) {
				originalZoomLevel = null;
				UpdateTextWithZoomLevel();

				// The combobox is too tall, but I want to use the style from the UI.Wpf dll
				if (horizontalScrollBarMargin == null) {
					horizontalScrollBarMargin = wpfTextViewHost.GetTextViewMargin(PredefinedMarginNames.HorizontalScrollBar);
					Debug.Assert(horizontalScrollBarMargin != null);
					if (horizontalScrollBarMargin != null)
						horizontalScrollBarMargin.VisualElement.SizeChanged += VisualElement_SizeChanged;
				}
				if (horizontalScrollBarMargin != null)
					Height = horizontalScrollBarMargin.VisualElement.Height;
			}
		}
		IWpfTextViewMargin horizontalScrollBarMargin;

		void VisualElement_SizeChanged(object sender, SizeChangedEventArgs e) =>
			Height = e.NewSize.Height;

		void UpdateTextWithZoomLevel() {
			var s = zoomLevelConverter.Convert(TextViewZoomLevel, typeof(string), null, CultureInfo.CurrentUICulture) as string;
			Text = s ?? TextViewZoomLevel.ToString("F0");
		}

		bool TryUpdateZoomLevel() {
			double? newValue = zoomLevelConverter.ConvertBack(Text, typeof(double), null, CultureInfo.CurrentUICulture) as double?;
			if (newValue == null || newValue.Value < ZoomConstants.MinZoom || newValue.Value > ZoomConstants.MaxZoom)
				return false;
			TextViewZoomLevel = newValue.Value;
			return true;
		}

		public void Dispose() {
			IsVisibleChanged -= ZoomControlMargin_IsVisibleChanged;
			wpfTextViewHost.TextView.Options.OptionChanged -= Options_OptionChanged;
			if (horizontalScrollBarMargin != null)
				horizontalScrollBarMargin.VisualElement.SizeChanged -= VisualElement_SizeChanged;
		}
	}
}
