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
using System.Collections;
using System.Collections.Generic;

namespace dnSpy.Contracts.Hex {
	/// <summary>
	/// Hex buffer tags
	/// </summary>
	public sealed class HexTags : IEnumerable<string> {
		readonly HashSet<string> tags;

		internal HexTags(IEnumerable<string> tags) {
			if (tags == null)
				throw new ArgumentNullException(nameof(tags));
			this.tags = new HashSet<string>(tags, StringComparer.OrdinalIgnoreCase);
		}

		/// <summary>
		/// Checks whether <paramref name="tag"/> exists in the collection
		/// </summary>
		/// <param name="tag">Tag</param>
		/// <returns></returns>
		public bool Contains(string tag) => tags.Contains(tag);

		/// <summary>
		/// Gets all tags
		/// </summary>
		/// <returns></returns>
		public IEnumerator<string> GetEnumerator() {
			foreach (var tag in tags)
				yield return tag;
		}

		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
	}
}
