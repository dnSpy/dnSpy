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
using dnSpy.Contracts.Debugger;

namespace dnSpy.Debugger.DotNet.CorDebug.Impl.Attach {
	/// <summary>
	/// .NET attach to process options
	/// </summary>
	sealed class DotNetAttachToProgramOptions : CorDebugAttachToProgramOptions {
		/// <summary>
		/// A string returned by <c>dbgshim.dll</c>'s <c>CreateVersionStringFromModule</c> function
		/// or null to use the first found CoreCLR in the process.
		/// </summary>
		public string? ClrModuleVersion { get; set; }

		/// <summary>
		/// Path to <c>coreclr.dll</c> or null to use the first found one in the process
		/// </summary>
		public string? CoreCLRFilename { get; set; }

		/// <summary>
		/// Clones this instance
		/// </summary>
		/// <returns></returns>
		public override DebugProgramOptions Clone() => CopyTo(new DotNetAttachToProgramOptions());

		/// <summary>
		/// Copies this instance to <paramref name="other"/> and returns it
		/// </summary>
		/// <param name="other">Destination</param>
		/// <returns></returns>
		public DotNetAttachToProgramOptions CopyTo(DotNetAttachToProgramOptions other) {
			if (other is null)
				throw new ArgumentNullException(nameof(other));
			base.CopyTo(other);
			other.ClrModuleVersion = ClrModuleVersion;
			other.CoreCLRFilename = CoreCLRFilename;
			return other;
		}
	}
}
