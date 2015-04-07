
using System.Collections.Generic;
using ICSharpCode.ILSpy.TreeNodes;

namespace ICSharpCode.ILSpy.AsmEditor
{
	public interface IUndoCommand
	{
		/// <summary>
		/// Gets a description of the command
		/// </summary>
		string Description { get; }

		/// <summary>
		/// Executes the command
		/// </summary>
		void Execute();

		/// <summary>
		/// Undoes what <see cref="Execute()"/> did
		/// </summary>
		void Undo();

		/// <summary>
		/// Gets all tree nodes it operates on. The implementer can decide to only return one node
		/// if all the other nodes it operates on are (grand) children of that node.
		/// </summary>
		IEnumerable<ILSpyTreeNode> TreeNodes { get; }
	}
}
