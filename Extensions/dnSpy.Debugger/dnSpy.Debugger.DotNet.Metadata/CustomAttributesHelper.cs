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

using System.Collections.Generic;

namespace dnSpy.Debugger.DotNet.Metadata {
	static class CustomAttributesHelper {
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
	}
}
