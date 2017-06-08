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
		readonly Dictionary<DmdType, DmdType> fullyResolvedTypes;

		public DmdAppDomainImpl(DmdRuntimeImpl runtime, int id) {
			assemblies = new List<DmdAssemblyImpl>();
			fullyResolvedTypes = new Dictionary<DmdType, DmdType>(DmdMemberInfoEqualityComparer.Default);
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
			lock (LockObject) {
				foreach (var assembly in assemblies) {
					if (StringComparer.OrdinalIgnoreCase.Equals(assembly.GetName().Name, simpleName))
						return assembly;
				}
			}
			return null;
		}

		public override DmdAssembly GetAssembly(DmdAssemblyName name) {
			if (name == null)
				throw new ArgumentNullException(nameof(name));
			lock (LockObject) {
				foreach (var assembly in assemblies) {
					if (DmdMemberInfoEqualityComparer.Default.Equals(assembly.GetName(), name))
						return assembly;
				}
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

		public override DmdMemberInfo GetWellKnownMember(DmdWellKnownMember wellKnownMember, bool isOptional) => throw new NotImplementedException();//TODO:
		public override DmdType GetWellKnownType(DmdWellKnownType wellKnownType, bool isOptional) => throw new NotImplementedException();//TODO:

		public override DmdType MakePointerType(DmdType elementType) {
			if ((object)elementType == null)
				throw new ArgumentNullException(nameof(elementType));
			if (elementType.AppDomain != this)
				throw new InvalidOperationException();
			var et = elementType as DmdTypeBase;
			if ((object)et == null)
				throw new ArgumentException();
			et = et.FullResolve() ?? et;

			var res = new DmdPointerType(et);
			lock (LockObject) {
				if (fullyResolvedTypes.TryGetValue(res, out var cachedType))
					return cachedType;
				if (res.IsFullyResolved)
					fullyResolvedTypes.Add(res, res);
			}

			return res;
		}

		public override DmdType MakeByRefType(DmdType elementType) {
			if ((object)elementType == null)
				throw new ArgumentNullException(nameof(elementType));
			if (elementType.AppDomain != this)
				throw new InvalidOperationException();
			var et = elementType as DmdTypeBase;
			if ((object)et == null)
				throw new ArgumentException();
			et = et.FullResolve() ?? et;

			var res = new DmdByRefType(et);
			lock (LockObject) {
				if (fullyResolvedTypes.TryGetValue(res, out var cachedType))
					return cachedType;
				if (res.IsFullyResolved)
					fullyResolvedTypes.Add(res, res);
			}

			return res;
		}

		public override DmdType MakeArrayType(DmdType elementType) {
			if ((object)elementType == null)
				throw new ArgumentNullException(nameof(elementType));
			if (elementType.AppDomain != this)
				throw new InvalidOperationException();
			var et = elementType as DmdTypeBase;
			if ((object)et == null)
				throw new ArgumentException();
			et = et.FullResolve() ?? et;

			var res = new DmdSZArrayType(et);
			lock (LockObject) {
				if (fullyResolvedTypes.TryGetValue(res, out var cachedType))
					return cachedType;
				if (res.IsFullyResolved)
					fullyResolvedTypes.Add(res, res);
			}

			return res;
		}

		public override DmdType MakeArrayType(DmdType elementType, int rank, IList<int> sizes, IList<int> lowerBounds) {
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
			et = et.FullResolve() ?? et;

			var res = new DmdMDArrayType(et, rank, sizes, lowerBounds);
			lock (LockObject) {
				if (fullyResolvedTypes.TryGetValue(res, out var cachedType))
					return cachedType;
				if (res.IsFullyResolved)
					fullyResolvedTypes.Add(res, res);
			}

			return res;
		}

		public override DmdType MakeGenericType(DmdType genericTypeDefinition, IList<DmdType> typeArguments) {
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

			DmdTypeBase res;
			var gtDef = genericTypeDefinition.Resolve() as DmdTypeDef;
			if ((object)gtDef == null) {
				var gtRef = genericTypeDefinition as DmdTypeRef;
				if ((object)gtRef == null)
					throw new ArgumentException();
				typeArguments = DmdTypeUtilities.FullResolve(typeArguments) ?? typeArguments;
				res = new DmdGenericInstanceTypeRef(gtRef, typeArguments);
			}
			else {
				gtDef = (DmdTypeDef)gtDef.FullResolve() ?? gtDef;
				if (!gtDef.IsGenericTypeDefinition)
					throw new ArgumentException();
				if (gtDef.GetReadOnlyGenericArguments().Count != typeArguments.Count)
					throw new ArgumentException();
				typeArguments = DmdTypeUtilities.FullResolve(typeArguments) ?? typeArguments;
				res = new DmdGenericInstanceType(gtDef, typeArguments);
			}

			lock (LockObject) {
				if (fullyResolvedTypes.TryGetValue(res, out var cachedType))
					return cachedType;
				if (res.IsFullyResolved)
					fullyResolvedTypes.Add(res, res);
			}

			return res;
		}

		public override DmdType MakeFunctionPointerType(DmdSignatureCallingConvention flags, int genericParameterCount, DmdType returnType, IList<DmdType> parameterTypes, IList<DmdType> varArgsParameterTypes) {
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
			returnType = ((DmdTypeBase)returnType).FullResolve() ?? returnType;
			parameterTypes = DmdTypeUtilities.FullResolve(parameterTypes) ?? parameterTypes;
			varArgsParameterTypes = DmdTypeUtilities.FullResolve(varArgsParameterTypes) ?? varArgsParameterTypes;
			var methodSignature = new DmdMethodSignatureImpl(flags, genericParameterCount, returnType, parameterTypes, varArgsParameterTypes);

			var res = new DmdFunctionPointerType(methodSignature);
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

			//TODO:

			if (throwOnError)
				throw new TypeResolveException(typeRef);
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
