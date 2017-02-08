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

using System.Collections.Generic;
using System.Threading;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Tagging;

namespace dnSpy.Contracts.Text.Tagging {
	/// <summary>
	/// Synchronous <see cref="ITagAggregator{T}"/>
	/// </summary>
	/// <typeparam name="T">Tag type</typeparam>
	interface ISynchronousTagAggregator<out T> : ITagAggregator<T> where T : ITag {
		/// <summary>
		/// Gets all the tags
		/// </summary>
		/// <param name="span">Span to tag</param>
		/// <param name="cancellationToken">Cancellation token</param>
		/// <returns></returns>
		IEnumerable<IMappingTagSpan<T>> GetTags(SnapshotSpan span, CancellationToken cancellationToken);

		/// <summary>
		/// Gets all the tags
		/// </summary>
		/// <param name="span">Span to tag</param>
		/// <param name="cancellationToken">Cancellation token</param>
		/// <returns></returns>
		IEnumerable<IMappingTagSpan<T>> GetTags(IMappingSpan span, CancellationToken cancellationToken);

		/// <summary>
		/// Gets all the tags
		/// </summary>
		/// <param name="snapshotSpans">Spans to tag</param>
		/// <param name="cancellationToken">Cancellation token</param>
		/// <returns></returns>
		IEnumerable<IMappingTagSpan<T>> GetTags(NormalizedSnapshotSpanCollection snapshotSpans, CancellationToken cancellationToken);
	}
}
