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

using dnSpy.Debugger.DotNet.Metadata;

namespace dnSpy.Roslyn.Shared.Debugger.Formatters {
	static class StateMachineUtils {
		public static bool TryGetKickoffMethod(DmdMethodBase method, out DmdMethodBase kickoffMethod) {
			var name = method.DeclaringType.Name;
			if (!string.IsNullOrEmpty(name) && name[0] == '<') {
				var type = method.DeclaringType.DeclaringType;
				if ((object)type != null) {
					string attrName;
					// These attributes could be missing from the type (eg. it's a Unity assembly)
					if (method.DeclaringType.CanCastTo(type.AppDomain.GetWellKnownType(DmdWellKnownType.System_Runtime_CompilerServices_IAsyncStateMachine, isOptional: true)))
						attrName = "System.Runtime.CompilerServices.AsyncStateMachineAttribute";
					else if (method.DeclaringType.CanCastTo(type.AppDomain.GetWellKnownType(DmdWellKnownType.System_Collections_IEnumerator, isOptional: true)))
						attrName = "System.Runtime.CompilerServices.IteratorStateMachineAttribute";
					else
						attrName = null;
					if (attrName != null) {
						foreach (var m in type.DeclaredMethods) {
							var ca = m.FindCustomAttribute(attrName, false);
							if (ca == null || ca.ConstructorArguments.Count != 1)
								continue;
							var smType = ca.ConstructorArguments[0].Value as DmdType;
							if (smType == method.DeclaringType) {
								kickoffMethod = m;
								return true;
							}
						}
					}
				}
			}

			kickoffMethod = null;
			return false;
		}
	}
}
