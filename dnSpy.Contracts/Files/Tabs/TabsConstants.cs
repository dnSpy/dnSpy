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

using dnSpy.Contracts.Files.Tabs.TextEditor;

namespace dnSpy.Contracts.Files.Tabs {
	/// <summary>
	/// Constants
	/// </summary>
	public static class TabsConstants {
		/// <summary>
		/// Order of decompile <see cref="IFileTabContentFactory"/> instance
		/// </summary>
		public const double ORDER_DECOMPILEFILETABCONTENTFACTORY = double.MaxValue;

		/// <summary>
		/// Order of default <see cref="IDecompileNode"/> instance
		/// </summary>
		public const double ORDER_DEFAULTDECOMPILENODE = double.MaxValue;

		/// <summary>
		/// Order of <see cref="IFileTabUIContextCreator"/> instance that creates <see cref="ITextEditorUIContext"/> instances
		/// </summary>
		public const double ORDER_TEXTEDITORUICONTEXTCREATOR = double.MaxValue;
	}
}
