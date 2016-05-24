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
using dnSpy.Contracts.Text;
using dnSpy.Contracts.Text.Editor;

namespace dnSpy.Text.Editor {
	sealed class WpfTextView : IWpfTextView {
		public PropertyCollection Properties { get; }
		public FrameworkElement VisualElement => DnSpyTextEditor.FocusedElement;
		public object UIObject => DnSpyTextEditor;
		public IInputElement FocusedElement => DnSpyTextEditor.FocusedElement;
		public FrameworkElement ScaleElement => DnSpyTextEditor.ScaleElement;
		public object Tag { get; set; }
		public DnSpyTextEditor DnSpyTextEditor { get; }

		public WpfTextView(DnSpyTextEditor dnSpyTextEditor, ITextViewModel textViewModel) {
			if (dnSpyTextEditor == null)
				throw new ArgumentNullException(nameof(dnSpyTextEditor));
			if (textViewModel == null)
				throw new ArgumentNullException(nameof(textViewModel));
			Properties = new PropertyCollection();
			this.DnSpyTextEditor = dnSpyTextEditor;
			TextViewModel = textViewModel;
		}

		public ITextBuffer TextBuffer => TextViewModel.EditBuffer;
		public ITextSnapshot TextSnapshot => TextBuffer.CurrentSnapshot;
		public ITextSnapshot VisualSnapshot => TextViewModel.VisualBuffer.CurrentSnapshot;
		public ITextDataModel TextDataModel => TextViewModel.DataModel;
		public ITextViewModel TextViewModel { get; }

		public bool IsClosed { get; set; }
		public event EventHandler Closed;
		public void Close() {
			if (IsClosed)
				throw new InvalidOperationException();
			TextViewModel.Dispose();
			IsClosed = true;
			Closed?.Invoke(this, EventArgs.Empty);
		}
	}
}
