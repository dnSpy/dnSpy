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
using System.Threading;

namespace dnSpy.Debugger.DotNet.Metadata {
	/// <summary>
	/// A .NET AppDomain
	/// </summary>
	public abstract class DmdAppDomain : DmdObject {
		/// <summary>
		/// Dummy abstract method to make sure no-one outside this assembly can create their own <see cref="DmdAppDomain"/>
		/// </summary>
		internal abstract void YouCantDeriveFromThisClass();

		/// <summary>
		/// Gets the runtime
		/// </summary>
		public abstract DmdRuntime Runtime { get; }

		/// <summary>
		/// Gets the unique AppDomain id
		/// </summary>
		public abstract int Id { get; }

		/// <summary>
		/// Gets all assemblies
		/// </summary>
		/// <returns></returns>
		public abstract DmdAssembly[] GetAssemblies();

		/// <summary>
		/// Gets an assembly or returns null if there's no such assembly
		/// </summary>
		/// <param name="simpleName">Simple name of the assembly, eg. "System"</param>
		/// <returns></returns>
		public abstract DmdAssembly GetAssembly(string simpleName);

		/// <summary>
		/// Gets an assembly or returns null if there's no such assembly
		/// </summary>
		/// <param name="name">Assembly name</param>
		/// <returns></returns>
		public abstract DmdAssembly GetAssembly(IDmdAssemblyName name);

		/// <summary>
		/// Loads an assembly
		/// </summary>
		/// <param name="context">Evaluation context</param>
		/// <param name="assemblyName">Full assembly name</param>
		/// <returns></returns>
		public DmdAssembly Load(IDmdEvaluationContext context, string assemblyName) => Load(context, new DmdReadOnlyAssemblyName(assemblyName));

		/// <summary>
		/// Loads an assembly
		/// </summary>
		/// <param name="context">Evaluation context</param>
		/// <param name="name">Assembly name</param>
		/// <returns></returns>
		public abstract DmdAssembly Load(IDmdEvaluationContext context, IDmdAssemblyName name);

		/// <summary>
		/// Loads an assembly. Will fail on .NET Core 1.x (but not on .NET Core 2.x or later)
		/// </summary>
		/// <param name="context">Evaluation context</param>
		/// <param name="assemblyFile">Assembly name or path to assembly</param>
		/// <returns></returns>
		public abstract DmdAssembly LoadFrom(IDmdEvaluationContext context, string assemblyFile);

		/// <summary>
		/// Loads an assembly. Will fail on .NET Core 1.x (but not on .NET Core 2.x or later)
		/// </summary>
		/// <param name="context">Evaluation context</param>
		/// <param name="path">Path to assembly</param>
		/// <returns></returns>
		public abstract DmdAssembly LoadFile(IDmdEvaluationContext context, string path);

		/// <summary>
		/// Gets the core library (eg. mscorlib if it's .NET Framework)
		/// </summary>
		public abstract DmdAssembly CorLib { get; }

		/// <summary>
		/// Checks if a well known type exists in one of the loaded assemblies
		/// </summary>
		/// <param name="wellKnownType">Well known type</param>
		/// <returns></returns>
		public bool HasWellKnownType(DmdWellKnownType wellKnownType) => (object)GetWellKnownType(wellKnownType, isOptional: true) != null;

		/// <summary>
		/// Gets a well known type
		/// </summary>
		/// <param name="wellKnownType">Well known type</param>
		/// <returns></returns>
		public DmdType GetWellKnownType(DmdWellKnownType wellKnownType) => GetWellKnownType(wellKnownType, isOptional: false);

		/// <summary>
		/// Gets a well known type
		/// </summary>
		/// <param name="wellKnownType">Well known type</param>
		/// <param name="isOptional">Used if the type couldn't be found. If true, null is returned, and if false, an exception is thrown</param>
		/// <returns></returns>
		public DmdType GetWellKnownType(DmdWellKnownType wellKnownType, bool isOptional) =>
			GetWellKnownType(wellKnownType, isOptional, onlyCorlib: false);

		internal abstract DmdType GetWellKnownType(DmdWellKnownType wellKnownType, bool isOptional, bool onlyCorlib);

#pragma warning disable 1591 // Missing XML comment for publicly visible type or member
		public DmdType System_Object => GetWellKnownType(DmdWellKnownType.System_Object);
		public DmdType System_Enum => GetWellKnownType(DmdWellKnownType.System_Enum);
		public DmdType System_MulticastDelegate => GetWellKnownType(DmdWellKnownType.System_MulticastDelegate);
		public DmdType System_Delegate => GetWellKnownType(DmdWellKnownType.System_Delegate);
		public DmdType System_ValueType => GetWellKnownType(DmdWellKnownType.System_ValueType);
		public DmdType System_Void => GetWellKnownType(DmdWellKnownType.System_Void);
		public DmdType System_Boolean => GetWellKnownType(DmdWellKnownType.System_Boolean);
		public DmdType System_Char => GetWellKnownType(DmdWellKnownType.System_Char);
		public DmdType System_SByte => GetWellKnownType(DmdWellKnownType.System_SByte);
		public DmdType System_Byte => GetWellKnownType(DmdWellKnownType.System_Byte);
		public DmdType System_Int16 => GetWellKnownType(DmdWellKnownType.System_Int16);
		public DmdType System_UInt16 => GetWellKnownType(DmdWellKnownType.System_UInt16);
		public DmdType System_Int32 => GetWellKnownType(DmdWellKnownType.System_Int32);
		public DmdType System_UInt32 => GetWellKnownType(DmdWellKnownType.System_UInt32);
		public DmdType System_Int64 => GetWellKnownType(DmdWellKnownType.System_Int64);
		public DmdType System_UInt64 => GetWellKnownType(DmdWellKnownType.System_UInt64);
		public DmdType System_Decimal => GetWellKnownType(DmdWellKnownType.System_Decimal);
		public DmdType System_Single => GetWellKnownType(DmdWellKnownType.System_Single);
		public DmdType System_Double => GetWellKnownType(DmdWellKnownType.System_Double);
		public DmdType System_String => GetWellKnownType(DmdWellKnownType.System_String);
		public DmdType System_IntPtr => GetWellKnownType(DmdWellKnownType.System_IntPtr);
		public DmdType System_UIntPtr => GetWellKnownType(DmdWellKnownType.System_UIntPtr);
		public DmdType System_Array => GetWellKnownType(DmdWellKnownType.System_Array);
		public DmdType System_Collections_IEnumerable => GetWellKnownType(DmdWellKnownType.System_Collections_IEnumerable);
		public DmdType System_Collections_Generic_IEnumerable_T => GetWellKnownType(DmdWellKnownType.System_Collections_Generic_IEnumerable_T);
		public DmdType System_Collections_Generic_IList_T => GetWellKnownType(DmdWellKnownType.System_Collections_Generic_IList_T);
		public DmdType System_Collections_Generic_ICollection_T => GetWellKnownType(DmdWellKnownType.System_Collections_Generic_ICollection_T);
		public DmdType System_Collections_IEnumerator => GetWellKnownType(DmdWellKnownType.System_Collections_IEnumerator);
		public DmdType System_Collections_Generic_IEnumerator_T => GetWellKnownType(DmdWellKnownType.System_Collections_Generic_IEnumerator_T);
		public DmdType System_Collections_Generic_IReadOnlyList_T => GetWellKnownType(DmdWellKnownType.System_Collections_Generic_IReadOnlyList_T);
		public DmdType System_Collections_Generic_IReadOnlyCollection_T => GetWellKnownType(DmdWellKnownType.System_Collections_Generic_IReadOnlyCollection_T);
		public DmdType System_Nullable_T => GetWellKnownType(DmdWellKnownType.System_Nullable_T);
		public DmdType System_DateTime => GetWellKnownType(DmdWellKnownType.System_DateTime);
		public DmdType System_Runtime_CompilerServices_IsVolatile => GetWellKnownType(DmdWellKnownType.System_Runtime_CompilerServices_IsVolatile);
		public DmdType System_IDisposable => GetWellKnownType(DmdWellKnownType.System_IDisposable);
		public DmdType System_TypedReference => GetWellKnownType(DmdWellKnownType.System_TypedReference);
		public DmdType System_ArgIterator => GetWellKnownType(DmdWellKnownType.System_ArgIterator);
		public DmdType System_RuntimeArgumentHandle => GetWellKnownType(DmdWellKnownType.System_RuntimeArgumentHandle);
		public DmdType System_RuntimeFieldHandle => GetWellKnownType(DmdWellKnownType.System_RuntimeFieldHandle);
		public DmdType System_RuntimeMethodHandle => GetWellKnownType(DmdWellKnownType.System_RuntimeMethodHandle);
		public DmdType System_RuntimeTypeHandle => GetWellKnownType(DmdWellKnownType.System_RuntimeTypeHandle);
		public DmdType System_IAsyncResult => GetWellKnownType(DmdWellKnownType.System_IAsyncResult);
		public DmdType System_AsyncCallback => GetWellKnownType(DmdWellKnownType.System_AsyncCallback);
		public DmdType System_Type => GetWellKnownType(DmdWellKnownType.System_Type);
#pragma warning restore 1591 // Missing XML comment for publicly visible type or member

		/// <summary>
		/// Returns a cached type if present else the input type
		/// </summary>
		/// <param name="type">Type</param>
		/// <param name="options">Options</param>
		/// <returns></returns>
		public abstract DmdType Intern(DmdType type, MakeTypeOptions options = MakeTypeOptions.None);

		/// <summary>
		/// Makes a pointer type
		/// </summary>
		/// <param name="elementType">Element type</param>
		/// <param name="customModifiers">Custom modifiers or null</param>
		/// <param name="options">Options</param>
		/// <returns></returns>
		public abstract DmdType MakePointerType(DmdType elementType, IList<DmdCustomModifier> customModifiers, MakeTypeOptions options = MakeTypeOptions.None);

		/// <summary>
		/// Makes a by-ref type
		/// </summary>
		/// <param name="elementType">Element type</param>
		/// <param name="customModifiers">Custom modifiers or null</param>
		/// <param name="options">Options</param>
		/// <returns></returns>
		public abstract DmdType MakeByRefType(DmdType elementType, IList<DmdCustomModifier> customModifiers, MakeTypeOptions options = MakeTypeOptions.None);

		/// <summary>
		/// Makes a SZ array type
		/// </summary>
		/// <param name="elementType">Element type</param>
		/// <param name="customModifiers">Custom modifiers or null</param>
		/// <param name="options">Options</param>
		/// <returns></returns>
		public abstract DmdType MakeArrayType(DmdType elementType, IList<DmdCustomModifier> customModifiers, MakeTypeOptions options = MakeTypeOptions.None);

		/// <summary>
		/// Makes a multi-dimensional array type
		/// </summary>
		/// <param name="elementType">Element type</param>
		/// <param name="rank">Number of dimensions</param>
		/// <param name="sizes">Sizes</param>
		/// <param name="lowerBounds">Lower bounds</param>
		/// <param name="customModifiers">Custom modifiers or null</param>
		/// <param name="options">Options</param>
		/// <returns></returns>
		public abstract DmdType MakeArrayType(DmdType elementType, int rank, IList<int> sizes, IList<int> lowerBounds, IList<DmdCustomModifier> customModifiers, MakeTypeOptions options = MakeTypeOptions.None);

		/// <summary>
		/// Makes a generic type
		/// </summary>
		/// <param name="genericTypeDefinition">Generic type definition</param>
		/// <param name="typeArguments">Generic arguments</param>
		/// <param name="customModifiers">Custom modifiers or null</param>
		/// <param name="options">Options</param>
		/// <returns></returns>
		public abstract DmdType MakeGenericType(DmdType genericTypeDefinition, IList<DmdType> typeArguments, IList<DmdCustomModifier> customModifiers, MakeTypeOptions options = MakeTypeOptions.None);

		/// <summary>
		/// Makes a generic method
		/// </summary>
		/// <param name="genericMethodDefinition">Generic method definition</param>
		/// <param name="typeArguments">Generic arguments</param>
		/// <param name="options">Options</param>
		/// <returns></returns>
		public abstract DmdMethodInfo MakeGenericMethod(DmdMethodInfo genericMethodDefinition, IList<DmdType> typeArguments, MakeTypeOptions options = MakeTypeOptions.None);

		/// <summary>
		/// Makes a function pointer type
		/// </summary>
		/// <param name="methodSignature">Method signature</param>
		/// <param name="customModifiers">Custom modifiers or null</param>
		/// <param name="options">Options</param>
		/// <returns></returns>
		public abstract DmdType MakeFunctionPointerType(DmdMethodSignature methodSignature, IList<DmdCustomModifier> customModifiers, MakeTypeOptions options = MakeTypeOptions.None);

		/// <summary>
		/// Makes a function pointer type
		/// </summary>
		/// <param name="flags">Flags</param>
		/// <param name="genericParameterCount">Number of generic parameters</param>
		/// <param name="returnType">Return type</param>
		/// <param name="parameterTypes">Parameter types</param>
		/// <param name="varArgsParameterTypes">VarArgs parameter types</param>
		/// <param name="customModifiers">Custom modifiers or null</param>
		/// <param name="options">Options</param>
		/// <returns></returns>
		public abstract DmdType MakeFunctionPointerType(DmdSignatureCallingConvention flags, int genericParameterCount, DmdType returnType, IList<DmdType> parameterTypes, IList<DmdType> varArgsParameterTypes, IList<DmdCustomModifier> customModifiers, MakeTypeOptions options = MakeTypeOptions.None);

		/// <summary>
		/// Makes a generic type parameter
		/// </summary>
		/// <param name="position">Position</param>
		/// <param name="declaringType">Declaring type</param>
		/// <returns></returns>
		public DmdType MakeGenericTypeParameter(int position, DmdType declaringType) => MakeGenericTypeParameter(position, declaringType, string.Empty, 0, null);

		/// <summary>
		/// Makes a generic type parameter
		/// </summary>
		/// <param name="position">Position</param>
		/// <param name="declaringType">Declaring type</param>
		/// <param name="name">Name</param>
		/// <param name="attributes">Attributes</param>
		/// <param name="customModifiers">Custom modifiers or null</param>
		/// <param name="options">Options</param>
		/// <returns></returns>
		public abstract DmdType MakeGenericTypeParameter(int position, DmdType declaringType, string name, DmdGenericParameterAttributes attributes, IList<DmdCustomModifier> customModifiers, MakeTypeOptions options = MakeTypeOptions.None);

		/// <summary>
		/// Makes a generic method parameter
		/// </summary>
		/// <param name="position">Position</param>
		/// <param name="declaringMethod">Declaring method</param>
		/// <returns></returns>
		public DmdType MakeGenericMethodParameter(int position, DmdMethodBase declaringMethod) => MakeGenericMethodParameter(position, declaringMethod, string.Empty, 0, null);

		/// <summary>
		/// Makes a generic method parameter
		/// </summary>
		/// <param name="position">Position</param>
		/// <param name="declaringMethod">Declaring method</param>
		/// <param name="name">Name</param>
		/// <param name="attributes">Attributes</param>
		/// <param name="customModifiers">Custom modifiers or null</param>
		/// <param name="options">Options</param>
		/// <returns></returns>
		public abstract DmdType MakeGenericMethodParameter(int position, DmdMethodBase declaringMethod, string name, DmdGenericParameterAttributes attributes, IList<DmdCustomModifier> customModifiers, MakeTypeOptions options = MakeTypeOptions.None);

		/// <summary>
		/// Gets a type
		/// </summary>
		/// <param name="type">Type</param>
		/// <returns></returns>
		public DmdType GetType(Type type) => GetType(type, DmdGetTypeOptions.None);

		/// <summary>
		/// Gets a type and throws if it couldn't be found
		/// </summary>
		/// <param name="type"></param>
		/// <returns></returns>
		public DmdType GetTypeThrow(Type type) => GetType(type, DmdGetTypeOptions.ThrowOnError);

		/// <summary>
		/// Gets a type
		/// </summary>
		/// <param name="type">Type</param>
		/// <param name="options">Options</param>
		/// <returns></returns>
		public DmdType GetType(Type type, DmdGetTypeOptions options) {
			if ((object)type == null)
				throw new ArgumentNullException(nameof(type));
			return GetType(type.AssemblyQualifiedName, options);
		}

		/// <summary>
		/// Gets a type
		/// </summary>
		/// <param name="typeName">Full name of the type (<see cref="DmdType.FullName"/>) or the assembly qualified name (<see cref="DmdType.AssemblyQualifiedName"/>).
		/// Version, public key token and culture are optional.</param>
		/// <param name="options">Options</param>
		/// <returns></returns>
		public abstract DmdType GetType(string typeName, DmdGetTypeOptions options);

		/// <summary>
		/// Gets a type
		/// </summary>
		/// <param name="typeName">Full name of the type (<see cref="DmdType.FullName"/>) or the assembly qualified name (<see cref="DmdType.AssemblyQualifiedName"/>).
		/// Version, public key token and culture are optional.</param>
		/// <returns></returns>
		public DmdType GetType(string typeName) => GetType(typeName, DmdGetTypeOptions.None);

		/// <summary>
		/// Gets a type and throws if it couldn't be found
		/// </summary>
		/// <param name="typeName">Full name of the type (<see cref="DmdType.FullName"/>) or the assembly qualified name (<see cref="DmdType.AssemblyQualifiedName"/>).
		/// Version, public key token and culture are optional.</param>
		/// <returns></returns>
		public DmdType GetTypeThrow(string typeName) => GetType(typeName, DmdGetTypeOptions.ThrowOnError);

		/// <summary>
		/// Executes a method
		/// </summary>
		/// <param name="context">Evaluation context</param>
		/// <param name="method">Method to call</param>
		/// <param name="obj">Instance object or null if it's a constructor or a static method</param>
		/// <param name="parameters">Parameters passed to the method</param>
		/// <param name="cancellationToken">Cancellation token</param>
		/// <returns></returns>
		public abstract object Invoke(IDmdEvaluationContext context, DmdMethodBase method, object obj, object[] parameters, CancellationToken cancellationToken = default(CancellationToken));

		/// <summary>
		/// Loads a field
		/// </summary>
		/// <param name="context">Evaluation context</param>
		/// <param name="field">Field</param>
		/// <param name="obj">Instance object or null if it's a static field</param>
		/// <param name="cancellationToken">Cancellation token</param>
		/// <returns></returns>
		public abstract object LoadField(IDmdEvaluationContext context, DmdFieldInfo field, object obj, CancellationToken cancellationToken = default(CancellationToken));

		/// <summary>
		/// Stores a value in a field
		/// </summary>
		/// <param name="context">Evaluation context</param>
		/// <param name="field">Field</param>
		/// <param name="obj">Instance object or null if it's a static field</param>
		/// <param name="value">Value to store in the field</param>
		/// <param name="cancellationToken">Cancellation token</param>
		public abstract void StoreField(IDmdEvaluationContext context, DmdFieldInfo field, object obj, object value, CancellationToken cancellationToken = default(CancellationToken));

		/// <summary>
		/// Executes a method
		/// </summary>
		/// <param name="context">Evaluation context</param>
		/// <param name="method">Method to call</param>
		/// <param name="obj">Instance object or null if it's a constructor or a static method</param>
		/// <param name="parameters">Parameters passed to the method</param>
		/// <param name="callback">Notified when the method is complete</param>
		/// <param name="cancellationToken">Cancellation token</param>
		public abstract void Invoke(IDmdEvaluationContext context, DmdMethodBase method, object obj, object[] parameters, Action<object> callback, CancellationToken cancellationToken = default(CancellationToken));

		/// <summary>
		/// Loads a field
		/// </summary>
		/// <param name="context">Evaluation context</param>
		/// <param name="field">Field</param>
		/// <param name="obj">Instance object or null if it's a static field</param>
		/// <param name="callback">Notified when the method is complete</param>
		/// <param name="cancellationToken">Cancellation token</param>
		public abstract void LoadField(IDmdEvaluationContext context, DmdFieldInfo field, object obj, Action<object> callback, CancellationToken cancellationToken = default(CancellationToken));

		/// <summary>
		/// Stores a value in a field
		/// </summary>
		/// <param name="context">Evaluation context</param>
		/// <param name="field">Field</param>
		/// <param name="obj">Instance object or null if it's a static field</param>
		/// <param name="value">Value to store in the field</param>
		/// <param name="callback">Notified when the method is complete</param>
		/// <param name="cancellationToken">Cancellation token</param>
		public abstract void StoreField(IDmdEvaluationContext context, DmdFieldInfo field, object obj, object value, Action callback, CancellationToken cancellationToken = default(CancellationToken));
	}

	/// <summary>
	/// Options used when creating types
	/// </summary>
	[Flags]
	public enum MakeTypeOptions {
		/// <summary>
		/// No bit is set
		/// </summary>
		None				= 0,

		/// <summary>
		/// Don't try to resolve a reference
		/// </summary>
		NoResolve			= 0x00000001,
	}

	/// <summary>
	/// Options used when finding a type
	/// </summary>
	[Flags]
	public enum DmdGetTypeOptions {
		/// <summary>
		/// No bit is set
		/// </summary>
		None				= 0,

		/// <summary>
		/// Throw if the type couldn't be found
		/// </summary>
		ThrowOnError		= 0x00000001,

		/// <summary>
		/// Ignore case
		/// </summary>
		IgnoreCase			= 0x00000002,
	}
}
