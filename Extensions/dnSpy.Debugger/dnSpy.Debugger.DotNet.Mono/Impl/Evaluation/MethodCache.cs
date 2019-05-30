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
using dnSpy.Debugger.DotNet.Metadata;
using Mono.Debugger.Soft;

namespace dnSpy.Debugger.DotNet.Mono.Impl.Evaluation {
	sealed class MethodCache {
		readonly DbgEngineImpl engine;
		readonly Dictionary<DmdMethodBase, MethodMirror> toMonoMethod = new Dictionary<DmdMethodBase, MethodMirror>(DmdMemberInfoEqualityComparer.DefaultMember);

		MethodCache(DmdAppDomain reflectionAppDomain) =>
			engine = DbgEngineImpl.TryGetEngine(reflectionAppDomain.Runtime.GetDebuggerRuntime()) ?? throw new InvalidOperationException();

		public static MethodMirror GetMethod(DmdMethodBase method, MonoTypeLoader? monoTypeLoader) =>
			GetOrCreate(method.AppDomain).GetMethodCore(method, monoTypeLoader);

		static MethodCache GetOrCreate(DmdAppDomain reflectionAppDomain) {
			if (reflectionAppDomain.TryGetData(out MethodCache? methodCache))
				return methodCache;
			return GetOrCreateMethodCacheCore(reflectionAppDomain);

			MethodCache GetOrCreateMethodCacheCore(DmdAppDomain reflectionAppDomain2) =>
				reflectionAppDomain2.GetOrCreateData(() => new MethodCache(reflectionAppDomain2));
		}

		MethodMirror GetMethodCore(DmdMethodBase method, MonoTypeLoader? monoTypeLoader) {
			MethodMirror? monoMethod;

			var mi = method as DmdMethodInfo;
			if (!(mi is null) && mi.IsConstructedGenericMethod) {
				if (toMonoMethod.TryGetValue(method, out monoMethod))
					return monoMethod;
				if (!engine.MonoVirtualMachine.Version.AtLeast(2, 24))
					throw new InvalidOperationException();
				monoMethod = TryGetMethodCore2(mi.GetGenericMethodDefinition(), monoTypeLoader);
				if (!(monoMethod is null)) {
					var genArgs = mi.GetGenericArguments();
					var monoGenArgs = new TypeMirror[genArgs.Count];
					for (int i = 0; i < monoGenArgs.Length; i++)
						monoGenArgs[i] = MonoDebugTypeCreator.GetType(engine, genArgs[i], monoTypeLoader);
					monoMethod = monoMethod.MakeGenericMethod(monoGenArgs);
					toMonoMethod[method] = monoMethod;
					return monoMethod;
				}
			}
			else {
				monoMethod = TryGetMethodCore2(method, monoTypeLoader);
				if (!(monoMethod is null))
					return monoMethod;
			}

			throw new InvalidOperationException();
		}

		MethodMirror? TryGetMethodCore2(DmdMethodBase method, MonoTypeLoader? monoTypeLoader) {
			if (toMonoMethod.TryGetValue(method, out var monoMethod))
				return monoMethod;

			var monoType = MonoDebugTypeCreator.GetType(engine, method.ReflectedType!, monoTypeLoader);
			DmdType? methodDeclType = method.ReflectedType!;
			while (methodDeclType != method.DeclaringType) {
				methodDeclType = methodDeclType!.BaseType ?? throw new InvalidOperationException();
				monoType = monoType.BaseType ?? MonoDebugTypeCreator.GetType(engine, method.AppDomain.System_Object, monoTypeLoader);
			}

			var monoMethods = monoType.GetMethods();
			var methods = methodDeclType.DeclaredMethods;
			if (monoMethods.Length != methods.Count)
				throw new InvalidOperationException();
			for (int i = 0; i < monoMethods.Length; i++) {
				Debug.Assert(methods[i].Name == monoMethods[i].Name);
				Debug.Assert(methods[i].GetMethodSignature().GetParameterTypes().Count == monoMethods[i].GetParameters().Length);
				toMonoMethod[methods[i]] = monoMethods[i];
			}

			if (toMonoMethod.TryGetValue(method, out monoMethod))
				return monoMethod;

			return null;
		}
	}
}
