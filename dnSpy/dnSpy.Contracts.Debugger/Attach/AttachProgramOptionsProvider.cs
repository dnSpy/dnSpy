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
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace dnSpy.Contracts.Debugger.Attach {
	/// <summary>
	/// Creates <see cref="AttachProgramOptions"/>
	/// </summary>
	public abstract class AttachProgramOptionsProvider {
		/// <summary>
		/// Creates new <see cref="AttachProgramOptions"/> instances. This method is called on a background thread.
		/// </summary>
		/// <param name="context">Context</param>
		/// <returns></returns>
		public abstract IEnumerable<AttachProgramOptions> Create(AttachProgramOptionsProviderContext context);
	}

	/// <summary>
	/// Context passed to <see cref="AttachProgramOptionsProvider.Create(AttachProgramOptionsProviderContext)"/>
	/// </summary>
	public sealed class AttachProgramOptionsProviderContext {
		/// <summary>
		/// Cancellation token
		/// </summary>
		public CancellationToken CancellationToken { get; }

		/// <summary>
		/// All valid process ids or empty if any process id is valid
		/// </summary>
		public int[] ProcessIds { get; }

		/// <summary>
		/// Checks if it's a valid process. May be null.
		/// </summary>
		public Func<Process, bool>? IsValidProcess { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="cancellationToken">Cancellation token</param>
		public AttachProgramOptionsProviderContext(CancellationToken cancellationToken) : this(null, null, cancellationToken) { }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="processIds">All valid process ids or null/empty if any process id is valid</param>
		/// <param name="isValidProcess">Checks if it's a valid process. May be null.</param>
		/// <param name="cancellationToken">Cancellation token</param>
		public AttachProgramOptionsProviderContext(int[]? processIds, Func<Process, bool>? isValidProcess, CancellationToken cancellationToken) {
			ProcessIds = processIds ?? Array.Empty<int>();
			IsValidProcess = isValidProcess;
			CancellationToken = cancellationToken;
		}
	}
}
