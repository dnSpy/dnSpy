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

using System.Threading;
using dnSpy.Contracts.Hex.Editor;

namespace dnSpy.Contracts.Hex.Formatting {
	/// <summary>
	/// Creates HTML strings
	/// </summary>
	public abstract class HexHtmlBuilderService {
		/// <summary>
		/// Constructor
		/// </summary>
		protected HexHtmlBuilderService() { }

		/// <summary>
		/// Gets the default delimiter
		/// </summary>
		public virtual string DefaultDelimiter => "<br/>";

		/// <summary>
		/// Creates an HTML fragment that can be copied to the clipboard
		/// </summary>
		/// <param name="spans">Spans</param>
		/// <param name="bufferLines">Buffer lines provider</param>
		/// <param name="cancellationToken">Cancellation token</param>
		/// <returns></returns>
		public string GenerateHtmlFragment(NormalizedHexBufferSpanCollection spans, HexBufferLineProvider bufferLines, CancellationToken cancellationToken) =>
			GenerateHtmlFragment(spans, bufferLines, DefaultDelimiter, cancellationToken);

		/// <summary>
		/// Creates an HTML fragment that can be copied to the clipboard
		/// </summary>
		/// <param name="spans">Spans</param>
		/// <param name="bufferLines">Buffer lines provider</param>
		/// <param name="delimiter">Delimiter added between generated html strings</param>
		/// <param name="cancellationToken"></param>
		/// <returns></returns>
		public abstract string GenerateHtmlFragment(NormalizedHexBufferSpanCollection spans, HexBufferLineProvider bufferLines, string delimiter, CancellationToken cancellationToken);

		/// <summary>
		/// Creates an HTML fragment that can be copied to the clipboard
		/// </summary>
		/// <param name="spans">Spans</param>
		/// <param name="hexView">Hex view</param>
		/// <param name="cancellationToken">Cancellation token</param>
		/// <returns></returns>
		public string GenerateHtmlFragment(NormalizedHexBufferSpanCollection spans, HexView hexView, CancellationToken cancellationToken) =>
			GenerateHtmlFragment(spans, hexView, DefaultDelimiter, cancellationToken);

		/// <summary>
		/// Creates an HTML fragment that can be copied to the clipboard
		/// </summary>
		/// <param name="spans">Spans</param>
		/// <param name="hexView">Hex view</param>
		/// <param name="delimiter">Delimiter added between generated html strings</param>
		/// <param name="cancellationToken">Cancellation token</param>
		/// <returns></returns>
		public abstract string GenerateHtmlFragment(NormalizedHexBufferSpanCollection spans, HexView hexView, string delimiter, CancellationToken cancellationToken);
	}
}
