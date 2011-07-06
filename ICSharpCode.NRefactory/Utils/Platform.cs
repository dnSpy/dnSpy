// Copyright (c) 2010 AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT license (for details please see \doc\license.txt)

using System;

namespace ICSharpCode.NRefactory.Utils
{
	/// <summary>
	/// Platform-specific code.
	/// </summary>
	static class Platform
	{
		public static StringComparer FileNameComparer {
			get {
				switch (Environment.OSVersion.Platform) {
					case PlatformID.Unix:
					case PlatformID.MacOSX:
						return StringComparer.Ordinal;
					default:
						return StringComparer.OrdinalIgnoreCase;
				}
			}
		}
	}
}
