/*
    Copyright (C) 2014-2017 de4dot@gmail.com

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
using System.Collections.ObjectModel;
using System.Runtime.InteropServices;

namespace dnSpy.Debugger.DotNet.Metadata.Impl {
	abstract class DmdTypeDef : DmdTypeBase {
		public override DmdTypeSignatureKind TypeSignatureKind => DmdTypeSignatureKind.Type;
		public override DmdTypeScope TypeScope => new DmdTypeScope(Module);
		public override bool IsMetadataReference => false;
		public override bool IsGenericType => GetGenericArguments().Count != 0;
		public override bool IsGenericTypeDefinition => GetGenericArguments().Count != 0;
		public override int MetadataToken => (int)(0x02000000 + rid);
		internal override bool HasTypeEquivalence {
			get {
				const byte BoolBit = 1;
				const byte InitializedBit = 2;
				const byte CalculatingBit = 4;
				if ((hasTypeEquivalenceFlags & (InitializedBit | CalculatingBit)) == 0) {
					lock (LockObject) {
						if ((hasTypeEquivalenceFlags & (InitializedBit | CalculatingBit)) == 0) {
							// In case we get called recursively
							hasTypeEquivalenceFlags |= CalculatingBit;

							byte result = InitializedBit;
							if (CalculateHasTypeEquivalence())
								result |= BoolBit;
							hasTypeEquivalenceFlags = result;
						}
					}
				}
				return (hasTypeEquivalenceFlags & BoolBit) != 0;
			}
		}
		byte hasTypeEquivalenceFlags;

		bool CalculateHasTypeEquivalence() {
			if (BaseType?.HasTypeEquivalence == true)
				return true;

			if (TIAHelper.IsTypeDefEquivalent(this))
				return true;

			var hash = GetAllInterfaces(this);
			foreach (var ifaceType in hash) {
				if (ifaceType.HasTypeEquivalence) {
					ObjectPools.Free(ref hash);
					return true;
				}
			}
			ObjectPools.Free(ref hash);

			return false;
		}

		public override StructLayoutAttribute StructLayoutAttribute {
			get {
				if (IsInterface || HasElementType || IsGenericParameter)
					return null;
				var (packingSize, classSize) = GetClassLayout();
				if (packingSize <= 0)
					packingSize = 8;

				LayoutKind layoutKind;
				switch (Attributes & DmdTypeAttributes.LayoutMask) {
				default:
				case DmdTypeAttributes.AutoLayout:			layoutKind = LayoutKind.Auto; break;
				case DmdTypeAttributes.SequentialLayout:	layoutKind = LayoutKind.Sequential; break;
				case DmdTypeAttributes.ExplicitLayout:		layoutKind = LayoutKind.Explicit; break;
				}

				CharSet charSet;
				switch (Attributes & DmdTypeAttributes.StringFormatMask) {
				case DmdTypeAttributes.AnsiClass:			charSet = CharSet.Ansi; break;
				case DmdTypeAttributes.UnicodeClass:		charSet = CharSet.Unicode; break;
				case DmdTypeAttributes.AutoClass:			charSet = CharSet.Auto; break;
				default:
				case DmdTypeAttributes.CustomFormatClass:	charSet = CharSet.None; break;
				}

				return new StructLayoutAttribute(layoutKind) {
					CharSet = charSet,
					Pack = packingSize,
					Size = classSize,
				};
			}
		}

		protected abstract (int packingSize, int classSize) GetClassLayout();

		public override DmdType DeclaringType {
			get {
				if (!declaringTypeInitd) {
					lock (LockObject) {
						if (!declaringTypeInitd) {
							__declaringType_DONT_USE = GetDeclaringType();
							declaringTypeInitd = true;
						}
					}
				}
				return __declaringType_DONT_USE;
			}
		}
		DmdType __declaringType_DONT_USE;
		bool declaringTypeInitd;

		public override DmdType BaseType {
			get {
				if (!baseTypeInitd) {
					lock (LockObject) {
						if (!baseTypeInitd) {
							__baseType_DONT_USE = GetBaseType(GetGenericArguments());
							if (IsImport && !IsInterface && __baseType_DONT_USE is DmdType bt && bt == AppDomain.System_Object) {
								if (IsWindowsRuntime)
									__baseType_DONT_USE = AppDomain.GetWellKnownType(DmdWellKnownType.System_Runtime_InteropServices_WindowsRuntime_RuntimeClass, isOptional: false, onlyCorlib: true) ?? bt;
								else
									__baseType_DONT_USE = AppDomain.GetWellKnownType(DmdWellKnownType.System___ComObject, isOptional: false, onlyCorlib: true) ?? bt;
							}
							baseTypeInitd = true;
						}
					}
				}
				return __baseType_DONT_USE;
			}
		}
		DmdType __baseType_DONT_USE;
		bool baseTypeInitd;

		protected abstract DmdType GetDeclaringType();
		protected abstract DmdType GetBaseTypeCore(IList<DmdType> genericTypeArguments);
		public DmdType GetBaseType(IList<DmdType> genericTypeArguments) => GetBaseTypeCore(genericTypeArguments);

		protected uint Rid => rid;
		readonly uint rid;

		protected DmdTypeDef(uint rid, IList<DmdCustomModifier> customModifiers) : base(customModifiers) => this.rid = rid;

		protected DmdTypeAttributes FixAttributes(DmdTypeAttributes flags) {
			if (Module.IsCorLib) {
				// See coreclr: RuntimeTypeHandle::GetAttributes
				if (MetadataName == "__ComObject" && MetadataNamespace == "System") {
					// This matches the original C++ code and should not be "& ~"
					flags = (flags & DmdTypeAttributes.VisibilityMask) | DmdTypeAttributes.Public;
				}
			}
			return flags;
		}

		protected override DmdType ResolveNoThrowCore() => this;

		protected abstract DmdType[] CreateGenericParameters();
		protected override ReadOnlyCollection<DmdType> GetGenericArgumentsCore() {
			if (__genericParameters_DONT_USE != null)
				return __genericParameters_DONT_USE;
			lock (LockObject) {
				if (__genericParameters_DONT_USE != null)
					return __genericParameters_DONT_USE;
				var res = CreateGenericParameters();
				__genericParameters_DONT_USE = ReadOnlyCollectionHelpers.Create(res);
				return __genericParameters_DONT_USE;
			}
		}
		ReadOnlyCollection<DmdType> __genericParameters_DONT_USE;

		public override DmdType GetGenericTypeDefinition() => IsGenericType ? this : throw new InvalidOperationException();

		public abstract DmdFieldInfo[] ReadDeclaredFields(DmdType declaringType, DmdType reflectedType, IList<DmdType> genericTypeArguments);
		public abstract DmdMethodBase[] ReadDeclaredMethods(DmdType declaringType, DmdType reflectedType, IList<DmdType> genericTypeArguments);
		public abstract DmdPropertyInfo[] ReadDeclaredProperties(DmdType declaringType, DmdType reflectedType, IList<DmdType> genericTypeArguments);
		public abstract DmdEventInfo[] ReadDeclaredEvents(DmdType declaringType, DmdType reflectedType, IList<DmdType> genericTypeArguments);

		public sealed override DmdFieldInfo[] CreateDeclaredFields(DmdType reflectedType) => ReadDeclaredFields(this, reflectedType, GetGenericArguments());
		public sealed override DmdMethodBase[] CreateDeclaredMethods(DmdType reflectedType) => ReadDeclaredMethods(this, reflectedType, GetGenericArguments());
		public sealed override DmdPropertyInfo[] CreateDeclaredProperties(DmdType reflectedType) => ReadDeclaredProperties(this, reflectedType, GetGenericArguments());
		public sealed override DmdEventInfo[] CreateDeclaredEvents(DmdType reflectedType) => ReadDeclaredEvents(this, reflectedType, GetGenericArguments());

		public override bool IsFullyResolved => true;
		public override DmdTypeBase FullResolve() => this;

		protected override IList<DmdType> ReadDeclaredInterfaces() => ReadDeclaredInterfacesCore(GetGenericArguments());
		internal IList<DmdType> ReadDeclaredInterfaces2() => ReadDeclaredInterfacesCore(GetGenericArguments());
		internal IList<DmdType> ReadDeclaredInterfaces(IList<DmdType> genericTypeArguments) => ReadDeclaredInterfacesCore(genericTypeArguments);
		protected abstract DmdType[] ReadDeclaredInterfacesCore(IList<DmdType> genericTypeArguments);

		protected abstract override DmdType[] CreateNestedTypes();
		internal DmdType[] CreateNestedTypes2() => CreateNestedTypes();

		public abstract override (DmdCustomAttributeData[] cas, DmdCustomAttributeData[] sas) CreateCustomAttributes();
	}
}
