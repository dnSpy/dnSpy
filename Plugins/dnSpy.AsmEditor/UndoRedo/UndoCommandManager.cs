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
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using System.Windows.Threading;

namespace dnSpy.AsmEditor.UndoRedo {
	interface IUndoCommandManager {
		/// <summary>
		/// true if we can undo a command group
		/// </summary>
		bool CanUndo { get; }

		/// <summary>
		/// true if we can redo a command group
		/// </summary>
		bool CanRedo { get; }

		event EventHandler<UndoCommandManagerEventArgs> OnEvent;

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
		void CallGc();
		IEnumerable<object> GetModifiedDocuments();
		IEnumerable<IUndoObject> GetAllObjects();
		IUndoObject GetUndoObject(object obj);
		IEnumerable<object> GetUniqueDocuments(IEnumerable<object> docs);
		void ClearRedo();
	}

	[Export, Export(typeof(IUndoCommandManager)), PartCreationPolicy(CreationPolicy.Shared)]
	sealed class UndoCommandManager : IUndoCommandManager {
		readonly List<UndoState> undoCommands = new List<UndoState>();
		readonly List<UndoState> redoCommands = new List<UndoState>();
		readonly Lazy<IUndoableDocumentsProvider>[] undoableDocumentsProviders;
		UndoState currentCommands;
		int commandCounter;
		int currentCommandCounter;

		public event EventHandler<UndoCommandManagerEventArgs> OnEvent;

		void NotifyEvent(UndoCommandManagerEventType type, IUndoObject obj = null) =>
			OnEvent?.Invoke(this, new UndoCommandManagerEventArgs(type, obj));

		[ImportingConstructor]
		UndoCommandManager([ImportMany] Lazy<IUndoableDocumentsProvider>[] undoableDocumentsProviders) {
			this.undoableDocumentsProviders = undoableDocumentsProviders.ToArray();
			commandCounter = currentCommandCounter = 1;
		}

		sealed class UndoState {
			public readonly HashSet<IUndoObject> ModifiedObjects = new HashSet<IUndoObject>();
			public readonly List<IUndoCommand> Commands = new List<IUndoCommand>();
			public readonly int CommandCounter;
			public readonly int PrevCommandCounter;

			public UndoState(int prevCommandCounter, int commandCounter) {
				this.PrevCommandCounter = prevCommandCounter;
				this.CommandCounter = commandCounter;
			}
		}

		struct BeginEndAdder : IDisposable {
			readonly UndoCommandManager mgr;

			public BeginEndAdder(UndoCommandManager mgr) {
				this.mgr = mgr;
				mgr.BeginAddInternal();
			}

			public void Dispose() => mgr.EndAddInternal();
		}

		public bool CanUndo => undoCommands.Count != 0;
		public bool CanRedo => redoCommands.Count != 0;
		public int NumberOfModifiedDocuments => GetModifiedDocuments().Count();
		bool IsAdding => currentCommands != null;

		public void Add(IUndoCommand command) {
			if (currentCommands == null) {
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
			Debug.Assert(currentCommands == null);
			if (currentCommands != null)
				throw new InvalidOperationException();

			return new BeginEndAdder(this);
		}

		void BeginAddInternal() {
			Debug.Assert(currentCommands == null);
			if (currentCommands != null)
				throw new InvalidOperationException();

			int prev = currentCommandCounter;
			commandCounter++;
			currentCommands = new UndoState(prev, commandCounter);
		}

		void EndAddInternal() {
			Debug.Assert(currentCommands != null);
			if (currentCommands == null)
				throw new InvalidOperationException();

			currentCommands.Commands.TrimExcess();
			undoCommands.Add(currentCommands);

			bool callGc = NeedsToCallGc(redoCommands);
			Clear(redoCommands);
			if (callGc)
				CallGc();

			UpdateAssemblySavedStateRedo(currentCommands);
			currentCommands = null;
			NotifyEvent(UndoCommandManagerEventType.Add);
		}

		static bool NeedsToCallGc(List<UndoState> list) {
			foreach (var state in list) {
				foreach (var c in state.Commands) {
					var c2 = c as IGCUndoCommand;
					if (c2 != null && c2.CallGarbageCollectorAfterDispose)
						return true;
				}
			}
			return false;
		}

		public void ClearRedo() => Clear(false, true, redoCommands.Count != 0);
		public void Clear() => Clear(true, true, undoCommands.Count != 0 || redoCommands.Count != 0);

		void Clear(bool clearUndo, bool clearRedo, bool forceCallGc) {
			Debug.Assert(currentCommands == null);
			if (currentCommands != null)
				throw new InvalidOperationException();

			bool callGc = forceCallGc;
			if (clearUndo) {
				callGc |= NeedsToCallGc(undoCommands);
				Clear(undoCommands);
				NotifyEvent(UndoCommandManagerEventType.ClearUndo);
			}
			if (clearRedo) {
				callGc |= NeedsToCallGc(redoCommands);
				Clear(redoCommands);
				NotifyEvent(UndoCommandManagerEventType.ClearRedo);
			}

			if (callGc)
				CallGc();

			if (clearUndo && clearRedo) {
				foreach (var p in undoableDocumentsProviders) {
					foreach (var uo in p.Value.GetObjects()) {
						Debug.Assert(uo != null);
						if (uo != null && !IsModified(uo))
							uo.SavedCommand = 0;
					}
				}
			}
		}

		public void CallGc() {
			if (!callingGc) {
				callingGc = true;
				// Some removed assemblies need to be GC'd. The AssemblyList already does this but
				// we might cache them so we need to call the GC again.
				Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.ContextIdle, new Action(delegate {
					callingGc = false;
					GC.Collect();
					GC.WaitForPendingFinalizers();
				}));
			}
		}
		bool callingGc = false;

		static void Clear(List<UndoState> list) {
			foreach (var group in list) {
				foreach (var cmd in group.Commands) {
					var id = cmd as IDisposable;
					if (id != null)
						id.Dispose();
				}
			}
			list.Clear();
			list.TrimExcess();
		}

		public void Undo() {
			Debug.Assert(currentCommands == null);
			if (currentCommands != null)
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
			NotifyEvent(UndoCommandManagerEventType.Undo);
		}

		public void Redo() {
			Debug.Assert(currentCommands == null);
			if (currentCommands != null)
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
			NotifyEvent(UndoCommandManagerEventType.Redo);
		}

		public IEnumerable<object> GetModifiedDocuments() {
			var hash = new HashSet<object>();
			foreach (var p in undoableDocumentsProviders) {
				foreach (var uo in p.Value.GetObjects()) {
					Debug.Assert(uo != null);
					if (uo != null && IsModified(uo)) {
						var doc = p.Value.GetDocument(uo);
						Debug.Assert(doc != null);
						if (doc == null)
							throw new InvalidOperationException();
						hash.Add(doc);
					}
				}
			}
			return hash;
		}

		object GetDocument(IUndoObject uo) {
			foreach (var p in undoableDocumentsProviders) {
				var doc = p.Value.GetDocument(uo);
				if (doc != null)
					return doc;
			}

			Debug.Fail("Couldn't get the document");
			return null;
		}

		public IEnumerable<object> GetUniqueDocuments(IEnumerable<object> docs) {
			var hash = new HashSet<object>();
			foreach (var doc in docs) {
				var uo = GetUndoObject(doc);
				if (uo == null)
					continue;
				var doc2 = GetDocument(uo);
				if (doc2 == null)
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
				NotifyEvent(UndoCommandManagerEventType.Dirty, obj);
			else
				NotifyEvent(UndoCommandManagerEventType.Saved, obj);
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
				if (uo != null)
					yield return uo;
			}
		}

		public IUndoObject GetUndoObject(object obj) {
			foreach (var up in undoableDocumentsProviders) {
				var uo = up.Value.GetUndoObject(obj);
				if (uo != null)
					return uo;
			}

			Debug.Fail(string.Format("Unknown modified object: {0}: {1}", obj?.GetType(), obj));
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

				Debug.Assert(found, string.Format("Unknown modified object: {0}: {1}", obj?.GetType(), obj));
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
					if (cmd2 == null)
						continue;
					foreach (var obj in cmd2.NonModifiedObjects) {
						var uo = GetUndoObject(obj);
						if (uo != null)
							yield return uo;
					}
				}
				foreach (var obj in grp.ModifiedObjects)
					yield return obj;
			}
		}
	}
}
