/*
    Copyright (C) 2014-2015 de4dot@gmail.com

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

using System.Collections.Generic;
using dnlib.DotNet;

namespace dnSpy.AsmEditor {
	static class RefFinder {
		const SigComparerOptions SigComparerOptionsFlags =
			SigComparerOptions.CompareDeclaringTypes |
			SigComparerOptions.CompareAssemblyPublicKeyToken |
			SigComparerOptions.TypeRefCanReferenceGlobalType |
			SigComparerOptions.PrivateScopeIsComparable |
			SigComparerOptions.DontProjectWinMDRefs;
		public static readonly TypeEqualityComparer TypeEqualityComparerInstance = new TypeEqualityComparer(SigComparerOptionsFlags);
		public static readonly FieldEqualityComparer FieldEqualityComparerInstance = new FieldEqualityComparer(SigComparerOptionsFlags);
		public static readonly MethodEqualityComparer MethodEqualityComparerInstance = new MethodEqualityComparer(SigComparerOptionsFlags);

		public static IEnumerable<AssemblyRef> FindAssemblyRefsToThisModule(ModuleDef module) {
			if (!module.IsManifestModule)
				yield break;
			var asm = module.Assembly;
			if (asm == null)
				yield break;

			foreach (var tr in FindTypeRefsToThisModule(module)) {
				var asmRef = tr.ResolutionScope as AssemblyRef;
				if (asmRef != null && asmRef.Name == asm.Name)
					yield return asmRef;
			}
		}

		// Returns type refs that reference this module. Can return type refs that reference some
		// other assembly too.
		public static IEnumerable<TypeRef> FindTypeRefsToThisModule(ModuleDef module) {
			var finder = new MemberFinder().FindAll(module);
			return finder.TypeRefs.Keys;
		}

		// Returns member refs that reference this module. Can return member refs that reference some
		// other assembly too.
		public static IEnumerable<MemberRef> FindMemberRefsToThisModule(ModuleDef module) {
			var finder = new MemberFinder().FindAll(module);
			return finder.MemberRefs.Keys;
		}
	}
}
