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

using System.Collections.Generic;

namespace dnSpy.AsmEditor.UndoRedo {
	/// <summary>
	/// An assembly editor command that can be undone. Dispose() is called when the history is cleared.
	/// </summary>
	interface IUndoCommand {
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
		IEnumerable<object> ModifiedObjects { get; }
	}

	interface IUndoCommand2 : IUndoCommand {
		IEnumerable<object> NonModifiedObjects { get; }
	}

	interface IGCUndoCommand : IUndoCommand {
		bool CallGarbageCollectorAfterDispose { get; }
	}
}
