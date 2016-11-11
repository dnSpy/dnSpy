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
using System.Threading;

namespace dnSpy.Contracts.Hex.Tagging {
	/// <summary>
	/// Tagger
	/// </summary>
	public abstract class HexTagger<T> where T : HexTag {
		/// <summary>
		/// Constructor
		/// </summary>
		protected HexTagger() { }

		/// <summary>
		/// Raised after tags have changed
		/// </summary>
		public abstract event EventHandler<HexBufferSpanEventArgs> TagsChanged;

		/// <summary>
		/// Gets all tags intersecting with <paramref name="spans"/>
		/// </summary>
		/// <param name="spans">Spans</param>
		/// <returns></returns>
		public abstract IEnumerable<HexTagSpan<T>> GetTags(NormalizedHexBufferSpanCollection spans);

		/// <summary>
		/// Gets all tags intersecting with <paramref name="spans"/>. This method is synchronous.
		/// </summary>
		/// <param name="spans">Spans</param>
		/// <param name="cancellationToken">Cancellation token</param>
		/// <returns></returns>
		public virtual IEnumerable<HexTagSpan<T>> GetTags(NormalizedHexBufferSpanCollection spans, CancellationToken cancellationToken) => GetTags(spans);
	}
}
