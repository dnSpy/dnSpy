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
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace dnSpy.Debugger.DotNet.Metadata {
	static class CustomAttributesHelper {
		static readonly ReadOnlyCollection<DmdCustomAttributeData> emptyCustomAttributeCollection = new ReadOnlyCollection<DmdCustomAttributeData>(Array.Empty<DmdCustomAttributeData>());

		public static bool IsDefined(IList<DmdCustomAttributeData> customAttributes, string attributeTypeFullName) {
			for (int i = 0; i < customAttributes.Count; i++) {
				if (customAttributes[i].AttributeType.FullName == attributeTypeFullName)
					return true;
			}
			return false;
		}

		public static bool IsDefined(IList<DmdCustomAttributeData> customAttributes, DmdType attributeType) {
			for (int i = 0; i < customAttributes.Count; i++) {
				if (DmdMemberInfoEqualityComparer.Default.Equals(customAttributes[i].AttributeType, attributeType))
					return true;
			}
			return false;
		}

		public static bool IsDefined(DmdType type, string attributeTypeFullName, bool inherit) {
			for (var currentType = type; (object)currentType != null; currentType = currentType.BaseType) {
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

		public static bool IsDefined(DmdType type, DmdType attributeType, bool inherit) {
			for (var currentType = type; (object)currentType != null; currentType = currentType.BaseType) {
				var customAttributes = currentType.GetCustomAttributesData();
				for (int i = 0; i < customAttributes.Count; i++) {
					var ca = customAttributes[i];
					if ((object)currentType != type && ca.IsPseudoCustomAttribute)
						continue;
					if (DmdMemberInfoEqualityComparer.Default.Equals(ca.AttributeType, attributeType))
						return true;
				}
				if (!inherit)
					break;
			}
			return false;
		}

		public static bool IsDefined(DmdMethodInfo method, string attributeTypeFullName, bool inherit) {
			for (var currentMethod = method; (object)currentMethod != null; currentMethod = currentMethod.GetParentDefinition()) {
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

		public static bool IsDefined(DmdMethodInfo method, DmdType attributeType, bool inherit) {
			for (var currentMethod = method; (object)currentMethod != null; currentMethod = currentMethod.GetParentDefinition()) {
				var customAttributes = currentMethod.GetCustomAttributesData();
				for (int i = 0; i < customAttributes.Count; i++) {
					var ca = customAttributes[i];
					if ((object)currentMethod != method && ca.IsPseudoCustomAttribute)
						continue;
					if (DmdMemberInfoEqualityComparer.Default.Equals(ca.AttributeType, attributeType))
						return true;
				}
				if (!inherit)
					break;
			}
			return false;
		}

		struct SecurityAttributeInfo {
			public int Count => (object)ctor != null ? 1 : 0;
			readonly DmdConstructorInfo ctor;

			public SecurityAttributeInfo(DmdMemberInfo member) => ctor = null;//TODO:
			public SecurityAttributeInfo(DmdAssembly assembly) => ctor = null;//TODO:

			public void CopyTo(DmdCustomAttributeData[] destination, ref int index) {
				if (Count == 0)
					return;
				throw new NotImplementedException();//TODO:
			}
		}

		struct SerializableAttributeInfo {
			public int Count => (object)ctor != null ? 1 : 0;
			readonly DmdConstructorInfo ctor;

			public SerializableAttributeInfo(DmdType type) {
				if ((type.Attributes & DmdTypeAttributes.Serializable) != 0) {
					var caType = type.AppDomain.GetWellKnownType(DmdWellKnownType.System_SerializableAttribute, isOptional: true);
					ctor = caType?.GetConstructor(Array.Empty<DmdType>());
					Debug.Assert((object)caType == null || (object)ctor != null);
				}
				else
					ctor = null;
			}

			public void CopyTo(DmdCustomAttributeData[] destination, ref int index) {
				if (Count == 0)
					return;
				destination[index++] = new DmdCustomAttributeData(ctor, null, null, isPseudoCustomAttribute: true);
			}
		}

		struct ComImportAttributeInfo {
			public int Count => (object)ctor != null ? 1 : 0;
			readonly DmdConstructorInfo ctor;

			public ComImportAttributeInfo(DmdType type) {
				if ((type.Attributes & DmdTypeAttributes.Import) != 0) {
					var caType = type.AppDomain.GetWellKnownType(DmdWellKnownType.System_Runtime_InteropServices_ComImportAttribute, isOptional: true);
					ctor = caType?.GetConstructor(Array.Empty<DmdType>());
					Debug.Assert((object)caType == null || (object)ctor != null);
				}
				else
					ctor = null;
			}

			public void CopyTo(DmdCustomAttributeData[] destination, ref int index) {
				if (Count == 0)
					return;
				destination[index++] = new DmdCustomAttributeData(ctor, null, null, isPseudoCustomAttribute: true);
			}
		}

		struct MarshalAsAttributeInfo {
			public int Count => (object)ctor != null ? 1 : 0;
			readonly DmdConstructorInfo ctor;

			public MarshalAsAttributeInfo(DmdFieldInfo field) => ctor = null;//TODO:

			public void CopyTo(DmdCustomAttributeData[] destination, ref int index) {
				if (Count == 0)
					return;
				throw new NotImplementedException();//TODO:
			}
		}

		struct FieldOffsetAttributeInfo {
			public int Count => (object)ctor != null ? 1 : 0;
			readonly DmdConstructorInfo ctor;
			readonly int offset;

			public FieldOffsetAttributeInfo(DmdFieldInfo field, uint? offset) {
				if (offset != null) {
					this.offset = (int)offset.Value;
					var caType = field.AppDomain.GetWellKnownType(DmdWellKnownType.System_Runtime_InteropServices_FieldOffsetAttribute, isOptional: true);
					ctor = caType?.GetConstructor(new[] { field.AppDomain.System_Int32 });
					Debug.Assert((object)caType == null || (object)ctor != null);
				}
				else {
					ctor = null;
					this.offset = 0;
				}
			}

			public void CopyTo(DmdCustomAttributeData[] destination, ref int index) {
				if (Count == 0)
					return;
				var ctorArgs = new[] { new DmdCustomAttributeTypedArgument(ctor.AppDomain.System_Int32, offset) };
				destination[index++] = new DmdCustomAttributeData(ctor, ctorArgs, null, isPseudoCustomAttribute: true);
			}
		}

		struct NonSerializedAttributeInfo {
			public int Count => (object)ctor != null ? 1 : 0;
			readonly DmdConstructorInfo ctor;

			public NonSerializedAttributeInfo(DmdFieldInfo field) {
				if ((field.Attributes & DmdFieldAttributes.NotSerialized) != 0) {
					var caType = field.AppDomain.GetWellKnownType(DmdWellKnownType.System_NonSerializedAttribute, isOptional: true);
					ctor = caType?.GetConstructor(Array.Empty<DmdType>());
					Debug.Assert((object)caType == null || (object)ctor != null);
				}
				else
					ctor = null;
			}

			public void CopyTo(DmdCustomAttributeData[] destination, ref int index) {
				if (Count == 0)
					return;
				destination[index++] = new DmdCustomAttributeData(ctor, null, null, isPseudoCustomAttribute: true);
			}
		}

		struct DllImportAttributeInfo {
			public int Count => (object)ctor != null ? 1 : 0;
			readonly DmdConstructorInfo ctor;

			public DllImportAttributeInfo(DmdMethodInfo method, ref DmdImplMap? implMap) {
				if (implMap != null) {
					var appDomain = method.AppDomain;
					var caType = appDomain.GetWellKnownType(DmdWellKnownType.System_Runtime_InteropServices_DllImportAttribute, isOptional: true);
					var charSetType = appDomain.GetWellKnownType(DmdWellKnownType.System_Runtime_InteropServices_CharSet, isOptional: true);
					var callingConventionType = appDomain.GetWellKnownType(DmdWellKnownType.System_Runtime_InteropServices_CallingConvention, isOptional: true);
					if ((object)charSetType == null || (object)callingConventionType == null || (object)caType == null)
						ctor = null;
					else {
						var sig = new DmdType[9] {
							appDomain.System_String,
							appDomain.System_String,
							charSetType,
							appDomain.System_Boolean,
							appDomain.System_Boolean,
							appDomain.System_Boolean,
							callingConventionType,
							appDomain.System_Boolean,
							appDomain.System_Boolean,
						};
						ctor = caType.GetConstructor(DmdBindingFlags.Public | DmdBindingFlags.NonPublic | DmdBindingFlags.Instance, sig);
						Debug.Assert((object)ctor != null);
					}
				}
				else
					ctor = null;
			}

			public void CopyTo(DmdCustomAttributeData[] destination, ref int index, DmdMethodInfo method, ref DmdImplMap? implMap) {
				if (Count == 0)
					return;

				var appDomain = ctor.AppDomain;
				var im = implMap.Value;
				var attributes = im.Attributes;
				var charSetType = appDomain.GetWellKnownType(DmdWellKnownType.System_Runtime_InteropServices_CharSet, isOptional: false);
				var callingConventionType = appDomain.GetWellKnownType(DmdWellKnownType.System_Runtime_InteropServices_CallingConvention, isOptional: false);

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

				var ctorArgs = new DmdCustomAttributeTypedArgument[9] {
					new DmdCustomAttributeTypedArgument(appDomain.System_String, im.Module),
					new DmdCustomAttributeTypedArgument(appDomain.System_String, im.Name),
					new DmdCustomAttributeTypedArgument(charSetType, charSet),
					new DmdCustomAttributeTypedArgument(appDomain.System_Boolean, (attributes & DmdPInvokeAttributes.NoMangle) != 0),
					new DmdCustomAttributeTypedArgument(appDomain.System_Boolean, (attributes & DmdPInvokeAttributes.SupportsLastError) != 0),
					new DmdCustomAttributeTypedArgument(appDomain.System_Boolean, method.IsPreserveSig),
					new DmdCustomAttributeTypedArgument(callingConventionType, callingConvention),
					new DmdCustomAttributeTypedArgument(appDomain.System_Boolean, (attributes & DmdPInvokeAttributes.BestFitMask) == DmdPInvokeAttributes.BestFitEnabled),
					new DmdCustomAttributeTypedArgument(appDomain.System_Boolean, (attributes & DmdPInvokeAttributes.ThrowOnUnmappableCharMask) == DmdPInvokeAttributes.ThrowOnUnmappableCharEnabled),
				};
				destination[index++] = new DmdCustomAttributeData(ctor, ctorArgs, null, isPseudoCustomAttribute: true);
			}
		}

		struct PreserveSigAttributeInfo {
			public int Count => (object)ctor != null ? 1 : 0;
			readonly DmdConstructorInfo ctor;

			public PreserveSigAttributeInfo(DmdMethodInfo method) {
				if ((method.MethodImplementationFlags & DmdMethodImplAttributes.PreserveSig) != 0) {
					var caType = method.AppDomain.GetWellKnownType(DmdWellKnownType.System_Runtime_InteropServices_PreserveSigAttribute, isOptional: true);
					ctor = caType?.GetConstructor(Array.Empty<DmdType>());
					Debug.Assert((object)caType == null || (object)ctor != null);
				}
				else
					ctor = null;
			}

			public void CopyTo(DmdCustomAttributeData[] destination, ref int index) {
				if (Count == 0)
					return;
				destination[index++] = new DmdCustomAttributeData(ctor, null, null, isPseudoCustomAttribute: true);
			}
		}

		public static ReadOnlyCollection<DmdCustomAttributeData> AddPseudoCustomAttributes(DmdType type, DmdCustomAttributeData[] customAttributes) {
			if (customAttributes == null)
				customAttributes = Array.Empty<DmdCustomAttributeData>();

			var serializableAttributeInfo = new SerializableAttributeInfo(type);
			var comImportAttributeInfo = new ComImportAttributeInfo(type);
			var securityAttributeInfo = new SecurityAttributeInfo(type);

			int pseudoCount = serializableAttributeInfo.Count + comImportAttributeInfo.Count + securityAttributeInfo.Count;
			if (pseudoCount != 0) {
				var cas = new DmdCustomAttributeData[pseudoCount + customAttributes.Length];
				int index = 0;
				serializableAttributeInfo.CopyTo(cas, ref index);
				comImportAttributeInfo.CopyTo(cas, ref index);
				securityAttributeInfo.CopyTo(cas, ref index);
				if (pseudoCount != index)
					throw new InvalidOperationException();
				Array.Copy(customAttributes, 0, cas, pseudoCount, customAttributes.Length);
				customAttributes = cas;
			}

			return customAttributes.Length == 0 ? emptyCustomAttributeCollection : new ReadOnlyCollection<DmdCustomAttributeData>(customAttributes);
		}

		public static ReadOnlyCollection<DmdCustomAttributeData> AddPseudoCustomAttributes(DmdFieldInfo field, DmdCustomAttributeData[] customAttributes, uint? fieldOffset) {
			if (customAttributes == null)
				customAttributes = Array.Empty<DmdCustomAttributeData>();

			var marshalAsAttributeInfo = new MarshalAsAttributeInfo(field);
			var fieldOffsetAttributeInfo = new FieldOffsetAttributeInfo(field, fieldOffset);
			var nonSerializedAttributeInfo = new NonSerializedAttributeInfo(field);

			int pseudoCount = marshalAsAttributeInfo.Count + fieldOffsetAttributeInfo.Count + nonSerializedAttributeInfo.Count;
			if (pseudoCount != 0) {
				var cas = new DmdCustomAttributeData[pseudoCount + customAttributes.Length];
				int index = 0;
				marshalAsAttributeInfo.CopyTo(cas, ref index);
				fieldOffsetAttributeInfo.CopyTo(cas, ref index);
				nonSerializedAttributeInfo.CopyTo(cas, ref index);
				if (pseudoCount != index)
					throw new InvalidOperationException();
				Array.Copy(customAttributes, 0, cas, pseudoCount, customAttributes.Length);
				customAttributes = cas;
			}

			return customAttributes.Length == 0 ? emptyCustomAttributeCollection : new ReadOnlyCollection<DmdCustomAttributeData>(customAttributes);
		}

		public static ReadOnlyCollection<DmdCustomAttributeData> AddPseudoCustomAttributes(DmdConstructorInfo ctor, DmdCustomAttributeData[] customAttributes) {
			if (customAttributes == null)
				customAttributes = Array.Empty<DmdCustomAttributeData>();

			var securityAttributeInfo = new SecurityAttributeInfo(ctor);

			int pseudoCount = securityAttributeInfo.Count;
			if (pseudoCount != 0) {
				var cas = new DmdCustomAttributeData[pseudoCount + customAttributes.Length];
				int index = 0;
				securityAttributeInfo.CopyTo(cas, ref index);
				if (pseudoCount != index)
					throw new InvalidOperationException();
				Array.Copy(customAttributes, 0, cas, pseudoCount, customAttributes.Length);
				customAttributes = cas;
			}

			return customAttributes.Length == 0 ? emptyCustomAttributeCollection : new ReadOnlyCollection<DmdCustomAttributeData>(customAttributes);
		}

		public static ReadOnlyCollection<DmdCustomAttributeData> AddPseudoCustomAttributes(DmdMethodInfo method, DmdCustomAttributeData[] customAttributes, DmdImplMap? implMap) {
			if (customAttributes == null)
				customAttributes = Array.Empty<DmdCustomAttributeData>();

			var dllImportAttributeInfo = new DllImportAttributeInfo(method, ref implMap);
			var preserveSigAttributeInfo = new PreserveSigAttributeInfo(method);
			var securityAttributeInfo = new SecurityAttributeInfo(method);

			int pseudoCount = dllImportAttributeInfo.Count + preserveSigAttributeInfo.Count + securityAttributeInfo.Count;
			if (pseudoCount != 0) {
				var cas = new DmdCustomAttributeData[pseudoCount + customAttributes.Length];
				int index = 0;
				dllImportAttributeInfo.CopyTo(cas, ref index, method, ref implMap);
				preserveSigAttributeInfo.CopyTo(cas, ref index);
				securityAttributeInfo.CopyTo(cas, ref index);
				if (pseudoCount != index)
					throw new InvalidOperationException();
				Array.Copy(customAttributes, 0, cas, pseudoCount, customAttributes.Length);
				customAttributes = cas;
			}

			return customAttributes.Length == 0 ? emptyCustomAttributeCollection : new ReadOnlyCollection<DmdCustomAttributeData>(customAttributes);
		}

		public static ReadOnlyCollection<DmdCustomAttributeData> AddPseudoCustomAttributes(DmdAssembly assembly, DmdCustomAttributeData[] customAttributes) {
			if (customAttributes == null)
				customAttributes = Array.Empty<DmdCustomAttributeData>();

			var securityAttributeInfo = new SecurityAttributeInfo(assembly);

			int pseudoCount = securityAttributeInfo.Count;
			if (pseudoCount != 0) {
				var cas = new DmdCustomAttributeData[pseudoCount + customAttributes.Length];
				int index = 0;
				securityAttributeInfo.CopyTo(cas, ref index);
				if (pseudoCount != index)
					throw new InvalidOperationException();
				Array.Copy(customAttributes, 0, cas, pseudoCount, customAttributes.Length);
				customAttributes = cas;
			}

			return customAttributes.Length == 0 ? emptyCustomAttributeCollection : new ReadOnlyCollection<DmdCustomAttributeData>(customAttributes);
		}

		public static ReadOnlyCollection<DmdCustomAttributeData> AddPseudoCustomAttributes(DmdModule module, DmdCustomAttributeData[] customAttributes) =>
			customAttributes == null || customAttributes.Length == 0 ? emptyCustomAttributeCollection : new ReadOnlyCollection<DmdCustomAttributeData>(customAttributes);
	}
}
