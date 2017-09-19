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

namespace dnSpy.Roslyn.Shared.Debugger.ValueNodes {
	static class MemberUtils {
		public static object GetColor(DmdFieldInfo field) {
			if (field.ReflectedType.IsEnum)
				return BoxedTextColor.EnumField;
			if (field.IsLiteral)
				return BoxedTextColor.LiteralField;
			if (field.IsStatic)
				return BoxedTextColor.StaticField;
			return BoxedTextColor.InstanceField;
		}

		public static object GetColor(DmdMethodBase method, bool canBeModule) {
			if (method.IsConstructor)
				return Formatters.TypeFormatterUtils.GetTypeColor(method.DeclaringType, canBeModule);
			if (method.IsStatic) {
				if (method.IsDefined("System.Runtime.CompilerServices.ExtensionAttribute", inherit: false))
					return BoxedTextColor.ExtensionMethod;
				return BoxedTextColor.StaticMethod;
			}
			return BoxedTextColor.InstanceMethod;
		}

		public static object GetColor(DmdPropertyInfo property) {
			var methodSig = (property.GetGetMethod(DmdGetAccessorOptions.All) ?? property.GetSetMethod(DmdGetAccessorOptions.All))?.GetMethodSignature() ?? property.GetMethodSignature();
			return methodSig.HasThis ? BoxedTextColor.InstanceProperty : BoxedTextColor.StaticProperty;
		}
	}
}
