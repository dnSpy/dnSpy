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

using System.Threading;
using System.Threading.Tasks;

namespace dnSpy.Contracts.Debugger.Attach {
	/// <summary>
	/// Returns all processes that the debug engines support
	/// </summary>
	public abstract class AttachableProcessesService {
		/// <summary>
		/// Gets all programs that can be attached to
		/// </summary>
		/// <param name="cancellationToken">Cancellation token</param>
		/// <returns></returns>
		public Task<AttachableProcess[]> GetAttachableProcessesAsync(CancellationToken cancellationToken = default) =>
			GetAttachableProcessesAsync(null, null, null, cancellationToken);

		/// <summary>
		/// Gets all programs that can be attached to
		/// </summary>
		/// <param name="processName">Process name. If it's empty or null, it matches any string. This can include wildcards (* and ?).</param>
		/// <param name="cancellationToken">Cancellation token</param>
		/// <returns></returns>
		public Task<AttachableProcess[]> GetAttachableProcessesAsync(string processName, CancellationToken cancellationToken = default) =>
			GetAttachableProcessesAsync(string.IsNullOrEmpty(processName) ? null : new[] { processName }, null, null, cancellationToken);

		/// <summary>
		/// Gets all programs that can be attached to
		/// </summary>
		/// <param name="processNames">Process names or null/empty to match any process name. The process name can
		/// include wildcards (* and ?)</param>
		/// <param name="providerNames"><see cref="AttachProgramOptionsProviderFactory"/> names, see <see cref="PredefinedAttachProgramOptionsProviderNames"/></param>
		/// <param name="cancellationToken">Cancellation token</param>
		/// <returns></returns>
		public Task<AttachableProcess[]> GetAttachableProcessesAsync(string[] processNames, string[] providerNames, CancellationToken cancellationToken = default) =>
			GetAttachableProcessesAsync(processNames, null, providerNames, cancellationToken);

		/// <summary>
		/// Gets all programs that can be attached to
		/// </summary>
		/// <param name="processNames">Process names or null/empty to match any process name. The process name can
		/// include wildcards (* and ?)</param>
		/// <param name="processIds">Process ids or null/empty to match any process id</param>
		/// <param name="providerNames"><see cref="AttachProgramOptionsProviderFactory"/> names, see <see cref="PredefinedAttachProgramOptionsProviderNames"/></param>
		/// <param name="cancellationToken">Cancellation token</param>
		/// <returns></returns>
		public abstract Task<AttachableProcess[]> GetAttachableProcessesAsync(string[] processNames, int[] processIds, string[] providerNames, CancellationToken cancellationToken = default);
	}
}
