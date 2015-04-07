
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
		List<IUndoCommand[]> undoCommands = new List<IUndoCommand[]>();
		List<IUndoCommand[]> redoCommands = new List<IUndoCommand[]>();
		List<IUndoCommand> currentCommands;

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
				var modules = GetModules(command);
				command.Execute();
				OnExecuted(command, modules);
				currentCommands.Add(command);
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

			currentCommands = new List<IUndoCommand>();
		}

		void EndAddInternal()
		{
			Debug.Assert(currentCommands != null);
			if (currentCommands == null)
				throw new InvalidOperationException();

			undoCommands.Add(currentCommands.ToArray());
			redoCommands.Clear();
			currentCommands = null;
		}

		/// <summary>
		/// Clears undo and redo history
		/// </summary>
		public void Clear()
		{
			ClearUndo();
			ClearRedo();
		}

		/// <summary>
		/// Clears undo history
		/// </summary>
		public void ClearUndo()
		{
			Debug.Assert(currentCommands == null);
			if (currentCommands != null)
				throw new InvalidOperationException();

			undoCommands.Clear();
		}

		/// <summary>
		/// Clears redo history
		/// </summary>
		public void ClearRedo()
		{
			Debug.Assert(currentCommands == null);
			if (currentCommands != null)
				throw new InvalidOperationException();

			redoCommands.Clear();
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
			for (int i = group.Length - 1; i >= 0; i--) {
				var command = group[i];
				var modules = GetModules(command);
				command.Undo();
				OnExecuted(command, modules);
			}
			undoCommands.RemoveAt(undoCommands.Count - 1);
			redoCommands.Add(group);
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
			for (int i = 0; i < group.Length; i++) {
				var command = group[i];
				var modules = GetModules(command);
				command.Execute();
				OnExecuted(command, modules);
			}
			redoCommands.RemoveAt(redoCommands.Count - 1);
			undoCommands.Add(group);
		}

		static HashSet<ModuleDef> GetModules(IUndoCommand command, HashSet<ModuleDef> modules = null)
		{
			if (modules == null)
				modules = new HashSet<ModuleDef>();
			foreach (var node in command.TreeNodes) {
				var module = ILSpyTreeNode.GetModule(node);
				if (module != null)	// null if it's been removed
					modules.Add(module);
			}
			return modules;
		}

		void OnExecuted(IUndoCommand command, HashSet<ModuleDef> modules)
		{
			foreach (var module in GetModules(command, modules))
				module.ResetTypeDefFindCache();
		}
	}
}
