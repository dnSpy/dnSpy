
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using dnlib.DotNet;
using ICSharpCode.ILSpy.TreeNodes;

namespace ICSharpCode.ILSpy.AsmEditor
{
	public sealed class UndoCommandManager
	{
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
				foreach (var asmNode in GetAssemblyTreeNodes(command))
					currentCommands.ModifiedAssemblies[asmNode] = asmNode.LoadedAssembly.IsDirty;
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
			redoCommands.Clear();
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

			undoCommands.Clear();
			undoCommands.TrimExcess();
			redoCommands.Clear();
			redoCommands.TrimExcess();
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

		public void MarkAsSaved(AssemblyTreeNode asmNode)
		{
			MarkAsSaved(asmNode.LoadedAssembly);
		}

		public void MarkAsSaved(LoadedAssembly asm)
		{
			asm.IsDirty = false;
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
				var asmNode = ILSpyTreeNode.GetAssemblyTreeNode(node);
				Debug.Assert(asmNode != null);
				if (asmNode != null)
					yield return asmNode;
			}
		}

		void OnExecutedOneCommand(UndoState group)
		{
			foreach (var asmNode in group.ModifiedAssemblies.Keys)
				asmNode.LoadedAssembly.ModuleDefinition.ResetTypeDefFindCache();
		}
	}
}
