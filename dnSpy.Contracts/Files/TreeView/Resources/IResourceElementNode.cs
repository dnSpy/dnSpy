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

using dnlib.DotNet.Resources;

namespace dnSpy.Contracts.Files.TreeView.Resources {
	/// <summary>
	/// A resource created from a <see cref="ResourceElement"/>
	/// </summary>
	public interface IResourceElementNode : IFileTreeNodeData, IResourceDataProvider {
		/// <summary>
		/// Gets the resource element
		/// </summary>
		ResourceElement ResourceElement { get; }

		/// <summary>
		/// Gets the name
		/// </summary>
		string Name { get; }

		/// <summary>
		/// Checks whether <see cref="UpdateData(ResourceElement)"/> can execute. Used by the
		/// assembly editor. Returns null or an empty string if the data can be updated, else an
		/// error string that can be shown to the user.
		/// </summary>
		/// <param name="newResElem">New data</param>
		/// <returns></returns>
		string CheckCanUpdateData(ResourceElement newResElem);

		/// <summary>
		/// Updates the internal resource data. Must only be called if
		/// <see cref="CheckCanUpdateData(ResourceElement)"/> returned true. Used by the assembly
		/// editor.
		/// </summary>
		/// <param name="newResElem">New data</param>
		void UpdateData(ResourceElement newResElem);
	}
}
