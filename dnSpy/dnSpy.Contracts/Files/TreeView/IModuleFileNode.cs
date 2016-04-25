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

using dnSpy.Contracts.TreeView;

namespace dnSpy.Contracts.Files.TreeView {
	/// <summary>
	/// A .NET module file
	/// </summary>
	public interface IModuleFileNode : IDnSpyFileNode, IMDTokenNode {
		/// <summary>
		/// Gets the <see cref="IDnSpyFile"/> instance
		/// </summary>
		new IDnSpyDotNetFile DnSpyFile { get; }

		/// <summary>
		/// Creates a <see cref="INamespaceNode"/>
		/// </summary>
		/// <param name="name">Name</param>
		/// <returns></returns>
		INamespaceNode Create(string name);

		/// <summary>
		/// Returns an existing <see cref="INamespaceNode"/> instance or null
		/// </summary>
		/// <param name="ns">Namespace</param>
		/// <returns></returns>
		INamespaceNode FindNode(string ns);
	}
}
