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
using System.Diagnostics;
using System.Linq;
using dnSpy.Debugger.DotNet.Metadata;

namespace dnSpy.Roslyn.Debugger.Formatters {
	static class StateMachineUtils {
		const string StateMachineTypeNamePrefix = "VB$StateMachine_";

		public static bool TryGetKickoffMethod(DmdMethodBase method, out DmdMethodBase kickoffMethod) {
			var name = method.DeclaringType.MetadataName;
			char c;
			if (!string.IsNullOrEmpty(name) && ((c = name[0]) == '<' || (c == 'V' && name.StartsWith(StateMachineTypeNamePrefix, StringComparison.Ordinal)))) {
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
								CreateMethod(method, m, out kickoffMethod);
								return true;
							}
						}
					}
				}
				var kickoffMethodName = (object)type == null ? null : GetKickoffMethodName(method.DeclaringType);
				if (!string.IsNullOrEmpty(kickoffMethodName)) {
					DmdMethodBase possibleKickoffMethod = null;
					int methodGenArgs = method.ReflectedType.GetGenericArguments().Count - type.GetGenericArguments().Count;
					foreach (var m in method.DeclaringType.DeclaringType.DeclaredMethods) {
						if (m.Name != kickoffMethodName)
							continue;
						var sig = m.GetMethodSignature();
						if (sig.GenericParameterCount != methodGenArgs)
							continue;

						if ((object)possibleKickoffMethod != null) {
							// More than one method with the same name and partial signature
							possibleKickoffMethod = null;
							break;
						}
						possibleKickoffMethod = m;
					}
					if ((object)possibleKickoffMethod != null) {
						CreateMethod(method, possibleKickoffMethod, out kickoffMethod);
						return true;
					}
				}
			}

			kickoffMethod = null;
			return false;
		}

		static void CreateMethod(DmdMethodBase method, DmdMethodBase newMethod, out DmdMethodBase createdMethod) {
			var smGenArgs = method.ReflectedType.GetGenericArguments();
			Debug.Assert(method.GetGenericArguments().Count == 0, "Generic method args should be part of the state machine type");
			createdMethod = AddTypeArguments(newMethod, smGenArgs);
			Debug.Assert((object)createdMethod != null);
			if ((object)createdMethod == null)
				createdMethod = newMethod;
		}

		static string GetKickoffMethodName(DmdType type) {
			var name = type.MetadataName;

			if (name.StartsWith(StateMachineTypeNamePrefix)) {
				int i = StateMachineTypeNamePrefix.Length;
				while (i < name.Length && char.IsDigit(name[i]))
					i++;
				if (i >= name.Length || name[i++] != '_')
					return null;
				return RemoveGenericTick(name.Substring(i));
			}

			if (name.StartsWith("<")) {
				const int start = 1;
				int i = 1;
				int level = 1;
				while (i < name.Length) {
					char c = name[i++];
					if (c == '<')
						level++;
					else if (c == '>') {
						level--;
						if (level == 0)
							return RemoveGenericTick(name.Substring(start, i - start - 1));
					}
				}
			}

			return null;
		}

		static string RemoveGenericTick(string name) {
			int index = name.LastIndexOf('`');
			return index < 0 ? name : name.Substring(0, index);
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
