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

namespace dnSpy.Contracts.Hex.Operations {
	/// <summary>
	/// Search service
	/// </summary>
	public abstract class HexSearchService {
		/// <summary>
		/// Constructor
		/// </summary>
		protected HexSearchService() { }

		/// <summary>
		/// Finds the pattern
		/// </summary>
		/// <param name="startingPosition">Starting position</param>
		/// <param name="options">Options</param>
		/// <param name="cancellationToken">Cancellation token</param>
		/// <returns></returns>
		public HexBufferSpan? Find(HexBufferPoint startingPosition, HexFindOptions options, CancellationToken cancellationToken) {
			if (startingPosition.IsDefault)
				throw new ArgumentException();
			var buffer = startingPosition.Buffer;
			return Find(new HexBufferSpan(buffer, buffer.Span), startingPosition, options, cancellationToken);
		}

		/// <summary>
		/// Finds the pattern
		/// </summary>
		/// <param name="searchRange">Search range</param>
		/// <param name="startingPosition">Starting position</param>
		/// <param name="options">Options</param>
		/// <param name="cancellationToken">Cancellation token</param>
		/// <returns></returns>
		public HexBufferSpan? Find(HexBufferSpan searchRange, HexBufferPoint startingPosition, HexFindOptions options, CancellationToken cancellationToken) {
			foreach (var span in FindAll(searchRange, startingPosition, options, cancellationToken))
				return span;
			return null;
		}

		/// <summary>
		/// Finds all matches
		/// </summary>
		/// <param name="searchRange">Search range</param>
		/// <param name="options">Options</param>
		/// <param name="cancellationToken">Cancellation token</param>
		/// <returns></returns>
		public IEnumerable<HexBufferSpan> FindAll(HexBufferSpan searchRange, HexFindOptions options, CancellationToken cancellationToken) =>
			FindAll(searchRange, searchRange.Start, options, cancellationToken);

		/// <summary>
		/// Finds all matches
		/// </summary>
		/// <param name="searchRange">Search range</param>
		/// <param name="startingPosition">Starting position</param>
		/// <param name="options">Options</param>
		/// <param name="cancellationToken">Cancellation token</param>
		/// <returns></returns>
		public abstract IEnumerable<HexBufferSpan> FindAll(HexBufferSpan searchRange, HexBufferPoint startingPosition, HexFindOptions options, CancellationToken cancellationToken);
	}
}
