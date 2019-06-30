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
	/// Debugging options base class shared by .NET Framework code and .NET Core code
	/// </summary>
	abstract class CorDebugAttachToProgramOptions : AttachToProgramOptions {
		/// <summary>
		/// Gets the process id
		/// </summary>
		public int ProcessId { get; set; }

		/// <summary>
		/// Copies this instance to <paramref name="other"/>
		/// </summary>
		/// <param name="other">Destination</param>
		protected void CopyTo(CorDebugAttachToProgramOptions other) {
			if (other is null)
				throw new ArgumentNullException(nameof(other));
			base.CopyTo(other);
			other.ProcessId = ProcessId;
		}
	}
}
