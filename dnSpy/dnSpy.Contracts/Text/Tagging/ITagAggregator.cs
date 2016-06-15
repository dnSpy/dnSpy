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
	/// Aggregates all the tag providers in a buffer graph for the specified type of tag
	/// </summary>
	/// <typeparam name="T">The type of tag returned by the aggregator</typeparam>
	public interface ITagAggregator<out T> : IDisposable where T : ITag {
		/// <summary>
		/// Gets all the tags that overlap or are contained by the specified <paramref name="span"/> of the same type as the aggregator
		/// </summary>
		/// <param name="span">Span</param>
		/// <returns></returns>
		IEnumerable<IMappingTagSpan<T>> GetTags(SnapshotSpan span);

		/// <summary>
		/// Gets all the tags that overlap or are contained by the specified <paramref name="span"/> of the type of the aggregator
		/// </summary>
		/// <param name="span">Span</param>
		/// <returns></returns>
		IEnumerable<IMappingTagSpan<T>> GetTags(IMappingSpan span);

		/// <summary>
		/// Gets all the tags that overlap or are contained by the specified <paramref name="snapshotSpans"/> of the type of the aggregator
		/// </summary>
		/// <param name="snapshotSpans">Spans</param>
		/// <returns></returns>
		IEnumerable<IMappingTagSpan<T>> GetTags(NormalizedSnapshotSpanCollection snapshotSpans);

		/// <summary>
		/// Occurs on idle after one or more <see cref="TagsChanged"/> events
		/// </summary>
		event EventHandler<BatchedTagsChangedEventArgs> BatchedTagsChanged;

		/// <summary>
		/// Occurs when tags are added to or removed from providers
		/// </summary>
		event EventHandler<TagsChangedEventArgs> TagsChanged;
	}
}
