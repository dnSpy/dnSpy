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
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using dnSpy.Contracts.Text.Editor;

namespace dnSpy.Text.Editor {
	sealed class WpfTextViewHost : IWpfTextViewHost {
		public bool IsClosed { get; set; }
		public IWpfTextView TextView { get; }
		public event EventHandler Closed;
		public Control HostControl => contentControl;
		public object UIObject => contentControl;
		public IInputElement FocusedElement => TextView.FocusedElement;
		public FrameworkElement ScaleElement => TextView.ScaleElement;
		public object Tag { get; set; }

		readonly ContentControl contentControl;

		public WpfTextViewHost(IWpfTextView wpfTextView, bool setFocus) {
			if (wpfTextView == null)
				throw new ArgumentNullException(nameof(wpfTextView));
			TextView = wpfTextView;
			this.contentControl = new ContentControl {
				Focusable = false,
				Content = TextView.UIObject,
			};

			if (setFocus) {
				contentControl.Dispatcher.BeginInvoke(DispatcherPriority.Render, new Action(() => {
					if (!TextView.IsClosed)
						TextView.FocusedElement?.Focus();
				}));
			}
		}

		public void Close() {
			if (IsClosed)
				throw new InvalidOperationException();
			TextView.Close();
			IsClosed = true;
			Closed?.Invoke(this, EventArgs.Empty);
		}
	}
}
