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

using dnSpy.Contracts.Text;
using dnSpy.Debugger.DotNet.Metadata;

namespace dnSpy.Roslyn.Shared.Debugger.Formatters {
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
			if (type.MetadataNamespace != "System")
				return -1;
			var genArgs = type.GetGenericArguments();
			if (genArgs.Count == 0)
				return -1;
			if (type.IsNested)
				return -1;
			if (!type.IsValueType)
				return -1;
			int arity;
			switch (type.MetadataName) {
			case "ValueTuple`1": arity = 1; break;
			case "ValueTuple`2": arity = 2; break;
			case "ValueTuple`3": arity = 3; break;
			case "ValueTuple`4": arity = 4; break;
			case "ValueTuple`5": arity = 5; break;
			case "ValueTuple`6": arity = 6; break;
			case "ValueTuple`7": arity = 7; break;
			case "ValueTuple`8": arity = 8; break;
			default:
				return -1;
			}
			if (genArgs.Count != arity)
				return -1;
			return arity;
		}

		public static object GetTypeColor(DmdType type, bool canBeModule) {
			if (canBeModule && (object)type.DeclaringType == null && type.IsSealed && type.IsAbstract)
				return BoxedTextColor.Module;
			if (type.IsInterface)
				return BoxedTextColor.Interface;
			if (type.IsEnum)
				return BoxedTextColor.Enum;
			if (type.IsValueType)
				return BoxedTextColor.ValueType;
			if (type.BaseType == type.AppDomain.System_MulticastDelegate)
				return BoxedTextColor.Delegate;
			if (type.IsSealed && type.IsAbstract && type.BaseType == type.AppDomain.System_Object)
				return BoxedTextColor.StaticType;
			if (type.IsSealed)
				return BoxedTextColor.SealedType;
			return BoxedTextColor.Type;
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
	}
}
