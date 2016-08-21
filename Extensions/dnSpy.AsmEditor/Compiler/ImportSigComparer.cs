/*
    Copyright (C) 2014-2016 de4dot@gmail.com

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

namespace dnSpy.AsmEditor.Compiler {
	sealed class ImportSigComparerOptions {
		public ModuleDef SourceModule { get; }
		public ModuleDef TargetModule { get; }
		public ImportSigComparerOptions(ModuleDef sourceModule, ModuleDef targetModule) {
			SourceModule = sourceModule;
			TargetModule = targetModule;
		}
	}

	sealed class ImportTypeEqualityComparer : IEqualityComparer<TypeDef>, IEqualityComparer<ITypeDefOrRef>, IEqualityComparer<IType>, IEqualityComparer<TypeSig> {
		/*readonly*/ ImportSigComparer comparer;
		public ImportTypeEqualityComparer(ImportSigComparer comparer) {
			this.comparer = comparer;
		}
		public bool Equals(TypeDef x, TypeDef y) => comparer.Equals(x, y);
		public int GetHashCode(TypeDef obj) => comparer.GetHashCode(obj);
		public bool Equals(ITypeDefOrRef x, ITypeDefOrRef y) => comparer.Equals(x, y);
		public int GetHashCode(ITypeDefOrRef obj) => comparer.GetHashCode(obj);
		public bool Equals(IType x, IType y) => comparer.Equals(x, y);
		public int GetHashCode(IType obj) => comparer.GetHashCode(obj);
		public bool Equals(TypeSig x, TypeSig y) => comparer.Equals(x, y);
		public int GetHashCode(TypeSig obj) => comparer.GetHashCode(obj);
	}

	sealed class ImportPropertyEqualityComparer : IEqualityComparer<PropertyDef> {
		/*readonly*/ ImportSigComparer comparer;
		public ImportPropertyEqualityComparer(ImportSigComparer comparer) {
			this.comparer = comparer;
		}
		public bool Equals(PropertyDef x, PropertyDef y) => comparer.Equals(x, y);
		public int GetHashCode(PropertyDef obj) => comparer.GetHashCode(obj);
	}

	sealed class ImportEventEqualityComparer : IEqualityComparer<EventDef> {
		/*readonly*/ ImportSigComparer comparer;
		public ImportEventEqualityComparer(ImportSigComparer comparer) {
			this.comparer = comparer;
		}
		public bool Equals(EventDef x, EventDef y) => comparer.Equals(x, y);
		public int GetHashCode(EventDef obj) => comparer.GetHashCode(obj);
	}

	sealed class ImportMethodEqualityComparer : IEqualityComparer<MethodDef>, IEqualityComparer<MemberRef>, IEqualityComparer<IMethod> {
		/*readonly*/ ImportSigComparer comparer;
		public ImportMethodEqualityComparer(ImportSigComparer comparer) {
			this.comparer = comparer;
		}
		public bool Equals(MethodDef x, MethodDef y) => comparer.Equals(x, y);
		public int GetHashCode(MethodDef obj) => comparer.GetHashCode(obj);
		public bool Equals(MemberRef x, MemberRef y) => comparer.Equals(x, y);
		public int GetHashCode(MemberRef obj) => comparer.GetHashCode(obj);
		public bool Equals(IMethod x, IMethod y) => comparer.Equals(x, y);
		public int GetHashCode(IMethod obj) => comparer.GetHashCode(obj);
	}

	sealed class ImportFieldEqualityComparer : IEqualityComparer<FieldDef>, IEqualityComparer<MemberRef>, IEqualityComparer<IField> {
		/*readonly*/ ImportSigComparer comparer;
		public ImportFieldEqualityComparer(ImportSigComparer comparer) {
			this.comparer = comparer;
		}
		public bool Equals(FieldDef x, FieldDef y) => comparer.Equals(x, y);
		public int GetHashCode(FieldDef obj) => comparer.GetHashCode(obj);
		public bool Equals(MemberRef x, MemberRef y) => comparer.Equals(x, y);
		public int GetHashCode(MemberRef obj) => comparer.GetHashCode(obj);
		public bool Equals(IField x, IField y) => comparer.Equals(x, y);
		public int GetHashCode(IField obj) => comparer.GetHashCode(obj);
	}

	// Most code is from dnlib.DotNet.SigComparer
	struct ImportSigComparer {
		const int HASHCODE_MAGIC_GLOBAL_TYPE = 1654396648;
		const int HASHCODE_MAGIC_NESTED_TYPE = -1049070942;
		const int HASHCODE_MAGIC_ET_MODULE = -299744851;
		const int HASHCODE_MAGIC_ET_VALUEARRAY = -674970533;
		const int HASHCODE_MAGIC_ET_GENERICINST = -2050514639;
		const int HASHCODE_MAGIC_ET_VAR = 1288450097;
		const int HASHCODE_MAGIC_ET_MVAR = -990598495;
		const int HASHCODE_MAGIC_ET_ARRAY = -96331531;
		const int HASHCODE_MAGIC_ET_SZARRAY = 871833535;
		const int HASHCODE_MAGIC_ET_BYREF = -634749586;
		const int HASHCODE_MAGIC_ET_PTR = 1976400808;
		const int HASHCODE_MAGIC_ET_SENTINEL = 68439620;

		RecursionCounter recursionCounter;
		SigComparerOptions options;
		readonly ImportSigComparerOptions importOptions;
		GenericArguments genericArguments;
		readonly ModuleDef sourceModule;

		bool DontCompareTypeScope => (options & SigComparerOptions.DontCompareTypeScope) != 0;
		bool CompareMethodFieldDeclaringType => (options & SigComparerOptions.CompareMethodFieldDeclaringType) != 0;
		bool ComparePropertyDeclaringType => (options & SigComparerOptions.ComparePropertyDeclaringType) != 0;
		bool CompareEventDeclaringType => (options & SigComparerOptions.CompareEventDeclaringType) != 0;
		bool CompareSentinelParams => (options & SigComparerOptions.CompareSentinelParams) != 0;
		bool CompareAssemblyPublicKeyToken => (options & SigComparerOptions.CompareAssemblyPublicKeyToken) != 0;
		bool CompareAssemblyVersion => (options & SigComparerOptions.CompareAssemblyVersion) != 0;
		bool CompareAssemblyLocale => (options & SigComparerOptions.CompareAssemblyLocale) != 0;
		bool TypeRefCanReferenceGlobalType => (options & SigComparerOptions.TypeRefCanReferenceGlobalType) != 0;
		bool DontCompareReturnType => (options & SigComparerOptions.DontCompareReturnType) != 0;
		bool SubstituteGenericParameters => (options & SigComparerOptions.SubstituteGenericParameters) != 0;
		bool CaseInsensitiveTypeNamespaces => (options & SigComparerOptions.CaseInsensitiveTypeNamespaces) != 0;
		bool CaseInsensitiveTypeNames => (options & SigComparerOptions.CaseInsensitiveTypeNames) != 0;
		bool CaseInsensitiveMethodFieldNames => (options & SigComparerOptions.CaseInsensitiveMethodFieldNames) != 0;
		bool CaseInsensitivePropertyNames => (options & SigComparerOptions.CaseInsensitivePropertyNames) != 0;
		bool CaseInsensitiveEventNames => (options & SigComparerOptions.CaseInsensitiveEventNames) != 0;
		bool PrivateScopeFieldIsComparable => (options & SigComparerOptions.PrivateScopeFieldIsComparable) != 0;
		bool PrivateScopeMethodIsComparable => (options & SigComparerOptions.PrivateScopeMethodIsComparable) != 0;
		bool RawSignatureCompare => (options & SigComparerOptions.RawSignatureCompare) != 0;
		bool IgnoreModifiers => (options & SigComparerOptions.IgnoreModifiers) != 0;
		bool MscorlibIsNotSpecial => (options & SigComparerOptions.MscorlibIsNotSpecial) != 0;

		public ImportSigComparer(ImportSigComparerOptions importOptions, SigComparerOptions options)
			: this(importOptions, options, null) {
		}

		public ImportSigComparer(ImportSigComparerOptions importOptions, SigComparerOptions options, ModuleDef sourceModule) {
			if (importOptions == null)
				throw new ArgumentNullException(nameof(importOptions));
			this.recursionCounter = new RecursionCounter();
			this.options = options;
			this.importOptions = importOptions;
			this.genericArguments = null;
			this.sourceModule = sourceModule;
		}

		int GetHashCode_FnPtr_SystemIntPtr() {
			return GetHashCode_TypeNamespace("System") +
					GetHashCode_TypeName("IntPtr");
		}

		bool Equals_Names(bool caseInsensitive, UTF8String a, UTF8String b) {
			if (caseInsensitive)
				return UTF8String.ToSystemStringOrEmpty(a).Equals(UTF8String.ToSystemStringOrEmpty(b), StringComparison.OrdinalIgnoreCase);
			return UTF8String.Equals(a, b);
		}

		bool Equals_Names(bool caseInsensitive, string a, string b) {
			if (caseInsensitive)
				return (a ?? string.Empty).Equals(b ?? string.Empty, StringComparison.OrdinalIgnoreCase);
			return (a ?? string.Empty) == (b ?? string.Empty);
		}

		int GetHashCode_Name(bool caseInsensitive, string a) {
			if (caseInsensitive)
				return (a ?? string.Empty).ToUpperInvariant().GetHashCode();
			return (a ?? string.Empty).GetHashCode();
		}

		bool Equals_TypeNamespaces(UTF8String a, UTF8String b) {
			return Equals_Names(CaseInsensitiveTypeNamespaces, a, b);
		}

		bool Equals_TypeNamespaces(UTF8String a, string b) {
			return Equals_Names(CaseInsensitiveTypeNamespaces, UTF8String.ToSystemStringOrEmpty(a), b);
		}

		int GetHashCode_TypeNamespace(UTF8String a) {
			return GetHashCode_Name(CaseInsensitiveTypeNamespaces, UTF8String.ToSystemStringOrEmpty(a));
		}

		int GetHashCode_TypeNamespace(string a) {
			return GetHashCode_Name(CaseInsensitiveTypeNamespaces, a);
		}

		bool Equals_TypeNames(UTF8String a, UTF8String b) {
			return Equals_Names(CaseInsensitiveTypeNames, a, b);
		}

		bool Equals_TypeNames(UTF8String a, string b) {
			return Equals_Names(CaseInsensitiveTypeNames, UTF8String.ToSystemStringOrEmpty(a), b);
		}

		int GetHashCode_TypeName(UTF8String a) {
			return GetHashCode_Name(CaseInsensitiveTypeNames, UTF8String.ToSystemStringOrEmpty(a));
		}

		int GetHashCode_TypeName(string a) {
			return GetHashCode_Name(CaseInsensitiveTypeNames, a);
		}

		bool Equals_MethodFieldNames(UTF8String a, UTF8String b) {
			return Equals_Names(CaseInsensitiveMethodFieldNames, a, b);
		}

		bool Equals_MethodFieldNames(UTF8String a, string b) {
			return Equals_Names(CaseInsensitiveMethodFieldNames, UTF8String.ToSystemStringOrEmpty(a), b);
		}

		int GetHashCode_MethodFieldName(UTF8String a) {
			return GetHashCode_Name(CaseInsensitiveMethodFieldNames, UTF8String.ToSystemStringOrEmpty(a));
		}

		int GetHashCode_MethodFieldName(string a) {
			return GetHashCode_Name(CaseInsensitiveMethodFieldNames, a);
		}

		bool Equals_PropertyNames(UTF8String a, UTF8String b) {
			return Equals_Names(CaseInsensitivePropertyNames, a, b);
		}

		bool Equals_PropertyNames(UTF8String a, string b) {
			return Equals_Names(CaseInsensitivePropertyNames, UTF8String.ToSystemStringOrEmpty(a), b);
		}

		int GetHashCode_PropertyName(UTF8String a) {
			return GetHashCode_Name(CaseInsensitivePropertyNames, UTF8String.ToSystemStringOrEmpty(a));
		}

		int GetHashCode_PropertyName(string a) {
			return GetHashCode_Name(CaseInsensitivePropertyNames, a);
		}

		bool Equals_EventNames(UTF8String a, UTF8String b) {
			return Equals_Names(CaseInsensitiveEventNames, a, b);
		}

		bool Equals_EventNames(UTF8String a, string b) {
			return Equals_Names(CaseInsensitiveEventNames, UTF8String.ToSystemStringOrEmpty(a), b);
		}

		int GetHashCode_EventName(UTF8String a) {
			return GetHashCode_Name(CaseInsensitiveEventNames, UTF8String.ToSystemStringOrEmpty(a));
		}

		int GetHashCode_EventName(string a) {
			return GetHashCode_Name(CaseInsensitiveEventNames, a);
		}

		SigComparerOptions ClearOptions(SigComparerOptions flags) {
			var old = options;
			options &= ~flags;
			return old;
		}

		SigComparerOptions SetOptions(SigComparerOptions flags) {
			var old = options;
			options |= flags;
			return old;
		}

		void RestoreOptions(SigComparerOptions oldFlags) {
			options = oldFlags;
		}

		void InitializeGenericArguments() {
			if (genericArguments == null)
				genericArguments = new GenericArguments();
		}

		static GenericInstSig GetGenericInstanceType(IMemberRefParent parent) {
			var ts = parent as TypeSpec;
			if (ts == null)
				return null;
			return ts.TypeSig.RemoveModifiers() as GenericInstSig;
		}

		bool Equals(IAssembly aAsm, IAssembly bAsm, TypeRef b) {
			if (Equals(aAsm, bAsm))
				return true;

			// Could be an exported type. Resolve it and check again.

			var td = b.Resolve(sourceModule);
			return td != null && Equals(aAsm, td.Module.Assembly);
		}

		bool Equals(IAssembly aAsm, IAssembly bAsm, ExportedType b) {
			if (Equals(aAsm, bAsm))
				return true;

			var td = b.Resolve();
			return td != null && Equals(aAsm, td.Module.Assembly);
		}

		bool Equals(IAssembly aAsm, TypeRef a, IAssembly bAsm, TypeRef b) {
			if (Equals(aAsm, bAsm))
				return true;

			// Could be exported types. Resolve them and check again.

			var tda = a.Resolve(sourceModule);
			var tdb = b.Resolve(sourceModule);
			return tda != null && tdb != null && Equals(tda.Module.Assembly, tdb.Module.Assembly);
		}

		bool Equals(IAssembly aAsm, ExportedType a, IAssembly bAsm, ExportedType b) {
			if (Equals(aAsm, bAsm))
				return true;

			var tda = a.Resolve();
			var tdb = b.Resolve();
			return tda != null && tdb != null && Equals(tda.Module.Assembly, tdb.Module.Assembly);
		}

		bool Equals(IAssembly aAsm, TypeRef a, IAssembly bAsm, ExportedType b) {
			if (Equals(aAsm, bAsm))
				return true;

			// Could be an exported type. Resolve it and check again.

			var tda = a.Resolve(sourceModule);
			var tdb = b.Resolve();
			return tda != null && tdb != null && Equals(tda.Module.Assembly, tdb.Module.Assembly);
		}

		bool Equals(TypeDef a, IModule bMod, TypeRef b) {
			if (Equals(a.Module, bMod) && Equals(a.DefinitionAssembly, b.DefinitionAssembly))
				return true;

			// Could be an exported type. Resolve it and check again.

			var td = b.Resolve(sourceModule);
			if (td == null)
				return false;
			return Equals(a.Module, td.Module) && Equals(a.DefinitionAssembly, td.DefinitionAssembly);
		}

		bool Equals(TypeDef a, FileDef bFile, ExportedType b) {
			if (Equals(a.Module, bFile) && Equals(a.DefinitionAssembly, b.DefinitionAssembly))
				return true;

			var td = b.Resolve();
			return td != null && Equals(a.Module, td.Module) && Equals(a.DefinitionAssembly, td.DefinitionAssembly);
		}

		bool TypeDefScopeEquals(TypeDef a, TypeDef b) {
			if (a == null || b == null)
				return false;
			return Equals(a.Module, b.Module);
		}

		bool Equals(TypeRef a, IModule ma, TypeRef b, IModule mb) {
			if (Equals(ma, mb) && Equals(a.DefinitionAssembly, b.DefinitionAssembly))
				return true;

			// Could be exported types. Resolve them and check again.

			var tda = a.Resolve(sourceModule);
			var tdb = b.Resolve(sourceModule);
			return tda != null && tdb != null &&
				Equals(tda.Module, tdb.Module) && Equals(tda.DefinitionAssembly, tdb.DefinitionAssembly);
		}

		bool Equals(TypeRef a, IModule ma, ExportedType b, FileDef fb) {
			if (Equals(ma, fb) && Equals(a.DefinitionAssembly, b.DefinitionAssembly))
				return true;

			// Could be an exported type. Resolve it and check again.

			var tda = a.Resolve(sourceModule);
			var tdb = b.Resolve();
			return tda != null && tdb != null &&
				Equals(tda.Module, tdb.Module) && Equals(tda.DefinitionAssembly, tdb.DefinitionAssembly);
		}

		public bool Equals(IMemberRef a, IMemberRef b) {
			if (a == b)
				return true;
			if (a == null || b == null)
				return false;
			if (!recursionCounter.Increment())
				return false;

			bool result;
			IType ta, tb;
			IField fa, fb;
			IMethod ma, mb;
			PropertyDef pa, pb;
			EventDef ea, eb;

			if ((ta = a as IType) != null && (tb = b as IType) != null)
				result = Equals(ta, tb);
			else if ((fa = a as IField) != null && (fb = b as IField) != null && fa.IsField && fb.IsField)
				result = Equals(fa, fb);
			else if ((ma = a as IMethod) != null && (mb = b as IMethod) != null)
				result = Equals(ma, mb);
			else if ((pa = a as PropertyDef) != null && (pb = b as PropertyDef) != null)
				result = Equals(pa, pb);
			else if ((ea = a as EventDef) != null && (eb = b as EventDef) != null)
				result = Equals(ea, eb);
			else
				result = false;

			recursionCounter.Decrement();
			return result;
		}

		public int GetHashCode(IMemberRef a) {
			if (a == null)
				return 0;
			if (!recursionCounter.Increment())
				return 0;

			int result;
			IType ta;
			IField fa;
			IMethod ma;
			PropertyDef pa;
			EventDef ea;

			if ((ta = a as IType) != null)
				result = GetHashCode(ta);
			else if ((fa = a as IField) != null)
				result = GetHashCode(fa);
			else if ((ma = a as IMethod) != null)
				result = GetHashCode(ma);
			else if ((pa = a as PropertyDef) != null)
				result = GetHashCode(pa);
			else if ((ea = a as EventDef) != null)
				result = GetHashCode(ea);
			else
				result = 0;		// Should never be reached

			recursionCounter.Decrement();
			return result;
		}

		public bool Equals(ITypeDefOrRef a, ITypeDefOrRef b) {
			return Equals((IType)a, (IType)b);
		}

		public int GetHashCode(ITypeDefOrRef a) {
			return GetHashCode((IType)a);
		}

		public bool Equals(IType a, IType b) {
			if (a == b)
				return true;
			if (a == null || b == null)
				return false;
			if (!recursionCounter.Increment())
				return false;

			bool result;
			TypeDef tda, tdb;
			TypeRef tra, trb;
			TypeSpec tsa, tsb;
			TypeSig sa, sb;
			ExportedType eta, etb;

			if ((tda = a as TypeDef) != null & (tdb = b as TypeDef) != null)
				result = Equals(tda, tdb);
			else if ((tra = a as TypeRef) != null & (trb = b as TypeRef) != null)
				result = Equals(tra, trb);
			else if ((tsa = a as TypeSpec) != null & (tsb = b as TypeSpec) != null)
				result = Equals(tsa, tsb);
			else if ((sa = a as TypeSig) != null & (sb = b as TypeSig) != null)
				result = Equals(sa, sb);
			else if ((eta = a as ExportedType) != null & (etb = b as ExportedType) != null)
				result = Equals(eta, etb);
			else if (tda != null && trb != null)
				result = Equals(tda, trb);		// TypeDef vs TypeRef
			else if (tra != null && tdb != null)
				result = Equals(tdb, tra);		// TypeDef vs TypeRef
			else if (tda != null && tsb != null)
				result = Equals(tda, tsb);		// TypeDef vs TypeSpec
			else if (tsa != null && tdb != null)
				result = Equals(tdb, tsa);		// TypeDef vs TypeSpec
			else if (tda != null && sb != null)
				result = Equals(tda, sb);		// TypeDef vs TypeSig
			else if (sa != null && tdb != null)
				result = Equals(tdb, sa);		// TypeDef vs TypeSig
			else if (tda != null && etb != null)
				result = Equals(tda, etb);		// TypeDef vs ExportedType
			else if (eta != null && tdb != null)
				result = Equals(tdb, eta);		// TypeDef vs ExportedType
			else if (tra != null && tsb != null)
				result = Equals(tra, tsb);		// TypeRef vs TypeSpec
			else if (tsa != null && trb != null)
				result = Equals(trb, tsa);		// TypeRef vs TypeSpec
			else if (tra != null && sb != null)
				result = Equals(tra, sb);		// TypeRef vs TypeSig
			else if (sa != null && trb != null)
				result = Equals(trb, sa);		// TypeRef vs TypeSig
			else if (tra != null && etb != null)
				result = Equals(tra, etb);		// TypeRef vs ExportedType
			else if (eta != null && trb != null)
				result = Equals(trb, eta);		// TypeRef vs ExportedType
			else if (tsa != null && sb != null)
				result = Equals(tsa, sb);		// TypeSpec vs TypeSig
			else if (sa != null && tsb != null)
				result = Equals(tsb, sa);		// TypeSpec vs TypeSig
			else if (tsa != null && etb != null)
				result = Equals(tsa, etb);		// TypeSpec vs ExportedType
			else if (eta != null && tsb != null)
				result = Equals(tsb, eta);		// TypeSpec vs ExportedType
			else if (sa != null && etb != null)
				result = Equals(sa, etb);		// TypeSig vs ExportedType
			else if (eta != null && sb != null)
				result = Equals(sb, eta);		// TypeSig vs ExportedType
			else
				result = false;	// Should never be reached

			recursionCounter.Decrement();
			return result;
		}

		public int GetHashCode(IType a) {
			if (a == null)
				return 0;
			if (!recursionCounter.Increment())
				return 0;

			int hash;
			TypeDef td;
			TypeRef tr;
			TypeSpec ts;
			TypeSig sig;
			ExportedType et;

			if ((td = a as TypeDef) != null)
				hash = GetHashCode(td);
			else if ((tr = a as TypeRef) != null)
				hash = GetHashCode(tr);
			else if ((ts = a as TypeSpec) != null)
				hash = GetHashCode(ts);
			else if ((sig = a as TypeSig) != null)
				hash = GetHashCode(sig);
			else if ((et = a as ExportedType) != null)
				hash = GetHashCode(et);
			else
				hash = 0;	// Should never be reached

			recursionCounter.Decrement();
			return hash;
		}

		public bool Equals(TypeRef a, TypeDef b) {
			return Equals(b, a);
		}

		public bool Equals(TypeDef a, TypeRef b) {
			if ((object)a == (object)b)
				return true;	// both are null
			if (a == null || b == null)
				return false;
			if (!recursionCounter.Increment())
				return false;

			bool result;
			IModule bMod;
			AssemblyRef bAsm;
			TypeRef dtb;

			var scope = b.ResolutionScope;

			if (!Equals_TypeNames(a.Name, b.Name) || !Equals_TypeNamespaces(a.Namespace, b.Namespace))
				result = false;
			else if ((dtb = scope as TypeRef) != null)	// nested type
				result = Equals(a.DeclaringType, dtb);	// Compare enclosing types
			else if (a.DeclaringType != null) {
				// a is nested, b isn't
				result = false;
			}
			else if (DontCompareTypeScope)
				result = true;
			else if ((bMod = scope as IModule) != null)	// 'b' is defined in the same assembly as 'a'
				result = Equals(a, bMod, b);
			else if ((bAsm = scope as AssemblyRef) != null) {
				var aMod = a.Module;
				result = aMod != null && Equals(aMod.Assembly, bAsm, b);
			}
			else {
				result = false;
				//TODO: Handle the case where scope == null
			}

			if (result && !TypeRefCanReferenceGlobalType && a.IsGlobalModuleType)
				result = false;
			recursionCounter.Decrement();
			return result;
		}

		public bool Equals(ExportedType a, TypeDef b) {
			return Equals(b, a);
		}

		public bool Equals(TypeDef a, ExportedType b) {
			if ((object)a == (object)b)
				return true;	// both are null
			if (a == null || b == null)
				return false;
			if (!recursionCounter.Increment())
				return false;

			bool result;
			ExportedType dtb;
			FileDef bFile;
			AssemblyRef bAsm;

			var scope = b.Implementation;

			if (!Equals_TypeNames(a.Name, b.TypeName) || !Equals_TypeNamespaces(a.Namespace, b.TypeNamespace))
				result = false;
			else if ((dtb = scope as ExportedType) != null) {	// nested type
				result = Equals(a.DeclaringType, dtb);	// Compare enclosing types
			}
			else if (a.DeclaringType != null) {
				result = false;	// a is nested, b isn't
			}
			else if (DontCompareTypeScope)
				result = true;
			else {
				if ((bFile = scope as FileDef) != null)
					result = Equals(a, bFile, b);
				else if ((bAsm = scope as AssemblyRef) != null) {
					var aMod = a.Module;
					result = aMod != null && Equals(aMod.Assembly, bAsm, b);
				}
				else
					result = false;
			}

			if (result && !TypeRefCanReferenceGlobalType && a.IsGlobalModuleType)
				result = false;
			recursionCounter.Decrement();
			return result;
		}

		public bool Equals(TypeSpec a, TypeDef b) {
			return Equals(b, a);
		}

		public bool Equals(TypeDef a, TypeSpec b) {
			if ((object)a == (object)b)
				return true;	// both are null
			if (a == null || b == null)
				return false;
			return Equals(a, b.TypeSig);
		}

		public bool Equals(TypeSig a, TypeDef b) {
			return Equals(b, a);
		}

		public bool Equals(TypeDef a, TypeSig b) {
			if ((object)a == (object)b)
				return true;	// both are null
			if (a == null || b == null)
				return false;
			if (!recursionCounter.Increment())
				return false;
			bool result;

			var b2 = b as TypeDefOrRefSig;
			if (b2 != null)
				result = Equals(a, (IType)b2.TypeDefOrRef);
			else if (b is ModifierSig || b is PinnedSig)
				result = Equals(a, b.Next);
			else
				result = false;

			recursionCounter.Decrement();
			return result;
		}

		public bool Equals(TypeSpec a, TypeRef b) {
			return Equals(b, a);
		}

		public bool Equals(TypeRef a, TypeSpec b) {
			if ((object)a == (object)b)
				return true;	// both are null
			if (a == null || b == null)
				return false;
			return Equals(a, b.TypeSig);
		}

		public bool Equals(ExportedType a, TypeRef b) {
			return Equals(b, a);
		}

		public bool Equals(TypeRef a, ExportedType b) {
			if ((object)a == (object)b)
				return true;	// both are null
			if (a == null || b == null)
				return false;
			if (!recursionCounter.Increment())
				return false;

			bool result = Equals_TypeNames(a.Name, b.TypeName) &&
					Equals_TypeNamespaces(a.Namespace, b.TypeNamespace) &&
					EqualsScope(a, b);

			recursionCounter.Decrement();
			return result;
		}

		public bool Equals(TypeSig a, TypeRef b) {
			return Equals(b, a);
		}

		public bool Equals(TypeRef a, TypeSig b) {
			if ((object)a == (object)b)
				return true;	// both are null
			if (a == null || b == null)
				return false;
			if (!recursionCounter.Increment())
				return false;
			bool result;

			var b2 = b as TypeDefOrRefSig;
			if (b2 != null)
				result = Equals(a, (IType)b2.TypeDefOrRef);
			else if (b is ModifierSig || b is PinnedSig)
				result = Equals(a, b.Next);
			else
				result = false;

			recursionCounter.Decrement();
			return result;
		}

		public bool Equals(TypeSig a, TypeSpec b) {
			return Equals(b, a);
		}

		public bool Equals(TypeSpec a, TypeSig b) {
			if ((object)a == (object)b)
				return true;	// both are null
			if (a == null || b == null)
				return false;
			return Equals(a.TypeSig, b);
		}

		public bool Equals(ExportedType a, TypeSpec b) {
			return Equals(b, a);
		}

		public bool Equals(TypeSpec a, ExportedType b) {
			if ((object)a == (object)b)
				return true;	// both are null
			if (a == null || b == null)
				return false;
			return Equals(a.TypeSig, b);
		}

		public bool Equals(ExportedType a, TypeSig b) {
			return Equals(b, a);
		}

		public bool Equals(TypeSig a, ExportedType b) {
			if ((object)a == (object)b)
				return true;	// both are null
			if (a == null || b == null)
				return false;
			if (!recursionCounter.Increment())
				return false;
			bool result;

			var a2 = a as TypeDefOrRefSig;
			if (a2 != null)
				result = Equals(a2.TypeDefOrRef, b);
			else if (a is ModifierSig || a is PinnedSig)
				result = Equals(a.Next, b);
			else
				result = false;

			recursionCounter.Decrement();
			return result;
		}

		int GetHashCodeGlobalType() {
			// We don't always know the name+namespace of the global type, eg. when it's
			// referenced by a ModuleRef. Use the same hash for all global types.
			return HASHCODE_MAGIC_GLOBAL_TYPE;
		}

		public bool Equals(TypeRef a, TypeRef b) {
			if (a == b)
				return true;
			if (a == null || b == null)
				return false;
			if (!recursionCounter.Increment())
				return false;

			bool result = Equals_TypeNames(a.Name, b.Name) &&
					Equals_TypeNamespaces(a.Namespace, b.Namespace) &&
					EqualsResolutionScope(a, b);

			recursionCounter.Decrement();
			return result;
		}

		public int GetHashCode(TypeRef a) {
			if (a == null)
				return TypeRefCanReferenceGlobalType ? GetHashCodeGlobalType() : 0;

			int hash;
			hash = GetHashCode_TypeName(a.Name);
			if (a.ResolutionScope is TypeRef)
				hash += HASHCODE_MAGIC_NESTED_TYPE;
			else
				hash += GetHashCode_TypeNamespace(a.Namespace);
			return hash;
		}

		public bool Equals(ExportedType a, ExportedType b) {
			if (a == b)
				return true;
			if (a == null || b == null)
				return false;
			if (!recursionCounter.Increment())
				return false;

			bool result = Equals_TypeNames(a.TypeName, b.TypeName) &&
					Equals_TypeNamespaces(a.TypeNamespace, b.TypeNamespace) &&
					EqualsImplementation(a, b);

			recursionCounter.Decrement();
			return result;
		}

		public int GetHashCode(ExportedType a) {
			if (a == null)
				return TypeRefCanReferenceGlobalType ? GetHashCodeGlobalType() : 0;
			int hash;
			hash = GetHashCode_TypeName(a.TypeName);
			if (a.Implementation is ExportedType)
				hash += HASHCODE_MAGIC_NESTED_TYPE;
			else
				hash += GetHashCode_TypeNamespace(a.TypeNamespace);
			return hash;
		}

		public bool Equals(TypeDef a, TypeDef b) {
			if (a == b)
				return true;
			if (a == null || b == null)
				return false;
			if (!recursionCounter.Increment())
				return false;

			bool result;

			result = Equals_TypeNames(a.Name, b.Name) &&
					Equals_TypeNamespaces(a.Namespace, b.Namespace) &&
					Equals(a.DeclaringType, b.DeclaringType) &&
					(DontCompareTypeScope || TypeDefScopeEquals(a, b));

			recursionCounter.Decrement();
			return result;
		}

		public int GetHashCode(TypeDef a) {
			if (a == null || a.IsGlobalModuleType)
				return GetHashCodeGlobalType();

			int hash;
			hash = GetHashCode_TypeName(a.Name);
			if (a.DeclaringType != null)
				hash += HASHCODE_MAGIC_NESTED_TYPE;
			else
				hash += GetHashCode_TypeNamespace(a.Namespace);
			return hash;
		}

		public bool Equals(TypeSpec a, TypeSpec b) {
			if (a == b)
				return true;
			if (a == null || b == null)
				return false;
			if (!recursionCounter.Increment())
				return false;

			bool result = Equals(a.TypeSig, b.TypeSig);

			recursionCounter.Decrement();
			return result;
		}

		public int GetHashCode(TypeSpec a) {
			if (a == null)
				return 0;
			return GetHashCode(a.TypeSig);
		}

		bool EqualsResolutionScope(TypeRef a, TypeRef b) {
			if (a == b)
				return true;
			if (a == null || b == null)
				return false;
			var ra = a.ResolutionScope;
			var rb = b.ResolutionScope;
			if (ra == rb)
				return true;
			if (ra == null || rb == null)
				return false;
			if (!recursionCounter.Increment())
				return false;

			bool result;
			TypeRef ea, eb;
			IModule ma, mb;
			AssemblyRef aa, ab;
			ModuleDef modDef;

			// if one of them is a TypeRef, the other one must be too
			if ((ea = ra as TypeRef) != null | (eb = rb as TypeRef) != null)
				result = Equals(ea, eb);
			else if (DontCompareTypeScope)
				result = true;
			// only compare if both are modules
			else if ((ma = ra as IModule) != null & (mb = rb as IModule) != null)
				result = Equals(a, ma, b, mb);
			// only compare if both are assemblies
			else if ((aa = ra as AssemblyRef) != null & (ab = rb as AssemblyRef) != null)
				result = Equals(aa, a, ab, b);
			else if (aa != null && rb is ModuleRef) {
				var bMod = b.Module;
				result = bMod != null && Equals(bMod.Assembly, b, aa, a);
			}
			else if (ab != null && ra is ModuleRef) {
				var aMod = a.Module;
				result = aMod != null && Equals(aMod.Assembly, a, ab, b);
			}
			else if (aa != null && (modDef = rb as ModuleDef) != null)
				result = Equals(modDef.Assembly, aa, a);
			else if (ab != null && (modDef = ra as ModuleDef) != null)
				result = Equals(modDef.Assembly, ab, b);
			else
				result = false;

			recursionCounter.Decrement();
			return result;
		}

		bool EqualsImplementation(ExportedType a, ExportedType b) {
			if (a == b)
				return true;
			if (a == null || b == null)
				return false;
			var ia = a.Implementation;
			var ib = b.Implementation;
			if (ia == ib)
				return true;
			if (ia == null || ib == null)
				return false;
			if (!recursionCounter.Increment())
				return false;

			bool result;
			ExportedType ea, eb;
			FileDef fa, fb;
			AssemblyRef aa, ab;

			// if one of them is an ExportedType, the other one must be too
			if ((ea = ia as ExportedType) != null | (eb = ib as ExportedType) != null)
				result = Equals(ea, eb);
			else if (DontCompareTypeScope)
				result = true;
			// only compare if both are files
			else if ((fa = ia as FileDef) != null & (fb = ib as FileDef) != null)
				result = Equals(fa, fb);
			// only compare if both are assemblies
			else if ((aa = ia as AssemblyRef) != null & (ab = ib as AssemblyRef) != null)
				result = Equals(aa, a, ab, b);
			else if (fa != null && ab != null)
				result = Equals(a.DefinitionAssembly, ab, b);
			else if (fb != null && aa != null)
				result = Equals(b.DefinitionAssembly, aa, a);
			else
				result = false;

			recursionCounter.Decrement();
			return result;
		}

		bool EqualsScope(TypeRef a, ExportedType b) {
			if ((object)a == (object)b)
				return true;	// both are null
			if (a == null || b == null)
				return false;
			var ra = a.ResolutionScope;
			var ib = b.Implementation;
			if (ra == ib)
				return true;
			if (ra == null || ib == null)
				return false;
			if (!recursionCounter.Increment())
				return false;

			bool result;
			TypeRef ea;
			ExportedType eb;
			IModule ma;
			FileDef fb;
			AssemblyRef aa, ab;

			// If one is a nested type, the other one must be too
			if ((ea = ra as TypeRef) != null | (eb = ib as ExportedType) != null)
				result = Equals(ea, eb);
			else if (DontCompareTypeScope)
				result = true;
			else if ((ma = ra as IModule) != null & (fb = ib as FileDef) != null)
				result = Equals(a, ma, b, fb);
			else if ((aa = ra as AssemblyRef) != null & (ab = ib as AssemblyRef) != null)
				result = Equals(aa, a, ab, b);
			else if (ma != null && ab != null)
				result = Equals(a.DefinitionAssembly, ab, b);
			else if (fb != null && aa != null)
				result = Equals(b.DefinitionAssembly, aa, a);
			else
				result = false;

			recursionCounter.Decrement();
			return result;
		}

		bool Equals(FileDef a, FileDef b) {
			if (a == b)
				return true;
			if (a == null || b == null)
				return false;

			return UTF8String.CaseInsensitiveEquals(a.Name, b.Name);
		}

		bool Equals(IModule a, FileDef b) {
			if ((object)a == (object)b)
				return true;	// both are null
			if (a == null || b == null)
				return false;

			//TODO: You should compare against the module's file name, not the name in the metadata!
			return UTF8String.CaseInsensitiveEquals(a.Name, b.Name);
		}

		internal bool Equals(IModule a, IModule b) {
			if (a == b)
				return true;
			if (a == null || b == null)
				return false;
			if (!MscorlibIsNotSpecial && IsCorLib(a) && IsCorLib(b))
				return true;

			return UTF8String.CaseInsensitiveEquals(a.Name, b.Name) || (IsTargetOrSourceModule(a) && IsTargetOrSourceModule(b));
		}

		static bool IsCorLib(ModuleDef a) {
			return a != null && a.IsManifestModule && a.Assembly.IsCorLib();
		}

		static bool IsCorLib(IModule a) {
			var mod = a as ModuleDef;
			return mod != null && mod.IsManifestModule && mod.Assembly.IsCorLib();
		}

		static bool IsCorLib(IAssembly a) {
			return a.IsCorLib();
		}

		bool Equals(ModuleDef a, ModuleDef b) {
			if (a == b)
				return true;
			if (a == null || b == null)
				return false;
			if (!MscorlibIsNotSpecial && IsCorLib(a) && IsCorLib(b))
				return true;
			if (!recursionCounter.Increment())
				return false;

			bool result = Equals((IModule)a, (IModule)b) && Equals(a.Assembly, b.Assembly);

			recursionCounter.Decrement();
			return result;
		}

		bool Equals(IAssembly a, IAssembly b) {
			if (a == b)
				return true;
			if (a == null || b == null)
				return false;
			if (!MscorlibIsNotSpecial && IsCorLib(a) && IsCorLib(b))
				return true;
			if (!recursionCounter.Increment())
				return false;

			bool result = UTF8String.CaseInsensitiveEquals(a.Name, b.Name) &&
				(!CompareAssemblyPublicKeyToken || PublicKeyBase.TokenEquals(a.PublicKeyOrToken, b.PublicKeyOrToken)) &&
				(!CompareAssemblyVersion || dnlib_Utils.Equals(a.Version, b.Version)) &&
				(!CompareAssemblyLocale || dnlib_Utils.LocaleEquals(a.Culture, b.Culture));
			if (!result)
				result = IsTargetOrSourceAssembly(a) && IsTargetOrSourceAssembly(b);

			recursionCounter.Decrement();
			return result;
		}

		bool IsTargetOrSourceAssembly(IAssembly a) => IsTargetAssembly(a) || IsSourceAssembly(a);
		bool IsTargetAssembly(IAssembly a) => a != null && __AssemblyEquals(a, importOptions.TargetModule.Assembly);
		bool IsSourceAssembly(IAssembly a) => a != null && __AssemblyEquals(a, importOptions.SourceModule.Assembly);
		bool IsTargetOrSourceModule(IModule a) => IsTargetModule(a) || IsSourceModule(a);
		bool IsTargetModule(IModule a) => a != null && __ModuleEquals(a, importOptions.TargetModule);
		bool IsSourceModule(IModule a) => a != null && __ModuleEquals(a, importOptions.SourceModule);
		bool __AssemblyEquals(IAssembly a, AssemblyDef b) {
			if ((object)a == b)
				return true;
			if (a == null || b == null)
				return false;
			var a2 = a as AssemblyDef;
			if (a2 != null)
				return a2 == b;
			return AssemblyNameComparer.CompareAll.Equals(a, b);
		}
		bool __ModuleEquals(IModule a, ModuleDef b) {
			if ((object)a == b)
				return true;
			if (a == null || b == null)
				return false;
			var a2 = a as ModuleDef;
			if (a2 != null)
				return a2 == b;
			return StringComparer.OrdinalIgnoreCase.Equals(a.Name, b.Name);
		}

		public bool Equals(TypeSig a, TypeSig b) {
			if (IgnoreModifiers) {
				a = a.RemoveModifiers();
				b = b.RemoveModifiers();
			}
			if (a == b)
				return true;
			if (a == null || b == null)
				return false;
			if (!recursionCounter.Increment())
				return false;
			bool result;

			if (a.ElementType != b.ElementType) {
				// Signatures must be identical. It's possible to have a U4 in a sig (short form
				// of System.UInt32), or a ValueType + System.UInt32 TypeRef (long form), but these
				// should not match in a sig (also the long form is invalid).
				result = false;
			}
			else {
				switch (a.ElementType) {
				case ElementType.Void:
				case ElementType.Boolean:
				case ElementType.Char:
				case ElementType.I1:
				case ElementType.U1:
				case ElementType.I2:
				case ElementType.U2:
				case ElementType.I4:
				case ElementType.U4:
				case ElementType.I8:
				case ElementType.U8:
				case ElementType.R4:
				case ElementType.R8:
				case ElementType.String:
				case ElementType.TypedByRef:
				case ElementType.I:
				case ElementType.U:
				case ElementType.Object:
				case ElementType.Sentinel:
					result = true;
					break;

				case ElementType.Ptr:
				case ElementType.ByRef:
				case ElementType.SZArray:
				case ElementType.Pinned:
					result = Equals(a.Next, b.Next);
					break;

				case ElementType.Array:
					ArraySig ara = a as ArraySig, arb = b as ArraySig;
					result = ara.Rank == arb.Rank &&
							Equals(ara.Sizes, arb.Sizes) &&
							Equals(ara.LowerBounds, arb.LowerBounds) &&
							Equals(a.Next, b.Next);
					break;

				case ElementType.ValueType:
				case ElementType.Class:
					if (RawSignatureCompare)
						result = TokenEquals((a as ClassOrValueTypeSig).TypeDefOrRef, (b as ClassOrValueTypeSig).TypeDefOrRef);
					else
						result = Equals((IType)(a as ClassOrValueTypeSig).TypeDefOrRef, (IType)(b as ClassOrValueTypeSig).TypeDefOrRef);
					break;

				case ElementType.Var:
				case ElementType.MVar:
					result = (a as GenericSig).Number == (b as GenericSig).Number;
					break;

				case ElementType.GenericInst:
					var gia = (GenericInstSig)a;
					var gib = (GenericInstSig)b;
					if (RawSignatureCompare) {
						var gt1 = gia.GenericType;
						var gt2 = gib.GenericType;
						result = TokenEquals(gt1 == null ? null : gt1.TypeDefOrRef, gt2 == null ? null : gt2.TypeDefOrRef) &&
								Equals(gia.GenericArguments, gib.GenericArguments);
					}
					else {
						result = Equals(gia.GenericType, gib.GenericType) &&
								Equals(gia.GenericArguments, gib.GenericArguments);
					}
					break;

				case ElementType.FnPtr:
					result = Equals((a as FnPtrSig).Signature, (b as FnPtrSig).Signature);
					break;

				case ElementType.CModReqd:
				case ElementType.CModOpt:
					if (RawSignatureCompare)
						result = TokenEquals((a as ModifierSig).Modifier, (b as ModifierSig).Modifier) &&
								Equals(a.Next, b.Next);
					else
						result = Equals((IType)(a as ModifierSig).Modifier, (IType)(b as ModifierSig).Modifier) &&
								Equals(a.Next, b.Next);
					break;

				case ElementType.ValueArray:
					result = (a as ValueArraySig).Size == (b as ValueArraySig).Size && Equals(a.Next, b.Next);
					break;

				case ElementType.Module:
					result = (a as ModuleSig).Index == (b as ModuleSig).Index && Equals(a.Next, b.Next);
					break;

				case ElementType.End:
				case ElementType.R:
				case ElementType.Internal:
				default:
					result = false;
					break;
				}
			}

			recursionCounter.Decrement();
			return result;
		}

		static bool TokenEquals(ITypeDefOrRef a, ITypeDefOrRef b) {
			if (a == b)
				return true;
			if (a == null || b == null)
				return false;
			return a.MDToken == b.MDToken;
		}

		public int GetHashCode(TypeSig a) {
			if (a == null)
				return 0;
			if (!recursionCounter.Increment())
				return 0;
			int hash;

			if (genericArguments != null)
				a = genericArguments.Resolve(a);

			switch (a.ElementType) {
			case ElementType.Void:
			case ElementType.Boolean:
			case ElementType.Char:
			case ElementType.I1:
			case ElementType.U1:
			case ElementType.I2:
			case ElementType.U2:
			case ElementType.I4:
			case ElementType.U4:
			case ElementType.I8:
			case ElementType.U8:
			case ElementType.R4:
			case ElementType.R8:
			case ElementType.String:
			case ElementType.TypedByRef:
			case ElementType.I:
			case ElementType.U:
			case ElementType.Object:
			case ElementType.ValueType:
			case ElementType.Class:
				// When comparing an ExportedType/TypeDef/TypeRef to a TypeDefOrRefSig/Class/ValueType,
				// the ET is ignored, so we must ignore it when calculating the hash.
				hash = GetHashCode((IType)(a as TypeDefOrRefSig).TypeDefOrRef);
				break;

			case ElementType.Sentinel:
				hash = HASHCODE_MAGIC_ET_SENTINEL;
				break;

			case ElementType.Ptr:
				hash = HASHCODE_MAGIC_ET_PTR + GetHashCode(a.Next);
				break;

			case ElementType.ByRef:
				hash = HASHCODE_MAGIC_ET_BYREF + GetHashCode(a.Next);
				break;

			case ElementType.SZArray:
				hash = HASHCODE_MAGIC_ET_SZARRAY + GetHashCode(a.Next);
				break;

			case ElementType.CModReqd:
			case ElementType.CModOpt:
			case ElementType.Pinned:
				// When comparing an ExportedType/TypeDef/TypeRef to a ModifierSig/PinnedSig,
				// the ET is ignored, so we must ignore it when calculating the hash.
				hash = GetHashCode(a.Next);
				break;

			case ElementType.Array:
				// Don't include sizes and lower bounds since GetHashCode(Type) doesn't (and can't).
				ArraySig ara = (ArraySig)a;
				hash = HASHCODE_MAGIC_ET_ARRAY + (int)ara.Rank + GetHashCode(ara.Next);
				break;

			case ElementType.Var:
				hash = HASHCODE_MAGIC_ET_VAR + (int)(a as GenericVar).Number;
				break;

			case ElementType.MVar:
				hash = HASHCODE_MAGIC_ET_MVAR + (int)(a as GenericMVar).Number;
				break;

			case ElementType.GenericInst:
				var gia = (GenericInstSig)a;
				hash = HASHCODE_MAGIC_ET_GENERICINST;
				if (SubstituteGenericParameters) {
					InitializeGenericArguments();
					genericArguments.PushTypeArgs(gia.GenericArguments);
					hash += GetHashCode(gia.GenericType);
					genericArguments.PopTypeArgs();
				}
				else
					hash += GetHashCode(gia.GenericType);
				hash += GetHashCode(gia.GenericArguments);
				break;

			case ElementType.FnPtr:
				hash = GetHashCode_FnPtr_SystemIntPtr();
				break;

			case ElementType.ValueArray:
				hash = HASHCODE_MAGIC_ET_VALUEARRAY + (int)(a as ValueArraySig).Size + GetHashCode(a.Next);
				break;

			case ElementType.Module:
				hash = HASHCODE_MAGIC_ET_MODULE + (int)(a as ModuleSig).Index + GetHashCode(a.Next);
				break;

			case ElementType.End:
			case ElementType.R:
			case ElementType.Internal:
			default:
				hash = 0;
				break;
			}

			recursionCounter.Decrement();
			return hash;
		}

		public bool Equals(IList<TypeSig> a, IList<TypeSig> b) {
			if (a == b)
				return true;
			if (a == null || b == null)
				return false;
			if (!recursionCounter.Increment())
				return false;
			bool result;

			if (a.Count != b.Count)
				result = false;
			else {
				int i;
				for (i = 0; i < a.Count; i++) {
					if (!Equals(a[i], b[i]))
						break;
				}
				result = i == a.Count;
			}

			recursionCounter.Decrement();
			return result;
		}

		public int GetHashCode(IList<TypeSig> a) {
			if (a == null)
				return 0;
			if (!recursionCounter.Increment())
				return 0;
			uint hash = 0;
			for (int i = 0; i < a.Count; i++) {
				hash += (uint)GetHashCode(a[i]);
				hash = (hash << 13) | (hash >> 19);
			}
			recursionCounter.Decrement();
			return (int)hash;
		}

		bool Equals(IList<uint> a, IList<uint> b) {
			if (a == b)
				return true;
			if (a == null || b == null)
				return false;
			if (a.Count != b.Count)
				return false;
			for (int i = 0; i < a.Count; i++) {
				if (a[i] != b[i])
					return false;
			}
			return true;
		}

		bool Equals(IList<int> a, IList<int> b) {
			if (a == b)
				return true;
			if (a == null || b == null)
				return false;
			if (a.Count != b.Count)
				return false;
			for (int i = 0; i < a.Count; i++) {
				if (a[i] != b[i])
					return false;
			}
			return true;
		}

		public bool Equals(CallingConventionSig a, CallingConventionSig b) {
			if (a == b)
				return true;
			if (a == null || b == null)
				return false;
			if (!recursionCounter.Increment())
				return false;
			bool result;

			if (a.GetCallingConvention() != b.GetCallingConvention())
				result = false;
			else {
				switch (a.GetCallingConvention() & CallingConvention.Mask) {
				case CallingConvention.Default:
				case CallingConvention.C:
				case CallingConvention.StdCall:
				case CallingConvention.ThisCall:
				case CallingConvention.FastCall:
				case CallingConvention.VarArg:
				case CallingConvention.Property:
				case CallingConvention.NativeVarArg:
					MethodBaseSig ma = a as MethodBaseSig, mb = b as MethodBaseSig;
					result = ma != null && mb != null && Equals(ma, mb);
					break;

				case CallingConvention.Field:
					FieldSig fa = a as FieldSig, fb = b as FieldSig;
					result = fa != null && fb != null && Equals(fa, fb);
					break;

				case CallingConvention.LocalSig:
					LocalSig la = a as LocalSig, lb = b as LocalSig;
					result = la != null && lb != null && Equals(la, lb);
					break;

				case CallingConvention.GenericInst:
					GenericInstMethodSig ga = a as GenericInstMethodSig, gb = b as GenericInstMethodSig;
					result = ga != null && gb != null && Equals(ga, gb);
					break;

				case CallingConvention.Unmanaged:
				default:
					result = false;
					break;
				}
			}

			recursionCounter.Decrement();
			return result;
		}

		public int GetHashCode(CallingConventionSig a) {
			if (a == null)
				return 0;
			if (!recursionCounter.Increment())
				return 0;
			int hash;

			switch (a.GetCallingConvention() & CallingConvention.Mask) {
			case CallingConvention.Default:
			case CallingConvention.C:
			case CallingConvention.StdCall:
			case CallingConvention.ThisCall:
			case CallingConvention.FastCall:
			case CallingConvention.VarArg:
			case CallingConvention.Property:
			case CallingConvention.NativeVarArg:
				MethodBaseSig ma = a as MethodBaseSig;
				hash = ma == null ? 0 : GetHashCode(ma);
				break;

			case CallingConvention.Field:
				FieldSig fa = a as FieldSig;
				hash = fa == null ? 0 : GetHashCode(fa);
				break;

			case CallingConvention.LocalSig:
				LocalSig la = a as LocalSig;
				hash = la == null ? 0 : GetHashCode(la);
				break;

			case CallingConvention.GenericInst:
				GenericInstMethodSig ga = a as GenericInstMethodSig;
				hash = ga == null ? 0 : GetHashCode(ga);
				break;

			case CallingConvention.Unmanaged:
			default:
				hash = GetHashCode_CallingConvention(a);
				break;
			}

			recursionCounter.Decrement();
			return hash;
		}

		public bool Equals(MethodBaseSig a, MethodBaseSig b) {
			if (a == b)
				return true;
			if (a == null || b == null)
				return false;
			if (!recursionCounter.Increment())
				return false;

			bool result = a.GetCallingConvention() == b.GetCallingConvention() &&
					(DontCompareReturnType || Equals(a.RetType, b.RetType)) &&
					Equals(a.Params, b.Params) &&
					(!a.Generic || a.GenParamCount == b.GenParamCount) &&
					(!CompareSentinelParams || Equals(a.ParamsAfterSentinel, b.ParamsAfterSentinel));

			recursionCounter.Decrement();
			return result;
		}

		public int GetHashCode(MethodBaseSig a) {
			if (a == null)
				return 0;
			if (!recursionCounter.Increment())
				return 0;
			int hash;

			hash = GetHashCode_CallingConvention(a) +
					GetHashCode(a.Params);
			if (!DontCompareReturnType)
				hash += GetHashCode(a.RetType);
			if (a.Generic)
				hash += GetHashCode_ElementType_MVar((int)a.GenParamCount);
			if (CompareSentinelParams)
				hash += GetHashCode(a.ParamsAfterSentinel);

			recursionCounter.Decrement();
			return hash;
		}

		int GetHashCode_CallingConvention(CallingConventionSig a) {
			return GetHashCode(a.GetCallingConvention());
		}

		int GetHashCode(CallingConvention a) {
			switch (a & CallingConvention.Mask) {
			case CallingConvention.Default:
			case CallingConvention.C:
			case CallingConvention.StdCall:
			case CallingConvention.ThisCall:
			case CallingConvention.FastCall:
			case CallingConvention.VarArg:
			case CallingConvention.Property:
			case CallingConvention.GenericInst:
			case CallingConvention.Unmanaged:
			case CallingConvention.NativeVarArg:
			case CallingConvention.Field:
				return (int)(a & (CallingConvention.Generic | CallingConvention.HasThis | CallingConvention.ExplicitThis));

			case CallingConvention.LocalSig:
			default:
				return (int)a;
			}
		}

		public bool Equals(FieldSig a, FieldSig b) {
			if (a == b)
				return true;
			if (a == null || b == null)
				return false;
			if (!recursionCounter.Increment())
				return false;

			bool result = a.GetCallingConvention() == b.GetCallingConvention() && Equals(a.Type, b.Type);

			recursionCounter.Decrement();
			return result;
		}

		public int GetHashCode(FieldSig a) {
			if (a == null)
				return 0;
			if (!recursionCounter.Increment())
				return 0;
			int hash;

			hash = GetHashCode_CallingConvention(a) + GetHashCode(a.Type);

			recursionCounter.Decrement();
			return hash;
		}

		public bool Equals(LocalSig a, LocalSig b) {
			if (a == b)
				return true;
			if (a == null || b == null)
				return false;
			if (!recursionCounter.Increment())
				return false;

			bool result = a.GetCallingConvention() == b.GetCallingConvention() && Equals(a.Locals, b.Locals);

			recursionCounter.Decrement();
			return result;
		}

		public int GetHashCode(LocalSig a) {
			if (a == null)
				return 0;
			if (!recursionCounter.Increment())
				return 0;
			int hash;

			hash = GetHashCode_CallingConvention(a) + GetHashCode(a.Locals);

			recursionCounter.Decrement();
			return hash;
		}

		public bool Equals(GenericInstMethodSig a, GenericInstMethodSig b) {
			if (a == b)
				return true;
			if (a == null || b == null)
				return false;
			if (!recursionCounter.Increment())
				return false;

			bool result = a.GetCallingConvention() == b.GetCallingConvention() && Equals(a.GenericArguments, b.GenericArguments);

			recursionCounter.Decrement();
			return result;
		}

		public int GetHashCode(GenericInstMethodSig a) {
			if (a == null)
				return 0;
			if (!recursionCounter.Increment())
				return 0;
			int hash;

			hash = GetHashCode_CallingConvention(a) + GetHashCode(a.GenericArguments);

			recursionCounter.Decrement();
			return hash;
		}

		public bool Equals(IMethod a, IMethod b) {
			if (a == b)
				return true;
			if (a == null || b == null)
				return false;
			if (!recursionCounter.Increment())
				return false;

			bool result;
			MethodDef mda, mdb;
			MemberRef mra, mrb;
			MethodSpec msa, msb;

			if ((mda = a as MethodDef) != null & (mdb = b as MethodDef) != null)
				result = Equals(mda, mdb);
			else if ((mra = a as MemberRef) != null & (mrb = b as MemberRef) != null)
				result = Equals(mra, mrb);
			else if ((msa = a as MethodSpec) != null && (msb = b as MethodSpec) != null)
				result = Equals(msa, msb);
			else if (mda != null && mrb != null)
				result = Equals(mda, mrb);
			else if (mra != null && mdb != null)
				result = Equals(mdb, mra);
			else
				result = false;

			recursionCounter.Decrement();
			return result;
		}

		public int GetHashCode(IMethod a) {
			if (a == null)
				return 0;
			if (!recursionCounter.Increment())
				return 0;

			int hash;
			MethodDef mda;
			MemberRef mra;
			MethodSpec msa;

			if ((mda = a as MethodDef) != null)
				hash = GetHashCode(mda);
			else if ((mra = a as MemberRef) != null)
				hash = GetHashCode(mra);
			else if ((msa = a as MethodSpec) != null)
				hash = GetHashCode(msa);
			else
				hash = 0;

			recursionCounter.Decrement();
			return hash;
		}

		public bool Equals(MemberRef a, MethodDef b) {
			return Equals(b, a);
		}

		public bool Equals(MethodDef a, MemberRef b) {
			if ((object)a == (object)b)
				return true;	// both are null
			if (a == null || b == null)
				return false;
			if (!recursionCounter.Increment())
				return false;

			bool result;
			result = (PrivateScopeMethodIsComparable || !a.IsPrivateScope) &&
					Equals_MethodFieldNames(a.Name, b.Name) &&
					Equals(a.Signature, b.Signature) &&
					(!CompareMethodFieldDeclaringType || Equals(a.DeclaringType, b.Class));

			recursionCounter.Decrement();
			return result;
		}

		public bool Equals(MethodDef a, MethodDef b) {
			if (a == b)
				return true;
			if (a == null || b == null)
				return false;
			if (!recursionCounter.Increment())
				return false;

			bool result;
			result = Equals_MethodFieldNames(a.Name, b.Name) &&
					Equals(a.Signature, b.Signature) &&
					(!CompareMethodFieldDeclaringType || Equals(a.DeclaringType, b.DeclaringType));

			recursionCounter.Decrement();
			return result;
		}

		public int GetHashCode(MethodDef a) {
			if (a == null)
				return 0;

			if (!recursionCounter.Increment())
				return 0;

			int hash = GetHashCode_MethodFieldName(a.Name) +
					GetHashCode(a.Signature);
			if (CompareMethodFieldDeclaringType)
				hash += GetHashCode(a.DeclaringType);

			recursionCounter.Decrement();
			return hash;
		}

		public bool Equals(MemberRef a, MemberRef b) {
			if (a == b)
				return true;
			if (a == null || b == null)
				return false;
			if (!recursionCounter.Increment())
				return false;

			bool result = Equals_MethodFieldNames(a.Name, b.Name) &&
					Equals(a.Signature, b.Signature) &&
					(!CompareMethodFieldDeclaringType || Equals(a.Class, b.Class));

			recursionCounter.Decrement();
			return result;
		}

		public int GetHashCode(MemberRef a) {
			if (a == null)
				return 0;
			if (!recursionCounter.Increment())
				return 0;

			int hash = GetHashCode_MethodFieldName(a.Name);
			GenericInstSig git;
			if (SubstituteGenericParameters && (git = GetGenericInstanceType(a.Class)) != null) {
				InitializeGenericArguments();
				genericArguments.PushTypeArgs(git.GenericArguments);
				hash += GetHashCode(a.Signature);
				genericArguments.PopTypeArgs();
			}
			else
				hash += GetHashCode(a.Signature);
			if (CompareMethodFieldDeclaringType)
				hash += GetHashCode(a.Class);

			recursionCounter.Decrement();
			return hash;
		}

		public bool Equals(MethodSpec a, MethodSpec b) {
			if (a == b)
				return true;
			if (a == null || b == null)
				return false;
			if (!recursionCounter.Increment())
				return false;

			bool result = Equals(a.Method, b.Method) && Equals(a.Instantiation, b.Instantiation);

			recursionCounter.Decrement();
			return result;
		}

		public int GetHashCode(MethodSpec a) {
			if (a == null)
				return 0;
			if (!recursionCounter.Increment())
				return 0;

			// We must do this or it won't get the same hash code as some MethodInfos
			var oldOptions = SetOptions(SigComparerOptions.SubstituteGenericParameters);
			var gim = a.GenericInstMethodSig;
			if (gim != null) {
				InitializeGenericArguments();
				genericArguments.PushMethodArgs(gim.GenericArguments);
			}
			int hash = GetHashCode(a.Method);
			if (gim != null)
				genericArguments.PopMethodArgs();
			RestoreOptions(oldOptions);

			recursionCounter.Decrement();
			return hash;
		}

		bool Equals(IMemberRefParent a, IMemberRefParent b) {
			if (a == b)
				return true;
			if (a == null || b == null)
				return false;
			if (!recursionCounter.Increment())
				return false;

			bool result;
			ITypeDefOrRef ita, itb;
			ModuleRef moda, modb;
			MethodDef ma, mb;
			TypeDef td;

			if ((ita = a as ITypeDefOrRef) != null && (itb = b as ITypeDefOrRef) != null)
				result = Equals((IType)ita, (IType)itb);
			else if ((moda = a as ModuleRef) != null & (modb = b as ModuleRef) != null) {
				ModuleDef omoda = moda.Module, omodb = modb.Module;
				result = Equals((IModule)moda, (IModule)modb) &&
						Equals(omoda == null ? null : omoda.Assembly, omodb == null ? null : omodb.Assembly);
			}
			else if ((ma = a as MethodDef) != null && (mb = b as MethodDef) != null)
				result = Equals(ma, mb);
			else if (modb != null && (td = a as TypeDef) != null)
				result = EqualsGlobal(td, modb);
			else if (moda != null && (td = b as TypeDef) != null)
				result = EqualsGlobal(td, moda);
			else
				result = false;

			recursionCounter.Decrement();
			return result;
		}

		int GetHashCode(IMemberRefParent a) {
			if (a == null)
				return 0;
			if (!recursionCounter.Increment())
				return 0;

			int hash;
			ITypeDefOrRef ita;
			MethodDef ma;

			if ((ita = a as ITypeDefOrRef) != null)
				hash = GetHashCode((IType)ita);
			else if (a is ModuleRef)
				hash = GetHashCodeGlobalType();
			else if ((ma = a as MethodDef) != null) {
				// Only use the declaring type so we get the same hash code when hashing a MethodBase.
				hash = GetHashCode(ma.DeclaringType);
			}
			else
				hash = 0;

			recursionCounter.Decrement();
			return hash;
		}

		public bool Equals(IField a, IField b) {
			if (a == b)
				return true;
			if (a == null || b == null)
				return false;
			if (!recursionCounter.Increment())
				return false;

			bool result;
			FieldDef fa, fb;
			MemberRef ma, mb;

			if ((fa = a as FieldDef) != null & (fb = b as FieldDef) != null)
				result = Equals(fa, fb);
			else if ((ma = a as MemberRef) != null & (mb = b as MemberRef) != null)
				result = Equals(ma, mb);
			else if (fa != null && mb != null)
				result = Equals(fa, mb);
			else if (fb != null && ma != null)
				result = Equals(fb, ma);
			else
				result = false;

			recursionCounter.Decrement();
			return result;
		}

		public int GetHashCode(IField a) {
			if (a == null)
				return 0;
			if (!recursionCounter.Increment())
				return 0;

			int hash;
			FieldDef fa;
			MemberRef ma;

			if ((fa = a as FieldDef) != null)
				hash = GetHashCode(fa);
			else if ((ma = a as MemberRef) != null)
				hash = GetHashCode(ma);
			else
				hash = 0;

			recursionCounter.Decrement();
			return hash;
		}

		public bool Equals(MemberRef a, FieldDef b) {
			return Equals(b, a);
		}

		public bool Equals(FieldDef a, MemberRef b) {
			if ((object)a == (object)b)
				return true;	// both are null
			if (a == null || b == null)
				return false;
			if (!recursionCounter.Increment())
				return false;

			bool result = (PrivateScopeFieldIsComparable || !a.IsPrivateScope) &&
					Equals_MethodFieldNames(a.Name, b.Name) &&
					Equals(a.Signature, b.Signature) &&
					(!CompareMethodFieldDeclaringType || Equals(a.DeclaringType, b.Class));

			recursionCounter.Decrement();
			return result;
		}

		public bool Equals(FieldDef a, FieldDef b) {
			if (a == b)
				return true;
			if (a == null || b == null)
				return false;
			if (!recursionCounter.Increment())
				return false;

			bool result = Equals_MethodFieldNames(a.Name, b.Name) &&
					Equals(a.Signature, b.Signature) &&
					(!CompareMethodFieldDeclaringType || Equals(a.DeclaringType, b.DeclaringType));

			recursionCounter.Decrement();
			return result;
		}

		public int GetHashCode(FieldDef a) {
			if (a == null)
				return 0;
			if (!recursionCounter.Increment())
				return 0;

			int hash = GetHashCode_MethodFieldName(a.Name) +
					GetHashCode(a.Signature);
			if (CompareMethodFieldDeclaringType)
				hash += GetHashCode(a.DeclaringType);

			recursionCounter.Decrement();
			return hash;
		}

		public bool Equals(PropertyDef a, PropertyDef b) {
			if (a == b)
				return true;
			if (a == null || b == null)
				return false;
			if (!recursionCounter.Increment())
				return false;

			bool result = Equals_PropertyNames(a.Name, b.Name) &&
					Equals(a.Type, b.Type) &&
					(!ComparePropertyDeclaringType || Equals(a.DeclaringType, b.DeclaringType));

			recursionCounter.Decrement();
			return result;
		}

		public int GetHashCode(PropertyDef a) {
			if (a == null)
				return 0;
			if (!recursionCounter.Increment())
				return 0;

			var sig = a.PropertySig;
			int hash = GetHashCode_PropertyName(a.Name) +
					GetHashCode(sig == null ? null : sig.RetType);
			if (ComparePropertyDeclaringType)
				hash += GetHashCode(a.DeclaringType);

			recursionCounter.Decrement();
			return hash;
		}

		public bool Equals(EventDef a, EventDef b) {
			if (a == b)
				return true;
			if (a == null || b == null)
				return false;
			if (!recursionCounter.Increment())
				return false;

			bool result = Equals_EventNames(a.Name, b.Name) &&
					Equals((IType)a.EventType, (IType)b.EventType) &&
					(!CompareEventDeclaringType || Equals(a.DeclaringType, b.DeclaringType));

			recursionCounter.Decrement();
			return result;
		}

		public int GetHashCode(EventDef a) {
			if (a == null)
				return 0;
			if (!recursionCounter.Increment())
				return 0;

			int hash = GetHashCode_EventName(a.Name) +
					GetHashCode((IType)a.EventType);
			if (CompareEventDeclaringType)
				hash += GetHashCode(a.DeclaringType);

			recursionCounter.Decrement();
			return hash;
		}

		// Compares a with b, and a must be the global type
		bool EqualsGlobal(TypeDef a, ModuleRef b) {
			if ((object)a == (object)b)
				return true;	// both are null
			if (a == null || b == null)
				return false;
			if (!recursionCounter.Increment())
				return false;

			bool result = a.IsGlobalModuleType &&
				Equals((IModule)a.Module, (IModule)b) &&
				Equals(a.DefinitionAssembly, GetAssembly(b.Module));

			recursionCounter.Decrement();
			return result;
		}

		static AssemblyDef GetAssembly(ModuleDef module) {
			return module == null ? null : module.Assembly;
		}

		static int GetHashCode_ElementType_MVar(int numGenericParams) {
			return GetHashCode(numGenericParams, HASHCODE_MAGIC_ET_MVAR);
		}

		static int GetHashCode(int numGenericParams, int etypeHashCode) {
			uint hash = 0;
			for (int i = 0; i < numGenericParams; i++) {
				hash += (uint)(etypeHashCode + i);
				hash = (hash << 13) | (hash >> 19);
			}
			return (int)hash;
		}

		public override string ToString() {
			return string.Format("{0} - {1}", recursionCounter, options);
		}
	}

	// From dnlib.DotNet.dnlib_Utils
	static class dnlib_Utils {
		static int CompareTo(Version a, Version b) {
			if (a == null)
				a = new Version();
			if (b == null)
				b = new Version();
			if (a.Major != b.Major)
				return a.Major.CompareTo(b.Major);
			if (a.Minor != b.Minor)
				return a.Minor.CompareTo(b.Minor);
			if (GetDefaultVersionValue(a.Build) != GetDefaultVersionValue(b.Build))
				return GetDefaultVersionValue(a.Build).CompareTo(GetDefaultVersionValue(b.Build));
			return GetDefaultVersionValue(a.Revision).CompareTo(GetDefaultVersionValue(b.Revision));
		}

		internal static bool Equals(Version a, Version b) => CompareTo(a, b) == 0;
		static int GetDefaultVersionValue(int val) => val == -1 ? 0 : val;
		static int LocaleCompareTo(UTF8String a, UTF8String b) => GetCanonicalLocale(a).CompareTo(GetCanonicalLocale(b));
		internal static bool LocaleEquals(UTF8String a, UTF8String b) => LocaleCompareTo(a, b) == 0;
		static string GetCanonicalLocale(UTF8String locale) => GetCanonicalLocale(UTF8String.ToSystemStringOrEmpty(locale));

		static string GetCanonicalLocale(string locale) {
			var s = locale.ToUpperInvariant();
			if (s == "NEUTRAL")
				s = string.Empty;
			return s;
		}
	}
}
