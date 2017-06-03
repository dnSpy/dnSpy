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
using System.Threading;

namespace dnSpy.Debugger.DotNet.Metadata {
	/// <summary>
	/// A .NET AppDomain
	/// </summary>
	public abstract class DmdAppDomain : DmdObject {
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
		public abstract DmdAssembly GetAssembly(DmdAssemblyName name);

		/// <summary>
		/// Loads an assembly
		/// </summary>
		/// <param name="context">Evaluation context</param>
		/// <param name="assemblyName">Full assembly name</param>
		/// <returns></returns>
		public DmdAssembly Load(IDmdEvaluationContext context, string assemblyName) => Load(context, new DmdAssemblyName(assemblyName));

		/// <summary>
		/// Loads an assembly
		/// </summary>
		/// <param name="context">Evaluation context</param>
		/// <param name="name">Assembly name</param>
		/// <returns></returns>
		public abstract DmdAssembly Load(IDmdEvaluationContext context, DmdAssemblyName name);

		/// <summary>
		/// Gets the core library (eg. mscorlib if it's .NET Framework)
		/// </summary>
		public abstract DmdAssembly CorLib { get; }

		/// <summary>
		/// Checks if a well known member exists in one of the loaded assemblies
		/// </summary>
		/// <param name="wellKnownMember">Well known member</param>
		/// <returns></returns>
		public bool HasWellKnownMember(DmdWellKnownMember wellKnownMember) => (object)GetWellKnownMember(wellKnownMember, isOptional: true) != null;

		/// <summary>
		/// Returns a well known member or throws if it doesn't exist
		/// </summary>
		/// <param name="wellKnownMember">Well known member</param>
		/// <returns></returns>
		public DmdMemberInfo GetWellKnownMember(DmdWellKnownMember wellKnownMember) => GetWellKnownMember(wellKnownMember, isOptional: false);

		/// <summary>
		/// Returns a well known member
		/// </summary>
		/// <param name="wellKnownMember">Well known member</param>
		/// <param name="isOptional">Used if the member couldn't be found. If true, null is returned, and if false, an exception is thrown</param>
		/// <returns></returns>
		public abstract DmdMemberInfo GetWellKnownMember(DmdWellKnownMember wellKnownMember, bool isOptional);

		/// <summary>
		/// Gets a well known field
		/// </summary>
		/// <param name="wellKnownMember">Well known field</param>
		/// <returns></returns>
		public DmdFieldInfo GetWellKnownField(DmdWellKnownMember wellKnownMember) => (DmdFieldInfo)GetWellKnownMember(wellKnownMember);

		/// <summary>
		/// Gets a well known field
		/// </summary>
		/// <param name="wellKnownMember">Well known field</param>
		/// <param name="isOptional">Used if the member couldn't be found. If true, null is returned, and if false, an exception is thrown</param>
		/// <returns></returns>
		public DmdFieldInfo GetWellKnownField(DmdWellKnownMember wellKnownMember, bool isOptional) => (DmdFieldInfo)GetWellKnownMember(wellKnownMember, isOptional);

		/// <summary>
		/// Gets a well known constructor
		/// </summary>
		/// <param name="wellKnownMember">Well known constructor</param>
		/// <returns></returns>
		public DmdConstructorInfo GetWellKnownConstructor(DmdWellKnownMember wellKnownMember) => (DmdConstructorInfo)GetWellKnownMember(wellKnownMember);

		/// <summary>
		/// Gets a well known constructor
		/// </summary>
		/// <param name="wellKnownMember">Well known constructor</param>
		/// <param name="isOptional">Used if the member couldn't be found. If true, null is returned, and if false, an exception is thrown</param>
		/// <returns></returns>
		public DmdConstructorInfo GetWellKnownConstructor(DmdWellKnownMember wellKnownMember, bool isOptional) => (DmdConstructorInfo)GetWellKnownMember(wellKnownMember, isOptional);

		/// <summary>
		/// Gets a well known method
		/// </summary>
		/// <param name="wellKnownMember">Well known method</param>
		/// <returns></returns>
		public DmdMethodInfo GetWellKnownMethod(DmdWellKnownMember wellKnownMember) => (DmdMethodInfo)GetWellKnownMember(wellKnownMember);

		/// <summary>
		/// Gets a well known method
		/// </summary>
		/// <param name="wellKnownMember">Well known method</param>
		/// <param name="isOptional">Used if the member couldn't be found. If true, null is returned, and if false, an exception is thrown</param>
		/// <returns></returns>
		public DmdMethodInfo GetWellKnownMethod(DmdWellKnownMember wellKnownMember, bool isOptional) => (DmdMethodInfo)GetWellKnownMember(wellKnownMember, isOptional);

		/// <summary>
		/// Gets a well known property
		/// </summary>
		/// <param name="wellKnownMember">Well known property</param>
		/// <returns></returns>
		public DmdPropertyInfo GetWellKnownProperty(DmdWellKnownMember wellKnownMember) => (DmdPropertyInfo)GetWellKnownMember(wellKnownMember);

		/// <summary>
		/// Gets a well known property
		/// </summary>
		/// <param name="wellKnownMember">Well known property</param>
		/// <param name="isOptional">Used if the member couldn't be found. If true, null is returned, and if false, an exception is thrown</param>
		/// <returns></returns>
		public DmdPropertyInfo GetWellKnownProperty(DmdWellKnownMember wellKnownMember, bool isOptional) => (DmdPropertyInfo)GetWellKnownMember(wellKnownMember, isOptional);

		/// <summary>
		/// Gets a well known event
		/// </summary>
		/// <param name="wellKnownMember">Well known event</param>
		/// <returns></returns>
		public DmdEventInfo GetWellKnownEvent(DmdWellKnownMember wellKnownMember) => (DmdEventInfo)GetWellKnownMember(wellKnownMember);

		/// <summary>
		/// Gets a well known event
		/// </summary>
		/// <param name="wellKnownMember">Well known event</param>
		/// <param name="isOptional">Used if the member couldn't be found. If true, null is returned, and if false, an exception is thrown</param>
		/// <returns></returns>
		public DmdEventInfo GetWellKnownEvent(DmdWellKnownMember wellKnownMember, bool isOptional) => (DmdEventInfo)GetWellKnownMember(wellKnownMember, isOptional);

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
		public abstract DmdType GetWellKnownType(DmdWellKnownType wellKnownType, bool isOptional);

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
		/// Gets a type
		/// </summary>
		/// <param name="typeName">Full type name</param>
		/// <param name="throwOnError">If true, an exception is thrown if the type couldn't be found</param>
		/// <param name="ignoreCase">true to ignore case</param>
		/// <returns></returns>
		public abstract DmdType GetType(string typeName, bool throwOnError, bool ignoreCase);

		/// <summary>
		/// Gets a type
		/// </summary>
		/// <param name="typeName">Full type name</param>
		/// <param name="throwOnError">If true, an exception is thrown if the type couldn't be found</param>
		/// <returns></returns>
		public DmdType GetType(string typeName, bool throwOnError) => GetType(typeName, throwOnError, false);

		/// <summary>
		/// Gets a type or null if it wasn't found
		/// </summary>
		/// <param name="typeName">Full type name</param>
		/// <returns></returns>
		public DmdType GetType(string typeName) => GetType(typeName, false, false);

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
}
