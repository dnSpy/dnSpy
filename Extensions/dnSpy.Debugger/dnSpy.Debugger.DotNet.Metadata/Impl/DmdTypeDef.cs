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
using System.Collections.ObjectModel;
using System.Runtime.InteropServices;
using System.Threading;

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
				var f = ExtraFields;
				var flags = f.hasTypeEquivalenceFlags;
				if ((flags & (InitializedBit | CalculatingBit)) == 0) {
					// In case we get called recursively
					f.hasTypeEquivalenceFlags |= CalculatingBit;

					byte result = InitializedBit;
					if (CalculateHasTypeEquivalence())
						result |= BoolBit;
					f.hasTypeEquivalenceFlags = result;
					flags = result;
				}
				return (flags & BoolBit) != 0;
			}
		}

		bool CalculateHasTypeEquivalence() {
			if (BaseType?.HasTypeEquivalence == true)
				return true;

			if (TIAHelper.IsTypeDefEquivalent(this))
				return true;

			HashSet<DmdType>? hash = GetAllInterfaces(this);
			foreach (var ifaceType in hash) {
				if (ifaceType.HasTypeEquivalence) {
					ObjectPools.Free(ref hash);
					return true;
				}
			}
			ObjectPools.Free(ref hash);

			return false;
		}

		public override StructLayoutAttribute? StructLayoutAttribute {
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

		public override DmdType? DeclaringType {
			get {
				var f = ExtraFields;
				if (!f.declaringTypeInitd) {
					var declType = GetDeclaringType();
					lock (LockObject) {
						if (!f.declaringTypeInitd) {
							f.__declaringType_DONT_USE = declType;
							f.declaringTypeInitd = true;
						}
					}
				}
				return f.__declaringType_DONT_USE;
			}
		}

		public override DmdType? BaseType {
			get {
				var f = ExtraFields;
				if (!f.baseTypeInitd) {
					var newBT = GetBaseType(GetGenericArguments());
					if (IsImport && !IsInterface && newBT == AppDomain.System_Object) {
						if (IsWindowsRuntime)
							newBT = AppDomain.GetWellKnownType(DmdWellKnownType.System_Runtime_InteropServices_WindowsRuntime_RuntimeClass, isOptional: true, onlyCorlib: true) ?? newBT;
						else
							newBT = AppDomain.GetWellKnownType(DmdWellKnownType.System___ComObject, isOptional: true, onlyCorlib: true) ?? newBT;
					}

					lock (LockObject) {
						if (!f.baseTypeInitd) {
							f.__baseType_DONT_USE = newBT;
							f.baseTypeInitd = true;
						}
					}
				}
				return f.__baseType_DONT_USE;
			}
		}

		protected abstract DmdType? GetDeclaringType();
		protected abstract DmdType? GetBaseTypeCore(IList<DmdType> genericTypeArguments);
		public DmdType? GetBaseType(IList<DmdType> genericTypeArguments) => GetBaseTypeCore(genericTypeArguments);

		protected uint Rid => rid;
		readonly uint rid;

		protected DmdTypeDef(uint rid, IList<DmdCustomModifier>? customModifiers) : base(customModifiers) => this.rid = rid;

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

		protected override DmdType? ResolveNoThrowCore() => this;

		protected abstract DmdType[]? CreateGenericParameters();
		protected override ReadOnlyCollection<DmdType> GetGenericArgumentsCore() {
			var f = ExtraFields;
			// We loop here because the field could be cleared if it's a dynamic type
			for (;;) {
				var gps = f.__genericParameters_DONT_USE;
				if (!(gps is null))
					return gps;
				var res = CreateGenericParameters();
				Interlocked.CompareExchange(ref f.__genericParameters_DONT_USE, ReadOnlyCollectionHelpers.Create(res), null);
			}
		}

		public override DmdType GetGenericTypeDefinition() => IsGenericType ? this : throw new InvalidOperationException();

		public abstract DmdFieldInfo[]? ReadDeclaredFields(DmdType declaringType, DmdType reflectedType);
		public abstract DmdMethodBase[]? ReadDeclaredMethods(DmdType declaringType, DmdType reflectedType);
		public abstract DmdPropertyInfo[]? ReadDeclaredProperties(DmdType declaringType, DmdType reflectedType);
		public abstract DmdEventInfo[]? ReadDeclaredEvents(DmdType declaringType, DmdType reflectedType);

		public sealed override DmdFieldInfo[]? CreateDeclaredFields(DmdType reflectedType) => ReadDeclaredFields(this, reflectedType);
		public sealed override DmdMethodBase[]? CreateDeclaredMethods(DmdType reflectedType) => ReadDeclaredMethods(this, reflectedType);
		public sealed override DmdPropertyInfo[]? CreateDeclaredProperties(DmdType reflectedType) => ReadDeclaredProperties(this, reflectedType);
		public sealed override DmdEventInfo[]? CreateDeclaredEvents(DmdType reflectedType) => ReadDeclaredEvents(this, reflectedType);

		public override bool IsFullyResolved => true;
		public override DmdTypeBase? FullResolve() => this;

		public sealed override DmdType[]? ReadDeclaredInterfaces() => ReadDeclaredInterfacesCore(GetGenericArguments());
		public DmdType[]? ReadDeclaredInterfaces(IList<DmdType> genericTypeArguments) => ReadDeclaredInterfacesCore(genericTypeArguments);
		protected abstract DmdType[]? ReadDeclaredInterfacesCore(IList<DmdType> genericTypeArguments);

		public sealed override ReadOnlyCollection<DmdType> NestedTypes => NestedTypesCore;

		protected abstract override DmdType[]? CreateNestedTypes();
		public abstract override (DmdCustomAttributeData[]? cas, DmdCustomAttributeData[]? sas) CreateCustomAttributes();

		internal new void DynamicType_InvalidateCachedMembers() {
			if (__extraFields_DONT_USE is ExtraFieldsImpl f) {
				lock (LockObject) {
					f.declaringTypeInitd = false;
					f.baseTypeInitd = false;
				}
				// These aren't protected by a lock
				f.hasTypeEquivalenceFlags = 0;
				f.__genericParameters_DONT_USE = null;
			}
			base.DynamicType_InvalidateCachedMembers();
		}

		ExtraFieldsImpl ExtraFields {
			get {
				if (__extraFields_DONT_USE is ExtraFieldsImpl f)
					return f;
				Interlocked.CompareExchange(ref __extraFields_DONT_USE, new ExtraFieldsImpl(), null);
				return __extraFields_DONT_USE!;
			}
		}
		volatile ExtraFieldsImpl? __extraFields_DONT_USE;

		// Most of the fields aren't used so we alloc them when needed
		sealed class ExtraFieldsImpl {
			public volatile byte hasTypeEquivalenceFlags;
			public volatile DmdType? __declaringType_DONT_USE;
			public volatile bool declaringTypeInitd;
			public volatile DmdType? __baseType_DONT_USE;
			public volatile bool baseTypeInitd;
			public volatile ReadOnlyCollection<DmdType>? __genericParameters_DONT_USE;
		}
	}
}
