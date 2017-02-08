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
using System.Windows;
using dnSpy.Contracts.Hex.Editor;
using dnSpy.Contracts.Hex.Editor.OptionsExtensionMethods;
using CTC = dnSpy.Contracts.Text.Classification;
using TF = dnSpy.Text.Formatting;
using VSTC = Microsoft.VisualStudio.Text.Classification;
using VSTE = Microsoft.VisualStudio.Text.Editor;
using VSUTIL = Microsoft.VisualStudio.Utilities;

namespace dnSpy.Hex.Editor {
	[Export(typeof(WpfHexViewMarginProvider))]
	[VSTE.MarginContainer(PredefinedHexMarginNames.LeftSelection)]
	[VSUTIL.Name(PredefinedHexMarginNames.Spacer)]
	[VSTE.TextViewRole(PredefinedHexViewRoles.Interactive)]
	[VSUTIL.Order(After = PredefinedHexMarginNames.LineNumber)]
	[VSUTIL.Order(After = PredefinedHexMarginNames.CustomLineNumber)]
	sealed class SpacerMarginProvider : WpfHexViewMarginProvider {
		readonly VSTC.IClassificationFormatMapService classificationFormatMapService;
		readonly CTC.IThemeClassificationTypeService themeClassificationTypeService;
		readonly TF.ITextFormatterProvider textFormatterProvider;

		[ImportingConstructor]
		SpacerMarginProvider(VSTC.IClassificationFormatMapService classificationFormatMapService, CTC.IThemeClassificationTypeService themeClassificationTypeService, TF.ITextFormatterProvider textFormatterProvider) {
			this.classificationFormatMapService = classificationFormatMapService;
			this.themeClassificationTypeService = themeClassificationTypeService;
			this.textFormatterProvider = textFormatterProvider;
		}

		public override WpfHexViewMargin CreateMargin(WpfHexViewHost wpfHexViewHost, WpfHexViewMargin marginContainer) =>
			new SpacerMargin(wpfHexViewHost);
	}

	sealed class SpacerMargin : WpfHexViewMargin {
		const int SELECTION_MARGIN_WIDTH = 10;
		public override bool Enabled => wpfHexViewHost.HexView.Options.IsSelectionMarginEnabled();
		public override double MarginSize => frameworkElement.ActualWidth;
		public override FrameworkElement VisualElement => frameworkElement;

		readonly FrameworkElement frameworkElement;
		readonly WpfHexViewHost wpfHexViewHost;

		public SpacerMargin(WpfHexViewHost wpfHexViewHost) {
			if (wpfHexViewHost == null)
				throw new ArgumentNullException(nameof(wpfHexViewHost));
			frameworkElement = new FrameworkElement();
			this.wpfHexViewHost = wpfHexViewHost;
			wpfHexViewHost.HexView.Options.OptionChanged += Options_OptionChanged;
			frameworkElement.Width = SELECTION_MARGIN_WIDTH;
			frameworkElement.ClipToBounds = true;
			frameworkElement.IsHitTestVisible = false;
			UpdateVisibility();
		}

		void UpdateVisibility() => frameworkElement.Visibility = Enabled ? Visibility.Visible : Visibility.Collapsed;

		public override HexViewMargin GetHexViewMargin(string marginName) =>
			StringComparer.OrdinalIgnoreCase.Equals(marginName, PredefinedHexMarginNames.Spacer) ? this : null;

		void Options_OptionChanged(object sender, VSTE.EditorOptionChangedEventArgs e) {
			if (e.OptionId == DefaultHexViewHostOptions.SelectionMarginName)
				UpdateVisibility();
		}

		protected override void DisposeCore() =>
			wpfHexViewHost.HexView.Options.OptionChanged -= Options_OptionChanged;
	}
}
