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
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;

namespace dnSpy.Contracts.Text.Formatting {
	/// <summary>
	/// Creates HTML strings
	/// </summary>
	interface IHtmlBuilderService {
		/// <summary>
		/// Creates an HTML fragment that can be copied to the clipboard
		/// </summary>
		/// <param name="spans">Spans</param>
		/// <param name="cancellationToken">Cancellation token</param>
		/// <returns></returns>
		string GenerateHtmlFragment(NormalizedSnapshotSpanCollection spans, CancellationToken cancellationToken);

		/// <summary>
		/// Creates an HTML fragment that can be copied to the clipboard
		/// </summary>
		/// <param name="spans">Spans</param>
		/// <param name="delimiter">Delimiter added between generated html strings</param>
		/// <param name="cancellationToken"></param>
		/// <returns></returns>
		string GenerateHtmlFragment(NormalizedSnapshotSpanCollection spans, string delimiter, CancellationToken cancellationToken);

		/// <summary>
		/// Creates an HTML fragment that can be copied to the clipboard
		/// </summary>
		/// <param name="spans">Spans</param>
		/// <param name="textView">Text view</param>
		/// <param name="cancellationToken">Cancellation token</param>
		/// <returns></returns>
		string GenerateHtmlFragment(NormalizedSnapshotSpanCollection spans, ITextView textView, CancellationToken cancellationToken);

		/// <summary>
		/// Creates an HTML fragment that can be copied to the clipboard
		/// </summary>
		/// <param name="spans">Spans</param>
		/// <param name="textView">Text view</param>
		/// <param name="delimiter">Delimiter added between generated html strings</param>
		/// <param name="cancellationToken">Cancellation token</param>
		/// <returns></returns>
		string GenerateHtmlFragment(NormalizedSnapshotSpanCollection spans, ITextView textView, string delimiter, CancellationToken cancellationToken);
	}
}
