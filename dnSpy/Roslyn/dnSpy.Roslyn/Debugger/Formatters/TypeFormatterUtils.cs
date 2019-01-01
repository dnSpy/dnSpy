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

using System.Collections.ObjectModel;
using dnSpy.Contracts.Debugger.Text;
using dnSpy.Debugger.DotNet.Metadata;

namespace dnSpy.Roslyn.Debugger.Formatters {
	static class TypeFormatterUtils {
		public const int MAX_TUPLE_ARITY = 8;

		public static string GetArrayCommas(int rank) {
			switch (rank) {
			case 0:
			case 1:		return string.Empty;
			case 2:		return ",";
			case 3:		return ",,";
			case 4:		return ",,,";
			case 5:		return ",,,,";
			case 6:		return ",,,,,";
			case 7:		return ",,,,,,";
			case 8:		return ",,,,,,,";
			case 9:		return ",,,,,,,,";
			case 10:	return ",,,,,,,,,";
			case 11:	return ",,,,,,,,,,";
			case 12:	return ",,,,,,,,,,,";
			case 13:	return ",,,,,,,,,,,,";
			case 14:	return ",,,,,,,,,,,,,";
			case 15:	return ",,,,,,,,,,,,,,";
			case 16:	return ",,,,,,,,,,,,,,,";
			default:	return new string(',', rank - 1);
			}
		}

		public static bool IsTupleType(DmdType type) => GetTupleArity(type) >= 0;

		public static int GetTupleArity(DmdType type) {
			int arity = 0;
			for (;;) {
				if (type.MetadataNamespace != "System")
					return -1;
				var genArgs = type.GetGenericArguments();
				if (genArgs.Count == 0)
					return -1;
				if (type.IsNested)
					return -1;
				if (!type.IsValueType)
					return -1;
				int currentArity;
				switch (type.MetadataName) {
				case "ValueTuple`1": currentArity = 1; break;
				case "ValueTuple`2": currentArity = 2; break;
				case "ValueTuple`3": currentArity = 3; break;
				case "ValueTuple`4": currentArity = 4; break;
				case "ValueTuple`5": currentArity = 5; break;
				case "ValueTuple`6": currentArity = 6; break;
				case "ValueTuple`7": currentArity = 7; break;
				case "ValueTuple`8": currentArity = 8; break;
				default:
					return -1;
				}
				if (genArgs.Count != currentArity)
					return -1;
				if (currentArity != 8)
					return arity + currentArity;
				arity += currentArity - 1;
				type = genArgs[currentArity - 1];
			}
		}

		public static DbgTextColor GetColor(DmdType type, bool canBeModule) {
			if (canBeModule && (object)type.DeclaringType == null && type.IsSealed && type.IsAbstract)
				return DbgTextColor.Module;
			if (type.IsInterface)
				return DbgTextColor.Interface;
			if (type.IsEnum)
				return DbgTextColor.Enum;
			if (type.IsValueType)
				return DbgTextColor.ValueType;
			if (type.BaseType == type.AppDomain.System_MulticastDelegate)
				return DbgTextColor.Delegate;
			if (type.IsSealed && type.IsAbstract && type.BaseType == type.AppDomain.System_Object)
				return DbgTextColor.StaticType;
			if (type.IsSealed)
				return DbgTextColor.SealedType;
			return DbgTextColor.Type;
		}

		public static string RemoveGenericTick(string s) {
			int index = s.LastIndexOf('`');
			if (index < 0)
				return s;
			// Check if compiler generated name
			if (s[0] == '<')
				return s;
			return s.Substring(0, index);
		}

		public static (DmdPropertyInfo property, AccessorKind kind) TryGetProperty(DmdMethodBase method) {
			if ((object)method == null)
				return (null, AccessorKind.None);
			foreach (var p in method.DeclaringType.Properties) {
				if ((object)method == p.GetMethod)
					return (p, AccessorKind.Getter);
				if ((object)method == p.SetMethod)
					return (p, AccessorKind.Setter);
			}
			return (null, AccessorKind.None);
		}

		public static (DmdEventInfo @event, AccessorKind kind) TryGetEvent(DmdMethodBase method) {
			if ((object)method == null)
				return (null, AccessorKind.None);
			foreach (var e in method.DeclaringType.Events) {
				if ((object)method == e.AddMethod)
					return (e, AccessorKind.Adder);
				if ((object)method == e.RemoveMethod)
					return (e, AccessorKind.Remover);
			}
			return (null, AccessorKind.None);
		}

		static DbgTextColor GetColor(DmdMethodInfo method, DbgTextColor staticValue, DbgTextColor instanceValue) {
			if ((object)method == null)
				return instanceValue;
			if (method.IsStatic)
				return staticValue;
			return instanceValue;
		}

		public static DbgTextColor GetColor(DmdPropertyInfo property) =>
			GetColor(property.GetMethod ?? property.SetMethod, DbgTextColor.StaticProperty, DbgTextColor.InstanceProperty);

		public static DbgTextColor GetColor(DmdEventInfo @event) =>
			GetColor(@event.AddMethod ?? @event.RemoveMethod, DbgTextColor.StaticEvent, DbgTextColor.InstanceEvent);

		public static DbgTextColor GetColor(DmdMethodBase method, bool canBeModule) {
			if (method is DmdConstructorInfo)
				return GetColor(method.DeclaringType, canBeModule);
			if (method.IsStatic) {
				if (method.IsDefined("System.Runtime.CompilerServices.ExtensionAttribute", inherit: false))
					return DbgTextColor.ExtensionMethod;
				return DbgTextColor.StaticMethod;
			}
			return DbgTextColor.InstanceMethod;
		}

		public static bool TryGetMethodName(string name, out string containingMethodName, out string localFunctionName) {
			// Some local function metadata names (real names: Method2(), Method3()) (Roslyn: GeneratedNames.MakeLocalFunctionName())
			//
			//		<Method1>g__Method20_0
			//		<Method1>g__Method30_1
			//		<Method2>g__Method21_0
			//		<Method2>g__Method31_1
			// later C# compiler version
			//		<Method1>g__Method2|0_0
			//
			//	<XXX> = XXX = containing method
			//	'g' = GeneratedNameKind.LocalFunction
			//	0_0 = methodOrdinal '_' entityOrdinal
			//	Method2, Method3 = names of local funcs
			//
			// Since a method can end in a digit and method ordinal is a number, we have to guess where
			// the name ends.
			//
			// This has been fixed, see https://github.com/dotnet/roslyn/pull/21848

			containingMethodName = null;
			localFunctionName = null;

			if (name.Length == 0 || name[0] != '<')
				return false;
			int index = name.IndexOf('>');
			if (index < 0)
				return false;
			containingMethodName = name.Substring(1, index - 1);
			if (containingMethodName.Length == 0)
				return false;
			index++;
			const char GeneratedNameKind_LocalFunction = 'g';
			if (NextChar(name, ref index) != GeneratedNameKind_LocalFunction)
				return false;
			if (NextChar(name, ref index) != '_')
				return false;
			if (NextChar(name, ref index) != '_')
				return false;

			// If it's a later C# compiler version, we can easily find the real name
			int sepIndex = name.IndexOf('|', index);
			if (sepIndex >= 0) {
				if (sepIndex != index) {
					localFunctionName = name.Substring(index, sepIndex - index);
					return true;
				}
				return false;
			}

			int endIndex = name.IndexOf('_', index);
			if (endIndex < 0)
				endIndex = name.Length;
			if (char.IsDigit(name[endIndex - 1]))
				endIndex--;
			if (index != endIndex) {
				localFunctionName = name.Substring(index, endIndex - index);
				return true;
			}

			return false;
		}

		static char NextChar(string s, ref int index) {
			if (index >= s.Length)
				return (char)0;
			return s[index++];
		}

		public static bool IsReadOnlyProperty(DmdPropertyInfo property) => HasIsReadOnlyAttribute(property.CustomAttributes);

		public static bool IsReadOnlyMethod(DmdMethodBase method) {
			if (!(method is DmdMethodInfo m))
				return false;
			return HasIsReadOnlyAttribute(m.ReturnParameter.CustomAttributes);
		}

		public static bool IsReadOnlyParameter(DmdParameterInfo parameter) => HasIsReadOnlyAttribute(parameter.CustomAttributes);

		static bool HasIsReadOnlyAttribute(ReadOnlyCollection<DmdCustomAttributeData> customAttributes) {
			for (int i = 0; i < customAttributes.Count; i++) {
				var ca = customAttributes[i];
				if (ca.AttributeType.MetadataName == "IsReadOnlyAttribute" && ca.AttributeType.MetadataNamespace == "System.Runtime.CompilerServices" && (object)ca.AttributeType.DeclaringType == null)
					return true;
			}
			return false;
		}
	}

	enum AccessorKind {
		None,
		Getter,
		Setter,
		Adder,
		Remover,
	}
}
