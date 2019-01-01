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
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using dnSpy.Contracts.Debugger;
using dnSpy.Contracts.Debugger.Breakpoints.Code;
using dnSpy.Contracts.Debugger.CallStack;
using dnSpy.Contracts.Debugger.Code;
using dnSpy.Contracts.Debugger.Disassembly;

namespace dnSpy.Debugger.Disassembly {
	[Export(typeof(DbgNativeCodeProvider))]
	sealed class DbgNativeCodeProviderImpl : DbgNativeCodeProvider {
		readonly Lazy<DbgRuntimeNativeCodeProvider, IDbgRuntimeNativeCodeProviderMetadata>[] dbgRuntimeNativeCodeProviders;

		[ImportingConstructor]
		DbgNativeCodeProviderImpl([ImportMany] IEnumerable<Lazy<DbgRuntimeNativeCodeProvider, IDbgRuntimeNativeCodeProviderMetadata>> dbgRuntimeNativeCodeProviders) =>
			this.dbgRuntimeNativeCodeProviders = dbgRuntimeNativeCodeProviders.OrderBy(a => a.Metadata.Order).ToArray();

		IEnumerable<DbgRuntimeNativeCodeProvider> GetProviders(DbgRuntime runtime) {
			var checkedProviders = new List<DbgRuntimeNativeCodeProvider>();

			foreach (var lz in dbgRuntimeNativeCodeProviders) {
				var guidString = lz.Metadata.Guid;
				if (guidString == null)
					continue;
				bool b = Guid.TryParse(guidString, out var guid);
				Debug.Assert(b);
				if (!b)
					continue;
				if (guid != runtime.Guid)
					continue;
				var provider = lz.Value;
				if (checkedProviders.Contains(provider))
					continue;
				checkedProviders.Add(provider);
				yield return provider;
			}

			foreach (var lz in dbgRuntimeNativeCodeProviders) {
				var guidString = lz.Metadata.RuntimeKindGuid;
				if (guidString == null)
					continue;
				bool b = Guid.TryParse(guidString, out var guid);
				Debug.Assert(b);
				if (!b)
					continue;
				if (guid != runtime.RuntimeKindGuid)
					continue;
				var provider = lz.Value;
				if (checkedProviders.Contains(provider))
					continue;
				checkedProviders.Add(provider);
				yield return provider;
			}
		}

		public override bool CanGetNativeCode(DbgStackFrame frame) {
			if (frame == null)
				throw new ArgumentNullException(nameof(frame));

			foreach (var provider in GetProviders(frame.Runtime)) {
				if (provider.CanGetNativeCode(frame))
					return true;
			}

			return false;
		}

		public override bool TryGetNativeCode(DbgStackFrame frame, DbgNativeCodeOptions options, out GetNativeCodeResult result) {
			if (frame == null)
				throw new ArgumentNullException(nameof(frame));

			foreach (var provider in GetProviders(frame.Runtime)) {
				if (provider.TryGetNativeCode(frame, options, out result))
					return true;
			}

			result = default;
			return false;
		}

		public override bool CanGetNativeCode(DbgBoundCodeBreakpoint boundBreakpoint) {
			if (boundBreakpoint == null)
				throw new ArgumentNullException(nameof(boundBreakpoint));

			foreach (var provider in GetProviders(boundBreakpoint.Runtime)) {
				if (provider.CanGetNativeCode(boundBreakpoint))
					return true;
			}

			return false;
		}

		public override bool TryGetNativeCode(DbgBoundCodeBreakpoint boundBreakpoint, DbgNativeCodeOptions options, out GetNativeCodeResult result) {
			if (boundBreakpoint == null)
				throw new ArgumentNullException(nameof(boundBreakpoint));

			foreach (var provider in GetProviders(boundBreakpoint.Runtime)) {
				if (provider.TryGetNativeCode(boundBreakpoint, options, out result))
					return true;
			}

			result = default;
			return false;
		}

		public override bool CanGetNativeCode(DbgRuntime runtime, DbgCodeLocation location) {
			if (runtime == null)
				throw new ArgumentNullException(nameof(runtime));
			if (location == null)
				throw new ArgumentNullException(nameof(location));

			foreach (var provider in GetProviders(runtime)) {
				if (provider.CanGetNativeCode(runtime, location))
					return true;
			}

			return false;
		}

		public override bool TryGetNativeCode(DbgRuntime runtime, DbgCodeLocation location, DbgNativeCodeOptions options, out GetNativeCodeResult result) {
			if (runtime == null)
				throw new ArgumentNullException(nameof(runtime));
			if (location == null)
				throw new ArgumentNullException(nameof(location));

			foreach (var provider in GetProviders(runtime)) {
				if (provider.TryGetNativeCode(runtime, location, options, out result))
					return true;
			}

			result = default;
			return false;
		}
	}
}
