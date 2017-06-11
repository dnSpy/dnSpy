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
			for (var currentMethod = method; (object)currentMethod != null; currentMethod = currentMethod.GetBaseDefinition()) {
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
			for (var currentMethod = method; (object)currentMethod != null; currentMethod = currentMethod.GetBaseDefinition()) {
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

		struct SerializableAttributeInfo {
			public int Count => (object)ctor != null ? 1 : 0;
			readonly DmdConstructorInfo ctor;

			public SerializableAttributeInfo(DmdType type) {
				if ((type.Attributes & DmdTypeAttributes.Serializable) != 0) {
					var caType = type.AppDomain.GetWellKnownType(DmdWellKnownType.System_SerializableAttribute, isOptional: true);
					ctor = caType?.GetConstructor(Array.Empty<DmdType>());
				}
				else
					ctor = null;
			}

			public void CopyTo(DmdCustomAttributeData[] destination, ref int index) =>
				destination[index++] = new DmdCustomAttributeData(ctor, null, null, isPseudoCustomAttribute: true);
		}

		struct ComImportAttributeInfo {
			public int Count => (object)ctor != null ? 1 : 0;
			readonly DmdConstructorInfo ctor;

			public ComImportAttributeInfo(DmdType type) {
				if ((type.Attributes & DmdTypeAttributes.Import) != 0) {
					var caType = type.AppDomain.GetWellKnownType(DmdWellKnownType.System_Runtime_InteropServices_ComImportAttribute, isOptional: true);
					ctor = caType?.GetConstructor(Array.Empty<DmdType>());
				}
				else
					ctor = null;
			}

			public void CopyTo(DmdCustomAttributeData[] destination, ref int index) =>
				destination[index++] = new DmdCustomAttributeData(ctor, null, null, isPseudoCustomAttribute: true);
		}

		public static ReadOnlyCollection<DmdCustomAttributeData> AddPseudoCustomAttributes(DmdType type, DmdCustomAttributeData[] customAttributes) {
			if (customAttributes == null)
				customAttributes = Array.Empty<DmdCustomAttributeData>();

			var serializableAttributeInfo = new SerializableAttributeInfo(type);
			var comImportAttributeInfo = new ComImportAttributeInfo(type);

			//TODO: security attributes

			int pseudoCount = serializableAttributeInfo.Count + comImportAttributeInfo.Count;
			if (pseudoCount != 0) {
				var cas = new DmdCustomAttributeData[pseudoCount + customAttributes.Length];
				int index = 0;
				serializableAttributeInfo.CopyTo(cas, ref index);
				comImportAttributeInfo.CopyTo(cas, ref index);
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

			//TODO: security attributes

			return customAttributes.Length == 0 ? emptyCustomAttributeCollection : new ReadOnlyCollection<DmdCustomAttributeData>(customAttributes);
		}

		public static ReadOnlyCollection<DmdCustomAttributeData> AddPseudoCustomAttributes(DmdModule module, DmdCustomAttributeData[] customAttributes) =>
			customAttributes == null || customAttributes.Length == 0 ? emptyCustomAttributeCollection : new ReadOnlyCollection<DmdCustomAttributeData>(customAttributes);
	}
}
