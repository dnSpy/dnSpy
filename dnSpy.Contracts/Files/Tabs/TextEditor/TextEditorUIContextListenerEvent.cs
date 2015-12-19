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

using ICSharpCode.Decompiler;

namespace dnSpy.Contracts.Files.Tabs.TextEditor {
	/// <summary>
	/// <see cref="TextEditorUIContextListener"/> event
	/// </summary>
	public enum TextEditorUIContextListenerEvent {
		/// <summary>
		/// A <see cref="ITextEditorUIContext"/> has been created
		/// </summary>
		Added,

		/// <summary>
		/// A <see cref="ITextEditorUIContext"/> has been removed (eg. tab was closed). This event
		/// isn't raised if the <see cref="ITextEditorUIContext"/> instance has already been GC'd.
		/// </summary>
		Removed,

		/// <summary>
		/// New content has been added to the text editor. The <c>data</c> argument is a
		/// <see cref="ITextOutput"/>, most likely an <c>AvalonEditTextOutput</c>
		/// </summary>
		NewContent,
	}
}
