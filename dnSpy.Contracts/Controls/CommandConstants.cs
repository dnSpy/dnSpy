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
using System.Windows.Controls;
using dnSpy.Contracts.Files.Tabs.TextEditor;
using dnSpy.Contracts.TreeView;

namespace dnSpy.Contracts.Controls {
	/// <summary>
	/// Constants
	/// </summary>
	public static class CommandConstants {
		/// <summary>Guid of main window</summary>
		public static readonly Guid GUID_MAINWINDOW = new Guid("6C6DF6A3-2681-4A17-B81C-7EF8ABAC845C");

		/// <summary>Guid of <see cref="ITextEditorUIContext"/> UI control</summary>
		public static readonly Guid GUID_TEXTEDITOR_UICONTEXT = new Guid("AD0DD2EF-2DCB-4079-BBC4-1D678114D66A");

		/// <summary>Guid of <see cref="ITextEditorUIContext"/>'s text editor</summary>
		public static readonly Guid GUID_TEXTEDITOR_UICONTEXT_TEXTEDITOR = new Guid("B31795A9-44FA-4DCF-BEC4-5BEE981C9C9E");

		/// <summary>Guid of <see cref="ITextEditorUIContext"/>'s text area</summary>
		public static readonly Guid GUID_TEXTEDITOR_UICONTEXT_TEXTAREA = new Guid("DF8282E5-E5B0-4A57-8C8A-3B8F7D2624AA");

		/// <summary>Guid of file <see cref="ITreeView"/></summary>
		public static readonly Guid GUID_FILE_TREEVIEW = new Guid("E0ABA20F-5CD7-4CFD-A9D4-F9F3C655DD4A");

		/// <summary>Guid of analyzer <see cref="ITreeView"/></summary>
		public static readonly Guid GUID_ANALYZER_TREEVIEW = new Guid("6C62342D-8CBE-4EC4-9E05-828DDCCFE934");

		/// <summary>Guid of search control</summary>
		public static readonly Guid GUID_SEARCH_CONTROL = new Guid("D2699C68-1A08-4522-9A2D-C5DF6002F5FC");

		/// <summary>Guid of search <see cref="ListBox"/></summary>
		public static readonly Guid GUID_SEARCH_LISTBOX = new Guid("651FC97F-A9A7-4649-97AC-FC942168E6E2");

		/// <summary>Guid of debugger breakpoints control</summary>
		public static readonly Guid GUID_DEBUGGER_BREAKPOINTS_CONTROL = new Guid("00EC8F82-086C-4305-A07D-CC43CB035905");

		/// <summary>Guid of debugger breakpoints <see cref="ListView"/></summary>
		public static readonly Guid GUID_DEBUGGER_BREAKPOINTS_LISTVIEW = new Guid("E178917C-199C-4A99-95F9-9724806E528F");

		/// <summary>Guid of debugger call stack control</summary>
		public static readonly Guid GUID_DEBUGGER_CALLSTACK_CONTROL = new Guid("D0EDBB27-8367-4806-BB03-03B6990A7D32");

		/// <summary>Guid of debugger call stack <see cref="ListView"/></summary>
		public static readonly Guid GUID_DEBUGGER_CALLSTACK_LISTVIEW = new Guid("7E39E2DD-666C-4309-867E-9460D97361D2");

		/// <summary>Guid of debugger locals control</summary>
		public static readonly Guid GUID_DEBUGGER_LOCALS_CONTROL = new Guid("391EB04D-F544-459A-A242-2D856E3C6CDB");

		/// <summary>Guid of debugger locals <see cref="ListView"/></summary>
		public static readonly Guid GUID_DEBUGGER_LOCALS_LISTVIEW = new Guid("B50167E5-2AD7-44A3-B3E2-C486BD56BE3B");

		/// <summary>Guid of debugger exceptions control</summary>
		public static readonly Guid GUID_DEBUGGER_EXCEPTIONS_CONTROL = new Guid("FD139D3D-2C84-40C1-B088-11BD99840956");

		/// <summary>Guid of debugger exceptions <see cref="ListView"/></summary>
		public static readonly Guid GUID_DEBUGGER_EXCEPTIONS_LISTVIEW = new Guid("02BC921F-A601-456A-8C4F-84256C34A2A0");

		/// <summary>Guid of debugger threads control</summary>
		public static readonly Guid GUID_DEBUGGER_THREADS_CONTROL = new Guid("19AB4CCD-65AB-46B1-9855-79BDABBCDFFB");

		/// <summary>Guid of debugger threads <see cref="ListView"/></summary>
		public static readonly Guid GUID_DEBUGGER_THREADS_LISTVIEW = new Guid("44EB5AF6-D9D3-44AD-ABB0-288C6F95EE29");

		/// <summary>Guid of debugger modules control</summary>
		public static readonly Guid GUID_DEBUGGER_MODULES_CONTROL = new Guid("131B8A8D-771B-46DE-A8D4-20D4BBEBF2B1");

		/// <summary>Guid of debugger modules <see cref="ListView"/></summary>
		public static readonly Guid GUID_DEBUGGER_MODULES_LISTVIEW = new Guid("F91D9EA8-614D-4B36-AE27-B4EA541F6992");

		/// <summary>Guid of debugger memory control</summary>
		public static readonly Guid GUID_DEBUGGER_MEMORY_CONTROL = new Guid("D638F6E0-EA1E-4E2C-9969-A14751C800D1");

		/// <summary>Guid of debugger memory <c>HexBox</c></summary>
		public static readonly Guid GUID_DEBUGGER_MEMORY_HEXBOX = new Guid("34F1BAA4-36F1-4687-8552-7D71BDDBC1F3");

		/// <summary>Guid of C# Interactive's text editor</summary>
		public static readonly Guid GUID_REPL_CSHARP_TEXTEDITOR = new Guid("29157769-57F7-40B1-BE9B-F790A31049B9");

		/// <summary>Guid of C# Interactive's text area</summary>
		public static readonly Guid GUID_REPL_CSHARP_TEXTAREA = new Guid("C34EA6FC-E0AA-465F-A1C9-EA82DB3E9550");

		/// <summary>Guid of Visual Basic Interactive's text editor</summary>
		public static readonly Guid GUID_REPL_VISUAL_BASIC_TEXTEDITOR = new Guid("FA8135DF-F0FE-41CD-8C06-328D0FE3DAE9");

		/// <summary>Guid of Visual Basic Interactive's text area</summary>
		public static readonly Guid GUID_REPL_VISUAL_BASIC_TEXTAREA = new Guid("198589C0-D2F3-4672-A472-15DF63EFF47F");
	}
}
