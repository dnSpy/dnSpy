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

using dnlib.DotNet;
using ICSharpCode.ILSpy.TreeNodes;

namespace ICSharpCode.ILSpy.AsmEditor.ViewHelpers
{
	interface IDnlibTypePicker
	{
		/// <summary>
		/// Asks user to pick a type, method etc in an assembly
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="filter">Decides which nodes to show to the user</param>
		/// <param name="selectedObject">null or the object that should be selected in the UI</param>
		/// <param name="ownerModule">Module owning the returned reference</param>
		/// <returns></returns>
		T GetDnlibType<T>(ITreeViewNodeFilter filter, T selectedObject, ModuleDef ownerModule) where T : class;
	}
}
