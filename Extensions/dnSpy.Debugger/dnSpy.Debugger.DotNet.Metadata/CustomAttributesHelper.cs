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
using System.Linq;
using System.Runtime.InteropServices;

namespace dnSpy.Debugger.DotNet.Metadata {
	static class CustomAttributesHelper {
		public static bool IsDefined(ReadOnlyCollection<DmdCustomAttributeData> customAttributes, string attributeTypeFullName) {
			for (int i = 0; i < customAttributes.Count; i++) {
				if (customAttributes[i].AttributeType.FullName == attributeTypeFullName)
					return true;
			}
			return false;
		}

		public static bool IsDefined(ReadOnlyCollection<DmdCustomAttributeData> customAttributes, DmdType? attributeType) {
			for (int i = 0; i < customAttributes.Count; i++) {
				if (DmdMemberInfoEqualityComparer.DefaultType.Equals(customAttributes[i].AttributeType, attributeType))
					return true;
			}
			return false;
		}

		public static bool IsDefined(DmdType type, string attributeTypeFullName, bool inherit) {
			for (DmdType? currentType = type; currentType is not null; currentType = currentType.BaseType) {
				var customAttributes = currentType.GetCustomAttributesData();
				for (int i = 0; i < customAttributes.Count; i++) {
					var ca = customAttributes[i];
					if ((object)currentType != type && ca.IsPseudoCustomAttribute)
						continue;
					if (ca.AttributeType.FullName == attributeTypeFullName)
						return true;
				}
				if (!inherit)
					break;
			}
			return false;
		}

		public static bool IsDefined(DmdType type, DmdType? attributeType, bool inherit) {
			for (DmdType? currentType = type; currentType is not null; currentType = currentType.BaseType) {
				var customAttributes = currentType.GetCustomAttributesData();
				for (int i = 0; i < customAttributes.Count; i++) {
					var ca = customAttributes[i];
					if ((object)currentType != type && ca.IsPseudoCustomAttribute)
						continue;
					if (DmdMemberInfoEqualityComparer.DefaultType.Equals(ca.AttributeType, attributeType))
						return true;
				}
				if (!inherit)
					break;
			}
			return false;
		}

		public static bool IsDefined(DmdMethodInfo method, string attributeTypeFullName, bool inherit) {
			for (DmdMethodInfo? currentMethod = method; currentMethod is not null; currentMethod = currentMethod.GetParentDefinition()) {
				var customAttributes = currentMethod.GetCustomAttributesData();
				for (int i = 0; i < customAttributes.Count; i++) {
					var ca = customAttributes[i];
					if ((object)currentMethod != method && ca.IsPseudoCustomAttribute)
						continue;
					if (ca.AttributeType.FullName == attributeTypeFullName)
						return true;
				}
				if (!inherit)
					break;
			}
			return false;
		}

		public static bool IsDefined(DmdMethodInfo method, DmdType? attributeType, bool inherit) {
			for (DmdMethodInfo? currentMethod = method; currentMethod is not null; currentMethod = currentMethod.GetParentDefinition()) {
				var customAttributes = currentMethod.GetCustomAttributesData();
				for (int i = 0; i < customAttributes.Count; i++) {
					var ca = customAttributes[i];
					if ((object)currentMethod != method && ca.IsPseudoCustomAttribute)
						continue;
					if (DmdMemberInfoEqualityComparer.DefaultType.Equals(ca.AttributeType, attributeType))
						return true;
				}
				if (!inherit)
					break;
			}
			return false;
		}

		public static DmdCustomAttributeData? Find(ReadOnlyCollection<DmdCustomAttributeData> customAttributes, string attributeTypeFullName) {
			for (int i = 0; i < customAttributes.Count; i++) {
				var ca = customAttributes[i];
				if (ca.AttributeType.FullName == attributeTypeFullName)
					return ca;
			}
			return null;
		}

		public static DmdCustomAttributeData? Find(ReadOnlyCollection<DmdCustomAttributeData> customAttributes, DmdType? attributeType) {
			for (int i = 0; i < customAttributes.Count; i++) {
				var ca = customAttributes[i];
				if (DmdMemberInfoEqualityComparer.DefaultType.Equals(ca.AttributeType, attributeType))
					return ca;
			}
			return null;
		}

		public static DmdCustomAttributeData? Find(DmdType type, string attributeTypeFullName, bool inherit) {
			for (DmdType? currentType = type; currentType is not null; currentType = currentType.BaseType) {
				var customAttributes = currentType.GetCustomAttributesData();
				for (int i = 0; i < customAttributes.Count; i++) {
					var ca = customAttributes[i];
					if ((object)currentType != type && ca.IsPseudoCustomAttribute)
						continue;
					if (ca.AttributeType.FullName == attributeTypeFullName)
						return ca;
				}
				if (!inherit)
					break;
			}
			return null;
		}

		public static DmdCustomAttributeData? Find(DmdType type, DmdType? attributeType, bool inherit) {
			for (DmdType? currentType = type; currentType is not null; currentType = currentType.BaseType) {
				var customAttributes = currentType.GetCustomAttributesData();
				for (int i = 0; i < customAttributes.Count; i++) {
					var ca = customAttributes[i];
					if ((object)currentType != type && ca.IsPseudoCustomAttribute)
						continue;
					if (DmdMemberInfoEqualityComparer.DefaultType.Equals(ca.AttributeType, attributeType))
						return ca;
				}
				if (!inherit)
					break;
			}
			return null;
		}

		public static DmdCustomAttributeData? Find(DmdMethodInfo method, string attributeTypeFullName, bool inherit) {
			for (DmdMethodInfo? currentMethod = method; currentMethod is not null; currentMethod = currentMethod.GetParentDefinition()) {
				var customAttributes = currentMethod.GetCustomAttributesData();
				for (int i = 0; i < customAttributes.Count; i++) {
					var ca = customAttributes[i];
					if ((object)currentMethod != method && ca.IsPseudoCustomAttribute)
						continue;
					if (ca.AttributeType.FullName == attributeTypeFullName)
						return ca;
				}
				if (!inherit)
					break;
			}
			return null;
		}

		public static DmdCustomAttributeData? Find(DmdMethodInfo method, DmdType? attributeType, bool inherit) {
			for (DmdMethodInfo? currentMethod = method; currentMethod is not null; currentMethod = currentMethod.GetParentDefinition()) {
				var customAttributes = currentMethod.GetCustomAttributesData();
				for (int i = 0; i < customAttributes.Count; i++) {
					var ca = customAttributes[i];
					if ((object)currentMethod != method && ca.IsPseudoCustomAttribute)
						continue;
					if (DmdMemberInfoEqualityComparer.DefaultType.Equals(ca.AttributeType, attributeType))
						return ca;
				}
				if (!inherit)
					break;
			}
			return null;
		}

		readonly struct SecurityAttributeInfo {
			public int Count { get; }

			public SecurityAttributeInfo(ReadOnlyCollection<DmdCustomAttributeData> securityAttributes) => Count = securityAttributes.Count;

			public void CopyTo(DmdCustomAttributeData[] destination, ref int index, ReadOnlyCollection<DmdCustomAttributeData> securityAttributes) {
				if (Count == 0)
					return;
				for (int i = 0; i < securityAttributes.Count; i++) {
					var sa = securityAttributes[i];
					// Reflection uses the first public constructor it finds and uses empty ctor args and named args
					var ctor = sa.AttributeType.GetConstructors().FirstOrDefault() ?? sa.Constructor;
					destination[index++] = new DmdCustomAttributeData(ctor, null, null, isPseudoCustomAttribute: true);
				}
			}
		}

		readonly struct SerializableAttributeInfo {
			public int Count => ctor is not null ? 1 : 0;
			readonly DmdConstructorInfo? ctor;

			public SerializableAttributeInfo(DmdType type) {
				if ((type.Attributes & DmdTypeAttributes.Serializable) != 0) {
					var caType = type.AppDomain.GetWellKnownType(DmdWellKnownType.System_SerializableAttribute, isOptional: true);
					ctor = caType?.GetConstructor(Array.Empty<DmdType>());
					Debug2.Assert(caType is null || ctor is not null);
				}
				else
					ctor = null;
			}

			public void CopyTo(DmdCustomAttributeData[] destination, ref int index) {
				if (Count == 0)
					return;
				destination[index++] = new DmdCustomAttributeData(ctor!, null, null, isPseudoCustomAttribute: true);
			}
		}

		readonly struct ComImportAttributeInfo {
			public int Count => ctor is not null ? 1 : 0;
			readonly DmdConstructorInfo? ctor;

			public ComImportAttributeInfo(DmdType type) {
				if (type.IsImport) {
					var caType = type.AppDomain.GetWellKnownType(DmdWellKnownType.System_Runtime_InteropServices_ComImportAttribute, isOptional: true);
					ctor = caType?.GetConstructor(Array.Empty<DmdType>());
					Debug2.Assert(caType is null || ctor is not null);
				}
				else
					ctor = null;
			}

			public void CopyTo(DmdCustomAttributeData[] destination, ref int index) {
				if (Count == 0)
					return;
				destination[index++] = new DmdCustomAttributeData(ctor!, null, null, isPseudoCustomAttribute: true);
			}
		}

		readonly struct MarshalAsAttributeInfo {
			public int Count => ctor is not null ? 1 : 0;
			readonly DmdConstructorInfo? ctor;

			public MarshalAsAttributeInfo(DmdFieldInfo field, DmdMarshalType? marshalType) => ctor = Initialize(field.AppDomain, marshalType);
			public MarshalAsAttributeInfo(DmdParameterInfo parameter, DmdMarshalType? marshalType) => ctor = Initialize(parameter.Member.AppDomain, marshalType);

			static DmdConstructorInfo? Initialize(DmdAppDomain appDomain, DmdMarshalType? marshalType) {
				if (marshalType is null)
					return null;
				var caType = appDomain.GetWellKnownType(DmdWellKnownType.System_Runtime_InteropServices_MarshalAsAttribute, isOptional: true);
				var unmanagedTypeType = appDomain.GetWellKnownType(DmdWellKnownType.System_Runtime_InteropServices_UnmanagedType, isOptional: true);
				var varEnumType = appDomain.GetWellKnownType(DmdWellKnownType.System_Runtime_InteropServices_VarEnum, isOptional: true);
				if (caType is null || unmanagedTypeType is null || varEnumType is null)
					return null;
				var ctor = caType.GetConstructor(new[] { unmanagedTypeType });
				Debug2.Assert(ctor is not null);
				return ctor;
			}

			public void CopyTo(DmdCustomAttributeData[] destination, ref int index, DmdMarshalType? marshalType) {
				if (Count == 0)
					return;
				int argsCount = 5;
				if (marshalType!.MarshalType is not null)
					argsCount++;
				if (marshalType.MarshalTypeRef is not null)
					argsCount++;
				if (marshalType.MarshalCookie is not null)
					argsCount++;
				if (marshalType.SafeArrayUserDefinedSubType is not null)
					argsCount++;
				var type = ctor!.ReflectedType!;
				var appDomain = type.AppDomain;
				var unmanagedTypeType = appDomain.GetWellKnownType(DmdWellKnownType.System_Runtime_InteropServices_UnmanagedType);
				var varEnumType = appDomain.GetWellKnownType(DmdWellKnownType.System_Runtime_InteropServices_VarEnum);
				var namedArgs = new DmdCustomAttributeNamedArgument[argsCount];
				int w = 0;
				namedArgs[w++] = new DmdCustomAttributeNamedArgument(type.GetField("ArraySubType"), new DmdCustomAttributeTypedArgument(unmanagedTypeType, (int)marshalType.ArraySubType));
				namedArgs[w++] = new DmdCustomAttributeNamedArgument(type.GetField("SizeParamIndex"), new DmdCustomAttributeTypedArgument(appDomain.System_Int16, marshalType.SizeParamIndex));
				namedArgs[w++] = new DmdCustomAttributeNamedArgument(type.GetField("SizeConst"), new DmdCustomAttributeTypedArgument(appDomain.System_Int32, marshalType.SizeConst));
				namedArgs[w++] = new DmdCustomAttributeNamedArgument(type.GetField("IidParameterIndex"), new DmdCustomAttributeTypedArgument(appDomain.System_Int32, marshalType.IidParameterIndex));
				namedArgs[w++] = new DmdCustomAttributeNamedArgument(type.GetField("SafeArraySubType"), new DmdCustomAttributeTypedArgument(varEnumType, (int)marshalType.SafeArraySubType));
				if (marshalType.MarshalType is not null)
					namedArgs[w++] = new DmdCustomAttributeNamedArgument(type.GetField("MarshalType"), new DmdCustomAttributeTypedArgument(appDomain.System_String, marshalType.MarshalType));
				if (marshalType.MarshalTypeRef is not null)
					namedArgs[w++] = new DmdCustomAttributeNamedArgument(type.GetField("MarshalTypeRef"), new DmdCustomAttributeTypedArgument(appDomain.System_Type, marshalType.MarshalTypeRef));
				if (marshalType.MarshalCookie is not null)
					namedArgs[w++] = new DmdCustomAttributeNamedArgument(type.GetField("MarshalCookie"), new DmdCustomAttributeTypedArgument(appDomain.System_String, marshalType.MarshalCookie));
				if (marshalType.SafeArrayUserDefinedSubType is not null)
					namedArgs[w++] = new DmdCustomAttributeNamedArgument(type.GetField("SafeArrayUserDefinedSubType"), new DmdCustomAttributeTypedArgument(appDomain.System_Type, marshalType.SafeArrayUserDefinedSubType));
				if (namedArgs.Length != w)
					throw new InvalidOperationException();
				var ctorArgs = new[] { new DmdCustomAttributeTypedArgument(unmanagedTypeType, (int)marshalType.Value) };
				destination[index++] = new DmdCustomAttributeData(ctor, ctorArgs, namedArgs, isPseudoCustomAttribute: true);
			}
		}

		readonly struct FieldOffsetAttributeInfo {
			public int Count => ctor is not null ? 1 : 0;
			readonly DmdConstructorInfo? ctor;
			readonly int offset;

			public FieldOffsetAttributeInfo(DmdFieldInfo field, uint? offset) {
				if (offset is not null) {
					this.offset = (int)offset.Value;
					var caType = field.AppDomain.GetWellKnownType(DmdWellKnownType.System_Runtime_InteropServices_FieldOffsetAttribute, isOptional: true);
					ctor = caType?.GetConstructor(new[] { field.AppDomain.System_Int32 });
					Debug2.Assert(caType is null || ctor is not null);
				}
				else {
					ctor = null;
					this.offset = 0;
				}
			}

			public void CopyTo(DmdCustomAttributeData[] destination, ref int index) {
				if (Count == 0)
					return;
				var ctorArgs = new[] { new DmdCustomAttributeTypedArgument(ctor!.AppDomain.System_Int32, offset) };
				destination[index++] = new DmdCustomAttributeData(ctor, ctorArgs, null, isPseudoCustomAttribute: true);
			}
		}

		readonly struct NonSerializedAttributeInfo {
			public int Count => ctor is not null ? 1 : 0;
			readonly DmdConstructorInfo? ctor;

			public NonSerializedAttributeInfo(DmdFieldInfo field) {
				if ((field.Attributes & DmdFieldAttributes.NotSerialized) != 0) {
					var caType = field.AppDomain.GetWellKnownType(DmdWellKnownType.System_NonSerializedAttribute, isOptional: true);
					ctor = caType?.GetConstructor(Array.Empty<DmdType>());
					Debug2.Assert(caType is null || ctor is not null);
				}
				else
					ctor = null;
			}

			public void CopyTo(DmdCustomAttributeData[] destination, ref int index) {
				if (Count == 0)
					return;
				destination[index++] = new DmdCustomAttributeData(ctor!, null, null, isPseudoCustomAttribute: true);
			}
		}

		readonly struct DllImportAttributeInfo {
			public int Count => ctor is not null ? 1 : 0;
			readonly DmdConstructorInfo? ctor;

			public DllImportAttributeInfo(DmdMethodInfo method, in DmdImplMap? implMap) {
				if (implMap is not null) {
					var appDomain = method.AppDomain;
					var caType = appDomain.GetWellKnownType(DmdWellKnownType.System_Runtime_InteropServices_DllImportAttribute, isOptional: true);
					var charSetType = appDomain.GetWellKnownType(DmdWellKnownType.System_Runtime_InteropServices_CharSet, isOptional: true);
					var callingConventionType = appDomain.GetWellKnownType(DmdWellKnownType.System_Runtime_InteropServices_CallingConvention, isOptional: true);
					if (charSetType is null || callingConventionType is null || caType is null)
						ctor = null;
					else {
						ctor = caType.GetConstructor(DmdBindingFlags.Public | DmdBindingFlags.NonPublic | DmdBindingFlags.Instance, new[] { appDomain.System_String });
						Debug2.Assert(ctor is not null);
					}
				}
				else
					ctor = null;
			}

			public void CopyTo(DmdCustomAttributeData[] destination, ref int index, DmdMethodInfo method, in DmdImplMap? implMap) {
				if (Count == 0)
					return;

				var appDomain = ctor!.AppDomain;
				var im = implMap!.Value;
				var attributes = im.Attributes;
				var charSetType = appDomain.GetWellKnownType(DmdWellKnownType.System_Runtime_InteropServices_CharSet);
				var callingConventionType = appDomain.GetWellKnownType(DmdWellKnownType.System_Runtime_InteropServices_CallingConvention);

				CharSet charSet;
				switch (attributes & DmdPInvokeAttributes.CharSetMask) {
				default:
				case DmdPInvokeAttributes.CharSetNotSpec:	charSet = CharSet.None; break;
				case DmdPInvokeAttributes.CharSetAnsi:		charSet = CharSet.Ansi; break;
				case DmdPInvokeAttributes.CharSetUnicode:	charSet = CharSet.Unicode; break;
				case DmdPInvokeAttributes.CharSetAuto:		charSet = CharSet.Auto; break;
				}

				CallingConvention callingConvention;
				switch (attributes & DmdPInvokeAttributes.CallConvMask) {
				case DmdPInvokeAttributes.CallConvWinapi:		callingConvention = CallingConvention.Winapi; break;
				case DmdPInvokeAttributes.CallConvCdecl:		callingConvention = CallingConvention.Cdecl; break;
				case DmdPInvokeAttributes.CallConvStdcall:		callingConvention = CallingConvention.StdCall; break;
				case DmdPInvokeAttributes.CallConvThiscall:		callingConvention = CallingConvention.ThisCall; break;
				case DmdPInvokeAttributes.CallConvFastcall:		callingConvention = CallingConvention.FastCall; break;
				default:										callingConvention = CallingConvention.Cdecl; break;
				}

				var ctorArgs = new[] { new DmdCustomAttributeTypedArgument(appDomain.System_String, im.Module) };
				var type = ctor.ReflectedType!;
				var namedArgs = new DmdCustomAttributeNamedArgument[8] {
					new DmdCustomAttributeNamedArgument(type.GetField("EntryPoint"), new DmdCustomAttributeTypedArgument(appDomain.System_String, im.Name)),
					new DmdCustomAttributeNamedArgument(type.GetField("CharSet"), new DmdCustomAttributeTypedArgument(charSetType, (int)charSet)),
					new DmdCustomAttributeNamedArgument(type.GetField("ExactSpelling"), new DmdCustomAttributeTypedArgument(appDomain.System_Boolean, (attributes & DmdPInvokeAttributes.NoMangle) != 0)),
					new DmdCustomAttributeNamedArgument(type.GetField("SetLastError"), new DmdCustomAttributeTypedArgument(appDomain.System_Boolean, (attributes & DmdPInvokeAttributes.SupportsLastError) != 0)),
					new DmdCustomAttributeNamedArgument(type.GetField("PreserveSig"), new DmdCustomAttributeTypedArgument(appDomain.System_Boolean, method.IsPreserveSig)),
					new DmdCustomAttributeNamedArgument(type.GetField("CallingConvention"), new DmdCustomAttributeTypedArgument(callingConventionType, (int)callingConvention)),
					new DmdCustomAttributeNamedArgument(type.GetField("BestFitMapping"), new DmdCustomAttributeTypedArgument(appDomain.System_Boolean, (attributes & DmdPInvokeAttributes.BestFitMask) == DmdPInvokeAttributes.BestFitEnabled)),
					new DmdCustomAttributeNamedArgument(type.GetField("ThrowOnUnmappableChar"), new DmdCustomAttributeTypedArgument(appDomain.System_Boolean, (attributes & DmdPInvokeAttributes.ThrowOnUnmappableCharMask) == DmdPInvokeAttributes.ThrowOnUnmappableCharEnabled)),
				};
				destination[index++] = new DmdCustomAttributeData(ctor, ctorArgs, namedArgs, isPseudoCustomAttribute: true);
			}
		}

		readonly struct PreserveSigAttributeInfo {
			public int Count => ctor is not null ? 1 : 0;
			readonly DmdConstructorInfo? ctor;

			public PreserveSigAttributeInfo(DmdMethodInfo method) {
				if ((method.MethodImplementationFlags & DmdMethodImplAttributes.PreserveSig) != 0) {
					var caType = method.AppDomain.GetWellKnownType(DmdWellKnownType.System_Runtime_InteropServices_PreserveSigAttribute, isOptional: true);
					ctor = caType?.GetConstructor(Array.Empty<DmdType>());
					Debug2.Assert(caType is null || ctor is not null);
				}
				else
					ctor = null;
			}

			public void CopyTo(DmdCustomAttributeData[] destination, ref int index) {
				if (Count == 0)
					return;
				destination[index++] = new DmdCustomAttributeData(ctor!, null, null, isPseudoCustomAttribute: true);
			}
		}

		readonly struct InAttributeInfo {
			public int Count => ctor is not null ? 1 : 0;
			readonly DmdConstructorInfo? ctor;

			public InAttributeInfo(DmdParameterInfo parameter) {
				if (parameter.IsIn) {
					var caType = parameter.Member.AppDomain.GetWellKnownType(DmdWellKnownType.System_Runtime_InteropServices_InAttribute, isOptional: true);
					ctor = caType?.GetConstructor(Array.Empty<DmdType>());
					Debug2.Assert(caType is null || ctor is not null);
				}
				else
					ctor = null;
			}

			public void CopyTo(DmdCustomAttributeData[] destination, ref int index) {
				if (Count == 0)
					return;
				destination[index++] = new DmdCustomAttributeData(ctor!, null, null, isPseudoCustomAttribute: true);
			}
		}

		readonly struct OutAttributeInfo {
			public int Count => ctor is not null ? 1 : 0;
			readonly DmdConstructorInfo? ctor;

			public OutAttributeInfo(DmdParameterInfo parameter) {
				if (parameter.IsOut) {
					var caType = parameter.Member.AppDomain.GetWellKnownType(DmdWellKnownType.System_Runtime_InteropServices_OutAttribute, isOptional: true);
					ctor = caType?.GetConstructor(Array.Empty<DmdType>());
					Debug2.Assert(caType is null || ctor is not null);
				}
				else
					ctor = null;
			}

			public void CopyTo(DmdCustomAttributeData[] destination, ref int index) {
				if (Count == 0)
					return;
				destination[index++] = new DmdCustomAttributeData(ctor!, null, null, isPseudoCustomAttribute: true);
			}
		}

		readonly struct OptionalAttributeInfo {
			public int Count => ctor is not null ? 1 : 0;
			readonly DmdConstructorInfo? ctor;

			public OptionalAttributeInfo(DmdParameterInfo parameter) {
				if (parameter.IsOptional) {
					var caType = parameter.Member.AppDomain.GetWellKnownType(DmdWellKnownType.System_Runtime_InteropServices_OptionalAttribute, isOptional: true);
					ctor = caType?.GetConstructor(Array.Empty<DmdType>());
					Debug2.Assert(caType is null || ctor is not null);
				}
				else
					ctor = null;
			}

			public void CopyTo(DmdCustomAttributeData[] destination, ref int index) {
				if (Count == 0)
					return;
				destination[index++] = new DmdCustomAttributeData(ctor!, null, null, isPseudoCustomAttribute: true);
			}
		}

		public static ReadOnlyCollection<DmdCustomAttributeData> AddPseudoCustomAttributes(DmdType type, DmdCustomAttributeData[]? customAttributes, ReadOnlyCollection<DmdCustomAttributeData> securityAttributes) {
			if (customAttributes is null)
				customAttributes = Array.Empty<DmdCustomAttributeData>();

			var serializableAttributeInfo = new SerializableAttributeInfo(type);
			var comImportAttributeInfo = new ComImportAttributeInfo(type);
			var securityAttributeInfo = new SecurityAttributeInfo(securityAttributes);

			int pseudoCount = serializableAttributeInfo.Count + comImportAttributeInfo.Count + securityAttributeInfo.Count;
			if (pseudoCount != 0) {
				var cas = new DmdCustomAttributeData[pseudoCount + customAttributes.Length];
				int index = 0;
				serializableAttributeInfo.CopyTo(cas, ref index);
				comImportAttributeInfo.CopyTo(cas, ref index);
				securityAttributeInfo.CopyTo(cas, ref index, securityAttributes);
				if (pseudoCount != index)
					throw new InvalidOperationException();
				Array.Copy(customAttributes, 0, cas, pseudoCount, customAttributes.Length);
				customAttributes = cas;
			}

			return ReadOnlyCollectionHelpers.Create(customAttributes);
		}

		public static ReadOnlyCollection<DmdCustomAttributeData> AddPseudoCustomAttributes(DmdFieldInfo field, DmdCustomAttributeData[]? customAttributes, uint? fieldOffset, DmdMarshalType? marshalType) {
			if (customAttributes is null)
				customAttributes = Array.Empty<DmdCustomAttributeData>();

			var marshalAsAttributeInfo = new MarshalAsAttributeInfo(field, marshalType);
			var fieldOffsetAttributeInfo = new FieldOffsetAttributeInfo(field, fieldOffset);
			var nonSerializedAttributeInfo = new NonSerializedAttributeInfo(field);

			int pseudoCount = marshalAsAttributeInfo.Count + fieldOffsetAttributeInfo.Count + nonSerializedAttributeInfo.Count;
			if (pseudoCount != 0) {
				var cas = new DmdCustomAttributeData[pseudoCount + customAttributes.Length];
				int index = 0;
				marshalAsAttributeInfo.CopyTo(cas, ref index, marshalType);
				fieldOffsetAttributeInfo.CopyTo(cas, ref index);
				nonSerializedAttributeInfo.CopyTo(cas, ref index);
				if (pseudoCount != index)
					throw new InvalidOperationException();
				Array.Copy(customAttributes, 0, cas, pseudoCount, customAttributes.Length);
				customAttributes = cas;
			}

			return ReadOnlyCollectionHelpers.Create(customAttributes);
		}

		public static ReadOnlyCollection<DmdCustomAttributeData> AddPseudoCustomAttributes(DmdConstructorInfo ctor, DmdCustomAttributeData[]? customAttributes, ReadOnlyCollection<DmdCustomAttributeData> securityAttributes) {
			if (customAttributes is null)
				customAttributes = Array.Empty<DmdCustomAttributeData>();

			var securityAttributeInfo = new SecurityAttributeInfo(securityAttributes);

			int pseudoCount = securityAttributeInfo.Count;
			if (pseudoCount != 0) {
				var cas = new DmdCustomAttributeData[pseudoCount + customAttributes.Length];
				int index = 0;
				securityAttributeInfo.CopyTo(cas, ref index, securityAttributes);
				if (pseudoCount != index)
					throw new InvalidOperationException();
				Array.Copy(customAttributes, 0, cas, pseudoCount, customAttributes.Length);
				customAttributes = cas;
			}

			return ReadOnlyCollectionHelpers.Create(customAttributes);
		}

		public static ReadOnlyCollection<DmdCustomAttributeData> AddPseudoCustomAttributes(DmdMethodInfo method, DmdCustomAttributeData[]? customAttributes, ReadOnlyCollection<DmdCustomAttributeData> securityAttributes, in DmdImplMap? implMap) {
			if (customAttributes is null)
				customAttributes = Array.Empty<DmdCustomAttributeData>();

			var dllImportAttributeInfo = new DllImportAttributeInfo(method, implMap);
			var preserveSigAttributeInfo = new PreserveSigAttributeInfo(method);
			var securityAttributeInfo = new SecurityAttributeInfo(securityAttributes);

			int pseudoCount = dllImportAttributeInfo.Count + preserveSigAttributeInfo.Count + securityAttributeInfo.Count;
			if (pseudoCount != 0) {
				var cas = new DmdCustomAttributeData[pseudoCount + customAttributes.Length];
				int index = 0;
				dllImportAttributeInfo.CopyTo(cas, ref index, method, implMap);
				preserveSigAttributeInfo.CopyTo(cas, ref index);
				securityAttributeInfo.CopyTo(cas, ref index, securityAttributes);
				if (pseudoCount != index)
					throw new InvalidOperationException();
				Array.Copy(customAttributes, 0, cas, pseudoCount, customAttributes.Length);
				customAttributes = cas;
			}

			return ReadOnlyCollectionHelpers.Create(customAttributes);
		}

		public static ReadOnlyCollection<DmdCustomAttributeData> AddPseudoCustomAttributes(DmdPropertyInfo property, DmdCustomAttributeData[]? customAttributes) =>
			ReadOnlyCollectionHelpers.Create(customAttributes);

		public static ReadOnlyCollection<DmdCustomAttributeData> AddPseudoCustomAttributes(DmdEventInfo @event, DmdCustomAttributeData[]? customAttributes) =>
			ReadOnlyCollectionHelpers.Create(customAttributes);

		public static ReadOnlyCollection<DmdCustomAttributeData> AddPseudoCustomAttributes(DmdParameterInfo parameter, DmdCustomAttributeData[]? customAttributes, DmdMarshalType? marshalType) {
			if (customAttributes is null)
				customAttributes = Array.Empty<DmdCustomAttributeData>();

			var inAttributeInfo = new InAttributeInfo(parameter);
			var outAttributeInfo = new OutAttributeInfo(parameter);
			var optionalAttributeInfo = new OptionalAttributeInfo(parameter);
			var marshalAsAttributeInfo = new MarshalAsAttributeInfo(parameter, marshalType);

			int pseudoCount = inAttributeInfo.Count + outAttributeInfo.Count + optionalAttributeInfo.Count + marshalAsAttributeInfo.Count;
			if (pseudoCount != 0) {
				var cas = new DmdCustomAttributeData[pseudoCount + customAttributes.Length];
				int index = 0;
				inAttributeInfo.CopyTo(cas, ref index);
				outAttributeInfo.CopyTo(cas, ref index);
				optionalAttributeInfo.CopyTo(cas, ref index);
				marshalAsAttributeInfo.CopyTo(cas, ref index, marshalType);
				if (pseudoCount != index)
					throw new InvalidOperationException();
				Array.Copy(customAttributes, 0, cas, pseudoCount, customAttributes.Length);
				customAttributes = cas;
			}

			return ReadOnlyCollectionHelpers.Create(customAttributes);
		}

		public static ReadOnlyCollection<DmdCustomAttributeData> AddPseudoCustomAttributes(DmdAssembly assembly, DmdCustomAttributeData[]? customAttributes, ReadOnlyCollection<DmdCustomAttributeData> securityAttributes) {
			if (customAttributes is null)
				customAttributes = Array.Empty<DmdCustomAttributeData>();

			var securityAttributeInfo = new SecurityAttributeInfo(securityAttributes);

			int pseudoCount = securityAttributeInfo.Count;
			if (pseudoCount != 0) {
				var cas = new DmdCustomAttributeData[pseudoCount + customAttributes.Length];
				int index = 0;
				securityAttributeInfo.CopyTo(cas, ref index, securityAttributes);
				if (pseudoCount != index)
					throw new InvalidOperationException();
				Array.Copy(customAttributes, 0, cas, pseudoCount, customAttributes.Length);
				customAttributes = cas;
			}

			return ReadOnlyCollectionHelpers.Create(customAttributes);
		}

		public static ReadOnlyCollection<DmdCustomAttributeData> AddPseudoCustomAttributes(DmdModule module, DmdCustomAttributeData[]? customAttributes) =>
			ReadOnlyCollectionHelpers.Create(customAttributes);
	}
}
