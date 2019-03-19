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
using dnlib.DotNet;

namespace dnSpy.Documents {
	readonly struct TargetFrameworkAttributeInfo {
		public bool IsDotNetCore => Framework == ".NETCoreApp";
		public readonly string Framework;
		public readonly Version Version;
		public readonly string Profile;
		public TargetFrameworkAttributeInfo(string framework, Version version, string profile) {
			Framework = framework ?? throw new ArgumentNullException(nameof(framework));
			Version = version ?? throw new ArgumentNullException(nameof(version));
			Profile = profile;
		}

		public static bool TryCreateTargetFrameworkInfo(ModuleDef module, out TargetFrameworkAttributeInfo info) {
			var asm = module?.Assembly;
			if (asm != null) {
				if (asm.TryGetOriginalTargetFrameworkAttribute(out var framework, out var version, out var profile)) {
					info = new TargetFrameworkAttributeInfo(framework, version, profile);
					return true;
				}
			}
			info = default;
			return false;
		}
	}
}
