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
using System.Threading;

namespace dnSpy.Contracts.Hex.Tagging {
	/// <summary>
	/// Tag aggregator
	/// </summary>
	/// <typeparam name="T">Tag type</typeparam>
	public abstract class HexTagAggregator<T> : IDisposable where T : HexTag {
		/// <summary>
		/// Constructor
		/// </summary>
		protected HexTagAggregator() { }

		/// <summary>
		/// Gets the buffer
		/// </summary>
		public abstract HexBuffer Buffer { get; }

		/// <summary>
		/// Raised after tags have changed
		/// </summary>
		public abstract event EventHandler<HexTagsChangedEventArgs> TagsChanged;

		/// <summary>
		/// Raised on the original thread after tags have changed
		/// </summary>
		public abstract event EventHandler<HexBatchedTagsChangedEventArgs> BatchedTagsChanged;

		/// <summary>
		/// Returns all tags intersecting with <paramref name="span"/>
		/// </summary>
		/// <param name="span">Span</param>
		/// <returns></returns>
		public IEnumerable<IHexTagSpan<T>> GetTags(HexBufferSpan span) =>
			GetTags(new NormalizedHexBufferSpanCollection(span));

		/// <summary>
		/// Returns all tags intersecting with <paramref name="spans"/>
		/// </summary>
		/// <param name="spans">Span</param>
		/// <returns></returns>
		public abstract IEnumerable<IHexTagSpan<T>> GetTags(NormalizedHexBufferSpanCollection spans);

		/// <summary>
		/// Returns all tags intersecting with <paramref name="span"/>. This method is synchronous.
		/// </summary>
		/// <param name="span">Span</param>
		/// <param name="cancellationToken">Cancellation token</param>
		/// <returns></returns>
		public IEnumerable<IHexTagSpan<T>> GetTags(HexBufferSpan span, CancellationToken cancellationToken) =>
			GetTags(new NormalizedHexBufferSpanCollection(span), cancellationToken);

		/// <summary>
		/// Returns all tags intersecting with <paramref name="spans"/>. This method is synchronous.
		/// </summary>
		/// <param name="spans">Span</param>
		/// <param name="cancellationToken">Cancellation token</param>
		/// <returns></returns>
		public abstract IEnumerable<IHexTagSpan<T>> GetTags(NormalizedHexBufferSpanCollection spans, CancellationToken cancellationToken);

		/// <summary>
		/// Gets all tags intersecting with the line
		/// </summary>
		/// <param name="context">Context</param>
		/// <returns></returns>
		public abstract IEnumerable<IHexTextTagSpan<T>> GetLineTags(HexTaggerContext context);

		/// <summary>
		/// Gets all tags intersecting with the line. This method is synchronous.
		/// </summary>
		/// <param name="context">Context</param>
		/// <param name="cancellationToken">Cancellation token</param>
		/// <returns></returns>
		public abstract IEnumerable<IHexTextTagSpan<T>> GetLineTags(HexTaggerContext context, CancellationToken cancellationToken);

		/// <summary>
		/// Gets all tags intersecting with the line. It merges all <see cref="IHexTagSpan{T}"/> tags with all
		/// <see cref="IHexTextTagSpan{T}"/> tags.
		/// </summary>
		/// <param name="context">Context</param>
		/// <returns></returns>
		public abstract IEnumerable<IHexTextTagSpan<T>> GetAllTags(HexTaggerContext context);

		/// <summary>
		/// Gets all tags intersecting with the line. It merges all <see cref="IHexTagSpan{T}"/> tags with all
		/// <see cref="IHexTextTagSpan{T}"/> tags. This method is synchronous.
		/// </summary>
		/// <param name="context">Context</param>
		/// <param name="cancellationToken">Cancellation token</param>
		/// <returns></returns>
		public abstract IEnumerable<IHexTextTagSpan<T>> GetAllTags(HexTaggerContext context, CancellationToken cancellationToken);

		/// <summary>
		/// Disposes this instance
		/// </summary>
		public void Dispose() => DisposeCore();

		/// <summary>
		/// Disposes this instance
		/// </summary>
		protected virtual void DisposeCore() { }
	}
}
