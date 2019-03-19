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
using System.Collections.Generic;
using dnlib.DotNet;

namespace dnSpy.AsmEditor.Commands {
	static class RefFinder {
		const SigComparerOptions TypeSigComparerOptionsFlags =
			SigComparerOptions.CompareDeclaringTypes |
			SigComparerOptions.CompareAssemblyPublicKeyToken |
			SigComparerOptions.TypeRefCanReferenceGlobalType |
			SigComparerOptions.PrivateScopeIsComparable |
			SigComparerOptions.DontProjectWinMDRefs;
		const SigComparerOptions MemberSigComparerOptionsFlags =
			SigComparerOptions.CompareAssemblyPublicKeyToken |
			SigComparerOptions.TypeRefCanReferenceGlobalType |
			SigComparerOptions.PrivateScopeIsComparable |
			SigComparerOptions.DontProjectWinMDRefs;
		public static readonly TypeEqualityComparer TypeEqualityComparerInstance = new TypeEqualityComparer(TypeSigComparerOptionsFlags);

		public static IEnumerable<AssemblyRef> FindAssemblyRefsToThisModule(ModuleDef module) {
			if (module == null || !module.IsManifestModule)
				yield break;
			var asm = module.Assembly;
			if (asm == null)
				yield break;

			foreach (var tr in FindTypeRefsToThisModule(module)) {
				if (tr.ResolutionScope is AssemblyRef asmRef && asmRef.Name == asm.Name)
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

		public static bool Equals(MemberRef mr, FieldDef field) {
			if (mr == null || field == null)
				return false;
			if (!mr.IsFieldRef)
				return false;
			if (!new SigComparer(MemberSigComparerOptionsFlags).Equals(mr, field))
				return false;
			return Equals(mr.Class, field.DeclaringType);
		}

		public static bool Equals(MemberRef mr, MethodDef method) {
			if (mr == null || method == null)
				return false;
			if (!mr.IsMethodRef)
				return false;
			if (!new SigComparer(MemberSigComparerOptionsFlags).Equals(mr, method))
				return false;
			return Equals(mr.Class, method.DeclaringType);
		}

		static bool Equals(IMemberRefParent @class, TypeDef type) {
			if (@class == null || type == null)
				return false;
			if (@class is TypeDef td)
				return TypeEqualityComparerInstance.Equals(td, type);
			if (@class is TypeRef tr)
				return TypeEqualityComparerInstance.Equals(tr, type);
			if (@class is TypeSpec ts) {
				var typeSig = ts.TypeSig.RemovePinnedAndModifiers();
				var tdrSig = typeSig as TypeDefOrRefSig;
				if (tdrSig == null && typeSig is GenericInstSig gis)
					tdrSig = gis.GenericType;
				if (tdrSig != null) {
					if (tdrSig.TypeDefOrRef is TypeDef td2)
						return TypeEqualityComparerInstance.Equals(td2, type);
					if (tdrSig.TypeDefOrRef is TypeRef tr2)
						return TypeEqualityComparerInstance.Equals(tr2, type);
					return false;
				}
				return false;
			}
			if (@class is MethodDef md)
				return TypeEqualityComparerInstance.Equals(md.DeclaringType, type);
			if (@class is ModuleRef mr)
				return type.IsGlobalModuleType && StringComparer.OrdinalIgnoreCase.Equals(mr.Name, type.Module.Name);
			return false;
		}
	}
}
