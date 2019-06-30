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
using dnSpy.Contracts.Debugger.StartDebugging.Dialog;

namespace dnSpy.Contracts.Debugger {
	/// <summary>
	/// Debug a program base class. Created eg. by <see cref="StartDebuggingOptionsPage.GetOptions"/>
	/// </summary>
	public abstract class StartDebuggingOptions : DebugProgramOptions {
		/// <summary>
		/// Where to break, see <see cref="PredefinedBreakKinds"/>
		/// </summary>
		public string? BreakKind { get; set; }

		/// <summary>
		/// Copies this instance to <paramref name="other"/>
		/// </summary>
		/// <param name="other">Destination</param>
		protected void CopyTo(StartDebuggingOptions other) {
			if (other is null)
				throw new ArgumentNullException(nameof(other));
			other.BreakKind = BreakKind;
		}
	}

	/// <summary>
	/// Predefined break kinds, see <see cref="StartDebuggingOptions.BreakKind"/>
	/// </summary>
	public static class PredefinedBreakKinds {
		/// <summary>
		/// Don't break, let the program run
		/// </summary>
		public const string DontBreak = nameof(DontBreak);

		/// <summary>
		/// Break as soon as the process has been created
		/// </summary>
		public const string CreateProcess = nameof(CreateProcess);

		/// <summary>
		/// Entry point
		/// </summary>
		public const string EntryPoint = nameof(EntryPoint);
	}
}
