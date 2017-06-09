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
using System.Threading;

namespace dnSpy.Debugger.DotNet.Metadata.Impl {
	sealed class DmdAppDomainImpl : DmdAppDomain {
		internal sealed override void YouCantDeriveFromThisClass() => throw new InvalidOperationException();
		public override DmdRuntime Runtime => runtime;
		public override int Id { get; }

		public override DmdAssembly CorLib {
			get {
				lock (LockObject) {
					// Assume that the first assembly is always the corlib. This is documented in DmdAppDomainController.CreateAssembly()
					return assemblies.Count == 0 ? null : assemblies[0];
				}
			}
		}

		readonly DmdRuntimeImpl runtime;
		readonly List<DmdAssemblyImpl> assemblies;
		readonly Dictionary<string, DmdAssemblyImpl> simpleNameToAssembly;
		readonly Dictionary<DmdAssemblyName, DmdAssemblyImpl> assemblyNameToAssembly;
		readonly Dictionary<DmdType, DmdType> fullyResolvedTypes;
		readonly Dictionary<DmdModule, Dictionary<DmdType, DmdTypeDef>> toModuleTypeDict;
		readonly Dictionary<DmdModule, Dictionary<DmdType, DmdTypeRef>> toModuleExportedTypeDict;
		readonly WellKnownMemberResolver wellKnownMemberResolver;
		static readonly DmdMemberInfoEqualityComparer moduleTypeDictComparer = new DmdMemberInfoEqualityComparer(DmdSigComparerOptions.DontCompareTypeScope | DmdSigComparerOptions.DontCompareCustomModifiers);

		public DmdAppDomainImpl(DmdRuntimeImpl runtime, int id) {
			assemblies = new List<DmdAssemblyImpl>();
			simpleNameToAssembly = new Dictionary<string, DmdAssemblyImpl>(StringComparer.OrdinalIgnoreCase);
			assemblyNameToAssembly = new Dictionary<DmdAssemblyName, DmdAssemblyImpl>(DmdMemberInfoEqualityComparer.Default);
			fullyResolvedTypes = new Dictionary<DmdType, DmdType>(DmdMemberInfoEqualityComparer.Default);
			toModuleTypeDict = new Dictionary<DmdModule, Dictionary<DmdType, DmdTypeDef>>();
			toModuleExportedTypeDict = new Dictionary<DmdModule, Dictionary<DmdType, DmdTypeRef>>();
			wellKnownMemberResolver = new WellKnownMemberResolver(this);
			this.runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
			Id = id;
		}

		internal void Add(DmdAssemblyImpl assembly) {
			if (assembly == null)
				throw new ArgumentNullException(nameof(assembly));
			lock (LockObject) {
				Debug.Assert(!assemblies.Contains(assembly));
				assemblies.Add(assembly);
			}
		}

		internal void Remove(DmdAssemblyImpl assembly) {
			if (assembly == null)
				throw new ArgumentNullException(nameof(assembly));
			lock (LockObject) {
				bool b = assemblies.Remove(assembly);
				Debug.Assert(b);
			}
		}

		public override DmdAssembly[] GetAssemblies() {
			lock (LockObject)
				return assemblies.ToArray();
		}

		public override DmdAssembly GetAssembly(string simpleName) {
			if (simpleName == null)
				throw new ArgumentNullException(nameof(simpleName));
			return GetAssemblyCore(simpleName, null);
		}

		public override DmdAssembly GetAssembly(DmdAssemblyName name) {
			if (name == null)
				throw new ArgumentNullException(nameof(name));
			return GetAssemblyCore(name.Name, name);
		}

		DmdAssemblyImpl GetAssemblyCore(string simpleName, DmdAssemblyName name) {
			lock (LockObject) {
				if (name != null) {
					if (assemblyNameToAssembly.TryGetValue(name, out var cached))
						return cached;
				}
				else {
					if (simpleNameToAssembly.TryGetValue(simpleName, out var cached))
						return cached;
				}

				var assembly = GetAssemblySlowCore_NoLock(simpleName, name);
				if (assembly != null) {
					if (name != null)
						assemblyNameToAssembly.Add(name.Clone(), assembly);
					else
						simpleNameToAssembly.Add(simpleName, assembly);
				}
				return assembly;
			}
		}

		DmdAssemblyImpl GetAssemblySlowCore_NoLock(string simpleName, DmdAssemblyName name) {
			// Try to avoid reading the metadata in case we're debugging a program with lots of assemblies.

			// We first loop over all disk file assemblies since we can check simpleName without accessing metadata.
			foreach (var assembly in assemblies) {
				if (assembly.IsInMemory || assembly.IsDynamic)
					continue;
				if (!StringComparer.OrdinalIgnoreCase.Equals(simpleName, assembly.ApproximateSimpleName))
					continue;

				// Access metadata (when calling GetName())
				if (name == null || DmdMemberInfoEqualityComparer.Default.Equals(assembly.GetName(), name))
					return assembly;
			}

			// Check all in-memory and dynamic assemblies. We need to read their metadata.
			foreach (var assembly in assemblies) {
				if (!(assembly.IsInMemory || assembly.IsDynamic))
					continue;

				if (name == null) {
					if (StringComparer.OrdinalIgnoreCase.Equals(simpleName, assembly.GetName().Name))
						return assembly;
				}
				else if (DmdMemberInfoEqualityComparer.Default.Equals(assembly.GetName(), name))
					return assembly;
			}
			return null;
		}

		public override DmdAssembly Load(IDmdEvaluationContext context, DmdAssemblyName name) {
			if (name == null)
				throw new ArgumentNullException(nameof(name));
			var asm = GetAssembly(name);
			if (asm != null)
				return asm;
			throw new NotImplementedException();//TODO:
		}

		public override DmdMemberInfo GetWellKnownMember(DmdWellKnownMember wellKnownMember, bool isOptional) {
			var member = wellKnownMemberResolver.GetWellKnownMember(wellKnownMember);
			if (member == null && !isOptional)
				throw new ResolveException("Couldn't resolve well known member: " + wellKnownMember);
			return member;
		}

		public override DmdType GetWellKnownType(DmdWellKnownType wellKnownType, bool isOptional) {
			var type = wellKnownMemberResolver.GetWellKnownType(wellKnownType);
			if (type == null && !isOptional)
				throw new ResolveException("Couldn't resolve well known type: " + wellKnownType);
			return type;
		}

		public override DmdType Intern(DmdType type) {
			if ((object)type == null)
				throw new ArgumentNullException(nameof(type));
			if (type.AppDomain != this)
				throw new InvalidOperationException();

			var res = type as DmdTypeBase ?? throw new ArgumentException();
			res = res.FullResolve() ?? res;
			lock (LockObject) {
				if (fullyResolvedTypes.TryGetValue(res, out var cachedType))
					return cachedType;
				if (res.IsFullyResolved)
					fullyResolvedTypes.Add(res, res);
			}

			return res;
		}

		public override DmdType MakePointerType(DmdType elementType, IList<DmdCustomModifier> customModifiers) {
			if ((object)elementType == null)
				throw new ArgumentNullException(nameof(elementType));
			if (elementType.AppDomain != this)
				throw new InvalidOperationException();
			var et = elementType as DmdTypeBase;
			if ((object)et == null)
				throw new ArgumentException();
			if (customModifiers != null) {
				for (int i = 0; i < customModifiers.Count; i++) {
					if (customModifiers[i].Type.AppDomain != this)
						throw new ArgumentException();
				}
			}
			et = et.FullResolve() ?? et;

			var res = new DmdPointerType(et, customModifiers);
			lock (LockObject) {
				if (fullyResolvedTypes.TryGetValue(res, out var cachedType))
					return cachedType;
				if (res.IsFullyResolved)
					fullyResolvedTypes.Add(res, res);
			}

			return res;
		}

		public override DmdType MakeByRefType(DmdType elementType, IList<DmdCustomModifier> customModifiers) {
			if ((object)elementType == null)
				throw new ArgumentNullException(nameof(elementType));
			if (elementType.AppDomain != this)
				throw new InvalidOperationException();
			var et = elementType as DmdTypeBase;
			if ((object)et == null)
				throw new ArgumentException();
			if (customModifiers != null) {
				for (int i = 0; i < customModifiers.Count; i++) {
					if (customModifiers[i].Type.AppDomain != this)
						throw new ArgumentException();
				}
			}
			et = et.FullResolve() ?? et;

			var res = new DmdByRefType(et, customModifiers);
			lock (LockObject) {
				if (fullyResolvedTypes.TryGetValue(res, out var cachedType))
					return cachedType;
				if (res.IsFullyResolved)
					fullyResolvedTypes.Add(res, res);
			}

			return res;
		}

		public override DmdType MakeArrayType(DmdType elementType, IList<DmdCustomModifier> customModifiers) {
			if ((object)elementType == null)
				throw new ArgumentNullException(nameof(elementType));
			if (elementType.AppDomain != this)
				throw new InvalidOperationException();
			var et = elementType as DmdTypeBase;
			if ((object)et == null)
				throw new ArgumentException();
			if (customModifiers != null) {
				for (int i = 0; i < customModifiers.Count; i++) {
					if (customModifiers[i].Type.AppDomain != this)
						throw new ArgumentException();
				}
			}
			et = et.FullResolve() ?? et;

			var res = new DmdSZArrayType(et, customModifiers);
			lock (LockObject) {
				if (fullyResolvedTypes.TryGetValue(res, out var cachedType))
					return cachedType;
				if (res.IsFullyResolved)
					fullyResolvedTypes.Add(res, res);
			}

			return res;
		}

		public override DmdType MakeArrayType(DmdType elementType, int rank, IList<int> sizes, IList<int> lowerBounds, IList<DmdCustomModifier> customModifiers) {
			// Allow 0, it's allowed in the MD
			if (rank < 0)
				throw new ArgumentOutOfRangeException(nameof(rank));
			if ((object)elementType == null)
				throw new ArgumentNullException(nameof(elementType));
			if (elementType.AppDomain != this)
				throw new InvalidOperationException();
			if (sizes == null)
				throw new ArgumentNullException(nameof(sizes));
			if (lowerBounds == null)
				throw new ArgumentNullException(nameof(lowerBounds));
			var et = elementType as DmdTypeBase;
			if ((object)et == null)
				throw new ArgumentException();
			if (customModifiers != null) {
				for (int i = 0; i < customModifiers.Count; i++) {
					if (customModifiers[i].Type.AppDomain != this)
						throw new ArgumentException();
				}
			}
			et = et.FullResolve() ?? et;

			var res = new DmdMDArrayType(et, rank, sizes, lowerBounds, customModifiers);
			lock (LockObject) {
				if (fullyResolvedTypes.TryGetValue(res, out var cachedType))
					return cachedType;
				if (res.IsFullyResolved)
					fullyResolvedTypes.Add(res, res);
			}

			return res;
		}

		public override DmdType MakeGenericType(DmdType genericTypeDefinition, IList<DmdType> typeArguments, IList<DmdCustomModifier> customModifiers) {
			if ((object)genericTypeDefinition == null)
				throw new ArgumentNullException(nameof(genericTypeDefinition));
			if (genericTypeDefinition.AppDomain != this)
				throw new InvalidOperationException();
			if (typeArguments == null)
				throw new ArgumentNullException(nameof(typeArguments));
			for (int i = 0; i < typeArguments.Count; i++) {
				if (typeArguments[i].AppDomain != this)
					throw new InvalidOperationException();
			}
			if (customModifiers != null) {
				for (int i = 0; i < customModifiers.Count; i++) {
					if (customModifiers[i].Type.AppDomain != this)
						throw new ArgumentException();
				}
			}

			DmdTypeBase res;
			var gtDef = genericTypeDefinition.Resolve() as DmdTypeDef;
			if ((object)gtDef == null) {
				var gtRef = genericTypeDefinition as DmdTypeRef;
				if ((object)gtRef == null)
					throw new ArgumentException();
				typeArguments = DmdTypeUtilities.FullResolve(typeArguments) ?? typeArguments;
				res = new DmdGenericInstanceTypeRef(gtRef, typeArguments, customModifiers);
			}
			else {
				gtDef = (DmdTypeDef)gtDef.FullResolve() ?? gtDef;
				if (!gtDef.IsGenericTypeDefinition)
					throw new ArgumentException();
				if (gtDef.GetReadOnlyGenericArguments().Count != typeArguments.Count)
					throw new ArgumentException();
				typeArguments = DmdTypeUtilities.FullResolve(typeArguments) ?? typeArguments;
				res = new DmdGenericInstanceType(gtDef, typeArguments, customModifiers);
			}

			lock (LockObject) {
				if (fullyResolvedTypes.TryGetValue(res, out var cachedType))
					return cachedType;
				if (res.IsFullyResolved)
					fullyResolvedTypes.Add(res, res);
			}

			return res;
		}

		public override DmdType MakeFunctionPointerType(DmdSignatureCallingConvention flags, int genericParameterCount, DmdType returnType, IList<DmdType> parameterTypes, IList<DmdType> varArgsParameterTypes, IList<DmdCustomModifier> customModifiers) {
			if (genericParameterCount < 0)
				throw new ArgumentOutOfRangeException(nameof(genericParameterCount));
			if ((object)returnType == null)
				throw new ArgumentNullException(nameof(returnType));
			if (parameterTypes == null)
				throw new ArgumentNullException(nameof(parameterTypes));
			if (varArgsParameterTypes == null)
				throw new ArgumentNullException(nameof(varArgsParameterTypes));
			if (returnType.AppDomain != this)
				throw new ArgumentException();
			for (int i = 0; i < parameterTypes.Count; i++) {
				if (parameterTypes[i].AppDomain != this)
					throw new ArgumentException();
			}
			for (int i = 0; i < varArgsParameterTypes.Count; i++) {
				if (varArgsParameterTypes[i].AppDomain != this)
					throw new ArgumentException();
			}
			if (customModifiers != null) {
				for (int i = 0; i < customModifiers.Count; i++) {
					if (customModifiers[i].Type.AppDomain != this)
						throw new ArgumentException();
				}
			}
			returnType = ((DmdTypeBase)returnType).FullResolve() ?? returnType;
			parameterTypes = DmdTypeUtilities.FullResolve(parameterTypes) ?? parameterTypes;
			varArgsParameterTypes = DmdTypeUtilities.FullResolve(varArgsParameterTypes) ?? varArgsParameterTypes;
			var methodSignature = new DmdMethodSignatureImpl(flags, genericParameterCount, returnType, parameterTypes, varArgsParameterTypes);

			var res = new DmdFunctionPointerType(methodSignature, customModifiers);
			lock (LockObject) {
				if (fullyResolvedTypes.TryGetValue(res, out var cachedType))
					return cachedType;
				if (res.IsFullyResolved)
					fullyResolvedTypes.Add(res, res);
			}

			return res;
		}

		public override DmdType GetType(string typeName, bool throwOnError, bool ignoreCase) => throw new NotImplementedException();//TODO:

		internal DmdTypeDef Resolve(DmdTypeRef typeRef, bool throwOnError, bool ignoreCase) {
			if ((object)typeRef == null)
				throw new ArgumentNullException(nameof(typeRef));

			var type = ResolveCore(typeRef, ignoreCase);
			if ((object)type != null)
				return type;

			if (throwOnError)
				throw new TypeResolveException(typeRef);
			return null;
		}

		DmdTypeDef ResolveCore(DmdTypeRef typeRef, bool ignoreCase) {
			var nonNestedTypeRef = GetNonNestedTypeRef(typeRef);
			if ((object)nonNestedTypeRef == null)
				return null;

			DmdModule module;
			DmdAssembly assembly;
			var typeScope = nonNestedTypeRef.TypeScope;
			switch (typeScope.Kind) {
			case DmdTypeScopeKind.Invalid:
				Debug.Fail("Shouldn't be here");
				return null;

			case DmdTypeScopeKind.Module:
				module = (DmdModule)typeScope.Data;
				return Lookup(module, typeRef) ?? ResolveExportedType(new[] { module }, typeRef);

			case DmdTypeScopeKind.ModuleRef:
				assembly = GetAssembly((DmdAssemblyName)typeScope.Data2);
				if (assembly == null)
					return null;
				module = assembly.GetModule((string)typeScope.Data);
				if (module == null)
					return null;
				return Lookup(module, typeRef) ?? ResolveExportedType(new[] { module }, typeRef);

			case DmdTypeScopeKind.AssemblyRef:
				assembly = GetAssembly((DmdAssemblyName)typeScope.Data);
				if (assembly == null)
					return null;
				return Lookup(assembly, typeRef) ?? ResolveExportedType(assembly.GetModules(), typeRef);

			default:
				throw new InvalidOperationException();
			}
		}

		DmdTypeDef ResolveExportedType(DmdModule[] modules, DmdTypeRef typeRef) {
			for (int i = 0; i < 30; i++) {
				var exportedType = FindExportedType(modules, typeRef);
				if ((object)exportedType == null)
					return null;

				var nonNested = GetNonNestedTypeRef(exportedType);
				if ((object)nonNested == null)
					return null;
				var typeScope = nonNested.TypeScope;
				if (typeScope.Kind != DmdTypeScopeKind.AssemblyRef)
					return null;
				var etAsm = GetAssembly((DmdAssemblyName)typeScope.Data);
				if (etAsm == null)
					return null;

				var td = Lookup(etAsm, typeRef);
				if ((object)td != null)
					return td;

				modules = etAsm.GetModules();
			}

			return null;
		}

		Dictionary<DmdType, DmdTypeRef> GetModuleExportedTypeDictionary(DmdModule module) {
			lock (LockObject) {
				if (toModuleExportedTypeDict.TryGetValue(module, out var dict))
					return dict;
				dict = new Dictionary<DmdType, DmdTypeRef>(moduleTypeDictComparer);
				foreach (var type in module.GetTypes())
					dict[type] = (DmdTypeRef)type;
				return dict;
			}
		}

		DmdTypeRef FindExportedType(IList<DmdModule> modules, DmdTypeRef typeRef) {
			foreach (var module in modules) {
				if (GetModuleExportedTypeDictionary(module).TryGetValue(typeRef, out var exportedType))
					return exportedType;
			}
			return null;
		}

		DmdTypeDef Lookup(DmdAssembly assembly, DmdTypeRef typeRef) {
			// Most likely it's in the manifest module so we don't have to alloc an array (GetModules())
			var manifestModule = assembly.ManifestModule;
			if (manifestModule == null)
				return null;
			var type = Lookup(manifestModule, typeRef);
			if ((object)type != null)
				return type;

			foreach (var module in assembly.GetModules()) {
				if (manifestModule == module)
					continue;
				type = Lookup(module, typeRef);
				if ((object)type != null)
					return type;
			}
			return null;
		}

		Dictionary<DmdType, DmdTypeDef> GetModuleTypeDictionary(DmdModule module) {
			lock (LockObject) {
				if (toModuleTypeDict.TryGetValue(module, out var dict))
					return dict;
				dict = new Dictionary<DmdType, DmdTypeDef>(moduleTypeDictComparer);
				foreach (var type in module.GetTypes())
					dict[type] = (DmdTypeDef)type;
				return dict;
			}
		}

		DmdTypeDef Lookup(DmdModule module, DmdTypeRef typeRef) {
			if (GetModuleTypeDictionary(module).TryGetValue(typeRef, out var typeDef))
				return typeDef;
			return null;
		}

		static DmdTypeRef GetNonNestedTypeRef(DmdTypeRef typeRef) {
			for (int i = 0; i < 1000; i++) {
				var next = typeRef.DeclaringTypeRef;
				if ((object)next == null)
					return typeRef;
				typeRef = next;
			}
			return null;
		}

		public override object Invoke(IDmdEvaluationContext context, DmdMethodBase method, object obj, object[] parameters, CancellationToken cancellationToken) {
			if ((object)method == null)
				throw new ArgumentNullException(nameof(method));
			if ((method.MemberType == DmdMemberTypes.Constructor || method.IsStatic) != (obj == null))
				throw new ArgumentException();
			if (method.AppDomain != this)
				throw new ArgumentException();
			return runtime.Evaluator.Invoke(context, method, obj, parameters ?? Array.Empty<object>(), cancellationToken);
		}

		public override object LoadField(IDmdEvaluationContext context, DmdFieldInfo field, object obj, CancellationToken cancellationToken) {
			if ((object)field == null)
				throw new ArgumentNullException(nameof(field));
			if (field.IsStatic != (obj == null))
				throw new ArgumentException();
			if (field.AppDomain != this)
				throw new ArgumentException();
			return runtime.Evaluator.LoadField(context, field, obj, cancellationToken);
		}

		public override void StoreField(IDmdEvaluationContext context, DmdFieldInfo field, object obj, object value, CancellationToken cancellationToken) {
			if ((object)field == null)
				throw new ArgumentNullException(nameof(field));
			if (field.IsStatic != (obj == null))
				throw new ArgumentException();
			if (field.AppDomain != this)
				throw new ArgumentException();
			runtime.Evaluator.StoreField(context, field, obj, value, cancellationToken);
		}

		public override void Invoke(IDmdEvaluationContext context, DmdMethodBase method, object obj, object[] parameters, Action<object> callback, CancellationToken cancellationToken) {
			if ((object)method == null)
				throw new ArgumentNullException(nameof(method));
			if ((method.MemberType == DmdMemberTypes.Constructor || method.IsStatic) != (obj == null))
				throw new ArgumentException();
			if (method.AppDomain != this)
				throw new ArgumentException();
			if (callback == null)
				throw new ArgumentNullException(nameof(callback));
			runtime.Evaluator.Invoke(context, method, obj, parameters ?? Array.Empty<object>(), callback, cancellationToken);
		}

		public override void LoadField(IDmdEvaluationContext context, DmdFieldInfo field, object obj, Action<object> callback, CancellationToken cancellationToken) {
			if ((object)field == null)
				throw new ArgumentNullException(nameof(field));
			if (field.IsStatic != (obj == null))
				throw new ArgumentException();
			if (field.AppDomain != this)
				throw new ArgumentException();
			if (callback == null)
				throw new ArgumentNullException(nameof(callback));
			runtime.Evaluator.LoadField(context, field, obj, callback, cancellationToken);
		}

		public override void StoreField(IDmdEvaluationContext context, DmdFieldInfo field, object obj, object value, Action callback, CancellationToken cancellationToken) {
			if ((object)field == null)
				throw new ArgumentNullException(nameof(field));
			if (field.IsStatic != (obj == null))
				throw new ArgumentException();
			if (field.AppDomain != this)
				throw new ArgumentException();
			if (callback == null)
				throw new ArgumentNullException(nameof(callback));
			runtime.Evaluator.StoreField(context, field, obj, value, callback, cancellationToken);
		}
	}
}
