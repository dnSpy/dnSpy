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
using System.Collections.Generic;

namespace dnSpy.Contracts.Text.Tagging {
	/// <summary>
	/// A provider of tags over a buffer
	/// </summary>
	/// <typeparam name="T">The type of tags to generate</typeparam>
	public interface ITagger<out T> where T : ITag {
		/// <summary>
		/// Occurs when tags are added to or removed from the provider
		/// </summary>
		event EventHandler<SnapshotSpanEventArgs> TagsChanged;

		/// <summary>
		/// Gets all the tags that intersect the specified spans
		/// </summary>
		/// <param name="spans">Span collection</param>
		/// <returns></returns>
		IEnumerable<ITagSpan<T>> GetTags(NormalizedSnapshotSpanCollection spans);
	}
}
