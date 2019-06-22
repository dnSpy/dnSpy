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

namespace dnSpy.Contracts.Debugger.DotNet.Mono {
	/// <summary>
	/// Debugging options used when starting a Unity game. The game must use a patched mono.dll.
	/// </summary>
	public sealed class UnityStartDebuggingOptions : MonoStartDebuggingOptionsBase {
		static readonly TimeSpan DefaultUnityConnectionTimeout = TimeSpan.FromSeconds(30);

		/// <summary>
		/// Constructor
		/// </summary>
		public UnityStartDebuggingOptions() => ConnectionTimeout = DefaultUnityConnectionTimeout;

		/// <summary>
		/// Copies this instance to <paramref name="other"/> and returns it
		/// </summary>
		/// <param name="other">Destination</param>
		/// <returns></returns>
		public UnityStartDebuggingOptions CopyTo(UnityStartDebuggingOptions other) {
			if (other is null)
				throw new ArgumentNullException(nameof(other));
			base.CopyTo(other);
			return other;
		}

		/// <summary>
		/// Clones this instance
		/// </summary>
		/// <returns></returns>
		public override DebugProgramOptions Clone() => CopyTo(new UnityStartDebuggingOptions());
	}
}
