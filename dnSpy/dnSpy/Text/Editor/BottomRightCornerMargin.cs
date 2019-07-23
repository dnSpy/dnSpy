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

using System;
using System.ComponentModel.Composition;
using System.Windows;
using System.Windows.Controls;
using dnSpy.Contracts.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Editor.OptionsExtensionMethods;
using Microsoft.VisualStudio.Utilities;

namespace dnSpy.Text.Editor {
	[Export(typeof(IWpfTextViewMarginProvider))]
	[MarginContainer(PredefinedMarginNames.BottomRightCorner)]
	[Name(BottomRightCornerMargin.NAME)]
	[ContentType(ContentTypes.Text)]
	[TextViewRole(PredefinedTextViewRoles.Interactive)]
	[GridCellLength(1.0), GridUnitType(GridUnitType.Star)]
	sealed class BottomRightCornerMarginProvider : IWpfTextViewMarginProvider {
		public IWpfTextViewMargin? CreateMargin(IWpfTextViewHost wpfTextViewHost, IWpfTextViewMargin marginContainer) =>
			new BottomRightCornerMargin(wpfTextViewHost);
	}

	sealed class BottomRightCornerMargin : Canvas, IWpfTextViewMargin {
		public const string NAME = "BottomRightCornerMargin";

		public bool Enabled => wpfTextViewHost.TextView.Options.IsHorizontalScrollBarEnabled();
		public double MarginSize => ActualHeight;
		public FrameworkElement VisualElement => this;

		readonly IWpfTextViewHost wpfTextViewHost;

		public BottomRightCornerMargin(IWpfTextViewHost wpfTextViewHost) {
			this.wpfTextViewHost = wpfTextViewHost ?? throw new ArgumentNullException(nameof(wpfTextViewHost));
			wpfTextViewHost.TextView.Options.OptionChanged += Options_OptionChanged;
			SetResourceReference(BackgroundProperty, "EnvironmentScrollBarBackground");
			UpdateVisibility();
		}

		void UpdateVisibility() => Visibility = Enabled ? Visibility.Visible : Visibility.Collapsed;

		public ITextViewMargin? GetTextViewMargin(string marginName) =>
			StringComparer.OrdinalIgnoreCase.Equals(NAME, marginName) ? this : null;

		void Options_OptionChanged(object? sender, EditorOptionChangedEventArgs e) {
			if (e.OptionId == DefaultTextViewHostOptions.HorizontalScrollBarName)
				UpdateVisibility();
		}

		public void Dispose() => wpfTextViewHost.TextView.Options.OptionChanged -= Options_OptionChanged;
	}
}
