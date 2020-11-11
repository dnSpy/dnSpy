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
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using dnlib.DotNet;

namespace dnSpy.Documents.TreeView {
	struct MemberReferenceFinder {
		readonly ModuleDefMD module;

		public MemberReferenceFinder(ModuleDefMD module) => this.module = module;

		public Dictionary<ITypeDefOrRef, TypeRefInfo> Find() {
			const SigComparerOptions typeOptions =
				SigComparerOptions.CompareAssemblyLocale |
				SigComparerOptions.CompareAssemblyPublicKeyToken |
				SigComparerOptions.CompareAssemblyVersion |
				SigComparerOptions.IgnoreModifiers |
				SigComparerOptions.DontProjectWinMDRefs |
				SigComparerOptions.DontCheckTypeEquivalence;
			var typeDict = new Dictionary<ITypeDefOrRef, TypeRefInfo>(new TypeEqualityComparer(typeOptions));

			for (uint rid = 1; ; rid++) {
				var tr = module.ResolveTypeRef(rid);
				if (tr is null)
					break;
				Add(typeDict, tr);
			}

			for (uint rid = 1; ; rid++) {
				var ts = module.ResolveTypeSpec(rid);
				if (ts is null)
					break;
				Add(typeDict, ts);
			}

			for (uint rid = 1; ; rid++) {
				var mr = module.ResolveMemberRef(rid);
				if (mr is null)
					break;
				AddMemberRef(typeDict, mr);
			}

			for (uint rid = 1; ; rid++) {
				var ms = module.ResolveMethodSpec(rid);
				if (ms is null)
					break;
				AddMethodRef(typeDict, ms);
			}

			return typeDict;
		}

		void Add(Dictionary<ITypeDefOrRef, TypeRefInfo> typeDict, ITypeDefOrRef type) {
			var tdr = GetTypeDefOrRef(type);
			if (IsOurType(tdr))
				return;
			if (type is TypeSpec ts) {
				if (!TryGetTypeDefOrRef(ts, out var otherType) || IsOurType(otherType))
					return;
			}
			if (tdr != type && type is TypeSpec) {
				if (!typeDict.TryGetValue(tdr, out var typeInfo)) {
					Debug.Assert(false);
					return;
				}
				typeDict = typeInfo.TypeDict;
			}

			if (!typeDict.ContainsKey(type))
				typeDict.Add(type, new TypeRefInfo());
		}

		bool IsOurType(ITypeDefOrRef tdr) {
			if (tdr is TypeDef)
				return true;
			if (tdr is TypeRef tr) {
				if (tr.Scope == module)
					return true;
				if (tr.Scope is AssemblyRef asmRef)
					return AssemblyNameComparer.CompareAll.Equals(asmRef, module.Assembly);
				if (tr.Scope is ModuleRef modRef)
					return StringComparer.OrdinalIgnoreCase.Equals(modRef.Name, module.Name);
			}
			return false;
		}

		static bool TryGetTypeDefOrRef(TypeSpec ts, [NotNullWhen(true)] out ITypeDefOrRef? type) {
			type = null;
			var sig = ts.TypeSig;
			while (sig is NonLeafSig)
				sig = sig.Next;
			if (sig is GenericInstSig gis)
				type = gis.GenericType?.TypeDefOrRef;
			else if (sig is TypeDefOrRefSig tdrs)
				type = tdrs.TypeDefOrRef;
			return type is not null;
		}

		static ITypeDefOrRef GetTypeDefOrRef(ITypeDefOrRef typeRef) {
			if (typeRef is TypeSpec ts) {
				var sig = ts.TypeSig.RemovePinnedAndModifiers();
				if (sig is GenericInstSig gis)
					return gis.GenericType?.TypeDefOrRef ?? typeRef;
			}
			return typeRef;
		}

		static bool TryGetTypeRefInfo(Dictionary<ITypeDefOrRef, TypeRefInfo> typeDict, ITypeDefOrRef declType, [NotNullWhen(true)] out TypeRefInfo? typeInfo) {
			typeInfo = null;
			if (declType is null)
				return false;
			declType = GetTypeDefOrRef(declType);
			return typeDict.TryGetValue(declType, out typeInfo);
		}

		static void AddMemberRef(Dictionary<ITypeDefOrRef, TypeRefInfo> typeDict, MemberRef mr) {
			if (mr.IsMethodRef)
				AddMethodRef(typeDict, mr);
			else if (mr.IsFieldRef) {
				if (!TryGetTypeRefInfo(typeDict, mr.DeclaringType, out var typeInfo))
					return;
				typeInfo.FieldDict[mr] = mr;
			}
		}

		static void AddMethodRef(Dictionary<ITypeDefOrRef, TypeRefInfo> typeDict, IMethod method) {
			if (!TryGetTypeRefInfo(typeDict, method.DeclaringType, out var typeInfo))
				return;

			// For PERF reasons, the method refs aren't resolved. Instead we assume a method with
			// a get_ or set_ prefix is a property accessor, and a method with an add_, remove_, or
			// raise_ prefix is an event accessor.
			string methodName = method.Name;
			Dictionary<IMethod, HashSet<IMethod>> dict;
			if (methodName.StartsWith("get_") || methodName.StartsWith("set_"))
				dict = typeInfo.PropertyDict;
			else if (methodName.StartsWith("add_") || methodName.StartsWith("remove_") || methodName.StartsWith("raise_"))
				dict = typeInfo.EventDict;
			else
				dict = typeInfo.MethodDict;
			if (!dict.TryGetValue(method, out var hash))
				dict.Add(method, hash = new HashSet<IMethod>());
			hash.Add(method);
		}
	}

	sealed class TypeRefInfo {
		const SigComparerOptions memberOptions =
			SigComparerOptions.CompareAssemblyLocale |
			SigComparerOptions.CompareAssemblyPublicKeyToken |
			SigComparerOptions.CompareAssemblyVersion |
			SigComparerOptions.IgnoreModifiers |
			SigComparerOptions.PrivateScopeIsComparable |
			SigComparerOptions.DontProjectWinMDRefs |
			SigComparerOptions.DontCheckTypeEquivalence;

		public readonly Dictionary<ITypeDefOrRef, TypeRefInfo> TypeDict = new Dictionary<ITypeDefOrRef, TypeRefInfo>(new TypeEqualityComparer(memberOptions));
		public readonly Dictionary<IMethod, HashSet<IMethod>> MethodDict = new Dictionary<IMethod, HashSet<IMethod>>(new MethodEqualityComparer(memberOptions));
		public readonly Dictionary<MemberRef, MemberRef> FieldDict = new Dictionary<MemberRef, MemberRef>(new MethodEqualityComparer(memberOptions));
		public readonly Dictionary<IMethod, HashSet<IMethod>> PropertyDict = new Dictionary<IMethod, HashSet<IMethod>>(new MethodEqualityComparer(memberOptions));
		public readonly Dictionary<IMethod, HashSet<IMethod>> EventDict = new Dictionary<IMethod, HashSet<IMethod>>(new MethodEqualityComparer(memberOptions));
	}
}
