/*
    Copyright (C) 2014-2019 de4dot@gmail.com

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

namespace dnSpy.Debugger.DotNet.Metadata {
	static class ReadOnlyCollectionHelpers {
		public static ReadOnlyCollection<T> Empty<T>() => EmptyClass<T>.Empty;
		public static ReadOnlyCollection<T> Create<T>(T[] array) =>
			array == null || array.Length == 0 ? EmptyClass<T>.Empty : new ReadOnlyCollection<T>(array);
		public static ReadOnlyCollection<T> Create<T>(IList<T> list) =>
			list == null || list.Count == 0 ? EmptyClass<T>.Empty : list as ReadOnlyCollection<T> ?? new ReadOnlyCollection<T>(list);
		static class EmptyClass<T> {
			internal static readonly ReadOnlyCollection<T> Empty = new ReadOnlyCollection<T>(Array.Empty<T>());
		}
	}
}
