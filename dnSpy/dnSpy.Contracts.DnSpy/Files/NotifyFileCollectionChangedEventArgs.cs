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

namespace dnSpy.Contracts.Files {
	/// <summary>
	/// Event args
	/// </summary>
	public sealed class NotifyFileCollectionChangedEventArgs : EventArgs {
		/// <summary>
		/// Event type
		/// </summary>
		public NotifyFileCollectionType Type { get; private set; }

		/// <summary>
		/// All files
		/// </summary>
		public IDnSpyFile[] Files { get; private set; }

		/// <summary>
		/// User data
		/// </summary>
		public object Data { get; private set; }

		NotifyFileCollectionChangedEventArgs() {
		}

		/// <summary>
		/// Creates a <see cref="NotifyFileCollectionType.Clear"/> instance
		/// </summary>
		/// <param name="clearedFiles">All cleared files</param>
		/// <param name="data">Data to send to listeners</param>
		/// <returns></returns>
		public static NotifyFileCollectionChangedEventArgs CreateClear(IDnSpyFile[] clearedFiles, object data) {
			if (clearedFiles == null)
				throw new ArgumentNullException();
			var e = new NotifyFileCollectionChangedEventArgs();
			e.Type = NotifyFileCollectionType.Clear;
			e.Files = clearedFiles;
			e.Data = data;
			return e;
		}

		/// <summary>
		/// Creates a <see cref="NotifyFileCollectionType.Add"/> instance
		/// </summary>
		/// <param name="file">Added file</param>
		/// <param name="data">Data to send to listeners</param>
		/// <returns></returns>
		public static NotifyFileCollectionChangedEventArgs CreateAdd(IDnSpyFile file, object data) {
			if (file == null)
				throw new ArgumentNullException();
			var e = new NotifyFileCollectionChangedEventArgs();
			e.Type = NotifyFileCollectionType.Add;
			e.Files = new IDnSpyFile[] { file };
			e.Data = data;
			return e;
		}

		/// <summary>
		/// Creates a <see cref="NotifyFileCollectionType.Remove"/> instance
		/// </summary>
		/// <param name="file">Removed file</param>
		/// <param name="data">Data to send to listeners</param>
		/// <returns></returns>
		public static NotifyFileCollectionChangedEventArgs CreateRemove(IDnSpyFile file, object data) {
			if (file == null)
				throw new ArgumentNullException();
			return CreateRemove(new[] { file }, data);
		}

		/// <summary>
		/// Creates a <see cref="NotifyFileCollectionType.Remove"/> instance
		/// </summary>
		/// <param name="files">Removed files</param>
		/// <param name="data">Data to send to listeners</param>
		/// <returns></returns>
		public static NotifyFileCollectionChangedEventArgs CreateRemove(IDnSpyFile[] files, object data) {
			if (files == null)
				throw new ArgumentNullException();
			var e = new NotifyFileCollectionChangedEventArgs();
			e.Type = NotifyFileCollectionType.Remove;
			e.Files = files;
			e.Data = data;
			return e;
		}
	}
}
