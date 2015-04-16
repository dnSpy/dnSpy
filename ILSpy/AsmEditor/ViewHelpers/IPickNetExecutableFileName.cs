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

namespace ICSharpCode.ILSpy.AsmEditor.ViewHelpers
{
	interface IPickNetExecutableFileName
	{
		/// <summary>
		/// Lets the user pick a new filename. Returns null if the user didn't pick a new filename.
		/// </summary>
		/// <param name="currentFileName">Current filename or null</param>
		/// <param name="defaultExtension">Default extension. It must not contain a period. Eg. valid
		/// extensions are "exe" and "dll" but not ".exe"</param>
		/// <returns></returns>
		string GetFileName(string currentFileName, string defaultExtension);
	}
}
