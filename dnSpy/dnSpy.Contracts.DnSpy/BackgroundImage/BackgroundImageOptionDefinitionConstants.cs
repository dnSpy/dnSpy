/*
    Copyright (C) 2014-2017 de4dot@gmail.com

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

using dnSpy.Contracts.Documents.Tabs.DocViewer;
using dnSpy.Contracts.Text.Editor;

namespace dnSpy.Contracts.BackgroundImage {
	/// <summary>
	/// <see cref="IBackgroundImageOptionDefinition"/> constants
	/// </summary>
	public static class BackgroundImageOptionDefinitionConstants {
		/// <summary>
		/// Default order
		/// </summary>
		public const double AttrOrder_Default = 1000000;

		/// <summary>
		/// Order of <see cref="IDocumentViewer"/> definition
		/// </summary>
		public const double AttrOrder_DocumentViewer = AttrOrder_Default + 1000;

		/// <summary>
		/// Order of <see cref="IReplEditor"/> definition
		/// </summary>
		public const double AttrOrder_Repl = AttrOrder_Default + 2000;

		/// <summary>
		/// Order of <see cref="ICodeEditor"/> definition
		/// </summary>
		public const double AttrOrder_CodeEditor = AttrOrder_Default + 3000;

		/// <summary>
		/// Order of <see cref="ILogEditor"/> definition
		/// </summary>
		public const double AttrOrder_Logger = AttrOrder_Default + 4000;

		/// <summary>
		/// Order of hex editor definition
		/// </summary>
		public const double AttrOrder_HexEditor = AttrOrder_Default + 100000000;

		/// <summary>
		/// Order of hex editor (Debugger / Process Memory) definition
		/// </summary>
		public const double AttrOrder_HexEditorDebuggerMemory = AttrOrder_Default + 5000;

		/// <summary>
		/// UI order of <see cref="IDocumentViewer"/>
		/// </summary>
		public const double UIOrder_DocumentViewer = -1000;

		/// <summary>
		/// UI order of <see cref="IReplEditor"/>
		/// </summary>
		public const double UIOrder_Repl = 1000;

		/// <summary>
		/// UI order of <see cref="ICodeEditor"/>
		/// </summary>
		public const double UIOrder_CodeEditor = 2000;

		/// <summary>
		/// UI order of <see cref="ILogEditor"/>
		/// </summary>
		public const double UIOrder_Logger = 3000;

		/// <summary>
		/// UI order of hex editor
		/// </summary>
		public const double UIOrder_HexEditor = 4000;

		/// <summary>
		/// UI order of hex editor (Debugger / Process Memory)
		/// </summary>
		public const double UIOrder_HexEditorDebuggerMemory = 5000;
	}
}
