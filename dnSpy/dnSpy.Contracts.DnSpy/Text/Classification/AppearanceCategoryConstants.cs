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

namespace dnSpy.Contracts.Text.Classification {
	/// <summary>
	/// Appearance category constants
	/// </summary>
	static class AppearanceCategoryConstants {
		/// <summary>
		/// Default text editor
		/// </summary>
		public const string TextEditor = "dnSpy-" + nameof(TextEditor);

		/// <summary>
		/// Decompiled code and other content shown in the main tabs
		/// </summary>
		public const string Viewer = "dnSpy-" + nameof(Viewer);

		/// <summary>
		/// REPL
		/// </summary>
		public const string REPL = "dnSpy-" + nameof(REPL);

		/// <summary>
		/// Code editor
		/// </summary>
		public const string CodeEditor = "dnSpy-" + nameof(CodeEditor);

		/// <summary>
		/// Quick info tooltip
		/// </summary>
		public const string QuickInfoToolTip = "dnSpy-" + nameof(QuickInfoToolTip);

		/// <summary>
		/// Code completion
		/// </summary>
		public const string CodeCompletion = "dnSpy-" + nameof(CodeCompletion);

		/// <summary>
		/// Code completion tooltip
		/// </summary>
		public const string CodeCompletionToolTip = "dnSpy-" + nameof(CodeCompletionToolTip);

		/// <summary>
		/// Signature help tooltip
		/// </summary>
		public const string SignatureHelpToolTip = "dnSpy-" + nameof(SignatureHelpToolTip);

		/// <summary>
		/// Search
		/// </summary>
		public const string Search = "dnSpy-" + nameof(Search);

		/// <summary>
		/// Tabs dialog box
		/// </summary>
		public const string TabsDialog = "dnSpy-" + nameof(TabsDialog);

		/// <summary>
		/// GAC dialog box
		/// </summary>
		public const string GacDialog = "dnSpy-" + nameof(GacDialog);

		/// <summary>
		/// Document list dialog box
		/// </summary>
		public const string DocListDialog = "dnSpy-" + nameof(DocListDialog);

		/// <summary>
		/// Breakpoints window
		/// </summary>
		public const string BreakpointsWindow = "dnSpy-" + nameof(BreakpointsWindow);

		/// <summary>
		/// Call Stack window
		/// </summary>
		public const string CallStackWindow = "dnSpy-" + nameof(CallStackWindow);

		/// <summary>
		/// Attach to Process window
		/// </summary>
		public const string AttachToProcessWindow = "dnSpy-" + nameof(AttachToProcessWindow);

		/// <summary>
		/// Exception Settings window
		/// </summary>
		public const string ExceptionSettingsWindow = "dnSpy-" + nameof(ExceptionSettingsWindow);

		/// <summary>
		/// Locals window
		/// </summary>
		public const string LocalsWindow = "dnSpy-" + nameof(LocalsWindow);

		/// <summary>
		/// Modules window
		/// </summary>
		public const string ModulesWindow = "dnSpy-" + nameof(ModulesWindow);

		/// <summary>
		/// Threads window
		/// </summary>
		public const string ThreadsWindow = "dnSpy-" + nameof(ThreadsWindow);
	}
}
