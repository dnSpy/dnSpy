/*
    Copyright (C) 2014-2017 de4dot@gmail.com

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
using System.Collections.ObjectModel;

namespace dnSpy.Contracts.Debugger {
	/// <summary>
	/// Contains added or removed objects
	/// </summary>
	public struct DbgCollectionChangedEventArgs<T> {
		/// <summary>
		/// The objects that got added or removed (see <see cref="Added"/>)
		/// </summary>
		public ReadOnlyCollection<T> Objects { get; }

		/// <summary>
		/// true if <see cref="Objects"/> were added, false if they were removed
		/// </summary>
		public bool Added { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="objects">The objects that got added or removed (see <paramref name="added"/>)</param>
		/// <param name="added">true if <paramref name="objects"/> were added, false if they were removed</param>
		public DbgCollectionChangedEventArgs(ReadOnlyCollection<T> objects, bool added) {
			Objects = objects ?? throw new ArgumentNullException(nameof(objects));
			Added = added;
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="objects">The objects that got added or removed (see <paramref name="added"/>)</param>
		/// <param name="added">true if <paramref name="objects"/> were added, false if they were removed</param>
		public DbgCollectionChangedEventArgs(IList<T> objects, bool added) {
			Objects = new ReadOnlyCollection<T>(objects ?? throw new ArgumentNullException(nameof(objects)));
			Added = added;
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="obj">The object that got added or removed (see <paramref name="added"/>)</param>
		/// <param name="added">true if <paramref name="obj"/> was added, false if it was removed</param>
		public DbgCollectionChangedEventArgs(T obj, bool added) {
			if (obj == null)
				throw new ArgumentNullException(nameof(obj));
			Objects = new ReadOnlyCollection<T>(new[] { obj });
			Added = added;
		}
	}
}
