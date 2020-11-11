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
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using dnSpy.Contracts.Debugger.StartDebugging;
using dnSpy.Debugger.Properties;

namespace dnSpy.Debugger.DbgUI {
	abstract class DbgProcessStarterService {
		public abstract bool CanStart(string filename, out ProcessStarterResult result);
		public abstract bool TryStart(string filename, [NotNullWhen(false)] out string? error);
	}

	[Export(typeof(DbgProcessStarterService))]
	sealed class DbgProcessStarterServiceImpl : DbgProcessStarterService {
		readonly Lazy<DbgProcessStarter, IDbgProcessStarterMetadata>[] processStarters;

		[ImportingConstructor]
		DbgProcessStarterServiceImpl([ImportMany] IEnumerable<Lazy<DbgProcessStarter, IDbgProcessStarterMetadata>> processStarters) =>
			this.processStarters = processStarters.OrderBy(a => a.Metadata.Order).ToArray();

		public override bool CanStart(string filename, out ProcessStarterResult result) {
			if (filename is null)
				throw new ArgumentNullException(nameof(filename));
			foreach (var lz in processStarters) {
				if (lz.Value.IsSupported(filename, out result))
					return true;
			}

			result = ProcessStarterResult.None;
			return false;
		}

		public override bool TryStart(string filename, [NotNullWhen(false)] out string? error) {
			if (filename is null)
				throw new ArgumentNullException(nameof(filename));
			bool ok;
			try {
				ok = TryStartCore(filename, out error);
			}
			catch (Exception ex) {
				ok = false;
				error = string.Format(dnSpy_Debugger_Resources.Error_StartWithoutDebuggingCouldNotStart, filename, ex.Message);
			}
			if (ok)
				return true;

			Debug2.Assert(error is not null);
			if (error is null)
				error = "<Unknown error>";
			return false;
		}

		bool TryStartCore(string filename, [NotNullWhen(false)] out string? error) {
			foreach (var lz in processStarters) {
				if (lz.Value.IsSupported(filename, out _))
					return lz.Value.TryStart(filename, out error);
			}

			Debug.Fail("Shouldn't be here, since " + nameof(CanStart) + "() should be called before " + nameof(TryStart) + "()");
			error = "<Could not start the process>";
			return false;
		}
	}
}
