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
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Microsoft.VisualStudio.Text.Operations;

namespace dnSpy.Text.Operations {
	sealed class TextUndoTransaction : ITextUndoTransaction {
		public bool CanRedo => State == UndoTransactionState.Undone;
		public bool CanUndo => State == UndoTransactionState.Completed;
		public ITextUndoHistory History => history;
		public IMergeTextUndoTransactionPolicy MergePolicy { get; set; }
		public ITextUndoTransaction Parent { get; }
		public UndoTransactionState State { get; private set; }
		public IList<ITextUndoPrimitive> UndoPrimitives => readOnlyUndoPrimitives;

		public string Description {
			get { return description; }
			set {
				if (value == null)
					throw new ArgumentNullException(nameof(value));
				description = value;
			}
		}
		string description;

		readonly TextUndoHistory history;
		readonly List<ITextUndoPrimitive> undoPrimitives;
		readonly ReadOnlyCollection<ITextUndoPrimitive> readOnlyUndoPrimitives;

		public TextUndoTransaction(TextUndoHistory history, ITextUndoTransaction parent, string description) {
			if (history == null)
				throw new ArgumentNullException(nameof(history));
			if (description == null)
				throw new ArgumentNullException(nameof(description));
			this.history = history;
			Parent = parent;
			undoPrimitives = new List<ITextUndoPrimitive>();
			readOnlyUndoPrimitives = new ReadOnlyCollection<ITextUndoPrimitive>(undoPrimitives);
			State = UndoTransactionState.Open;
			this.description = description;
		}

		public void AddUndo(ITextUndoPrimitive undo) {
			if (undo == null)
				throw new ArgumentNullException(nameof(undo));
			if (State != UndoTransactionState.Open)
				throw new InvalidOperationException();
			undoPrimitives.Add(undo);
			undo.Parent = this;
		}

		public void Cancel() {
			if (State != UndoTransactionState.Open)
				throw new InvalidOperationException();
			State = UndoTransactionState.Canceled;
			history.OnCanceled(this);
		}

		public void Complete() {
			if (State != UndoTransactionState.Open)
				throw new InvalidOperationException();
			State = UndoTransactionState.Completed;
			history.OnCompleted(this);
		}

		public void Do() {
			if (State != UndoTransactionState.Undone)
				throw new InvalidOperationException();
			State = UndoTransactionState.Redoing;
			foreach (var undo in undoPrimitives)
				undo.Do();
			State = UndoTransactionState.Completed;
		}

		public void Undo() {
			if (State != UndoTransactionState.Completed)
				throw new InvalidOperationException();
			State = UndoTransactionState.Undoing;
			for (int i = undoPrimitives.Count - 1; i >= 0; i--)
				undoPrimitives[i].Undo();
			State = UndoTransactionState.Undone;
		}

		public void Invalidate() => State = UndoTransactionState.Invalid;

		public void Dispose() {
			if (State != UndoTransactionState.Completed && State != UndoTransactionState.Canceled)
				Cancel();
		}
	}
}
