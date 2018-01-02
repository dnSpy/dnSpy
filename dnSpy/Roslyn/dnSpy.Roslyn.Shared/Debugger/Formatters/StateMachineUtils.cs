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
using System.Diagnostics;
using System.Linq;
using dnSpy.Debugger.DotNet.Metadata;

namespace dnSpy.Roslyn.Shared.Debugger.Formatters {
	static class StateMachineUtils {
		public static bool TryGetKickoffMethod(DmdMethodBase method, out DmdMethodBase kickoffMethod) {
			var name = method.DeclaringType.Name;
			char c;
			if (!string.IsNullOrEmpty(name) && ((c = name[0]) == '<' || (c == 'V' && name.StartsWith("VB$StateMachine_", StringComparison.Ordinal)))) {
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
						var declTypeDef = method.DeclaringType;
						if (declTypeDef.IsConstructedGenericType)
							declTypeDef = declTypeDef.GetGenericTypeDefinition();
						foreach (var m in type.DeclaredMethods) {
							var ca = m.FindCustomAttribute(attrName, false);
							if (ca == null || ca.ConstructorArguments.Count != 1)
								continue;
							var smType = ca.ConstructorArguments[0].Value as DmdType;
							if (smType == declTypeDef) {
								var smGenArgs = method.ReflectedType.GetGenericArguments();
								Debug.Assert(method.GetGenericArguments().Count == 0, "Generic method args should be part of the state machine type");
								kickoffMethod = AddTypeArguments(m, smGenArgs);
								Debug.Assert((object)kickoffMethod != null);
								if ((object)kickoffMethod == null)
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

		static DmdMethodBase AddTypeArguments(DmdMethodBase method, IList<DmdType> typeAndMethodGenArgs) {
			var declType = method.ReflectedType;
			if (declType.IsConstructedGenericType)
				return null;
			int typeGenArgs = declType.GetGenericArguments().Count;
			int methodGenArgs = method.GetGenericArguments().Count;
			if (typeGenArgs + methodGenArgs != typeAndMethodGenArgs.Count)
				return null;

			if (typeGenArgs != 0) {
				var type = declType.MakeGenericType(typeAndMethodGenArgs.Take(typeGenArgs).ToArray());
				method = type.GetMethod(method.Module, method.MetadataToken, throwOnError: true);
			}
			if (methodGenArgs != 0)
				method = ((DmdMethodInfo)method).MakeGenericMethod(typeAndMethodGenArgs.Skip(typeGenArgs).ToArray());

			return method;
		}
	}
}
