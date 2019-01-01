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
using System.Collections.ObjectModel;

namespace dnSpy.Contracts.Bookmarks {
	/// <summary>
	/// Bookmark settings
	/// </summary>
	public struct BookmarkSettings : IEquatable<BookmarkSettings> {
		/// <summary>
		/// true if the bookmark is enabled
		/// </summary>
		public bool IsEnabled { get; set; }

		/// <summary>
		/// Name of the bookmark
		/// </summary>
		public string Name { get; set; }

		/// <summary>
		/// Labels
		/// </summary>
		public ReadOnlyCollection<string> Labels { get; set; }

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
		public static bool operator ==(BookmarkSettings left, BookmarkSettings right) => left.Equals(right);
		public static bool operator !=(BookmarkSettings left, BookmarkSettings right) => !left.Equals(right);
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member

		/// <summary>
		/// Compares this instance to <paramref name="other"/>
		/// </summary>
		/// <param name="other">Other instance</param>
		/// <returns></returns>
		public bool Equals(BookmarkSettings other) =>
			IsEnabled == other.IsEnabled &&
			StringComparer.Ordinal.Equals(Name ?? string.Empty, other.Name ?? string.Empty) &&
			LabelsEquals(Labels, other.Labels);

		static bool LabelsEquals(ReadOnlyCollection<string> a, ReadOnlyCollection<string> b) {
			if (a == null)
				a = emptyLabels;
			if (b == null)
				b = emptyLabels;
			if (a == b)
				return true;
			if (a.Count != b.Count)
				return false;
			for (int i = 0; i < a.Count; i++) {
				if (!StringComparer.Ordinal.Equals(a[i], b[i]))
					return false;
			}
			return true;
		}
		static readonly ReadOnlyCollection<string> emptyLabels = new ReadOnlyCollection<string>(Array.Empty<string>());

		static int LabelsGetHashCode(ReadOnlyCollection<string> a) {
			int hc = 0;
			foreach (var s in a ?? emptyLabels)
				hc ^= StringComparer.Ordinal.GetHashCode(s ?? string.Empty);
			return hc;
		}

		/// <summary>
		/// Compares this instance to <paramref name="obj"/>
		/// </summary>
		/// <param name="obj">Other instance</param>
		/// <returns></returns>
		public override bool Equals(object obj) => obj is BookmarkSettings other && Equals(other);

		/// <summary>
		/// Gets the hash code
		/// </summary>
		/// <returns></returns>
		public override int GetHashCode() =>
			(IsEnabled ? 1 : 0) ^
			StringComparer.Ordinal.GetHashCode(Name ?? string.Empty) ^
			LabelsGetHashCode(Labels);
	}
}
