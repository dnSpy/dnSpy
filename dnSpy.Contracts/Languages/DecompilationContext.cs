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
using System.Threading;
using dnlib.DotNet;

namespace dnSpy.Contracts.Languages {
	/// <summary>
	/// Decompilation options
	/// </summary>
	public class DecompilationContext : IDecompilationContext {
		/// <summary />
		public CancellationToken CancellationToken { get; set; }

		/// <summary>
		/// Returns true if the method body has been modified
		/// </summary>
		public Func<MethodDef, bool> IsBodyModified { get; set; }

		/// <summary>
		/// Disables assembly loading until Dispose() gets called
		/// </summary>
		public Func<IDisposable> GetDisableAssemblyLoad { get; set; }

		/// <summary>
		/// Constructor
		/// </summary>
		public DecompilationContext() {
			this.CancellationToken = CancellationToken.None;
			this.IsBodyModified = m => false;
		}

		/// <summary />
		public IDisposable DisableAssemblyLoad() {
			return GetDisableAssemblyLoad == null ? null : GetDisableAssemblyLoad();
		}
	}
}
