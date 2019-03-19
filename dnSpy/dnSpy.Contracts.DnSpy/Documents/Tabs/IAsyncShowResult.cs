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

namespace dnSpy.Contracts.Documents.Tabs {
	/// <summary>
	/// Result passed to <see cref="AsyncDocumentTabContent.OnShowAsync(IShowContext, IAsyncShowResult)"/>
	/// </summary>
	public interface IAsyncShowResult {
		/// <summary>
		/// The caught exception or null if none
		/// </summary>
		Exception Exception { get; }

		/// <summary>
		/// true if it was canceled (the cancellation token threw an exception)
		/// </summary>
		bool IsCanceled { get; }

		/// <summary>
		/// true if it's still the visible tab and the UI context can be written to. It can be false
		/// if the asynchronous operation got canceled by the user.
		/// </summary>
		bool CanShowOutput { get; }
	}
}
