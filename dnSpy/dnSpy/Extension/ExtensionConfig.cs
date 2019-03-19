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

namespace dnSpy.Extension {
	sealed class ExtensionConfig {
		/// <summary>
		/// Minimum OS version (<see cref="Environment.OSVersion"/>'s <see cref="OperatingSystem.Version"/> property) or null if any version
		/// </summary>
		public Version OSVersion { get; set; }

		/// <summary>
		/// Minimum .NET Framework version (<see cref="Environment.Version"/>) or null if any version
		/// </summary>
		public Version FrameworkVersion { get; set; }

		/// <summary>
		/// Minimum dnSpy version or null if any version
		/// </summary>
		public Version AppVersion { get; set; }

		public bool IsSupportedOSversion(Version version) => OSVersion == null || OSVersion <= version;
		public bool IsSupportedFrameworkVersion(Version version) => FrameworkVersion == null || FrameworkVersion <= version;
		public bool IsSupportedAppVersion(Version version) => AppVersion == null || AppVersion <= version;
	}
}
