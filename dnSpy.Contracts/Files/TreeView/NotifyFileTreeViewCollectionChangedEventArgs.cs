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
using System.Diagnostics;

namespace dnSpy.Contracts.Files.TreeView {
	/// <summary>
	/// Event args
	/// </summary>
	public sealed class NotifyFileTreeViewCollectionChangedEventArgs : EventArgs {
		/// <summary>
		/// Event type
		/// </summary>
		public NotifyFileTreeViewCollection Type { get; private set; }

		/// <summary>
		/// All file nodes
		/// </summary>
		public IDnSpyFileNode[] Nodes { get; private set; }

		NotifyFileTreeViewCollectionChangedEventArgs() {
		}

		/// <summary>
		/// Creates a <see cref="NotifyFileTreeViewCollection.Clear"/> instance
		/// </summary>
		/// <param name="clearedFiles">All cleared files</param>
		/// <returns></returns>
		public static NotifyFileTreeViewCollectionChangedEventArgs CreateClear(IDnSpyFileNode[] clearedFiles) {
			Debug.Assert(clearedFiles != null);
			var e = new NotifyFileTreeViewCollectionChangedEventArgs();
			e.Type = NotifyFileTreeViewCollection.Clear;
			e.Nodes = clearedFiles;
			return e;
		}

		/// <summary>
		/// Creates a <see cref="NotifyFileTreeViewCollection.Add"/> instance
		/// </summary>
		/// <param name="file">Added file</param>
		/// <returns></returns>
		public static NotifyFileTreeViewCollectionChangedEventArgs CreateAdd(IDnSpyFileNode file) {
			Debug.Assert(file != null);
			var e = new NotifyFileTreeViewCollectionChangedEventArgs();
			e.Type = NotifyFileTreeViewCollection.Add;
			e.Nodes = new IDnSpyFileNode[] { file };
			return e;
		}

		/// <summary>
		/// Creates a <see cref="NotifyFileTreeViewCollection.Remove"/> instance
		/// </summary>
		/// <param name="files">Removed files</param>
		/// <returns></returns>
		public static NotifyFileTreeViewCollectionChangedEventArgs CreateRemove(IDnSpyFileNode[] files) {
			Debug.Assert(files != null);
			var e = new NotifyFileTreeViewCollectionChangedEventArgs();
			e.Type = NotifyFileTreeViewCollection.Remove;
			e.Nodes = files;
			return e;
		}
	}
}
