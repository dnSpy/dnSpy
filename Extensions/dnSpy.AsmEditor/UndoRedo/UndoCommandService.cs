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
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using System.Windows.Input;

namespace dnSpy.AsmEditor.UndoRedo {
	interface IUndoCommandService {
		/// <summary>
		/// true if we can undo a command group
		/// </summary>
		bool CanUndo { get; }

		/// <summary>
		/// true if we can redo a command group
		/// </summary>
		bool CanRedo { get; }

		event EventHandler<UndoCommandServiceEventArgs>? OnEvent;

		int NumberOfModifiedDocuments { get; }

		/// <summary>
		/// Adds a command and executes it
		/// </summary>
		/// <param name="command"></param>
		void Add(IUndoCommand command);

		/// <summary>
		/// Clears undo and redo history
		/// </summary>
		void Clear();

		/// <summary>
		/// Undoes the previous command group and places it in the redo list
		/// </summary>
		void Undo();

		/// <summary>
		/// Undoes the previous undo command group and places it in the undo list
		/// </summary>
		void Redo();

		bool IsModified(IUndoObject obj);
		void MarkAsModified(IUndoObject obj);
		void MarkAsSaved(IUndoObject obj);
		IEnumerable<IUndoObject> UndoObjects { get; }
		IEnumerable<IUndoObject> RedoObjects { get; }
		bool CachedHasModifiedDocuments { get; }
		IEnumerable<object> GetModifiedDocuments();
		IEnumerable<IUndoObject> GetAllObjects();
		IUndoObject? GetUndoObject(object obj);
		IEnumerable<object> GetUniqueDocuments(IEnumerable<object> docs);
		void ClearRedo();
	}

	[Export(typeof(IUndoCommandService))]
	sealed class UndoCommandService : IUndoCommandService {
		readonly List<UndoState> undoCommands = new List<UndoState>();
		readonly List<UndoState> redoCommands = new List<UndoState>();
		readonly Lazy<IUndoableDocumentsProvider>[] undoableDocumentsProviders;
		UndoState? currentCommands;
		int commandCounter;
		int currentCommandCounter;

		public event EventHandler<UndoCommandServiceEventArgs>? OnEvent;

		void NotifyEvent(UndoCommandServiceEventType type, IUndoObject? obj = null) {
			UndoRedoChanged();
			OnEvent?.Invoke(this, new UndoCommandServiceEventArgs(type, obj));
		}

		[ImportingConstructor]
		UndoCommandService([ImportMany] Lazy<IUndoableDocumentsProvider>[] undoableDocumentsProviders) {
			this.undoableDocumentsProviders = undoableDocumentsProviders.ToArray();
			commandCounter = currentCommandCounter = 1;
		}

		sealed class UndoState {
			public readonly HashSet<IUndoObject> ModifiedObjects = new HashSet<IUndoObject>();
			public readonly List<IUndoCommand> Commands = new List<IUndoCommand>();
			public readonly int CommandCounter;
			public readonly int PrevCommandCounter;

			public UndoState(int prevCommandCounter, int commandCounter) {
				PrevCommandCounter = prevCommandCounter;
				CommandCounter = commandCounter;
			}
		}

		readonly struct BeginEndAdder : IDisposable {
			readonly UndoCommandService mgr;

			public BeginEndAdder(UndoCommandService mgr) {
				this.mgr = mgr;
				mgr.BeginAddInternal();
			}

			public void Dispose() => mgr.EndAddInternal();
		}

		public bool CanUndo => undoCommands.Count != 0;
		public bool CanRedo => redoCommands.Count != 0;
		public int NumberOfModifiedDocuments => GetModifiedDocuments().Count();
		bool IsAdding => !(currentCommands is null);

		public void Add(IUndoCommand command) {
			if (currentCommands is null) {
				using (BeginAdd())
					Add(command);
			}
			else {
				foreach (var o in GetModifiedObjects(command))
					currentCommands.ModifiedObjects.Add(o);
				command.Execute();
				OnExecutedOneCommand(currentCommands);
				currentCommands.Commands.Add(command);
			}
		}

		/// <summary>
		/// Call this to add a group of commands that belong in the same group. Call the Dispose()
		/// method to stop adding more commands to the same group.
		/// </summary>
		BeginEndAdder BeginAdd() {
			Debug2.Assert(currentCommands is null);
			if (!(currentCommands is null))
				throw new InvalidOperationException();

			return new BeginEndAdder(this);
		}

		void BeginAddInternal() {
			Debug2.Assert(currentCommands is null);
			if (!(currentCommands is null))
				throw new InvalidOperationException();

			int prev = currentCommandCounter;
			commandCounter++;
			currentCommands = new UndoState(prev, commandCounter);
		}

		void EndAddInternal() {
			Debug2.Assert(!(currentCommands is null));
			if (currentCommands is null)
				throw new InvalidOperationException();

			currentCommands.Commands.TrimExcess();
			undoCommands.Add(currentCommands);

			Clear(redoCommands);

			UpdateAssemblySavedStateRedo(currentCommands);
			currentCommands = null;
			NotifyEvent(UndoCommandServiceEventType.Add);
		}

		public void ClearRedo() => Clear(false, true);
		public void Clear() => Clear(true, true);

		void Clear(bool clearUndo, bool clearRedo) {
			Debug2.Assert(currentCommands is null);
			if (!(currentCommands is null))
				throw new InvalidOperationException();

			if (clearUndo) {
				Clear(undoCommands);
				NotifyEvent(UndoCommandServiceEventType.ClearUndo);
			}
			if (clearRedo) {
				Clear(redoCommands);
				NotifyEvent(UndoCommandServiceEventType.ClearRedo);
			}

			if (clearUndo && clearRedo) {
				foreach (var p in undoableDocumentsProviders) {
					foreach (var uo in p.Value.GetObjects()) {
						Debug2.Assert(!(uo is null));
						if (!(uo is null) && !IsModified(uo))
							uo.SavedCommand = 0;
					}
				}
			}
		}

		static void Clear(List<UndoState> list) {
			foreach (var group in list) {
				foreach (var cmd in group.Commands) {
					if (cmd is IDisposable id)
						id.Dispose();
				}
			}
			list.Clear();
			list.TrimExcess();
		}

		public void Undo() {
			Debug2.Assert(currentCommands is null);
			if (!(currentCommands is null))
				throw new InvalidOperationException();

			if (undoCommands.Count == 0)
				return;
			var group = undoCommands[undoCommands.Count - 1];
			for (int i = group.Commands.Count - 1; i >= 0; i--) {
				group.Commands[i].Undo();
				OnExecutedOneCommand(group);
			}
			undoCommands.RemoveAt(undoCommands.Count - 1);
			redoCommands.Add(group);
			UpdateAssemblySavedStateUndo(group);
			NotifyEvent(UndoCommandServiceEventType.Undo);
		}

		public void Redo() {
			Debug2.Assert(currentCommands is null);
			if (!(currentCommands is null))
				throw new InvalidOperationException();

			if (redoCommands.Count == 0)
				return;
			var group = redoCommands[redoCommands.Count - 1];
			for (int i = 0; i < group.Commands.Count; i++) {
				group.Commands[i].Execute();
				OnExecutedOneCommand(group);
			}
			redoCommands.RemoveAt(redoCommands.Count - 1);
			undoCommands.Add(group);
			UpdateAssemblySavedStateRedo(group);
			NotifyEvent(UndoCommandServiceEventType.Redo);
		}

		void UndoRedoChanged() {
			// Make sure the save all button gets enabled or disabled
			cachedHasModifiedDocumentsDateTime = DateTime.MinValue;
			CommandManager.InvalidateRequerySuggested();
		}

		// If there are many files opened (say 400), figuring out all the modified documents could
		// take a while because DsDocumentUndoableDocumentsProvider calls DocumentTreeView.FindNode()
		// for every file. This property caches the result to minimize CPU usage.
		// If we got notified every time a document got closed, we could count the number of
		// modified docs in WriteIsDirty() and remove this caching logic.
		public bool CachedHasModifiedDocuments {
			get {
				var currValue = DateTime.UtcNow;
				var diff = currValue - cachedHasModifiedDocumentsDateTime;
				const int CACHED_WAIT_MS = 2000;
				if (diff.TotalMilliseconds < CACHED_WAIT_MS)
					return cachedHasModifiedDocumentsValue;
				cachedHasModifiedDocumentsDateTime = currValue;
				return cachedHasModifiedDocumentsValue = HasModifiedDocuments;
			}
		}
		bool cachedHasModifiedDocumentsValue;
		DateTime cachedHasModifiedDocumentsDateTime = DateTime.MinValue;

		bool HasModifiedDocuments => GetModifiedDocuments().Any();

		public IEnumerable<object> GetModifiedDocuments() {
			var hash = new HashSet<object>();
			foreach (var p in undoableDocumentsProviders) {
				foreach (var uo in p.Value.GetObjects()) {
					Debug2.Assert(!(uo is null));
					if (!(uo is null) && IsModified(uo)) {
						var doc = p.Value.GetDocument(uo);
						Debug2.Assert(!(doc is null));
						if (doc is null)
							throw new InvalidOperationException();
						hash.Add(doc);
					}
				}
			}
			return hash;
		}

		object? GetDocument(IUndoObject uo) {
			foreach (var p in undoableDocumentsProviders) {
				var doc = p.Value.GetDocument(uo);
				if (!(doc is null))
					return doc;
			}

			Debug.Fail("Couldn't get the document");
			return null;
		}

		public IEnumerable<object> GetUniqueDocuments(IEnumerable<object> docs) {
			var hash = new HashSet<object>();
			foreach (var doc in docs) {
				var uo = GetUndoObject(doc);
				if (uo is null)
					continue;
				var doc2 = GetDocument(uo);
				if (doc2 is null)
					continue;

				hash.Add(doc2);
			}
			return hash;
		}

		public bool IsModified(IUndoObject obj) => obj.IsDirty && IsModifiedCounter(obj, currentCommandCounter);
		bool IsModifiedCounter(IUndoObject obj, int counter) => obj.SavedCommand != 0 && obj.SavedCommand != counter;

		public void MarkAsModified(IUndoObject obj) {
			if (obj.SavedCommand == 0)
				obj.SavedCommand = currentCommandCounter;
			WriteIsDirty(obj, true);
		}

		public void MarkAsSaved(IUndoObject obj) {
			obj.SavedCommand = GetNewSavedCommand(obj);
			WriteIsDirty(obj, false);
		}

		void WriteIsDirty(IUndoObject obj, bool newIsDirty) {
			// Always call NotifyEvent() even when value doesn't change.
			obj.IsDirty = newIsDirty;
			if (newIsDirty)
				NotifyEvent(UndoCommandServiceEventType.Dirty, obj);
			else
				NotifyEvent(UndoCommandServiceEventType.Saved, obj);
		}

		int GetNewSavedCommand(IUndoObject obj) {
			for (int i = undoCommands.Count - 1; i >= 0; i--) {
				var group = undoCommands[i];
				if (group.ModifiedObjects.Contains(obj))
					return group.CommandCounter;
			}
			if (undoCommands.Count > 0)
				return undoCommands[0].PrevCommandCounter;
			return currentCommandCounter;
		}

		void UpdateAssemblySavedStateRedo(UndoState executedGroup) =>
			UpdateAssemblySavedState(executedGroup.CommandCounter, executedGroup);
		void UpdateAssemblySavedStateUndo(UndoState executedGroup) =>
			UpdateAssemblySavedState(executedGroup.PrevCommandCounter, executedGroup);

		void UpdateAssemblySavedState(int newCurrentCommandCounter, UndoState executedGroup) {
			currentCommandCounter = newCurrentCommandCounter;
			foreach (var obj in executedGroup.ModifiedObjects) {
				Debug.Assert(obj.SavedCommand != 0);
				bool newValue = IsModifiedCounter(obj, currentCommandCounter);
				WriteIsDirty(obj, newValue);
			}
		}

		IEnumerable<IUndoObject> GetModifiedObjects(IUndoCommand command) {
			foreach (var obj in command.ModifiedObjects) {
				var uo = GetUndoObject(obj);
				if (!(uo is null))
					yield return uo;
			}
		}

		public IUndoObject? GetUndoObject(object obj) {
			foreach (var up in undoableDocumentsProviders) {
				var uo = up.Value.GetUndoObject(obj);
				if (!(uo is null))
					return uo;
			}

			Debug.Fail($"Unknown modified object: {obj?.GetType()}: {obj}");
			return null;
		}

		void OnExecutedOneCommand(UndoState group) {
			foreach (var obj in group.ModifiedObjects) {
				if (obj.SavedCommand == 0)
					obj.SavedCommand = group.PrevCommandCounter;

				bool found = false;
				foreach (var up in undoableDocumentsProviders) {
					found = up.Value.OnExecutedOneCommand(obj);
					if (found)
						break;
				}

				Debug.Assert(found, $"Unknown modified object: {obj?.GetType()}: {obj}");
			}
		}

		public IEnumerable<IUndoObject> UndoObjects => undoCommands.SelectMany(a => a.ModifiedObjects);
		public IEnumerable<IUndoObject> RedoObjects => redoCommands.SelectMany(a => a.ModifiedObjects);

		public IEnumerable<IUndoObject> GetAllObjects() {
			var list = new List<UndoState>(undoCommands);
			list.AddRange(redoCommands);
			foreach (var grp in list) {
				foreach (var cmd in grp.Commands) {
					var cmd2 = cmd as IUndoCommand2;
					if (cmd2 is null)
						continue;
					foreach (var obj in cmd2.NonModifiedObjects) {
						var uo = GetUndoObject(obj);
						if (!(uo is null))
							yield return uo;
					}
				}
				foreach (var obj in grp.ModifiedObjects)
					yield return obj;
			}
		}
	}
}
