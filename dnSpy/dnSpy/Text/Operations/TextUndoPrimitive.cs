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
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Operations;

namespace dnSpy.Text.Operations {
	sealed class TextUndoPrimitive : ITextUndoPrimitive {
		public bool CanRedo => !canUndo;
		public bool CanUndo => canUndo;
		public ITextUndoTransaction Parent { get; set; }

		readonly ITextBuffer textBuffer;
		readonly ChangeInfo info;
		readonly object editTag;
		bool canUndo;

		public TextUndoPrimitive(ITextBuffer textBuffer, ChangeInfo info, object editTag) {
			if (textBuffer == null)
				throw new ArgumentNullException(nameof(textBuffer));
			this.textBuffer = textBuffer;
			this.info = info;
			this.editTag = editTag;
			canUndo = true;
		}

		public bool CanMerge(ITextUndoPrimitive older) {
			if (older == null)
				throw new ArgumentNullException(nameof(older));
			return false;//TODO:
		}

		public ITextUndoPrimitive Merge(ITextUndoPrimitive older) {
			if (older == null)
				throw new ArgumentNullException(nameof(older));
			throw new NotSupportedException();//TODO:
		}

		public void Do() {
			if (!CanRedo)
				throw new InvalidOperationException();
			using (var ed = textBuffer.CreateEdit(EditOptions.None, info.AfterVersionNumber, editTag)) {
				foreach (var change in info.Collection)
					ed.Replace(change.OldSpan, change.NewText);
				ed.Apply();
			}
			canUndo = true;
		}

		public void Undo() {
			if (!CanUndo)
				throw new InvalidOperationException();
			using (var ed = textBuffer.CreateEdit(EditOptions.None, info.BeforeVersionNumber, editTag)) {
				foreach (var change in info.Collection)
					ed.Replace(change.NewSpan, change.OldText);
				ed.Apply();
			}
			canUndo = false;
		}
	}
}
