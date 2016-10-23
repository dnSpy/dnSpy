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
using System.Threading.Tasks;

namespace dnSpy.Contracts.Documents.Tabs {
	/// <summary>
	/// <see cref="IDocumentTabContent"/> that creates its output asynchronously in another thread
	/// </summary>
	public interface IAsyncDocumentTabContent : IDocumentTabContent {
		/// <summary>
		/// Returns true if <see cref="CreateContentAsync(IShowContext, CancellationTokenSource)"/>
		/// should be called
		/// </summary>
		/// <param name="ctx">Context</param>
		/// <returns></returns>
		bool NeedAsyncWork(IShowContext ctx);

		/// <summary>
		/// Called in the worker thread
		/// </summary>
		/// <param name="ctx">Context</param>
		/// <param name="source">Cancellation token source</param>
		Task CreateContentAsync(IShowContext ctx, CancellationTokenSource source);

		/// <summary>
		/// Called in the main UI thread after the worker thread has exited or was interrupted
		/// </summary>
		/// <param name="ctx">Context</param>
		/// <param name="result">Result</param>
		void OnShowAsync(IShowContext ctx, IAsyncShowResult result);
	}
}
