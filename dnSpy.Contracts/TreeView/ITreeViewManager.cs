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

using System;
using System.Collections.Generic;

namespace dnSpy.Contracts.TreeView {
	/// <summary>
	/// Treeview manager
	/// </summary>
	public interface ITreeViewManager {
		/// <summary>
		/// Creates a <see cref="ITreeView"/> instance
		/// </summary>
		/// <param name="guid">Guid of treeview</param>
		/// <param name="options">Treeview options</param>
		/// <returns></returns>
		ITreeView Create(Guid guid, TreeViewOptions options);

		/// <summary>
		/// Gets <see cref="ITreeNodeDataCreator"/>s
		/// </summary>
		/// <param name="guid">Guid of owner tree node (<see cref="ITreeView.Guid"/>)</param>
		/// <returns></returns>
		IEnumerable<ITreeNodeDataCreator> GetCreators(Guid guid);
	}
}
