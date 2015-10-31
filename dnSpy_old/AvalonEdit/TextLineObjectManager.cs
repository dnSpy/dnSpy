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
using System.Linq;

namespace dnSpy.AvalonEdit {
	public class TextLineObjectListModifiedEventArgs : EventArgs {
		/// <summary>
		/// Added/removed object
		/// </summary>
		public ITextLineObject TextLineObject { get; private set; }

		/// <summary>
		/// true if added, false if removed
		/// </summary>
		public bool Added { get; private set; }

		public TextLineObjectListModifiedEventArgs(ITextLineObject obj, bool added) {
			this.TextLineObject = obj;
			this.Added = added;
		}
	}

	/// <summary>
	/// Keeps track of text line objects, eg. code breakpoints, current line markers, etc
	/// </summary>
	public class TextLineObjectManager {
		public static readonly TextLineObjectManager Instance = new TextLineObjectManager();

		readonly HashSet<ITextLineObject> objects = new HashSet<ITextLineObject>();

		public event EventHandler<TextLineObjectListModifiedEventArgs> OnListModified;

		public ITextLineObject[] Objects {
			get { return objects.ToArray(); }
		}

		public T[] GetObjectsOfType<T>() where T : ITextLineObject {
			return objects.OfType<T>().ToArray();
		}

		TextLineObjectManager() {
		}

		public ITextLineObject Add(ITextLineObject obj) {
			if (obj == null)
				return obj;
			if (objects.Contains(obj))
				return obj;

			objects.Add(obj);

			if (OnListModified != null)
				OnListModified(this, new TextLineObjectListModifiedEventArgs(obj, true));

			return obj;
		}

		public void Remove(ITextLineObject obj) {
			if (obj == null)
				return;
			if (!objects.Remove(obj))
				return;

			if (OnListModified != null)
				OnListModified(this, new TextLineObjectListModifiedEventArgs(obj, false));
		}
	}
}
