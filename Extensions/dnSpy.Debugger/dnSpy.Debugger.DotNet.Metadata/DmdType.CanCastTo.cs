// Ported from coreclr: RuntimeTypeHandle::CanCastTo
//
// Orig licenese header:
//
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using DDN = dnlib.DotNet;

namespace dnSpy.Debugger.DotNet.Metadata {
	abstract partial class DmdType {
		enum __CastResult { CannotCast, CanCast, MaybeCast }

		static bool __CanCastTo(DmdType from, DmdType to) {
			if ((object)from == null)
				throw new ArgumentNullException(nameof(from));
			if ((object)to == null)
				throw new ArgumentNullException(nameof(to));

			bool res;
			var r = from.__CanCastToNoGC(to);
			if (r == __CastResult.MaybeCast)
				res = from.__CanCastTo(to);
			else
				res = r == __CastResult.CanCast;

			if (!res && to.IsNullable && !from.__IsTypeDesc()) {
				if (__IsNullableForType(to, from))
					res = true;
			}

			return res;
		}

		static bool __IsNullableForType(DmdType type, DmdType param) {
			if (type.__IsTypeDesc())
				return false;
			if (!type.IsConstructedGenericType)
				return false;
			return __IsNullableForTypeHelper(type, param);
		}

		static bool __IsNullableForTypeHelper(DmdType nullable, DmdType param) {
			if (!nullable.IsNullable)
				return false;
			return param.IsEquivalentTo(nullable.GetNullableElementType());
		}

		__CastResult __CanCastToNoGC(DmdType type) {
			if (this == type)
				return __CastResult.CanCast;

			if (__IsTypeDesc())
				return __TypeDesc_CanCastToNoGC(type);

			if (type.__IsTypeDesc())
				return __CastResult.CannotCast;

			return __CanCastToClassOrInterfaceNoGC(type);
		}

		__CastResult __TypeDesc_CanCastToNoGC(DmdType toType) {
			Debug.Assert(this != toType);

			if (IsGenericParameter) {
				if (toType == AppDomain.System_Object)
					return __CastResult.CanCast;

				if (toType == AppDomain.System_ValueType)
					return __CastResult.MaybeCast;

				foreach (var constraint in GetGenericParameterConstraints()) {
					if (constraint.__CanCastToNoGC(toType) == __CastResult.CanCast)
						return __CastResult.CanCast;
				}
				return __CastResult.MaybeCast;
			}

			if (!toType.__IsTypeDesc()) {
				if (!IsArray)
					return __CastResult.CannotCast;
				return __CanCastToClassOrInterfaceNoGC(toType);
			}

			var toKind = toType.__GetInternalCorElementType();
			var fromKind = __GetInternalCorElementType();

			if (!(toKind == fromKind || (toKind == DDN.ElementType.Array && fromKind == DDN.ElementType.SZArray)))
				return __CastResult.CannotCast;

			switch (toKind) {
			case DDN.ElementType.Array:
				if (GetArrayRank() != toType.GetArrayRank())
					return __CastResult.CannotCast;
				goto case DDN.ElementType.SZArray;
			case DDN.ElementType.SZArray:
			case DDN.ElementType.ByRef:
			case DDN.ElementType.Ptr:
				return __CanCastParamNoGC(GetElementType(), toType.GetElementType());

			case DDN.ElementType.Var:
			case DDN.ElementType.MVar:
			case DDN.ElementType.FnPtr:
				return __CastResult.CannotCast;

			default:
				return __CastResult.CanCast;
			}
		}

		bool __TypeDesc_CanCastTo(DmdType toType) {
			if (this == toType)
				return true;

			if (IsGenericParameter) {
				if (toType == AppDomain.System_Object)
					return true;

				if (toType == AppDomain.System_ValueType) {
					if ((GenericParameterAttributes & DmdGenericParameterAttributes.NotNullableValueTypeConstraint) != 0)
						return true;
				}

				foreach (var constraint in GetGenericParameterConstraints()) {
					if (constraint.__CanCastTo(toType))
						return true;
				}
				return false;
			}

			if (!toType.__IsTypeDesc()) {
				if (!IsArray)
					return false;

				if (__CanCastToClassOrInterface(toType))
					return true;

				if (toType.IsInterface) {
					if (__ArraySupportsBizarreInterface(this, toType))
						return true;
				}

				return false;
			}

			var toKind = toType.__GetInternalCorElementType();
			var fromKind = __GetInternalCorElementType();

			if (!(toKind == fromKind || (toKind == DDN.ElementType.Array && fromKind == DDN.ElementType.SZArray)))
				return false;

			switch (toKind) {
			case DDN.ElementType.Array:
				if (GetArrayRank() != toType.GetArrayRank())
					return false;
				goto case DDN.ElementType.SZArray;
			case DDN.ElementType.SZArray:
			case DDN.ElementType.ByRef:
			case DDN.ElementType.Ptr:
				return __CanCastParam(GetElementType(), toType.GetElementType());

			case DDN.ElementType.Var:
			case DDN.ElementType.MVar:
			case DDN.ElementType.FnPtr:
				return false;

			default:
				return true;
			}
		}

		bool __ArraySupportsBizarreInterface(DmdType arrayType, DmdType @interface) {
			if (arrayType.__GetInternalCorElementType() != DDN.ElementType.SZArray)
				return false;

			if (!__IsImplicitInterfaceOfSZArray(@interface))
				return false;

			return __CanCastParam(arrayType.GetElementType(), @interface.GetGenericArguments()[0]);
		}

		bool __IsImplicitInterfaceOfSZArray(DmdType @interface) {
			var appDomain = AppDomain;
			if (@interface.GetGenericArguments().Count == 0 || !@interface.Assembly.IsCorLib)
				return false;

			var token = @interface.MetadataToken;
			return token == appDomain.System_Collections_Generic_IList_T.MetadataToken ||
					token == appDomain.System_Collections_Generic_ICollection_T.MetadataToken ||
					token == appDomain.System_Collections_Generic_IEnumerable_T.MetadataToken ||
					token == appDomain.GetWellKnownType(DmdWellKnownType.System_Collections_Generic_IReadOnlyCollection_T, isOptional: true, onlyCorlib: true)?.MetadataToken ||
					token == appDomain.GetWellKnownType(DmdWellKnownType.System_Collections_Generic_IReadOnlyList_T, isOptional: true, onlyCorlib: true)?.MetadataToken;
		}

		__CastResult __CanCastParamNoGC(DmdType fromParam, DmdType toParam) {
			if (fromParam == toParam)
				return __CastResult.CanCast;

			var fromParamCorType = fromParam.__GetVerifierCorElementType();
			if (__IsObjRef(fromParamCorType))
				return fromParam.__CanCastToNoGC(toParam);
			else if (__IsGenericVariable(fromParamCorType)) {
				if (!fromParam.__ConstrainedAsObjRef())
					return __CastResult.CannotCast;

				return fromParam.__CanCastToNoGC(toParam);
			}
			else if (__IsPrimitiveType(fromParamCorType)) {
				var toParamCorType = toParam.__GetVerifierCorElementType();
				if (__IsPrimitiveType(toParamCorType)) {
					if (toParamCorType == fromParamCorType)
						return __CastResult.CanCast;

					if ((toParamCorType != DDN.ElementType.Boolean)
						&& (fromParamCorType != DDN.ElementType.Boolean)
						&& (toParamCorType != DDN.ElementType.Char)
						&& (fromParamCorType != DDN.ElementType.Char)) {
						if ((__Size(toParamCorType) == __Size(fromParamCorType))
							&& (__IsFloat(toParamCorType) == __IsFloat(fromParamCorType))) {
							return __CastResult.CanCast;
						}
					}
				}
			}
			else {
				if (fromParam.HasTypeEquivalence)
					return __CastResult.MaybeCast;
				if (toParam.HasTypeEquivalence)
					return __CastResult.MaybeCast;
			}

			return __CastResult.CannotCast;
		}

		bool __CanCastParam(DmdType fromParam, DmdType toParam) {
			if (fromParam.IsEquivalentTo(toParam))
				return true;

			var fromParamCorType = fromParam.__GetVerifierCorElementType();
			if (__IsObjRef(fromParamCorType)) {
				return fromParam.__CanCastTo(toParam);
			}
			else if (__IsGenericVariable(fromParamCorType)) {
				if (!fromParam.__ConstrainedAsObjRef())
					return false;

				return fromParam.__CanCastTo(toParam);
			}
			else if (__IsPrimitiveType(fromParamCorType)) {
				var toParamCorType = toParam.__GetVerifierCorElementType();
				if (__IsPrimitiveType(toParamCorType)) {
					if (toParamCorType == fromParamCorType)
						return true;

					if ((toParamCorType != DDN.ElementType.Boolean)
						&& (fromParamCorType != DDN.ElementType.Boolean)
						&& (toParamCorType != DDN.ElementType.Char)
						&& (fromParamCorType != DDN.ElementType.Char)) {
						if ((__Size(toParamCorType) == __Size(fromParamCorType))
							&& (__IsFloat(toParamCorType) == __IsFloat(fromParamCorType))) {
							return true;
						}
					}
				}
			}

			return false;
		}

		bool __ConstrainedAsObjRef() {
			if ((GenericParameterAttributes & DmdGenericParameterAttributes.ReferenceTypeConstraint) != 0)
				return true;

			return __ConstrainedAsObjRefHelper(0);
		}

		bool __ConstrainedAsObjRefHelper(int recursionCounter) {
			if (recursionCounter >= 100)
				return false;

			foreach (var constraint in GetGenericParameterConstraints()) {
				if (constraint.IsGenericParameter && constraint.__ConstrainedAsObjRefHelper(recursionCounter + 1))
					return true;

				if (!constraint.IsInterface && __IsObjRef(constraint.__GetInternalCorElementType())) {
					if (constraint != AppDomain.System_Object &&
						constraint != AppDomain.System_ValueType &&
						constraint != AppDomain.System_Enum) {
						return true;
					}
				}
			}

			return false;
		}

		bool __IsFloat(DDN.ElementType type) => type == DDN.ElementType.R4 || type == DDN.ElementType.R8;
		bool __IsPrimitiveType(DDN.ElementType type) => (DDN.ElementType.Void <= type && type <= DDN.ElementType.R8) || type == DDN.ElementType.I || type == DDN.ElementType.U;
		bool __IsGenericVariable(DDN.ElementType type) => type == DDN.ElementType.Var || type == DDN.ElementType.MVar;

		bool __IsObjRef(DDN.ElementType type) {
			switch (type) {
			case DDN.ElementType.String:
			case DDN.ElementType.Class:
			case DDN.ElementType.Array:
			case DDN.ElementType.Object:
			case DDN.ElementType.SZArray:
				return true;

			default:
				return false;
			}
		}

		int __Size(DDN.ElementType type) {
			switch (type) {
			case DDN.ElementType.End:			return -1;
			case DDN.ElementType.Void:			return 0;
			case DDN.ElementType.Boolean:		return 1;
			case DDN.ElementType.Char:			return 2;
			case DDN.ElementType.I1:
			case DDN.ElementType.U1:			return 1;
			case DDN.ElementType.I2:
			case DDN.ElementType.U2:			return 2;
			case DDN.ElementType.I4:
			case DDN.ElementType.U4:			return 4;
			case DDN.ElementType.I8:
			case DDN.ElementType.U8:			return 8;
			case DDN.ElementType.R4:			return 4;
			case DDN.ElementType.R8:			return 8;
			case DDN.ElementType.String:
			case DDN.ElementType.Ptr:
			case DDN.ElementType.ByRef:			return AppDomain.Runtime.PointerSize;
			case DDN.ElementType.ValueType:		return -1;
			case DDN.ElementType.Class:
			case DDN.ElementType.Var:
			case DDN.ElementType.Array:
			case DDN.ElementType.GenericInst:
			case DDN.ElementType.TypedByRef:	return AppDomain.Runtime.PointerSize;
			case DDN.ElementType.ValueArray:	return -1;
			case DDN.ElementType.I:
			case DDN.ElementType.U:				return AppDomain.Runtime.PointerSize;
			case DDN.ElementType.R:				return -1;
			case DDN.ElementType.FnPtr:
			case DDN.ElementType.Object:
			case DDN.ElementType.SZArray:
			case DDN.ElementType.MVar:			return AppDomain.Runtime.PointerSize;
			case DDN.ElementType.CModReqd:
			case DDN.ElementType.CModOpt:
			case DDN.ElementType.Internal:		return 0;
			default:
				return 0;
			}
		}

		DDN.ElementType __ComputeInternalCorElementTypeForValueType() => DDN.ElementType.ValueType;//TODO:

		DDN.ElementType __GetInternalCorElementType() {
			var type = this;
			if (type.IsEnum)
				type = type.GetEnumUnderlyingType();

			var etype = type.__GetCorElementType();

			if (AppDomain.Runtime.Machine == DmdImageFileMachine.I386) {
				if (type == AppDomain.GetWellKnownType(DmdWellKnownType.System_ByReference_T, isOptional: true, onlyCorlib: true))
					return DDN.ElementType.ValueType;
				if (etype == DDN.ElementType.ValueType)
					etype = __ComputeInternalCorElementTypeForValueType();
			}
			else {
				if (type == AppDomain.GetWellKnownType(DmdWellKnownType.System_RuntimeArgumentHandle, isOptional: true, onlyCorlib: true))
					return DDN.ElementType.I;
				if (type == AppDomain.GetWellKnownType(DmdWellKnownType.System_RuntimeMethodHandleInternal, isOptional: true, onlyCorlib: true))
					return DDN.ElementType.I;
			}

			return etype;
		}

		DDN.ElementType __GetCorElementType() {
			switch (TypeSignatureKind) {
			case DmdTypeSignatureKind.Type:
			case DmdTypeSignatureKind.GenericInstance:
				if (MetadataNamespace == "System" && !IsNested) {
					switch (MetadataName) {
					case "Void":	if (this == AppDomain.System_Void)		return DDN.ElementType.Void; break;
					case "Boolean":	if (this == AppDomain.System_Boolean)	return DDN.ElementType.Boolean; break;
					case "Char":	if (this == AppDomain.System_Char)		return DDN.ElementType.Char; break;
					case "SByte":	if (this == AppDomain.System_SByte)		return DDN.ElementType.I1; break;
					case "Byte":	if (this == AppDomain.System_Byte)		return DDN.ElementType.U1; break;
					case "Int16":	if (this == AppDomain.System_Int16)		return DDN.ElementType.I2; break;
					case "UInt16":	if (this == AppDomain.System_UInt16)	return DDN.ElementType.U2; break;
					case "Int32":	if (this == AppDomain.System_Int32)		return DDN.ElementType.I4; break;
					case "UInt32":	if (this == AppDomain.System_UInt32)	return DDN.ElementType.U4; break;
					case "Int64":	if (this == AppDomain.System_Int64)		return DDN.ElementType.I8; break;
					case "UInt64":	if (this == AppDomain.System_UInt64)	return DDN.ElementType.U8; break;
					case "Single":	if (this == AppDomain.System_Single)	return DDN.ElementType.R4; break;
					case "Double":	if (this == AppDomain.System_Double)	return DDN.ElementType.R8; break;
					case "String":	if (this == AppDomain.System_String)	return DDN.ElementType.String; break;
					case "IntPtr":	if (this == AppDomain.System_IntPtr)	return DDN.ElementType.I; break;
					case "UIntPtr":	if (this == AppDomain.System_UIntPtr)	return DDN.ElementType.U; break;
					case "TypedReference": if (this == AppDomain.System_TypedReference) return DDN.ElementType.TypedByRef; break;
					case "Object":	if (this == AppDomain.System_Object)	return DDN.ElementType.Object; break;
					}
				}
				if (IsValueType)
					return DDN.ElementType.ValueType;
				return DDN.ElementType.Class;

			case DmdTypeSignatureKind.Pointer:
				return DDN.ElementType.Ptr;
			case DmdTypeSignatureKind.ByRef:
				return DDN.ElementType.ByRef;
			case DmdTypeSignatureKind.TypeGenericParameter:
				return DDN.ElementType.Var;
			case DmdTypeSignatureKind.MethodGenericParameter:
				return DDN.ElementType.MVar;
			case DmdTypeSignatureKind.SZArray:
				return DDN.ElementType.SZArray;
			case DmdTypeSignatureKind.MDArray:
				return DDN.ElementType.Array;
			case DmdTypeSignatureKind.FunctionPointer:
				return DDN.ElementType.FnPtr;
			default:
				throw new InvalidOperationException();
			}
		}

		DDN.ElementType __GetVerifierCorElementType() {
			var type = this;
			if (type.IsEnum)
				type = type.GetEnumUnderlyingType();
			return type.__GetCorElementType();
		}

		__CastResult __CanCastToClassOrInterfaceNoGC(DmdType target) {
			if (target.IsInterface)
				return __CanCastToInterfaceNoGC(target);
			else
				return __CanCastToClassNoGC(target);
		}

		bool __CanCastToClassOrInterface(DmdType target) {
			if (target.IsInterface)
				return __CanCastToInterface(target);
			else
				return __CanCastToClass(target);
		}

		__CastResult __CanCastToInterfaceNoGC(DmdType target) {
			if (!target.__HasVariance() && !IsArray && !HasTypeEquivalence && !target.HasTypeEquivalence)
				return __CanCastToNonVariantInterface(target) ? __CastResult.CanCast : __CastResult.CannotCast;
			return __CastResult.MaybeCast;
		}

		bool __CanCastToInterface(DmdType target) {
			if (!target.__HasVariance()) {
				if (HasTypeEquivalence || target.HasTypeEquivalence) {
					if (IsInterface && IsEquivalentTo(target))
						return true;

					return __ImplementsEquivalentInterface(target);
				}

				return __CanCastToNonVariantInterface(target);
			}
			else {
				if (__CanCastByVarianceToInterfaceOrDelegate(target))
					return true;

				var hash = GetAllInterfaces(this);
				foreach (var iface in hash) {
					if (iface.__CanCastByVarianceToInterfaceOrDelegate(target)) {
						ObjectPools.Free(ref hash);
						return true;
					}
				}
				ObjectPools.Free(ref hash);
			}
			return false;
		}

		bool __ImplementsEquivalentInterface(DmdType pInterface) {
			if (__ImplementsInterface(pInterface, 0))
				return true;

			if (!pInterface.HasTypeEquivalence)
				return false;

			return __ImplementsInterface(pInterface, int.MinValue);
		}

		bool __CanCastByVarianceToInterfaceOrDelegate(DmdType target) {
			if (MetadataToken != target.MetadataToken || Module != target.Module)
				return false;

			// The original code used a list to check for infinite recursion, we'll use this code unless it throws too often
			try {
				RuntimeHelpers.EnsureSufficientExecutionStack();
			}
			catch (InsufficientExecutionStackException) {
				Debug.Fail("Should probably not happen often");
				return false;
			}

			var inst = GetGenericArguments();
			if (inst.Count > 0) {
				var targetInst = target.GetGenericArguments();
				var targetInstGenParams = target.GetGenericTypeDefinition().GetGenericArguments();

				for (int i = 0; i < inst.Count; i++) {
					var thArg = inst[i];
					var thTargetArg = targetInst[i];

					if (!thArg.IsEquivalentTo(thTargetArg)) {
						switch (targetInstGenParams[i].GenericParameterAttributes & DmdGenericParameterAttributes.VarianceMask) {
						case DmdGenericParameterAttributes.Covariant:
							if (!thArg.__IsBoxedAndCanCastTo(thTargetArg))
								return false;
							break;

						case DmdGenericParameterAttributes.Contravariant:
							if (!thTargetArg.__IsBoxedAndCanCastTo(thArg))
								return false;
							break;

						case DmdGenericParameterAttributes.None:
							return false;

						default:
							return false;
						}
					}
				}
			}

			return true;
		}

		bool __IsBoxedAndCanCastTo(DmdType type) {
			var fromParamCorType = __GetVerifierCorElementType();

			if (__IsObjRef(fromParamCorType))
				return __CanCastTo(type);
			else if (__IsGenericVariable(fromParamCorType)) {
				if (__ConstrainedAsObjRef())
					return __CanCastTo(type);
			}

			return false;
		}

		__CastResult __CanCastToClassNoGC(DmdType target) {
			if (target.__HasVariance())
				return __CastResult.MaybeCast;

			if (HasTypeEquivalence || target.HasTypeEquivalence)
				return __CastResult.MaybeCast;
			else {
				var type = this;
				for (int i = 0; i < 1000; i++) {
					if (type == target)
						return __CastResult.CanCast;

					type = type.BaseType;
					if ((object)type == null)
						break;
				}
			}

			return __CastResult.CannotCast;
		}

		bool __CanCastToClass(DmdType pTargetMT) {
			var type = this;

			if (pTargetMT.__HasVariance()) {
				for (int i = 0; i < 1000; i++) {
					if (type.IsEquivalentTo(pTargetMT))
						return true;

					if (type.__CanCastByVarianceToInterfaceOrDelegate(pTargetMT))
						return true;

					type = type.BaseType;
					if ((object)type == null)
						break;
				}
			}

			else {
				for (int i = 0; i < 1000; i++) {
					if (type.IsEquivalentTo(pTargetMT))
						return true;

					type = type.BaseType;
					if ((object)type == null)
						break;
				}
			}

			return false;
		}

		bool __CanCastToNonVariantInterface(DmdType target) {
			if (this == target)
				return true;
			return __ImplementsInterface(target, 0);
		}

		bool __CanCastTo(DmdType type) {
			if (this == type)
				return true;

			if (__IsTypeDesc())
				return __TypeDesc_CanCastTo(type);

			if (type.__IsTypeDesc())
				return false;

			return __CanCastToClassOrInterface(type);
		}

		bool __HasVariance() {
			if (!IsGenericType)
				return false;
			foreach (var gpType in GetGenericTypeDefinition().GetGenericArguments()) {
				if ((gpType.GenericParameterAttributes & DmdGenericParameterAttributes.VarianceMask) != 0)
					return true;
			}
			return false;
		}

		bool __IsTypeDesc() {
			switch (TypeSignatureKind) {
			case DmdTypeSignatureKind.Type:
			case DmdTypeSignatureKind.GenericInstance:
				return false;

			case DmdTypeSignatureKind.Pointer:
			case DmdTypeSignatureKind.ByRef:
			case DmdTypeSignatureKind.TypeGenericParameter:
			case DmdTypeSignatureKind.MethodGenericParameter:
			case DmdTypeSignatureKind.SZArray:
			case DmdTypeSignatureKind.MDArray:
			case DmdTypeSignatureKind.FunctionPointer:
				return true;

			default:
				throw new InvalidOperationException();
			}
		}

		// recursionCounter[31] = check type equivalence
		bool __ImplementsInterface(DmdType ifaceType, int recursionCounter) {
			if ((recursionCounter & int.MaxValue) >= 100)
				return false;
			var type = this;
			for (;;) {
				var comparer = new DmdSigComparer(recursionCounter < 0 ? DmdMemberInfoEqualityComparer.DefaultTypeOptions | DmdSigComparerOptions.CheckTypeEquivalence : DmdMemberInfoEqualityComparer.DefaultTypeOptions);
				foreach (var iface in type.GetInterfaces()) {
					if (comparer.Equals(iface, ifaceType) || iface.__ImplementsInterface(ifaceType, recursionCounter + 1))
						return true;
				}
				type = type.BaseType;
				if ((object)type == null)
					break;
			}
			return false;
		}
	}
}
