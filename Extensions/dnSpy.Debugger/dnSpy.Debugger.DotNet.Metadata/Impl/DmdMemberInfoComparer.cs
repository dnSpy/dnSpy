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

namespace dnSpy.Debugger.DotNet.Metadata.Impl {
	static class DmdMemberInfoComparer {
		public static bool IsMatch(DmdType type, DmdBindingFlags bindingAttr) {
			var attr = DmdBindingFlags.Default;
			if (type.IsPublic || type.IsNestedPublic)
				attr |= DmdBindingFlags.Public;
			else
				attr |= DmdBindingFlags.NonPublic;
			return (attr & bindingAttr) == attr;
		}

		public static bool IsMatch(DmdMethodBase method, DmdBindingFlags bindingAttr) {
			var attr = DmdBindingFlags.Default;
			if (method.IsPublic)
				attr |= DmdBindingFlags.Public;
			else
				attr |= DmdBindingFlags.NonPublic;
			if (method.IsStatic)
				attr |= DmdBindingFlags.Static;
			else
				attr |= DmdBindingFlags.Instance;
			if ((object)method.ReflectedType != method.DeclaringType) {
				if (method.IsStatic) {
					if (method.IsPrivate)
						return false;
					attr |= DmdBindingFlags.FlattenHierarchy;
				}
				else {
					if (!(method.IsVirtual || method.IsAbstract) && method.IsPrivate)
						return false;
				}
			}
			return (attr & bindingAttr) == attr;
		}

		public static bool IsMatch(DmdFieldInfo field, DmdBindingFlags bindingAttr) {
			var attr = DmdBindingFlags.Default;
			if (field.IsPublic)
				attr |= DmdBindingFlags.Public;
			else
				attr |= DmdBindingFlags.NonPublic;
			if (field.IsStatic)
				attr |= DmdBindingFlags.Static;
			else
				attr |= DmdBindingFlags.Instance;
			if ((object)field.ReflectedType != field.DeclaringType) {
				if (field.IsStatic) {
					if (field.IsPrivate)
						return false;
					attr |= DmdBindingFlags.FlattenHierarchy;
				}
				else {
					if (field.IsPrivate)
						return false;
				}
			}
			return (attr & bindingAttr) == attr;
		}

		public static bool IsMatch(DmdEventInfo @event, DmdBindingFlags bindingAttr) {
			var attr = DmdBindingFlags.Default;
			if (@event.AddMethod?.IsPublic == true || @event.RemoveMethod?.IsPublic == true || @event.RaiseMethod?.IsPublic == true)
				attr |= DmdBindingFlags.Public;
			else
				attr |= DmdBindingFlags.NonPublic;
			if (@event.AddMethod?.IsStatic == true || @event.RemoveMethod?.IsStatic == true || @event.RaiseMethod?.IsStatic == true)
				attr |= DmdBindingFlags.Static;
			else
				attr |= DmdBindingFlags.Instance;
			if ((object)@event.ReflectedType != @event.DeclaringType) {
				var method = @event.AddMethod;
				if ((object)method != null) {
					if (method.IsStatic) {
						if (method.IsPrivate)
							return false;
						attr |= DmdBindingFlags.FlattenHierarchy;
					}
					else {
						if (!(method.IsVirtual || method.IsAbstract) && method.IsPrivate)
							return false;
					}
				}
			}
			return (attr & bindingAttr) == attr;
		}

		public static bool IsMatch(DmdPropertyInfo property, DmdBindingFlags bindingAttr) {
			var attr = DmdBindingFlags.Default;
			if (property.GetMethod?.IsPublic == true || property.SetMethod?.IsPublic == true)
				attr |= DmdBindingFlags.Public;
			else
				attr |= DmdBindingFlags.NonPublic;
			if (property.GetMethod?.IsStatic == true || property.SetMethod?.IsStatic == true)
				attr |= DmdBindingFlags.Static;
			else
				attr |= DmdBindingFlags.Instance;
			if ((object)property.ReflectedType != property.DeclaringType) {
				var method = property.GetMethod;
				if ((object)method != null) {
					if (method.IsStatic) {
						if (method.IsPrivate)
							return false;
						attr |= DmdBindingFlags.FlattenHierarchy;
					}
					else {
						if (!(method.IsVirtual || method.IsAbstract) && method.IsPrivate)
							return false;
					}
				}
			}
			return (attr & bindingAttr) == attr;
		}

		static bool IsMatch(DmdMethodBase method, DmdCallingConventions callConvention) =>
			callConvention == DmdCallingConventions.Any ||
			(method.CallingConvention & DmdCallingConventions.Any) == (callConvention & DmdCallingConventions.Any);

		public static bool IsMatch(DmdMethodBase method, DmdBindingFlags bindingAttr, DmdCallingConventions callConvention) =>
			IsMatch(method, bindingAttr) && IsMatch(method, callConvention);

		public static bool IsMatch(DmdMethodBase method, IList<DmdType> types) =>
			IsMatch(method.GetMethodSignature().GetParameterTypes(), types ?? Array.Empty<DmdType>());

		public static bool IsMatch(DmdMethodBase method, DmdBindingFlags bindingAttr, DmdCallingConventions callConvention, IList<DmdType> types) {
			if (!IsMatch(method, bindingAttr, callConvention))
				return false;
			return IsMatch(method, types);
		}

		static bool IsMatch(IList<DmdType> p, IList<DmdType> types) {
			if (p.Count != types.Count)
				return false;
			for (int i = 0; i < p.Count; i++) {
				if (!DmdMemberInfoEqualityComparer.DefaultType.Equals(p[i], types[i]))
					return false;
			}
			return true;
		}

		public static bool IsMatch(DmdPropertyInfo property, DmdType returnType) {
			if ((object)returnType != null) {
				var comparer = new DmdSigComparer(DmdSigComparerOptions.CheckTypeEquivalence | DmdMemberInfoEqualityComparer.DefaultTypeOptions);
				if (!comparer.Equals(property.PropertyType, returnType))
					return false;
			}
			return true;
		}

		public static bool IsMatch(DmdPropertyInfo property, IList<DmdType> types) =>
			IsMatch(property.GetMethodSignature().GetParameterTypes(), types ?? Array.Empty<DmdType>());

		public static bool IsMatch(DmdMemberInfo member, string name, DmdBindingFlags bindingAttr) {
			if ((bindingAttr & DmdBindingFlags.IgnoreCase) != 0)
				return StringComparer.OrdinalIgnoreCase.Equals(member.Name, name);
			return StringComparer.Ordinal.Equals(member.Name, name);
		}

		public static bool IsMatch(DmdType type, string @namespace, string name, DmdBindingFlags bindingAttr) {
			// Namespace comparison is exact
			if (@namespace != null && type.Namespace != @namespace)
				return false;
			if ((bindingAttr & DmdBindingFlags.IgnoreCase) != 0)
				return StringComparer.OrdinalIgnoreCase.Equals(type.Name, name);
			return StringComparer.Ordinal.Equals(type.Name, name);
		}
	}
}
