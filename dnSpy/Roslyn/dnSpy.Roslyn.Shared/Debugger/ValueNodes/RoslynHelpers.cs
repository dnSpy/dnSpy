// Copied from Roslyn code

using System;

namespace dnSpy.Roslyn.Shared.Debugger.ValueNodes {
	static class RoslynHelpers {
		// This method is intended to match all the generated member names that
		// the EE (Result Provider) needs to know about (for both VB and C#).
		// The checks here much more encompassing than the actual set of generated
		// names that the compilers produce, but that is acceptable (considering
		// the fact that none of these will be valid language identifiers, and also
		// the fact that a broad check helps "future proof" this implementation).
		internal static bool IsCompilerGenerated(this string name) {
			return name.StartsWith("<", StringComparison.Ordinal) || (name.IndexOf('$') >= 0);
		}
	}
}
