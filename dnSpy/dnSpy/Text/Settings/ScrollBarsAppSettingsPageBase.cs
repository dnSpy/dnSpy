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
using dnSpy.Contracts.Settings.Dialog;
using dnSpy.Properties;

namespace dnSpy.Text.Settings {
	abstract class ScrollBarsAppSettingsPageBase : AppSettingsPage {
		public sealed override string Title => dnSpy_Resources.ScrollBarsSettings;
		public sealed override object? UIObject => this;

		public bool HorizontalScrollBar {
			get => horizontalScrollBar;
			set {
				if (horizontalScrollBar != value) {
					horizontalScrollBar = value;
					OnPropertyChanged(nameof(HorizontalScrollBar));
				}
			}
		}
		bool horizontalScrollBar;

		public bool VerticalScrollBar {
			get => verticalScrollBar;
			set {
				if (verticalScrollBar != value) {
					verticalScrollBar = value;
					OnPropertyChanged(nameof(VerticalScrollBar));
				}
			}
		}
		bool verticalScrollBar;

		readonly ICommonEditorOptions options;

		protected ScrollBarsAppSettingsPageBase(ICommonEditorOptions options) {
			this.options = options ?? throw new ArgumentNullException(nameof(options));
			HorizontalScrollBar = options.HorizontalScrollBar;
			VerticalScrollBar = options.VerticalScrollBar;
		}

		public override void OnApply() {
			options.HorizontalScrollBar = HorizontalScrollBar;
			options.VerticalScrollBar = VerticalScrollBar;
		}
	}
}
