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
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.VisualStudio.Text.Operations;
using Microsoft.VisualStudio.Utilities;

namespace dnSpy.Text.Operations {
	sealed class TextUndoHistory : ITextUndoHistory {
		public IPropertyOwner PropertyOwner { get; }
		public PropertyCollection Properties { get; }
		public TextUndoHistoryState State { get; private set; }
		public ITextUndoTransaction CurrentTransaction => currentTransaction;
		public event EventHandler<TextUndoRedoEventArgs> UndoRedoHappened;
		public event EventHandler<TextUndoTransactionCompletedEventArgs> UndoTransactionCompleted;

		public bool CanRedo => redoList.Count > 0;
		public bool CanUndo => undoList.Count > 0;
		public ITextUndoTransaction LastRedoTransaction => redoList.FirstOrDefault();
		public ITextUndoTransaction LastUndoTransaction => undoList.FirstOrDefault();
		public string RedoDescription => LastRedoTransaction?.Description;
		public string UndoDescription => LastUndoTransaction?.Description;
		public IEnumerable<ITextUndoTransaction> RedoStack => readOnlyRedoList;
		public IEnumerable<ITextUndoTransaction> UndoStack => readOnlyUndoList;

		readonly List<TextUndoTransaction> redoList;
		readonly List<TextUndoTransaction> undoList;
		readonly ReadOnlyCollection<TextUndoTransaction> readOnlyRedoList;
		readonly ReadOnlyCollection<TextUndoTransaction> readOnlyUndoList;
		TextUndoTransaction currentTransaction;

		public TextUndoHistory(IPropertyOwner propertyOwner) {
			if (propertyOwner == null)
				throw new ArgumentNullException(nameof(propertyOwner));
			State = TextUndoHistoryState.Idle;
			PropertyOwner = propertyOwner;
			Properties = new PropertyCollection();
			redoList = new List<TextUndoTransaction>();
			undoList = new List<TextUndoTransaction>();
			readOnlyRedoList = new ReadOnlyCollection<TextUndoTransaction>(redoList);
			readOnlyUndoList = new ReadOnlyCollection<TextUndoTransaction>(undoList);
		}

		public void OnCompleted(TextUndoTransaction transaction) {
			if (currentTransaction != transaction)
				throw new InvalidOperationException();
			currentTransaction = null;
			undoList.Add(transaction);
			UndoTransactionCompleted?.Invoke(this, new TextUndoTransactionCompletedEventArgs(transaction, TextUndoTransactionCompletionResult.TransactionAdded));
		}

		public void OnCanceled(TextUndoTransaction transaction) {
			if (currentTransaction != transaction)
				throw new InvalidOperationException();
			currentTransaction = null;
		}

		void ClearUndo() {
			foreach (var transaction in undoList)
				transaction.Invalidate();
			undoList.Clear();
		}

		void ClearRedo() {
			foreach (var transaction in redoList)
				transaction.Invalidate();
			redoList.Clear();
		}

		public ITextUndoTransaction CreateTransaction(string description) {
			if (State != TextUndoHistoryState.Idle)
				throw new InvalidOperationException();
			if (description == null)
				throw new ArgumentNullException(nameof(description));
			if (currentTransaction != null)
				throw new InvalidOperationException();
			ClearRedo();
			return currentTransaction = new TextUndoTransaction(this, undoList.LastOrDefault(), description);
		}

		public void Redo(int count) {
			if (count < 0)
				throw new ArgumentOutOfRangeException(nameof(count));
			if (currentTransaction != null)
				throw new InvalidOperationException();
			if (State != TextUndoHistoryState.Idle)
				throw new InvalidOperationException();
			if (count > redoList.Count)
				throw new ArgumentOutOfRangeException(nameof(count));
			State = TextUndoHistoryState.Redoing;
			for (int i = 0; i < count; i++) {
				var transaction = redoList[redoList.Count - 1];
				redoList.RemoveAt(redoList.Count - 1);
				undoList.Add(transaction);
				transaction.Do();
				UndoRedoHappened?.Invoke(this, new TextUndoRedoEventArgs(TextUndoHistoryState.Redoing, transaction));
			}
			State = TextUndoHistoryState.Idle;
		}

		public void Undo(int count) {
			if (count < 0)
				throw new ArgumentOutOfRangeException(nameof(count));
			if (currentTransaction != null)
				throw new InvalidOperationException();
			if (State != TextUndoHistoryState.Idle)
				throw new InvalidOperationException();
			if (count > undoList.Count)
				throw new ArgumentOutOfRangeException(nameof(count));
			State = TextUndoHistoryState.Undoing;
			for (int i = 0; i < count; i++) {
				var transaction = undoList[undoList.Count - 1];
				undoList.RemoveAt(undoList.Count - 1);
				redoList.Add(transaction);
				transaction.Undo();
				UndoRedoHappened?.Invoke(this, new TextUndoRedoEventArgs(TextUndoHistoryState.Undoing, transaction));
			}
			State = TextUndoHistoryState.Idle;
		}

		public void Dispose() {
			currentTransaction?.Invalidate();
			currentTransaction = null;
			ClearRedo();
			ClearUndo();
		}
	}
}
