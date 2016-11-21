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

namespace dnSpy.Contracts.Command {
	/// <summary>
	/// <see cref="ICommandTargetFilter"/> order constants
	/// </summary>
	public static class CommandTargetFilterOrder {
		/// <summary>Text editor</summary>
		public const double TextEditor = 50000;

		/// <summary>Undo/redo</summary>
		public const double UndoRedo = TextEditor - 1;

		/// <summary>Search service when UI is visible</summary>
		public const double SearchServiceFocused = TextEditor - 1000000;

		/// <summary>Search service</summary>
		public const double SearchService = TextEditor - 1000;

		/// <summary>Document viewer</summary>
		public const double DocumentViewer = TextEditor - 3000;

		/// <summary>REPL editor</summary>
		public const double REPL = TextEditor - 3000;

		/// <summary>Output logger text pane</summary>
		public const double OutputTextPane = TextEditor - 3000;

		/// <summary>Edit Code</summary>
		public const double EditCode = TextEditor - 3000;

		/// <summary>Intellisense session stack</summary>
		public const double IntellisenseSessionStack = TextEditor - 4000;

		/// <summary>Default statement completion</summary>
		public const double IntellisenseDefaultStatmentCompletion = IntellisenseSessionStack - 1000;

		/// <summary>Roslyn statement completion</summary>
		public const double IntellisenseRoslynStatmentCompletion = IntellisenseDefaultStatmentCompletion - 1000;

		/// <summary>Roslyn signature help</summary>
		public const double IntellisenseRoslynSignatureHelp = IntellisenseRoslynStatmentCompletion - 1000;

		/// <summary>Roslyn quick info</summary>
		public const double IntellisenseRoslynQuickInfo = IntellisenseRoslynSignatureHelp - 1000;

		/// <summary>Hex editor</summary>
		public const double HexEditor = TextEditor;
	}
}
