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
	/// Debugging options used when starting a Mono program
	/// </summary>
	public sealed class MonoStartDebuggingOptions : MonoStartDebuggingOptionsBase {
		/// <summary>
		/// Path to <c>mono.exe</c> or null / empty string if it should be auto detected
		/// </summary>
		public string? MonoExePath { get; set; }

		/// <summary>
		/// <c>mono.exe</c> options
		/// </summary>
		public MonoExeOptions MonoExeOptions { get; set; }

		/// <summary>
		/// Constructor
		/// </summary>
		public MonoStartDebuggingOptions() {
			if (IntPtr.Size == 4)
				MonoExeOptions = MonoExeOptions.Prefer32 | MonoExeOptions.Debug32 | MonoExeOptions.Debug64;
			else
				MonoExeOptions = MonoExeOptions.Prefer64 | MonoExeOptions.Debug32 | MonoExeOptions.Debug64;
		}

		/// <summary>
		/// Copies this instance to <paramref name="other"/> and returns it
		/// </summary>
		/// <param name="other">Destination</param>
		/// <returns></returns>
		public MonoStartDebuggingOptions CopyTo(MonoStartDebuggingOptions other) {
			if (other is null)
				throw new ArgumentNullException(nameof(other));
			base.CopyTo(other);
			other.MonoExePath = MonoExePath;
			other.MonoExeOptions = MonoExeOptions;
			return other;
		}

		/// <summary>
		/// Clones this instance
		/// </summary>
		/// <returns></returns>
		public override DebugProgramOptions Clone() => CopyTo(new MonoStartDebuggingOptions());
	}

	/// <summary>
	/// <c>mono.exe</c> options
	/// </summary>
	[Flags]
	public enum MonoExeOptions {
		/// <summary>
		/// No bit is set
		/// </summary>
		None				= 0,

		/// <summary>
		/// 32-bit <c>mono.exe</c> can be used
		/// </summary>
		Debug32				= 0x00000001,

		/// <summary>
		/// 64-bit <c>mono.exe</c> can be used
		/// </summary>
		Debug64				= 0x00000002,

		/// <summary>
		/// Prefer 32-bit <c>mono.exe</c> over 64-bit <c>mono.exe</c>
		/// </summary>
		Prefer32			= 0x000000004,

		/// <summary>
		/// Prefer 64-bit <c>mono.exe</c> over 32-bit <c>mono.exe</c>
		/// </summary>
		Prefer64			= 0x00000008,
	}
}
