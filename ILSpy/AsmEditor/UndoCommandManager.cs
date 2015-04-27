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
using ICSharpCode.ILSpy.TreeNodes;

namespace ICSharpCode.ILSpy.AsmEditor
{
	[Export(typeof(IPlugin))]
	sealed class UndoCommandManagerLoader : IPlugin
	{
		public static readonly RoutedUICommand Undo;
		public static readonly RoutedUICommand Redo;

		static UndoCommandManagerLoader()
		{
			// Create our own Undo/Redo commands because if a text box has the focus (eg. search
			// pane), it will send undo/redo events and the undo/redo toolbar buttons will be
			// enabled/disabled based on the text box's undo/redo state, not the asm editor's
			// undo/redo state.
			ApplicationCommands.Redo.InputGestures.Add(new KeyGesture(Key.Z, ModifierKeys.Control | ModifierKeys.Shift));
			Undo = new RoutedUICommand("Undo", "Undo", typeof(UndoCommandManagerLoader), new InputGestureCollection(ApplicationCommands.Undo.InputGestures));
			Redo = new RoutedUICommand("Redo", "Redo", typeof(UndoCommandManagerLoader), new InputGestureCollection(ApplicationCommands.Redo.InputGestures));
		}

		void IPlugin.OnLoaded()
		{
			MainWindow.Instance.CommandBindings.Add(new CommandBinding(Undo, UndoExecuted, UndoCanExecute));
			MainWindow.Instance.CommandBindings.Add(new CommandBinding(Redo, RedoExecuted, RedoCanExecute));
			MainWindow.Instance.Closing += MainWindow_Closing;
		}

		void MainWindow_Closing(object sender, CancelEventArgs e)
		{
			var nodes = UndoCommandManager.Instance.GetModifiedAssemblyTreeNodes().ToArray();
			if (nodes.Length != 0) {
				var msg = nodes.Length == 1 ? "There is an unsaved file." : string.Format("There are {0} unsaved files.", nodes.Length);
				var res = MainWindow.Instance.ShowMessageBox(string.Format("{0} Are you sure you want to exit?", msg), System.Windows.MessageBoxButton.YesNo);
				if (res == MsgBoxButton.No)
					e.Cancel = true;
			}
		}

		void UndoCanExecute(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = UndoCommandManager.Instance.CanUndo;
		}

		void UndoExecuted(object sender, ExecutedRoutedEventArgs e)
		{
			UndoCommandManager.Instance.Undo();
		}

		void RedoCanExecute(object sender, CanExecuteRoutedEventArgs e)
		{
			e.CanExecute = UndoCommandManager.Instance.CanRedo;
		}

		void RedoExecuted(object sender, ExecutedRoutedEventArgs e)
		{
			UndoCommandManager.Instance.Redo();
		}
	}

	public sealed class UndoCommandManager
	{
		public static readonly UndoCommandManager Instance = new UndoCommandManager();

		List<UndoState> undoCommands = new List<UndoState>();
		List<UndoState> redoCommands = new List<UndoState>();
		UndoState currentCommands;

		sealed class UndoState
		{
			public readonly Dictionary<AssemblyTreeNode, bool> ModifiedAssemblies = new Dictionary<AssemblyTreeNode, bool>();
			public readonly List<IUndoCommand> Commands = new List<IUndoCommand>();
		}

		public struct BeginEndAdder : IDisposable
		{
			readonly UndoCommandManager mgr;

			public BeginEndAdder(UndoCommandManager mgr)
			{
				this.mgr = mgr;
				mgr.BeginAddInternal();
			}

			public void Dispose()
			{
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
		public void Add(IUndoCommand command)
		{
			if (currentCommands == null) {
				using (BeginAdd())
					Add(command);
			}
			else {
				foreach (var asmNode in GetAssemblyTreeNodes(command)) {
					if (!currentCommands.ModifiedAssemblies.ContainsKey(asmNode))
						currentCommands.ModifiedAssemblies.Add(asmNode, asmNode.LoadedAssembly.IsDirty);
				}
				command.Execute();
				OnExecutedOneCommand(currentCommands);
				currentCommands.Commands.Add(command);
			}
		}

		/// <summary>
		/// Call this to add a group of commands that belong in the same group. Call the Dispose()
		/// method to stop adding more commands to the same group.
		/// </summary>
		public BeginEndAdder BeginAdd()
		{
			Debug.Assert(currentCommands == null);
			if (currentCommands != null)
				throw new InvalidOperationException();

			return new BeginEndAdder(this);
		}

		void BeginAddInternal()
		{
			Debug.Assert(currentCommands == null);
			if (currentCommands != null)
				throw new InvalidOperationException();

			currentCommands = new UndoState();
		}

		void EndAddInternal()
		{
			Debug.Assert(currentCommands != null);
			if (currentCommands == null)
				throw new InvalidOperationException();

			currentCommands.Commands.TrimExcess();
			undoCommands.Add(currentCommands);
			Clear(redoCommands);
			UpdateAssemblySavedStateRedo(currentCommands);
			currentCommands = null;
		}

		/// <summary>
		/// Clears undo and redo history
		/// </summary>
		public void Clear()
		{
			Debug.Assert(currentCommands == null);
			if (currentCommands != null)
				throw new InvalidOperationException();

			bool callGc = undoCommands.Count != 0 || redoCommands.Count != 0;
			Clear(undoCommands);
			Clear(redoCommands);

			if (callGc && !callingGc) {
				callingGc = true;
				// Some removed assemblies need to be GC'd. The AssemblyList already does this but
				// we might cache them so we need to call the GC again.
				App.Current.Dispatcher.BeginInvoke(DispatcherPriority.ContextIdle, new Action(delegate {
					GC.Collect();
					callingGc = false;
				}));
			}
		}
		bool callingGc = false;

		static void Clear(List<UndoState> list)
		{
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
		public void Undo()
		{
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
		}

		/// <summary>
		/// Undoes the previous undo command group and places it in the undo list
		/// </summary>
		public void Redo()
		{
			Debug.Assert(currentCommands == null);
			if (currentCommands != null)
				throw new InvalidOperationException();

			if (redoCommands.Count == 0)
				return;
			var group = redoCommands[redoCommands.Count - 1];
			foreach (var asmNode in group.ModifiedAssemblies.Keys.ToArray())
				group.ModifiedAssemblies[asmNode] = asmNode.LoadedAssembly.IsDirty;
			for (int i = 0; i < group.Commands.Count; i++) {
				group.Commands[i].Execute();
				OnExecutedOneCommand(group);
			}
			redoCommands.RemoveAt(redoCommands.Count - 1);
			undoCommands.Add(group);
			UpdateAssemblySavedStateRedo(group);
		}

		/// <summary>
		/// Gets all modified <see cref="AssemblyTreeNode"/>s
		/// </summary>
		/// <returns></returns>
		public IEnumerable<AssemblyTreeNode> GetModifiedAssemblyTreeNodes()
		{
			if (MainWindow.Instance.AssemblyListTreeNode == null)
				yield break;
			foreach (AssemblyTreeNode asmNode in MainWindow.Instance.AssemblyListTreeNode.Children) {
				// If it's a netmodule it has no asm children. If it's an asm and its children haven't
				// been initialized yet, they can't be modified.
				if (asmNode.Children.Count == 0 || !(asmNode.Children[0] is AssemblyTreeNode)) {
					if (IsModified(asmNode))
						yield return asmNode;
				}
				else {
					foreach (AssemblyTreeNode childNode in asmNode.Children) {
						if (IsModified(childNode))
							yield return childNode;
					}
				}
			}
		}

		public bool IsModified(AssemblyTreeNode asmNode)
		{
			return IsModified(asmNode.LoadedAssembly);
		}

		public bool IsModified(LoadedAssembly asm)
		{
			return asm.IsDirty;
		}

		internal void MarkAsModified(LoadedAssembly asm)
		{
			asm.IsDirty = true;
		}

		public void MarkAsSaved(AssemblyTreeNode asmNode)
		{
			MarkAsSaved(asmNode.LoadedAssembly);
		}

		public void MarkAsSaved(LoadedAssembly asm)
		{
			asm.IsDirty = false;
			foreach (var group in undoCommands) {
				foreach (var key in group.ModifiedAssemblies.Keys.ToArray())
					group.ModifiedAssemblies[key] = true;
			}
		}

		void UpdateAssemblySavedStateRedo(UndoState executedGroup)
		{
			foreach (var kv in executedGroup.ModifiedAssemblies)
				kv.Key.LoadedAssembly.IsDirty = true;
		}

		void UpdateAssemblySavedStateUndo(UndoState executedGroup)
		{
			foreach (var kv in executedGroup.ModifiedAssemblies) {
				// If it wasn't dirty, then it's now dirty, else use the previous value
				kv.Key.LoadedAssembly.IsDirty = !kv.Key.LoadedAssembly.IsDirty ? true : kv.Value;
			}
		}

		static IEnumerable<AssemblyTreeNode> GetAssemblyTreeNodes(IUndoCommand command)
		{
			foreach (var node in command.TreeNodes) {
				var asmNode = ILSpyTreeNode.GetNode<AssemblyTreeNode>(node);
				Debug.Assert(asmNode != null);
				if (asmNode != null)
					yield return asmNode;
			}
		}

		void OnExecutedOneCommand(UndoState group)
		{
			foreach (var asmNode in group.ModifiedAssemblies.Keys) {
				var module = asmNode.LoadedAssembly.ModuleDefinition;
				if (module != null)
					module.ResetTypeDefFindCache();
				Utils.InvalidateDecompilationCache(asmNode);
			}
		}
	}
}
