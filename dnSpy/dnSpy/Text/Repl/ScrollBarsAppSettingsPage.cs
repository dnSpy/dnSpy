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
using dnSpy.Contracts.Images;
using dnSpy.Contracts.MVVM;
using dnSpy.Contracts.Settings.Dialog;
using dnSpy.Properties;

namespace dnSpy.Text.Repl {
	sealed class ScrollBarsAppSettingsPage : ViewModelBase, IAppSettingsPage {
		public Guid ParentGuid => options.Guid;
		public Guid Guid { get; }
		public double Order => AppSettingsConstants.ORDER_REPL_LANGUAGES_SCROLLBARS;
		public string Title => dnSpy_Resources.ScrollBarsSettings;
		public ImageReference Icon => ImageReference.None;
		public object UIObject => this;

		public bool HorizontalScrollBar {
			get { return horizontalScrollBar; }
			set {
				if (horizontalScrollBar != value) {
					horizontalScrollBar = value;
					OnPropertyChanged(nameof(HorizontalScrollBar));
				}
			}
		}
		bool horizontalScrollBar;

		public bool VerticalScrollBar {
			get { return verticalScrollBar; }
			set {
				if (verticalScrollBar != value) {
					verticalScrollBar = value;
					OnPropertyChanged(nameof(VerticalScrollBar));
				}
			}
		}
		bool verticalScrollBar;

		readonly IReplOptions options;

		public ScrollBarsAppSettingsPage(IReplOptions options, Guid guid) {
			if (options == null)
				throw new ArgumentNullException(nameof(options));
			this.options = options;
			Guid = guid;
			HorizontalScrollBar = options.HorizontalScrollBar;
			VerticalScrollBar = options.VerticalScrollBar;
		}

		public void OnApply() {
			options.HorizontalScrollBar = HorizontalScrollBar;
			options.VerticalScrollBar = VerticalScrollBar;
		}

		public void OnClosed() { }
	}
}
