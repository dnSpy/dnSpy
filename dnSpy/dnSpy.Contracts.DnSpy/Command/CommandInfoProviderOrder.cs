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
	/// <see cref="ICommandInfoProvider"/> order constants
	/// </summary>
	public static class CommandInfoProviderOrder {
		/// <summary>Default</summary>
		public const double Default = 100000;

		/// <summary>Text editor</summary>
		public const double TextEditor = 50000;

		/// <summary>Text references</summary>
		public const double TextReferences = TextEditor - 2000;

		/// <summary>Document viewer</summary>
		public const double DocumentViewer = TextEditor - 3000;

		/// <summary>REPL editor</summary>
		public const double REPL = TextEditor - 3000;

		/// <summary>Output logger text pane</summary>
		public const double OutputTextPane = TextEditor - 3000;

		/// <summary>Edit Code</summary>
		public const double EditCode = TextEditor - 3000;
	}
}
