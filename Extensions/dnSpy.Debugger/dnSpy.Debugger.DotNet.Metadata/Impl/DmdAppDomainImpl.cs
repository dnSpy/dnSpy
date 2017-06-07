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
		public override DmdRuntime Runtime => runtime;
		public override int Id { get; }

		public override DmdAssembly CorLib {
			get {
				lock (lockObj) {
					// Assume that the first assembly is always the corlib. This is documented in DmdAppDomainController.CreateAssembly()
					return assemblies.Count == 0 ? null : assemblies[0];
				}
			}
		}

		readonly object lockObj;
		readonly DmdRuntimeImpl runtime;
		readonly List<DmdAssemblyImpl> assemblies;

		public DmdAppDomainImpl(DmdRuntimeImpl runtime, int id) {
			lockObj = new object();
			assemblies = new List<DmdAssemblyImpl>();
			this.runtime = runtime ?? throw new ArgumentNullException(nameof(runtime));
			Id = id;
		}

		internal void Add(DmdAssemblyImpl assembly) {
			if (assembly == null)
				throw new ArgumentNullException(nameof(assembly));
			lock (lockObj) {
				Debug.Assert(!assemblies.Contains(assembly));
				assemblies.Add(assembly);
			}
		}

		internal void Remove(DmdAssemblyImpl assembly) {
			if (assembly == null)
				throw new ArgumentNullException(nameof(assembly));
			lock (lockObj) {
				bool b = assemblies.Remove(assembly);
				Debug.Assert(b);
			}
		}

		public override DmdAssembly[] GetAssemblies() {
			lock (lockObj)
				return assemblies.ToArray();
		}

		public override DmdAssembly GetAssembly(string simpleName) {
			if (simpleName == null)
				throw new ArgumentNullException(nameof(simpleName));
			lock (lockObj) {
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
			lock (lockObj) {
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
			if (elementType == null)
				throw new ArgumentNullException(nameof(elementType));
			if (elementType.AppDomain != this)
				throw new InvalidOperationException();
			var et = (elementType.ResolveNoThrow() ?? elementType) as DmdTypeBase;
			if (et == null)
				throw new ArgumentException();
			throw new NotImplementedException();//TODO:
		}

		public override DmdType MakeByRefType(DmdType elementType) {
			if (elementType == null)
				throw new ArgumentNullException(nameof(elementType));
			if (elementType.AppDomain != this)
				throw new InvalidOperationException();
			var et = (elementType.ResolveNoThrow() ?? elementType) as DmdTypeBase;
			if (et == null)
				throw new ArgumentException();
			throw new NotImplementedException();//TODO:
		}

		public override DmdType MakeArrayType(DmdType elementType) {
			if (elementType == null)
				throw new ArgumentNullException(nameof(elementType));
			if (elementType.AppDomain != this)
				throw new InvalidOperationException();
			var et = (elementType.ResolveNoThrow() ?? elementType) as DmdTypeBase;
			if (et == null)
				throw new ArgumentException();
			return new DmdSZArrayType(et);
		}

		public override DmdType MakeArrayType(DmdType elementType, int rank, IList<int> sizes, IList<int> lowerBounds) {
			if (elementType == null)
				throw new ArgumentNullException(nameof(elementType));
			if (elementType.AppDomain != this)
				throw new InvalidOperationException();
			if (sizes == null)
				throw new ArgumentNullException(nameof(sizes));
			if (lowerBounds == null)
				throw new ArgumentNullException(nameof(lowerBounds));
			var et = (elementType.ResolveNoThrow() ?? elementType) as DmdTypeBase;
			if (et == null)
				throw new ArgumentException();
			throw new NotImplementedException();//TODO:
		}

		public override DmdType MakeGenericType(DmdType genericTypeDefinition, IList<DmdType> typeArguments) {
			if (genericTypeDefinition == null)
				throw new ArgumentNullException(nameof(genericTypeDefinition));
			if (genericTypeDefinition.AppDomain != this)
				throw new InvalidOperationException();
			if (typeArguments == null)
				throw new ArgumentNullException(nameof(typeArguments));
			for (int i = 0; i < typeArguments.Count; i++) {
				if (typeArguments[i].AppDomain != this)
					throw new InvalidOperationException();
			}
			var gtDef = genericTypeDefinition.Resolve() as DmdTypeDef;
			if (gtDef == null)
				throw new ArgumentException();
			if (!gtDef.IsGenericTypeDefinition)
				throw new ArgumentException();
			if (gtDef.GetReadOnlyGenericArguments().Count != typeArguments.Count)
				throw new ArgumentException();
			throw new NotImplementedException();//TODO:
		}

		public override DmdType MakeFunctionPointerType(DmdMethodSignature methodSignature) => throw new NotImplementedException();//TODO:
		public override DmdType GetType(string typeName, bool throwOnError, bool ignoreCase) => throw new NotImplementedException();//TODO:

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
