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
using System.Windows;
using dnSpy.Contracts.Text;
using dnSpy.Contracts.Text.Classification;
using dnSpy.Text.Formatting;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.OptionsExtensionMethods;
using Microsoft.VisualStudio.Utilities;

namespace dnSpy.Text.Editor {
	[Export(typeof(IWpfTextViewMarginProvider))]
	[MarginContainer(PredefinedMarginNames.LeftSelection)]
	[Name(PredefinedMarginNames.Spacer)]
	[ContentType(ContentTypes.TEXT)]
	[TextViewRole(PredefinedTextViewRoles.Interactive)]
	[Order(After = PredefinedMarginNames.LineNumber)]
	sealed class SpacerMarginProvider : IWpfTextViewMarginProvider {
		readonly IClassificationFormatMapService classificationFormatMapService;
		readonly IThemeClassificationTypes themeClassificationTypes;
		readonly ITextFormatterProvider textFormatterProvider;

		[ImportingConstructor]
		SpacerMarginProvider(IClassificationFormatMapService classificationFormatMapService, IThemeClassificationTypes themeClassificationTypes, ITextFormatterProvider textFormatterProvider) {
			this.classificationFormatMapService = classificationFormatMapService;
			this.themeClassificationTypes = themeClassificationTypes;
			this.textFormatterProvider = textFormatterProvider;
		}

		public IWpfTextViewMargin CreateMargin(IWpfTextViewHost wpfTextViewHost, IWpfTextViewMargin marginContainer) =>
			new SpacerMargin(wpfTextViewHost);
	}

	sealed class SpacerMargin : FrameworkElement, IWpfTextViewMargin {
		const int SELECTION_MARGIN_WIDTH = 10;
		public bool Enabled => wpfTextViewHost.TextView.Options.IsSelectionMarginEnabled();
		public double MarginSize => ActualWidth;
		public FrameworkElement VisualElement => this;

		readonly IWpfTextViewHost wpfTextViewHost;

		public SpacerMargin(IWpfTextViewHost wpfTextViewHost) {
			if (wpfTextViewHost == null)
				throw new ArgumentNullException(nameof(wpfTextViewHost));
			this.wpfTextViewHost = wpfTextViewHost;
			wpfTextViewHost.TextView.Options.OptionChanged += Options_OptionChanged;
			Width = SELECTION_MARGIN_WIDTH;
			ClipToBounds = true;
			IsHitTestVisible = false;
		}

		public ITextViewMargin GetTextViewMargin(string marginName) =>
			StringComparer.OrdinalIgnoreCase.Equals(marginName, PredefinedMarginNames.Spacer) ? this : null;

		void Options_OptionChanged(object sender, EditorOptionChangedEventArgs e) {
			if (e.OptionId == DefaultTextViewHostOptions.SelectionMarginId.Name)
				Visibility = Enabled ? Visibility.Visible : Visibility.Collapsed;
		}

		public void Dispose() =>
			wpfTextViewHost.TextView.Options.OptionChanged -= Options_OptionChanged;
	}
}
