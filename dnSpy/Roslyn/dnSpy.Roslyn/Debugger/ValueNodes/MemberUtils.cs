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

using dnSpy.Contracts.Debugger.Text;
using dnSpy.Debugger.DotNet.Metadata;

namespace dnSpy.Roslyn.Debugger.ValueNodes {
	static class MemberUtils {
		public static DbgTextColor GetColor(DmdFieldInfo field) {
			if (field.ReflectedType!.IsEnum)
				return DbgTextColor.EnumField;
			if (field.IsLiteral)
				return DbgTextColor.LiteralField;
			if (field.IsStatic)
				return DbgTextColor.StaticField;
			return DbgTextColor.InstanceField;
		}

		public static DbgTextColor GetColor(DmdPropertyInfo property) {
			var methodSig = (property.GetGetMethod(DmdGetAccessorOptions.All) ?? property.GetSetMethod(DmdGetAccessorOptions.All))?.GetMethodSignature() ?? property.GetMethodSignature();
			return methodSig.HasThis ? DbgTextColor.InstanceProperty : DbgTextColor.StaticProperty;
		}
	}
}
