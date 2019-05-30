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

namespace dnSpy.Contracts.Debugger.DotNet.CorDebug {
	/// <summary>
	/// Runtime version
	/// </summary>
	public readonly struct CorDebugRuntimeVersion : IEquatable<CorDebugRuntimeVersion> {
		/// <summary>
		/// Gets the kind
		/// </summary>
		public CorDebugRuntimeKind Kind { get; }

		/// <summary>
		/// Gets the version string, eg. "v2.0.50727" or "v4.0.30319" if it's .NET Framework.
		/// If it's .NET Core, this is currently an empty string.
		/// </summary>
		public string Version { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="kind">Kind</param>
		/// <param name="version">Version string</param>
		public CorDebugRuntimeVersion(CorDebugRuntimeKind kind, string version) {
			Kind = kind;
			Version = version ?? throw new ArgumentNullException(nameof(version));
		}

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
		public static bool operator ==(CorDebugRuntimeVersion left, CorDebugRuntimeVersion right) => left.Equals(right);
		public static bool operator !=(CorDebugRuntimeVersion left, CorDebugRuntimeVersion right) => !left.Equals(right);

		public bool Equals(CorDebugRuntimeVersion other) => Kind == other.Kind && StringComparer.Ordinal.Equals(Version, other.Version);
		public override bool Equals(object? obj) => obj is CorDebugRuntimeVersion && Equals((CorDebugRuntimeVersion)obj);
		public override int GetHashCode() => (int)Kind ^ StringComparer.Ordinal.GetHashCode(Version ?? string.Empty);
		public override string ToString() => $"{Kind} {Version}";
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
	}
}
