/*
    Copyright (C) 2014-2015 de4dot@gmail.com

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
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using System.Windows.Input;
using System.Windows.Threading;
using ICSharpCode.ILSpy;
using ICSharpCode.ILSpy.TreeNodes;

namespace dnSpy.AsmEditor {
	[Export(typeof(IPlugin))]
	sealed class UndoCommandManagerLoader : IPlugin {
		public static readonly RoutedUICommand Undo;
		public static readonly RoutedUICommand Redo;

		static UndoCommandManagerLoader() {
			// Create our own Undo/Redo commands because if a text box has the focus (eg. search
			// pane), it will send undo/redo events and the undo/redo toolbar buttons will be
			// enabled/disabled based on the text box's undo/redo state, not the asm editor's
			// undo/redo state.
			ApplicationCommands.Redo.InputGestures.Add(new KeyGesture(Key.Z, ModifierKeys.Control | ModifierKeys.Shift));
			Undo = new RoutedUICommand("Undo", "Undo", typeof(UndoCommandManagerLoader), new InputGestureCollection(ApplicationCommands.Undo.InputGestures));
			Redo = new RoutedUICommand("Redo", "Redo", typeof(UndoCommandManagerLoader), new InputGestureCollection(ApplicationCommands.Redo.InputGestures));
		}

		void IPlugin.OnLoaded() {
			MainWindow.Instance.CommandBindings.Add(new CommandBinding(Undo, UndoExecuted, UndoCanExecute));
			MainWindow.Instance.CommandBindings.Add(new CommandBinding(Redo, RedoExecuted, RedoCanExecute));
			MainWindow.Instance.Closing += MainWindow_Closing;
		}

		void MainWindow_Closing(object sender, CancelEventArgs e) {
			var count = UndoCommandManager.Instance.GetModifiedObjects().Count();
			if (count != 0) {
				var msg = count == 1 ? "There is an unsaved file." : string.Format("There are {0} unsaved files.", count);
				var res = MainWindow.Instance.ShowMessageBox(string.Format("{0} Are you sure you want to exit?", msg), System.Windows.MessageBoxButton.YesNo);
				if (res == MsgBoxButton.No || res == MsgBoxButton.None)
					e.Cancel = true;
			}
		}

		void UndoCanExecute(object sender, CanExecuteRoutedEventArgs e) {
			e.CanExecute = UndoCommandManager.Instance.CanUndo;
		}

		void UndoExecuted(object sender, ExecutedRoutedEventArgs e) {
			UndoCommandManager.Instance.Undo();
		}

		void RedoCanExecute(object sender, CanExecuteRoutedEventArgs e) {
			e.CanExecute = UndoCommandManager.Instance.CanRedo;
		}

		void RedoExecuted(object sender, ExecutedRoutedEventArgs e) {
			UndoCommandManager.Instance.Redo();
		}
	}

	public enum UndoCommandManagerEventType {
		Add,
		Undo,
		Redo,
		ClearUndo,
		ClearRedo,
		Saved,
		Dirty,
	}

	public class UndoCommandManagerEventArgs : EventArgs {
		public readonly UndoCommandManagerEventType Type;
		public readonly IUndoObject UndoObject;

		public UndoCommandManagerEventArgs(UndoCommandManagerEventType type, IUndoObject obj) {
			this.Type = type;
			this.UndoObject = obj;
		}
	}

	public sealed class UndoCommandManager {
		public static readonly UndoCommandManager Instance = new UndoCommandManager();

		List<UndoState> undoCommands = new List<UndoState>();
		List<UndoState> redoCommands = new List<UndoState>();
		UndoState currentCommands;
		int commandCounter;
		int currentCommandCounter;

		public event EventHandler<UndoCommandManagerEventArgs> OnEvent;

		void NotifyEvent(UndoCommandManagerEventType type, IUndoObject obj = null) {
			var evt = OnEvent;
			if (evt != null)
				evt(this, new UndoCommandManagerEventArgs(type, obj));
		}

		UndoCommandManager() {
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

		public struct BeginEndAdder : IDisposable {
			readonly UndoCommandManager mgr;

			public BeginEndAdder(UndoCommandManager mgr) {
				this.mgr = mgr;
				mgr.BeginAddInternal();
			}

			public void Dispose() {
				mgr.EndAddInternal();
			}
		}

		/// <summary>
		/// true if we can undo a command group
		/// </summary>
		public bool CanUndo {
			get { return undoCommands.Count != 0; }
		}

		/// <summary>
		/// true if we can redo a command group
		/// </summary>
		public bool CanRedo {
			get { return redoCommands.Count != 0; }
		}

		/// <summary>
		/// true if <see cref="BeginAdd()"/> has been called and we're still adding commands to the
		/// same group.
		/// </summary>
		public bool IsAdding {
			get { return currentCommands != null; }
		}

		/// <summary>
		/// Adds a command and executes it
		/// </summary>
		/// <param name="command"></param>
		public void Add(IUndoCommand command) {
			if (currentCommands == null) {
				using (BeginAdd())
					Add(command);
			}
			else {
				currentCommands.ModifiedObjects.AddRange(GetModifiedObjects(command));
				command.Execute();
				OnExecutedOneCommand(currentCommands);
				currentCommands.Commands.Add(command);
			}
		}

		/// <summary>
		/// Call this to add a group of commands that belong in the same group. Call the Dispose()
		/// method to stop adding more commands to the same group.
		/// </summary>
		public BeginEndAdder BeginAdd() {
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

		/// <summary>
		/// Clears undo and redo history
		/// </summary>
		public void Clear() {
			Clear(true, true, undoCommands.Count != 0 || redoCommands.Count != 0);
		}

		void Clear(bool clearUndo, bool clearRedo, bool forceCallGc = false) {
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

			foreach (var asm in MainWindow.Instance.GetAllLoadedAssemblyInstances()) {
				if (!IsModified(asm))
					asm.SavedCommand = 0;
			}
		}

		internal void CallGc() {
			if (!callingGc) {
				callingGc = true;
				// Some removed assemblies need to be GC'd. The AssemblyList already does this but
				// we might cache them so we need to call the GC again.
				App.Current.Dispatcher.BeginInvoke(DispatcherPriority.ContextIdle, new Action(delegate {
					callingGc = false;
					GC.Collect();
					GC.WaitForPendingFinalizers();
				}));
			}
		}
		bool callingGc = false;

		static void Clear(List<UndoState> list) {
			foreach (var group in list) {
				foreach (var cmd in group.Commands)
					cmd.Dispose();
			}
			list.Clear();
			list.TrimExcess();
		}

		/// <summary>
		/// Undoes the previous command group and places it in the redo list
		/// </summary>
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

		/// <summary>
		/// Undoes the previous undo command group and places it in the undo list
		/// </summary>
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

		public IEnumerable<IUndoObject> GetModifiedObjects() {
			foreach (var asm in MainWindow.Instance.GetAllLoadedAssemblyInstances()) {
				if (IsModified(asm))
					yield return asm;
			}

			foreach (var doc in HexDocumentManager.Instance.GetDocuments()) {
				if (IsModified(doc))
					yield return doc;
			}
		}

		public bool IsModified(IUndoObject obj) {
			return obj.IsDirty && IsModifiedCounter(obj, currentCommandCounter);
		}

		bool IsModifiedCounter(IUndoObject obj, int counter) {
			return obj.SavedCommand != 0 && obj.SavedCommand != counter;
		}

		internal void MarkAsModified(IUndoObject obj) {
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

		void UpdateAssemblySavedStateRedo(UndoState executedGroup) {
			UpdateAssemblySavedState(executedGroup.CommandCounter, executedGroup);
		}

		void UpdateAssemblySavedStateUndo(UndoState executedGroup) {
			UpdateAssemblySavedState(executedGroup.PrevCommandCounter, executedGroup);
		}

		void UpdateAssemblySavedState(int newCurrentCommandCounter, UndoState executedGroup) {
			currentCommandCounter = newCurrentCommandCounter;
			foreach (var obj in executedGroup.ModifiedObjects) {
				Debug.Assert(obj.SavedCommand != 0);
				bool newValue = IsModifiedCounter(obj, currentCommandCounter);
				WriteIsDirty(obj, newValue);
			}
		}

		static IEnumerable<IUndoObject> GetModifiedObjects(IUndoCommand command) {
			foreach (var obj in command.ModifiedObjects) {
				var uo = GetUndoObject(obj);
				if (uo != null)
					yield return uo;
			}
		}

		static IUndoObject GetUndoObject(object obj) {
			var node = obj as ILSpyTreeNode;
			if (node != null) {
				var asmNode = ILSpyTreeNode.GetNode<AssemblyTreeNode>(node);
				Debug.Assert(asmNode != null);
				if (asmNode != null)
					return asmNode.LoadedAssembly;
				return null;
			}

			var doc = obj as AsmEdHexDocument;
			if (doc != null)
				return doc;

			Debug.Fail(string.Format("Unknown modified object: {0}: {1}", obj == null ? null : obj.GetType(), obj));
			return null;
		}

		void OnExecutedOneCommand(UndoState group) {
			foreach (var obj in group.ModifiedObjects) {
				if (obj.SavedCommand == 0)
					obj.SavedCommand = group.PrevCommandCounter;

				var asm = obj as LoadedAssembly;
				if (asm != null) {
					var module = asm.ModuleDefinition;
					if (module != null)
						module.ResetTypeDefFindCache();
					Utils.NotifyModifiedAssembly(asm);
					continue;
				}

				Debug.Assert(obj is AsmEdHexDocument, string.Format("Unknown modified object: {0}: {1}", obj == null ? null : obj.GetType(), obj));
			}
		}

		public struct UndoRedoInfo {
			public bool IsInUndo;
			public bool IsInRedo;
			public AssemblyTreeNode Node;

			public UndoRedoInfo(AssemblyTreeNode node, bool isInUndo, bool isInRedo) {
				this.IsInUndo = isInUndo;
				this.IsInRedo = isInRedo;
				this.Node = node;
			}
		}

		public IEnumerable<UndoRedoInfo> GetUndoRedoInfo(IEnumerable<AssemblyTreeNode> nodes) {
			var modifiedUndoAsms = new HashSet<LoadedAssembly>(undoCommands.SelectMany(a => a.ModifiedObjects.Where(b => b is LoadedAssembly).Cast<LoadedAssembly>()));
			var modifiedRedoAsms = new HashSet<LoadedAssembly>(redoCommands.SelectMany(a => a.ModifiedObjects.Where(b => b is LoadedAssembly).Cast<LoadedAssembly>()));
			foreach (var node in nodes) {
				bool isInUndo = modifiedUndoAsms.Contains(node.LoadedAssembly);
				bool isInRedo = modifiedRedoAsms.Contains(node.LoadedAssembly);
				yield return new UndoRedoInfo(node, isInUndo, isInRedo);
			}
		}

		public IEnumerable<LoadedAssembly> GetAssemblies() {
			var list = new List<UndoState>(undoCommands);
			list.AddRange(redoCommands);
			foreach (var grp in list) {
				foreach (var cmd in grp.Commands) {
					var cmd2 = cmd as IUndoCommand2;
					if (cmd2 == null)
						continue;
					foreach (var obj in cmd2.NonModifiedObjects) {
						var asm = GetUndoObject(obj) as LoadedAssembly;
						if (asm != null)
							yield return asm;
					}
				}
				foreach (var obj in grp.ModifiedObjects) {
					var asm = obj as LoadedAssembly;
					if (asm != null)
						yield return asm;
				}
			}
		}
	}
}
