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
using Microsoft.CodeAnalysis.Text;
using Microsoft.VisualStudio.Text;

namespace dnSpy.Roslyn.Text {
	sealed class TextBufferSourceTextContainer : SourceTextContainer {
		public ITextBuffer TextBuffer { get; }
		public override SourceText CurrentText => TextBuffer.CurrentSnapshot.AsText();

		readonly object lockObj = new object();
		EventHandler<TextChangeEventArgs>? realTextChangedEvent;
		public override event EventHandler<TextChangeEventArgs>? TextChanged {
			add {
				lock (lockObj) {
					if (realTextChangedEvent is null)
						TextBuffer.Changed += TextBuffer_Changed;
					realTextChangedEvent += value;
				}
			}
			remove {
				lock (lockObj) {
					realTextChangedEvent -= value;
					if (realTextChangedEvent is null)
						TextBuffer.Changed -= TextBuffer_Changed;
				}
			}
		}

		void TextBuffer_Changed(object? sender, TextContentChangedEventArgs e) =>
			realTextChangedEvent?.Invoke(this, e.ToTextChangeEventArgs());

		public TextBufferSourceTextContainer(ITextBuffer textBuffer) => TextBuffer = textBuffer;
	}
}
