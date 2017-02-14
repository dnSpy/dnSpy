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
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using dnSpy.Contracts.Hex.Editor;
using dnSpy.Contracts.Hex.Editor.OptionsExtensionMethods;
using dnSpy.Contracts.Hex.Operations;
using VSTE = Microsoft.VisualStudio.Text.Editor;
using VSUTIL = Microsoft.VisualStudio.Utilities;

namespace dnSpy.Hex.Editor {
	[Export(typeof(WpfHexViewMarginProvider))]
	[VSTE.MarginContainer(PredefinedHexMarginNames.BottomControl)]
	[VSUTIL.Name(PredefinedHexMarginNames.ZoomControl)]
	[VSTE.TextViewRole(PredefinedHexViewRoles.Zoomable)]
	[VSUTIL.Order(Before = PredefinedHexMarginNames.HorizontalScrollBarContainer)]
	sealed class ZoomControlMarginProvider : WpfHexViewMarginProvider {
		readonly HexEditorOperationsFactoryService editorOperationsFactoryService;

		[ImportingConstructor]
		ZoomControlMarginProvider(HexEditorOperationsFactoryService editorOperationsFactoryService) => this.editorOperationsFactoryService = editorOperationsFactoryService;

		public override WpfHexViewMargin CreateMargin(WpfHexViewHost wpfHexViewHost, WpfHexViewMargin marginContainer) =>
			new ZoomControlMargin(wpfHexViewHost, editorOperationsFactoryService.GetEditorOperations(wpfHexViewHost.HexView));
	}

	sealed class ZoomControlMargin : WpfHexViewMargin {
		public override bool Enabled => wpfHexViewHost.HexView.Options.IsZoomControlEnabled() && wpfHexViewHost.HexView.Options.IsHorizontalScrollBarEnabled();
		public override double MarginSize => zoomControl.ActualHeight;
		public override FrameworkElement VisualElement => zoomControl;

		sealed class TheZoomControl : VSTE.ZoomControl {
			readonly ZoomControlMargin owner;
			public TheZoomControl(ZoomControlMargin owner) {
				this.owner = owner;
				IsVisibleChanged += ZoomControlMargin_IsVisibleChanged;
			}

			double HexViewZoomLevel {
				get { return owner.wpfHexViewHost.HexView.ZoomLevel; }
				set {
					if (owner.wpfHexViewHost.HexView.Options.IsOptionDefined(DefaultWpfHexViewOptions.ZoomLevelId, true))
						owner.wpfHexViewHost.HexView.Options.SetOptionValue(DefaultWpfHexViewOptions.ZoomLevelId, value);
					else
						owner.editorOperations.ZoomTo(value);
				}
			}

			protected override void OnGotKeyboardFocus(KeyboardFocusChangedEventArgs e) {
				base.OnGotKeyboardFocus(e);
				originalZoomLevel = HexViewZoomLevel;
			}
			double? originalZoomLevel;

			protected override void OnLostKeyboardFocus(KeyboardFocusChangedEventArgs e) {
				base.OnLostKeyboardFocus(e);
				originalZoomLevel = null;
				UpdateTextWithZoomLevel();
			}

			protected override void OnKeyDown(KeyEventArgs e) {
				if (owner.Enabled) {
					if (Keyboard.Modifiers == ModifierKeys.None && e.Key == Key.Enter) {
						TryUpdateZoomLevel();
						owner.wpfHexViewHost.HexView.VisualElement.Focus();
						e.Handled = true;
						return;
					}
					if (Keyboard.Modifiers == ModifierKeys.None && e.Key == Key.Escape) {
						Debug.Assert(originalZoomLevel != null);
						if (originalZoomLevel != null)
							HexViewZoomLevel = originalZoomLevel.Value;
						UpdateTextWithZoomLevel();
						owner.wpfHexViewHost.HexView.VisualElement.Focus();
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

			static readonly VSTE.ZoomLevelConverter zoomLevelConverter = new VSTE.ZoomLevelConverter();

			void ZoomControlMargin_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e) {
				if (Visibility == Visibility.Visible) {
					originalZoomLevel = null;
					owner.RegisterEvents();
					UpdateTextWithZoomLevel();

					// The combobox is too tall, but I want to use the style from the UI.Wpf dll
					if (horizontalScrollBarMargin == null) {
						horizontalScrollBarMargin = owner.wpfHexViewHost.GetHexViewMargin(PredefinedHexMarginNames.HorizontalScrollBar);
						Debug.Assert(horizontalScrollBarMargin != null);
						if (horizontalScrollBarMargin != null)
							horizontalScrollBarMargin.VisualElement.SizeChanged += VisualElement_SizeChanged;
					}
					if (horizontalScrollBarMargin != null)
						Height = horizontalScrollBarMargin.VisualElement.Height;
				}
				else
					owner.UnregisterEvents();
			}
			WpfHexViewMargin horizontalScrollBarMargin;

			void VisualElement_SizeChanged(object sender, SizeChangedEventArgs e) =>
				Height = e.NewSize.Height;

			public void UpdateTextWithZoomLevel() {
				var s = zoomLevelConverter.Convert(HexViewZoomLevel, typeof(string), null, CultureInfo.CurrentUICulture) as string;
				Text = s ?? HexViewZoomLevel.ToString("F0");
			}

			bool TryUpdateZoomLevel() {
				double? newValue = zoomLevelConverter.ConvertBack(Text, typeof(double), null, CultureInfo.CurrentUICulture) as double?;
				if (newValue == null || newValue.Value < VSTE.ZoomConstants.MinZoom || newValue.Value > VSTE.ZoomConstants.MaxZoom)
					return false;
				HexViewZoomLevel = newValue.Value;
				return true;
			}

			public void Dispose() {
				IsVisibleChanged -= ZoomControlMargin_IsVisibleChanged;
				if (horizontalScrollBarMargin != null)
					horizontalScrollBarMargin.VisualElement.SizeChanged -= VisualElement_SizeChanged;
			}
		}

		readonly TheZoomControl zoomControl;
		readonly WpfHexViewHost wpfHexViewHost;
		readonly HexEditorOperations editorOperations;

		public ZoomControlMargin(WpfHexViewHost wpfHexViewHost, HexEditorOperations editorOperations) {
			zoomControl = new TheZoomControl(this);
			this.wpfHexViewHost = wpfHexViewHost ?? throw new ArgumentNullException(nameof(wpfHexViewHost));
			this.editorOperations = editorOperations ?? throw new ArgumentNullException(nameof(editorOperations));

			wpfHexViewHost.HexView.Options.OptionChanged += Options_OptionChanged;

			// Need to set these explicitly so our themed styles are used
			zoomControl.SetResourceReference(FrameworkElement.StyleProperty, typeof(ComboBox));
			zoomControl.SetResourceReference(ItemsControl.ItemContainerStyleProperty, typeof(ComboBoxItem));
			zoomControl.MinHeight = 0;
			zoomControl.Margin = new Thickness(0);
			zoomControl.Width = 60;
			UpdateVisibility();
		}

		void UpdateVisibility() => zoomControl.Visibility = Enabled ? Visibility.Visible : Visibility.Collapsed;

		public override HexViewMargin GetHexViewMargin(string marginName) =>
			StringComparer.OrdinalIgnoreCase.Equals(PredefinedHexMarginNames.ZoomControl, marginName) ? this : null;

		void Options_OptionChanged(object sender, VSTE.EditorOptionChangedEventArgs e) {
			if (e.OptionId == DefaultHexViewHostOptions.ZoomControlName || e.OptionId == DefaultHexViewHostOptions.HorizontalScrollBarName)
				UpdateVisibility();
			else if (!Enabled) {
				// Ignore all other options
			}
			else if (e.OptionId == DefaultWpfHexViewOptions.ZoomLevelName)
				zoomControl.UpdateTextWithZoomLevel();
		}

		void HexView_ZoomLevelChanged(object sender, VSTE.ZoomLevelChangedEventArgs e) => zoomControl.UpdateTextWithZoomLevel();

		bool hasRegisteredEvents;
		void RegisterEvents() {
			if (hasRegisteredEvents)
				return;
			if (wpfHexViewHost.IsClosed)
				return;
			hasRegisteredEvents = true;
			wpfHexViewHost.HexView.ZoomLevelChanged += HexView_ZoomLevelChanged;
		}

		void UnregisterEvents() {
			hasRegisteredEvents = false;
			wpfHexViewHost.HexView.ZoomLevelChanged -= HexView_ZoomLevelChanged;
		}

		protected override void DisposeCore() {
			zoomControl.Dispose();
			wpfHexViewHost.HexView.Options.OptionChanged -= Options_OptionChanged;
			UnregisterEvents();
		}
	}
}
