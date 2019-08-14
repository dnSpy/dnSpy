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
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace dnSpy.Debugger.DotNet.Metadata {
	/// <summary>
	/// Type and member comparer options
	/// </summary>
	[Flags]
	public enum DmdSigComparerOptions {
		/// <summary>
		/// No bit is set
		/// </summary>
		None = 0,

		/// <summary>
		/// Don't compare type scope (assembly / module)
		/// </summary>
		DontCompareTypeScope = 1,

		/// <summary>
		/// Compare declaring type. It's ignored if it's a nested type and only used if it's a field, constructor, method, property, event, parameter
		/// </summary>
		CompareDeclaringType = 2,

		/// <summary>
		/// Don't compare return types
		/// </summary>
		DontCompareReturnType = 4,

		/// <summary>
		/// Case insensitive member names
		/// </summary>
		CaseInsensitiveMemberNames = 8,

		/// <summary>
		/// Project WinMD references
		/// </summary>
		ProjectWinMDReferences = 0x10,

		/// <summary>
		/// Check type equivalence
		/// </summary>
		CheckTypeEquivalence = 0x20,

		/// <summary>
		/// Compare optional and required C modifiers
		/// </summary>
		CompareCustomModifiers = 0x40,

		/// <summary>
		/// Compare generic type/method parameter's declaring member
		/// </summary>
		CompareGenericParameterDeclaringMember = 0x80,

		/// <summary>
		/// When comparing types, don't compare a multi-dimensional array's lower bounds and sizes
		/// </summary>
		IgnoreMultiDimensionalArrayLowerBoundsAndSizes = 0x100,
	}

	/// <summary>
	/// Compares types and members
	/// </summary>
	public struct DmdSigComparer {
		const int HASHCODE_MAGIC_TYPE = -674970533;
		const int HASHCODE_MAGIC_NESTED_TYPE = -1049070942;
		const int HASHCODE_MAGIC_ET_GENERICINST = -2050514639;
		const int HASHCODE_MAGIC_ET_VAR = 1288450097;
		const int HASHCODE_MAGIC_ET_MVAR = -990598495;
		const int HASHCODE_MAGIC_ET_ARRAY = -96331531;
		const int HASHCODE_MAGIC_ET_SZARRAY = 871833535;
		const int HASHCODE_MAGIC_ET_BYREF = -634749586;
		const int HASHCODE_MAGIC_ET_PTR = 1976400808;
		const int HASHCODE_MAGIC_ET_FNPTR = 68439620;

		DmdSigComparerOptions options;
		const int MAX_RECURSION_COUNT = 100;
		int recursionCounter;

		bool DontCompareTypeScope => (options & DmdSigComparerOptions.DontCompareTypeScope) != 0;
		bool CompareDeclaringType => (options & DmdSigComparerOptions.CompareDeclaringType) != 0;
		bool DontCompareReturnType => (options & DmdSigComparerOptions.DontCompareReturnType) != 0;
		bool CaseInsensitiveMemberNames => (options & DmdSigComparerOptions.CaseInsensitiveMemberNames) != 0;
		//TODO: Use this option
		bool ProjectWinMDReferences => (options & DmdSigComparerOptions.ProjectWinMDReferences) != 0;
		bool CheckTypeEquivalence => (options & DmdSigComparerOptions.CheckTypeEquivalence) != 0;
		bool CompareCustomModifiers => (options & DmdSigComparerOptions.CompareCustomModifiers) != 0;
		bool CompareGenericParameterDeclaringMember => (options & DmdSigComparerOptions.CompareGenericParameterDeclaringMember) != 0;
		bool IgnoreMultiDimensionalArrayLowerBoundsAndSizes => (options & DmdSigComparerOptions.IgnoreMultiDimensionalArrayLowerBoundsAndSizes) != 0;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="options">Options</param>
		public DmdSigComparer(DmdSigComparerOptions options) {
			this.options = options;
			recursionCounter = 0;
		}

		bool IncrementRecursionCounter() {
			if (recursionCounter >= MAX_RECURSION_COUNT)
				return false;
			recursionCounter++;
			return true;
		}
		void DecrementRecursionCounter() => recursionCounter--;

		bool MemberNameEquals(string? a, string? b) {
			if (CaseInsensitiveMemberNames)
				return StringComparer.OrdinalIgnoreCase.Equals(a, b);
			return StringComparer.Ordinal.Equals(a, b);
		}

		int MemberNameGetHashCode(string? a) {
			if (a is null)
				return 0;
			if (CaseInsensitiveMemberNames)
				return StringComparer.OrdinalIgnoreCase.GetHashCode(a);
			return StringComparer.Ordinal.GetHashCode(a);
		}

		/// <summary>
		/// Compares two members
		/// </summary>
		/// <param name="a">First member</param>
		/// <param name="b">Second member</param>
		/// <returns></returns>
		public bool Equals(DmdMemberInfo? a, DmdMemberInfo? b) {
			if ((object?)a == b)
				return true;
			if (a is null || b is null)
				return false;

			switch (a.MemberType) {
			case DmdMemberTypes.TypeInfo:
			case DmdMemberTypes.NestedType:
				return Equals((DmdType)a, b as DmdType);

			case DmdMemberTypes.Field:
				return Equals((DmdFieldInfo)a, b as DmdFieldInfo);

			case DmdMemberTypes.Method:
			case DmdMemberTypes.Constructor:
				return Equals((DmdMethodBase)a, b as DmdMethodBase);

			case DmdMemberTypes.Property:
				return Equals((DmdPropertyInfo)a, b as DmdPropertyInfo);

			case DmdMemberTypes.Event:
				return Equals((DmdEventInfo)a, b as DmdEventInfo);
			}

			return false;
		}

		/// <summary>
		/// Compares two types
		/// </summary>
		/// <param name="a">First type</param>
		/// <param name="b">Second type</param>
		/// <returns></returns>
		public bool Equals(DmdType? a, DmdType? b) {
			if ((object?)a == b)
				return true;
			if (a is null || b is null)
				return false;

			if (!IncrementRecursionCounter())
				return false;

			bool result;
			var at = a.TypeSignatureKind;
			if (at != b.TypeSignatureKind)
				result = false;
			else if (CompareCustomModifiers && !Equals(a.GetCustomModifiers(), b.GetCustomModifiers()))
				result = false;
			else {
				switch (at) {
				case DmdTypeSignatureKind.Type:
					result = MemberNameEquals(a.MetadataName, b.MetadataName) &&
						MemberNameEquals(a.MetadataNamespace, b.MetadataNamespace) &&
						Equals(a.DeclaringType, b.DeclaringType);
					// Type scope only needs to be checked if it's a non-nested type
					if (result && !DontCompareTypeScope && a.DeclaringType is null) {
						result = TypeScopeEquals(a, b);
						if (!result) {
							// One or both of the types could be exported types. We need to
							// resolve them and then compare again.
							var ra = a.ResolveNoThrow();
							var rb = ra is null ? null : b.ResolveNoThrow();
							result = !(ra is null) && !(rb is null) && TypeScopeEquals(ra, rb);
							if (!result && CheckTypeEquivalence)
								result = TIAHelper.Equivalent(ra, rb);
						}
					}
					break;

				case DmdTypeSignatureKind.Pointer:
				case DmdTypeSignatureKind.ByRef:
				case DmdTypeSignatureKind.SZArray:
					result = Equals(a.GetElementType(), b.GetElementType());
					break;

				case DmdTypeSignatureKind.TypeGenericParameter:
					result = a.GenericParameterPosition == b.GenericParameterPosition;
					if (result && CompareGenericParameterDeclaringMember)
						result = Equals(a.DeclaringType, b.DeclaringType);
					break;

				case DmdTypeSignatureKind.MethodGenericParameter:
					result = a.GenericParameterPosition == b.GenericParameterPosition;
					if (result && CompareGenericParameterDeclaringMember) {
						options &= ~DmdSigComparerOptions.CompareGenericParameterDeclaringMember;
						result = Equals(a.DeclaringMethod, b.DeclaringMethod);
						options |= DmdSigComparerOptions.CompareGenericParameterDeclaringMember;
					}
					break;

				case DmdTypeSignatureKind.MDArray:
					result = a.GetArrayRank() == b.GetArrayRank() &&
						(IgnoreMultiDimensionalArrayLowerBoundsAndSizes ||
						(Equals(a.GetArraySizes(), b.GetArraySizes()) &&
						Equals(a.GetArrayLowerBounds(), b.GetArrayLowerBounds()))) &&
						Equals(a.GetElementType(), b.GetElementType());
					break;

				case DmdTypeSignatureKind.GenericInstance:
					result = Equals(a.GetGenericTypeDefinition(), b.GetGenericTypeDefinition()) &&
							Equals(a.GetGenericArguments(), b.GetGenericArguments());
					break;

				case DmdTypeSignatureKind.FunctionPointer:
					result = Equals(a.GetFunctionPointerMethodSignature(), b.GetFunctionPointerMethodSignature());
					break;

				default: throw new InvalidOperationException();
				}
			}

			DecrementRecursionCounter();
			return result;
		}

		/// <summary>
		/// Compares two fields
		/// </summary>
		/// <param name="a">First field</param>
		/// <param name="b">Second field</param>
		/// <returns></returns>
		public bool Equals(DmdFieldInfo? a, DmdFieldInfo? b) {
			if ((object?)a == b)
				return true;
			if (a is null || b is null)
				return false;

			return MemberNameEquals(a.Name, b.Name) &&
				Equals(a.FieldType, b.FieldType) &&
				(!CompareDeclaringType || Equals(a.DeclaringType, b.DeclaringType));
		}

		/// <summary>
		/// Compares two methods or constructors
		/// </summary>
		/// <param name="a">First method or constructor</param>
		/// <param name="b">Second method or constructor</param>
		/// <returns></returns>
		public bool Equals(DmdMethodBase? a, DmdMethodBase? b) {
			if ((object?)a == b)
				return true;
			if (a is null || b is null)
				return false;

			return MemberNameEquals(a.Name, b.Name) &&
				Equals(a.GetMethodSignature(), b.GetMethodSignature()) &&
				(!CompareDeclaringType || Equals(a.DeclaringType, b.DeclaringType));
		}

		/// <summary>
		/// Compares two properties
		/// </summary>
		/// <param name="a">First property</param>
		/// <param name="b">Second property</param>
		/// <returns></returns>
		public bool Equals(DmdPropertyInfo? a, DmdPropertyInfo? b) {
			if ((object?)a == b)
				return true;
			if (a is null || b is null)
				return false;

			return MemberNameEquals(a.Name, b.Name) &&
				Equals(a.GetMethodSignature(), b.GetMethodSignature()) &&
				(!CompareDeclaringType || Equals(a.DeclaringType, b.DeclaringType));
		}

		/// <summary>
		/// Compares two events
		/// </summary>
		/// <param name="a">First event</param>
		/// <param name="b">Second event</param>
		/// <returns></returns>
		public bool Equals(DmdEventInfo? a, DmdEventInfo? b) {
			if ((object?)a == b)
				return true;
			if (a is null || b is null)
				return false;

			return MemberNameEquals(a.Name, b.Name) &&
				Equals(a.EventHandlerType, b.EventHandlerType) &&
				(!CompareDeclaringType || Equals(a.DeclaringType, b.DeclaringType));
		}

		/// <summary>
		/// Compares two method parameters
		/// </summary>
		/// <param name="a">First method parameter</param>
		/// <param name="b">Second method parameter</param>
		/// <returns></returns>
		public bool Equals(DmdParameterInfo? a, DmdParameterInfo? b) {
			if ((object?)a == b)
				return true;
			if (a is null || b is null)
				return false;

			return a.Position == b.Position &&
				Equals(a.ParameterType, b.ParameterType) &&
				(!CompareDeclaringType || Equals(a.Member, b.Member));
		}

		/// <summary>
		/// Compares two assembly names
		/// </summary>
		/// <param name="a">First assembly name</param>
		/// <param name="b">Second assembly name</param>
		/// <returns></returns>
		public bool Equals(IDmdAssemblyName? a, IDmdAssemblyName? b) {
			if ((object?)a == b)
				return true;
			if (a is null || b is null)
				return false;

			// We do not compare the version number. The runtime can redirect an assembly
			// reference from a requested version to any other version.
			// The public key token is also ignored. Only .NET Framwork checks it (.NET Core
			// and Unity ignore it). We could add a new option to ignore the PKT but it would
			// require too many changes to the code (they access singleton comparers) and isn't
			// worth it. It's also being replaced by .NET Core. It's not common for two
			// assemblies loaded in the same process to have the same assembly name but a
			// different public key token.
			const DmdAssemblyNameFlags flagsMask = DmdAssemblyNameFlags.ContentType_Mask;
			return (a.RawFlags & flagsMask) == (b.RawFlags & flagsMask) &&
				StringComparer.OrdinalIgnoreCase.Equals(a.Name, b.Name) &&
				StringComparer.OrdinalIgnoreCase.Equals(a.CultureName ?? string.Empty, b.CultureName ?? string.Empty);
		}

		/// <summary>
		/// Compares two method signatures
		/// </summary>
		/// <param name="a">First method signature</param>
		/// <param name="b">Second method signature</param>
		/// <returns></returns>
		public bool Equals(DmdMethodSignature? a, DmdMethodSignature? b) {
			if ((object?)a == b)
				return true;
			if (a is null || b is null)
				return false;

			return a.Flags == b.Flags &&
				a.GenericParameterCount == b.GenericParameterCount &&
				(DontCompareReturnType || Equals(a.ReturnType, b.ReturnType)) &&
				Equals(a.GetParameterTypes(), b.GetParameterTypes()) &&
				Equals(a.GetVarArgsParameterTypes(), b.GetVarArgsParameterTypes());
		}

		/// <summary>
		/// Compares two custom modifiers
		/// </summary>
		/// <param name="a">First custom modifier</param>
		/// <param name="b">Second custom modifier</param>
		/// <returns></returns>
		public bool Equals(DmdCustomModifier a, DmdCustomModifier b) => a.IsRequired == b.IsRequired && Equals(a.Type, b.Type);

		bool Equals(ReadOnlyCollection<DmdCustomModifier>? a, ReadOnlyCollection<DmdCustomModifier>? b) {
			if (a == b)
				return true;
			if (a is null || b is null)
				return false;
			if (a.Count != b.Count)
				return false;
			for (int i = 0; i < a.Count; i++) {
				if (!Equals(a[i], b[i]))
					return false;
			}
			return true;
		}

		bool Equals(ReadOnlyCollection<DmdType>? a, ReadOnlyCollection<DmdType>? b) {
			if (a == b)
				return true;
			if (a is null || b is null)
				return false;
			if (a.Count != b.Count)
				return false;
			for (int i = 0; i < a.Count; i++) {
				if (!Equals(a[i], b[i]))
					return false;
			}
			return true;
		}

		bool Equals(ReadOnlyCollection<int>? a, ReadOnlyCollection<int>? b) {
			if (a == b)
				return true;
			if (a is null || b is null)
				return false;
			if (a.Count != b.Count)
				return false;
			for (int i = 0; i < a.Count; i++) {
				if (a[i] != b[i])
					return false;
			}
			return true;
		}

		bool TypeScopeEquals(DmdType a, DmdType b) {
			Debug2.Assert(!(a is null) && !(b is null) && !a.HasElementType && !b.HasElementType);
			if (DontCompareTypeScope)
				return true;
			if ((object?)a == b)
				return true;

			var at = a.TypeScope;
			var bt = b.TypeScope;
			switch (at.Kind) {
			case DmdTypeScopeKind.Invalid:
				return false;

			case DmdTypeScopeKind.Module:
				switch (bt.Kind) {
				case DmdTypeScopeKind.Invalid:
					return false;
				case DmdTypeScopeKind.Module:
					return at.Data == bt.Data;
				case DmdTypeScopeKind.ModuleRef:
					return StringComparer.OrdinalIgnoreCase.Equals(((DmdModule)at.Data!).ScopeName, (string)bt.Data!) &&
						Equals(((DmdModule)at.Data).Assembly.GetName(), (IDmdAssemblyName)bt.Data2!);
				case DmdTypeScopeKind.AssemblyRef:
					return Equals(((DmdModule)at.Data!).Assembly.GetName(), (IDmdAssemblyName)bt.Data!);
				default:
					throw new InvalidOperationException();
				}

			case DmdTypeScopeKind.ModuleRef:
				switch (bt.Kind) {
				case DmdTypeScopeKind.Invalid:
					return false;
				case DmdTypeScopeKind.Module:
					return StringComparer.OrdinalIgnoreCase.Equals((string)at.Data!, ((DmdModule)bt.Data!).ScopeName) &&
						Equals((IDmdAssemblyName)at.Data2!, ((DmdModule)bt.Data).Assembly.GetName());
				case DmdTypeScopeKind.ModuleRef:
					return StringComparer.OrdinalIgnoreCase.Equals((string)at.Data!, (string)bt.Data!) &&
						Equals((IDmdAssemblyName)at.Data2!, (IDmdAssemblyName)bt.Data2!);
				case DmdTypeScopeKind.AssemblyRef:
					return Equals((IDmdAssemblyName)at.Data2!, (IDmdAssemblyName)bt.Data!);
				default:
					throw new InvalidOperationException();
				}

			case DmdTypeScopeKind.AssemblyRef:
				switch (bt.Kind) {
				case DmdTypeScopeKind.Invalid:
					return false;
				case DmdTypeScopeKind.Module:
					return Equals((IDmdAssemblyName)at.Data!, ((DmdModule)bt.Data!).Assembly.GetName());
				case DmdTypeScopeKind.ModuleRef:
					return Equals((IDmdAssemblyName)at.Data!, (IDmdAssemblyName)bt.Data2!);
				case DmdTypeScopeKind.AssemblyRef:
					return Equals((IDmdAssemblyName)at.Data!, (IDmdAssemblyName)bt.Data!);
				default:
					throw new InvalidOperationException();
				}

			default:
				throw new InvalidOperationException();
			}
		}

		/// <summary>
		/// Gets the hash code of a member
		/// </summary>
		/// <param name="a">Member</param>
		/// <returns></returns>
		public int GetHashCode(DmdMemberInfo? a) {
			if (a is null)
				return 0;

			switch (a.MemberType) {
			case DmdMemberTypes.TypeInfo:
			case DmdMemberTypes.NestedType:
				return GetHashCode((DmdType)a);

			case DmdMemberTypes.Field:
				return GetHashCode((DmdFieldInfo)a);

			case DmdMemberTypes.Method:
			case DmdMemberTypes.Constructor:
				return GetHashCode((DmdMethodBase)a);

			case DmdMemberTypes.Property:
				return GetHashCode((DmdPropertyInfo)a);

			case DmdMemberTypes.Event:
				return GetHashCode((DmdEventInfo)a);
			}

			Debug.Fail($"Unknown type: {a.GetType()}");
			return 0;
		}

		/// <summary>
		/// Gets the hash code of a type
		/// </summary>
		/// <param name="a">Type</param>
		/// <returns></returns>
		public int GetHashCode(DmdType? a) {
			if (a is null)
				return 0;

			if (!IncrementRecursionCounter())
				return 0;

			int hc = CompareCustomModifiers ? GetHashCode(a.GetCustomModifiers()) : 0;
			switch (a.TypeSignatureKind) {
			case DmdTypeSignatureKind.Type:
				hc ^= a.DeclaringType is null ? HASHCODE_MAGIC_TYPE : HASHCODE_MAGIC_NESTED_TYPE;
				hc ^= MemberNameGetHashCode(a.MetadataName);
				hc ^= MemberNameGetHashCode(a.MetadataNamespace);
				hc ^= GetHashCode(a.DeclaringType);
				// Don't include the type scope in the hash since it can be one of Module, ModuleRef, AssemblyRef
				// and we could only hash the common denominator which isn't much at all.
				break;

			case DmdTypeSignatureKind.Pointer:
				hc ^= HASHCODE_MAGIC_ET_PTR ^ GetHashCode(a.GetElementType());
				break;

			case DmdTypeSignatureKind.ByRef:
				hc ^= HASHCODE_MAGIC_ET_BYREF ^ GetHashCode(a.GetElementType());
				break;

			case DmdTypeSignatureKind.SZArray:
				hc ^= HASHCODE_MAGIC_ET_SZARRAY ^ GetHashCode(a.GetElementType());
				break;

			case DmdTypeSignatureKind.TypeGenericParameter:
				hc ^= HASHCODE_MAGIC_ET_VAR ^ a.GenericParameterPosition;
				if (CompareGenericParameterDeclaringMember)
					hc ^= GetHashCode(a.DeclaringType);
				break;

			case DmdTypeSignatureKind.MethodGenericParameter:
				hc ^= HASHCODE_MAGIC_ET_MVAR ^ a.GenericParameterPosition;
				if (CompareGenericParameterDeclaringMember) {
					options &= ~DmdSigComparerOptions.CompareGenericParameterDeclaringMember;
					hc ^= GetHashCode(a.DeclaringMethod);
					options |= DmdSigComparerOptions.CompareGenericParameterDeclaringMember;
				}
				break;

			case DmdTypeSignatureKind.MDArray:
				hc ^= HASHCODE_MAGIC_ET_ARRAY;
				hc ^= a.GetArrayRank();
				if (!IgnoreMultiDimensionalArrayLowerBoundsAndSizes) {
					hc ^= GetHashCode(a.GetArraySizes());
					hc ^= GetHashCode(a.GetArrayLowerBounds());
				}
				hc ^= GetHashCode(a.GetElementType());
				break;

			case DmdTypeSignatureKind.GenericInstance:
				hc ^= HASHCODE_MAGIC_ET_GENERICINST;
				hc ^= GetHashCode(a.GetGenericTypeDefinition());
				hc ^= GetHashCode(a.GetGenericArguments());
				break;

			case DmdTypeSignatureKind.FunctionPointer:
				hc ^= HASHCODE_MAGIC_ET_FNPTR;
				hc ^= GetHashCode(a.GetFunctionPointerMethodSignature());
				break;

			default: throw new InvalidOperationException();
			}

			DecrementRecursionCounter();
			return hc;
		}

		/// <summary>
		/// Gets the hash code of a field
		/// </summary>
		/// <param name="a">Field</param>
		/// <returns></returns>
		public int GetHashCode(DmdFieldInfo? a) {
			if (a is null)
				return 0;

			int hc = MemberNameGetHashCode(a.Name);
			hc ^= GetHashCode(a.FieldType);
			if (CompareDeclaringType)
				hc ^= GetHashCode(a.DeclaringType);
			return hc;
		}

		/// <summary>
		/// Gets the hash code of a method or constructor
		/// </summary>
		/// <param name="a">Method or constructor</param>
		/// <returns></returns>
		public int GetHashCode(DmdMethodBase? a) {
			if (a is null)
				return 0;

			int hc = MemberNameGetHashCode(a.Name);
			hc ^= GetHashCode(a.GetMethodSignature());
			if (CompareDeclaringType)
				hc ^= GetHashCode(a.DeclaringType);
			return hc;
		}

		/// <summary>
		/// Gets the hash code of a property
		/// </summary>
		/// <param name="a">Property</param>
		/// <returns></returns>
		public int GetHashCode(DmdPropertyInfo? a) {
			if (a is null)
				return 0;

			int hc = MemberNameGetHashCode(a.Name);
			hc ^= GetHashCode(a.GetMethodSignature());
			if (CompareDeclaringType)
				hc ^= GetHashCode(a.DeclaringType);
			return hc;
		}

		/// <summary>
		/// Gets the hash code of an event
		/// </summary>
		/// <param name="a">Event</param>
		/// <returns></returns>
		public int GetHashCode(DmdEventInfo? a) {
			if (a is null)
				return 0;

			int hc = MemberNameGetHashCode(a.Name);
			hc ^= GetHashCode(a.EventHandlerType);
			if (CompareDeclaringType)
				hc ^= GetHashCode(a.DeclaringType);
			return hc;
		}

		/// <summary>
		/// Gets the hash code of a method parameter
		/// </summary>
		/// <param name="a">Method parameter</param>
		/// <returns></returns>
		public int GetHashCode(DmdParameterInfo? a) {
			if (a is null)
				return 0;

			int hc = a.Position;
			hc ^= GetHashCode(a.ParameterType);
			if (CompareDeclaringType)
				hc ^= GetHashCode(a.Member);
			return hc;
		}

		/// <summary>
		/// Gets the hash code of an assembly name
		/// </summary>
		/// <param name="a">Assembly name</param>
		/// <returns></returns>
		public int GetHashCode(IDmdAssemblyName? a) {
			if (a is null)
				return 0;
			return StringComparer.OrdinalIgnoreCase.GetHashCode(a.Name ?? string.Empty);
		}

		/// <summary>
		/// Gets the hash code of a method signature
		/// </summary>
		/// <param name="a">Method signature</param>
		/// <returns></returns>
		public int GetHashCode(DmdMethodSignature? a) {
			if (a is null)
				return 0;
			int hc = (int)a.Flags;
			hc ^= a.GenericParameterCount;
			if (!DontCompareReturnType)
				hc ^= GetHashCode(a.ReturnType);
			hc ^= GetHashCode(a.GetParameterTypes());
			hc ^= GetHashCode(a.GetVarArgsParameterTypes());
			return hc;
		}

		/// <summary>
		/// Gets the hash code of a custom modifier
		/// </summary>
		/// <param name="a">Custom modifier</param>
		/// <returns></returns>
		public int GetHashCode(DmdCustomModifier a) => (a.IsRequired ? -1 : 0) ^ GetHashCode(a.Type);

		int GetHashCode(ReadOnlyCollection<DmdCustomModifier>? a) {
			if (a is null)
				return 0;
			int hc = a.Count;
			for (int i = 0; i < a.Count; i++)
				hc ^= GetHashCode(a[i]);
			return hc;
		}

		int GetHashCode(ReadOnlyCollection<DmdType>? a) {
			if (a is null)
				return 0;
			int hc = a.Count;
			for (int i = 0; i < a.Count; i++)
				hc ^= GetHashCode(a[i]);
			return hc;
		}

		int GetHashCode(ReadOnlyCollection<int>? a) {
			if (a is null)
				return 0;
			int hc = a.Count;
			for (int i = 0; i < a.Count; i++)
				hc ^= a[i];
			return hc;
		}
	}
}
