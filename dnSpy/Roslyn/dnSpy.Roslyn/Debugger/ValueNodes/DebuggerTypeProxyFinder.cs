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

namespace dnSpy.Roslyn.Debugger.ValueNodes {
	sealed class DebuggerTypeProxyFinder {
		sealed class AssemblyState {
			readonly struct TypeKey : IEquatable<TypeKey> {
				readonly DmdModule module;
				readonly int metadataToken;
				public TypeKey(DmdType type) {
					module = type.Module;
					metadataToken = type.MetadataToken;
				}

				public bool Equals(TypeKey other) => module == other.module && metadataToken == other.metadataToken;
				public override bool Equals(object? obj) => obj is TypeKey other && Equals(other);
				public override int GetHashCode() => module.GetHashCode() ^ metadataToken;
			}
			readonly Dictionary<TypeKey, DmdType> dict;

			public AssemblyState(DmdAssembly assembly) {
				dict = new Dictionary<TypeKey, DmdType>();
				var proxyAttr = assembly.AppDomain.GetWellKnownType(DmdWellKnownType.System_Diagnostics_DebuggerTypeProxyAttribute, isOptional: true);
				Debug2.Assert(!(proxyAttr is null));
				if (!(proxyAttr is null)) {
					foreach (var ca in assembly.CustomAttributes) {
						if (ca.AttributeType != proxyAttr)
							continue;
						if (ca.ConstructorArguments.Count != 1)
							continue;
						var proxyType = DebuggerTypeProxyFinder.GetType(assembly, ca.ConstructorArguments[0].Value);
						if (proxyType is null)
							continue;

						DmdType? targetType = null;
						foreach (var namedArg in ca.NamedArguments) {
							var prop = namedArg.MemberInfo as DmdPropertyInfo;
							if (prop is null)
								continue;
							if (prop.Name == nameof(DebuggerTypeProxyAttribute.Target) || prop.Name == nameof(DebuggerTypeProxyAttribute.TargetTypeName)) {
								targetType = DebuggerTypeProxyFinder.GetType(assembly, namedArg.TypedValue.Value);
								break;
							}
						}
						if (targetType is null)
							continue;

						dict[new TypeKey(targetType)] = proxyType;
					}
				}
			}

			public DmdType? GetProxyType(DmdType type) {
				if (dict.TryGetValue(new TypeKey(type), out var proxyType))
					return proxyType;
				return null;
			}
		}
		sealed class ProxyState {
			public readonly DmdConstructorInfo? Constructor;
			public ProxyState(DmdConstructorInfo? constructor) => Constructor = constructor;
		}

		public static DmdConstructorInfo? GetDebuggerTypeProxyConstructor(DmdType type) {
			if (type.TypeSignatureKind != DmdTypeSignatureKind.Type && type.TypeSignatureKind != DmdTypeSignatureKind.GenericInstance)
				return null;
			if (type.IsInterface)
				return null;
			if (type.IsGenericType && !type.IsConstructedGenericType)
				return null;

			if (type.TryGetData(out ProxyState? proxyState))
				return proxyState.Constructor;

			var ctor = GetProxyTypeConstructor(type);
			return CreateProxyState(type, ctor).Constructor;

			ProxyState CreateProxyState(DmdType targetType, DmdConstructorInfo? proxyCtor) =>
				targetType.GetOrCreateData(() => new ProxyState(proxyCtor));
		}

		static DmdConstructorInfo? GetProxyTypeConstructor(DmdType targetType) {
			var proxyAttr = targetType.AppDomain.GetWellKnownType(DmdWellKnownType.System_Diagnostics_DebuggerTypeProxyAttribute, isOptional: true);
			Debug2.Assert(!(proxyAttr is null));
			if (proxyAttr is null)
				return null;
			DmdType? currentType = targetType;
			for (;;) {
				DmdConstructorInfo? proxyCtor;

				var ca = currentType.FindCustomAttribute(proxyAttr, inherit: false);
				if (!(ca is null) && ca.ConstructorArguments.Count == 1) {
					proxyCtor = GetConstructor(GetType(currentType.Assembly, ca.ConstructorArguments[0].Value), currentType);
					if (!(proxyCtor is null))
						return proxyCtor;
				}

				var asmState = GetAssemblyState(currentType.Assembly);
				proxyCtor = GetConstructor(asmState.GetProxyType(currentType), currentType);
				if (!(proxyCtor is null))
					return proxyCtor;

				currentType = currentType.BaseType;
				if (currentType is null)
					return null;
			}
		}

		static DmdConstructorInfo? GetConstructor(DmdType? proxyType, DmdType targetType) {
			if (proxyType is null)
				return null;
			if (proxyType.IsConstructedGenericType)
				return null;

			var proxyTypeGenericArgs = proxyType.GetGenericArguments();
			var targetTypeGenericArgs = targetType.GetGenericArguments();
			if (proxyTypeGenericArgs.Count != targetTypeGenericArgs.Count)
				return null;

			if (targetTypeGenericArgs.Count != 0)
				proxyType = proxyType.MakeGenericType(targetTypeGenericArgs);
			var ctors = proxyType.GetConstructors(DmdBindingFlags.Public | DmdBindingFlags.NonPublic | DmdBindingFlags.Instance);
			foreach (var ctor in ctors) {
				var types = ctor.GetMethodSignature().GetParameterTypes();
				if (types.Count != 1)
					continue;
				if (!types[0].IsAssignableFrom(targetType))
					continue;

				return ctor;
			}
			return null;
		}

		static DmdType? GetType(DmdAssembly assembly, object? value) {
			if (value is DmdType type)
				return type;
			if (value is string typeName)
				return assembly.GetType(typeName, DmdGetTypeOptions.None);
			return null;
		}

		static AssemblyState GetAssemblyState(DmdAssembly assembly) {
			if (assembly.TryGetData(out AssemblyState? state))
				return state;
			return CreateAssemblyState(assembly);
		}

		static AssemblyState CreateAssemblyState(DmdAssembly assembly) {
			var state = new AssemblyState(assembly);
			return assembly.GetOrCreateData(() => state);
		}
	}
}
