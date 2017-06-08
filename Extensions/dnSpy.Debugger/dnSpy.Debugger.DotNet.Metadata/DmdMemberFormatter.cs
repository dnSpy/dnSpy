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
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text;
using System.Threading;

namespace dnSpy.Debugger.DotNet.Metadata {
	struct DmdMemberFormatter : IDisposable {
		static class StringBuilderPool {
			const int MAX_LEN = 0x200;
			static StringBuilder sb;
			public static StringBuilder Alloc() => Interlocked.Exchange(ref sb, null) ?? new StringBuilder();
			public static void Return(ref StringBuilder builder) {
				var returnedBuilder = builder;
				builder = null;
				if (returnedBuilder.Capacity <= MAX_LEN) {
					returnedBuilder.Clear();
					sb = returnedBuilder;
				}
			}
		}

		readonly GlobalFlags globalFlags;
		StringBuilder writer;
		const int MAX_RECURSION_COUNT = 100;
		int recursionCounter;

		[Flags]
		enum GlobalFlags {
			None					= 0,
			Serializable			= 0x00000001,
		}

		DmdMemberFormatter(GlobalFlags flags) {
			globalFlags = flags;
			writer = StringBuilderPool.Alloc();
			recursionCounter = 0;
		}

		public void Dispose() => StringBuilderPool.Return(ref writer);

		bool IncrementRecursionCounter() {
			if (recursionCounter >= MAX_RECURSION_COUNT)
				return false;
			recursionCounter++;
			return true;
		}
		void DecrementRecursionCounter() => recursionCounter--;

		static DmdType GetNonNestedType(DmdType typeRef) {
			for (int i = 0; i < 1000; i++) {
				var next = typeRef.DeclaringType;
				if ((object)next == null)
					return typeRef;
				typeRef = next;
			}
			return null;
		}

		static bool IsGenericTypeDefinition(DmdType type) {
			if (!type.IsMetadataReference)
				return type.IsGenericTypeDefinition;
			// It's a TypeRef, make sure it won't throw if it can't resolve the type
			var resolvedType = type.ResolveNoThrow();
			if ((object)resolvedType != null)
				return resolvedType.IsGenericTypeDefinition;
			// Guess based on name
			return type.Name.LastIndexOf('`') >= 0;
		}

		static string GetAssemblyFullName(DmdType type) {
			if (!type.IsMetadataReference) {
				if (type.TypeSignatureKind != DmdTypeSignatureKind.GenericInstance)
					return type.Assembly.FullName;

				// Won't throw
				type = type.GetGenericTypeDefinition();
			}

			var nonNested = GetNonNestedType(type);
			if ((object)nonNested != null) {
				var typeScope = nonNested.TypeScope;
				switch (typeScope.Kind) {
				case DmdTypeScopeKind.Invalid:
					break;

				case DmdTypeScopeKind.Module:
					return ((DmdModule)typeScope.Data).Assembly.FullName;

				case DmdTypeScopeKind.ModuleRef:
					return ((DmdAssemblyName)typeScope.Data2).FullName;

				case DmdTypeScopeKind.AssemblyRef:
					return ((DmdAssemblyName)typeScope.Data).FullName;
				}
			}
			return "???";
		}

		static DmdType GetGenericTypeDefinition(DmdType type) {
			if (!type.IsMetadataReference)
				return type.GetGenericTypeDefinition();

			var resolvedType = type.ResolveNoThrow();
			if ((object)resolvedType != null)
				return resolvedType.GetGenericTypeDefinition();

			// Guess
			return type.Name.LastIndexOf('`') >= 0 ? type : null;
		}

		static ReadOnlyCollection<DmdType> GetReadOnlyGenericArguments(DmdType type) {
			if (!type.IsMetadataReference)
				return type.GetReadOnlyGenericArguments();

			var resolvedType = type.ResolveNoThrow();
			if ((object)resolvedType != null)
				return resolvedType.GetReadOnlyGenericArguments();

			return emtpyTypeCollection;
		}
		static readonly ReadOnlyCollection<DmdType> emtpyTypeCollection = new ReadOnlyCollection<DmdType>(Array.Empty<DmdType>());

		static DmdType[] GetGenericArguments(DmdMethodBase method) {
			if (!method.IsMetadataReference)
				return method.GetGenericArguments();

			var resolvedMethod = method.ResolveMethodBaseNoThrow();
			if ((object)resolvedMethod != null)
				return resolvedMethod.GetGenericArguments();

			return Array.Empty<DmdType>();
		}

		public static string FormatFullName(DmdType type) {
			if (type.IsGenericParameter)
				return null;
			return Format(type, serializable: true);
		}

		public static string FormatAssemblyQualifiedName(DmdType type) {
			var fullName = FormatFullName(type);
			if (fullName == null)
				return null;
			return DmdAssembly.CreateQualifiedName(GetAssemblyFullName(type), fullName);
		}

		public static string Format(DmdMemberInfo member, bool serializable = false) {
			using (var formatter = new DmdMemberFormatter(serializable ? GlobalFlags.Serializable : GlobalFlags.None))
				return formatter.FormatCore(member);
		}

		public static string Format(DmdType type, bool serializable = false) {
			using (var formatter = new DmdMemberFormatter(serializable ? GlobalFlags.Serializable : GlobalFlags.None))
				return formatter.FormatCore(type);
		}

		public static string Format(DmdFieldInfo field, bool serializable = false) {
			using (var formatter = new DmdMemberFormatter(serializable ? GlobalFlags.Serializable : GlobalFlags.None))
				return formatter.FormatCore(field);
		}

		public static string Format(DmdMethodBase method, bool serializable = false) {
			using (var formatter = new DmdMemberFormatter(serializable ? GlobalFlags.Serializable : GlobalFlags.None))
				return formatter.FormatCore(method);
		}

		public static string Format(DmdPropertyInfo property, bool serializable = false) {
			using (var formatter = new DmdMemberFormatter(serializable ? GlobalFlags.Serializable : GlobalFlags.None))
				return formatter.FormatCore(property);
		}

		public static string Format(DmdEventInfo @event, bool serializable = false) {
			using (var formatter = new DmdMemberFormatter(serializable ? GlobalFlags.Serializable : GlobalFlags.None))
				return formatter.FormatCore(@event);
		}

		public static string Format(DmdParameterInfo parameter, bool serializable = false) {
			using (var formatter = new DmdMemberFormatter(serializable ? GlobalFlags.Serializable : GlobalFlags.None))
				return formatter.FormatCore(parameter);
		}

		public static string FormatName(DmdType type) {
			using (var formatter = new DmdMemberFormatter(GlobalFlags.None))
				return formatter.FormatNameCore(type);
		}

		string FormatCore(DmdMemberInfo member) {
			Write(member);
			return writer.ToString();
		}

		string FormatCore(DmdType type) {
			Write(type);
			return writer.ToString();
		}

		string FormatCore(DmdFieldInfo field) {
			Write(field);
			return writer.ToString();
		}

		string FormatCore(DmdMethodBase method) {
			Write(method);
			return writer.ToString();
		}

		string FormatCore(DmdPropertyInfo property) {
			Write(property);
			return writer.ToString();
		}

		string FormatCore(DmdEventInfo @event) {
			Write(@event);
			return writer.ToString();
		}

		string FormatCore(DmdParameterInfo parameter) {
			Write(parameter);
			return writer.ToString();
		}

		string FormatNameCore(DmdType type) {
			WriteName(type);
			return writer.ToString();
		}

		void Write(DmdMemberInfo member) {
			switch (member.MemberType) {
			case DmdMemberTypes.TypeInfo:
			case DmdMemberTypes.NestedType:
				Write((DmdType)member);
				break;

			case DmdMemberTypes.Field:
				Write((DmdFieldInfo)member);
				break;

			case DmdMemberTypes.Constructor:
			case DmdMemberTypes.Method:
				Write((DmdMethodBase)member);
				break;

			case DmdMemberTypes.Property:
				Write((DmdPropertyInfo)member);
				break;

			case DmdMemberTypes.Event:
				Write((DmdEventInfo)member);
				break;

			default:
				Debug.Fail($"Unknown member: {member.GetType()}");
				break;
			}
		}

		TypeFlags GetTypeFlags(bool shortTypeNames) {
			var flags = TypeFlags.None;
			if (shortTypeNames)
				flags |= TypeFlags.ShortSpecialNames | TypeFlags.NoDeclaringTypeNames;
			return flags;
		}

		void Write(DmdType type) => Write(type, GetTypeFlags(false));

		[Flags]
		enum TypeFlags {
			None					= 0,
			UseByRefString			= 0x00000001,
			ShortSpecialNames		= 0x00000002,
			NoDeclaringTypeNames	= 0x00000004,
			NoGenericDefParams		= 0x00000008,
		}

		static bool IsShortNameType(DmdType type) => type.IsPrimitive || type == type.AppDomain.System_Void || type == type.AppDomain.System_TypedReference;

		void Write(DmdType type, TypeFlags flags) {
			if ((object)type == null) {
				writer.Append("???");
				return;
			}

			if (!IncrementRecursionCounter()) {
				writer.Append("???");
				return;
			}

			switch (type.TypeSignatureKind) {
			case DmdTypeSignatureKind.Type:
				if ((flags & TypeFlags.NoDeclaringTypeNames) == 0 && type.DeclaringType is DmdType declType && !type.IsGenericParameter) {
					Write(declType, flags | (IsGenericTypeDefinition(type) ? TypeFlags.NoGenericDefParams : 0));
					writer.Append('+');
				}
				if (type.Namespace is string ns && ns.Length > 0) {
					if ((globalFlags & GlobalFlags.Serializable) != 0 || (flags & TypeFlags.ShortSpecialNames) == 0 || !IsShortNameType(type)) {
						writer.Append(ns);
						writer.Append('.');
					}
				}
				writer.Append(type.Name);
				if ((flags & TypeFlags.NoGenericDefParams) == 0 && (globalFlags & GlobalFlags.Serializable) == 0)
					WriteGenericArguments(GetReadOnlyGenericArguments(type), flags & ~TypeFlags.NoGenericDefParams);
				break;

			case DmdTypeSignatureKind.Pointer:
				Write(type.GetElementType(), flags);
				writer.Append('*');
				break;

			case DmdTypeSignatureKind.ByRef:
				Write(type.GetElementType(), flags);
				if ((flags & TypeFlags.UseByRefString) != 0)
					writer.Append(" ByRef");
				else
					writer.Append('&');
				break;

			case DmdTypeSignatureKind.TypeGenericParameter:
			case DmdTypeSignatureKind.MethodGenericParameter:
				writer.Append(type.Name);
				break;

			case DmdTypeSignatureKind.SZArray:
				Write(type.GetElementType(), flags);
				writer.Append("[]");
				break;

			case DmdTypeSignatureKind.MDArray:
				Write(type.GetElementType(), flags);
				writer.Append('[');
				var rank = type.GetArrayRank();
				if (rank <= 0)
					writer.Append("???");
				else if (rank == 1)
					writer.Append('*');
				else
					writer.Append(',', rank - 1);
				writer.Append(']');
				break;

			case DmdTypeSignatureKind.GenericInstance:
				Write(GetGenericTypeDefinition(type), flags | TypeFlags.NoGenericDefParams);
				WriteGenericArguments(GetReadOnlyGenericArguments(type), flags);
				break;

			case DmdTypeSignatureKind.FunctionPointer:
				Write(type.AppDomain.System_IntPtr, flags);
				break;

			default: throw new InvalidOperationException();
			}

			DecrementRecursionCounter();
		}

		void WriteGenericArguments(IList<DmdType> genericArguments, TypeFlags flags) {
			if (genericArguments.Count == 0)
				return;
			writer.Append('[');
			for (int i = 0; i < genericArguments.Count; i++) {
				if (i > 0) {
					// No whitespace is added
					writer.Append(',');
				}
				Write(genericArguments[i], flags);
			}
			writer.Append(']');
		}

		void WriteParameters(IList<DmdType> parameters, TypeFlags flags) {
			for (int i = 0; i < parameters.Count; i++) {
				if (i > 0)
					writer.Append(", ");
				Write(parameters[i], flags);
			}
		}

		void Write(DmdFieldInfo field) {
			Write(field.FieldType, GetTypeFlags(true));
			writer.Append(' ');
			writer.Append(field.Name);
		}

		void Write(DmdMethodBase method) => WriteMethod(method.Name, method.GetMethodSignature(), GetGenericArguments(method), true);
		void Write(DmdPropertyInfo property) => WriteMethod(property.Name, property.GetMethodSignature(), null, false);

		void WriteMethod(string name, DmdMethodSignature sig, IList<DmdType> genericArguments, bool isMethod) {
			var flags = GetTypeFlags(true);
			Write(sig.ReturnType, flags);
			writer.Append(' ');
			writer.Append(name);
			if (genericArguments != null)
				WriteGenericArguments(genericArguments, flags);
			flags |= TypeFlags.UseByRefString;
			if (isMethod || sig.GetReadOnlyParameterTypes().Count != 0 || sig.GetReadOnlyVarArgsParameterTypes().Count != 0) {
				writer.Append(isMethod ? '(' : '[');
				WriteParameters(sig.GetReadOnlyParameterTypes(), flags);
				if (sig.GetReadOnlyVarArgsParameterTypes().Count != 0) {
					if (sig.GetReadOnlyParameterTypes().Count > 0)
						writer.Append(", ");
					writer.Append("...");
				}
				writer.Append(isMethod ? ')' : ']');
			}
		}

		void Write(DmdEventInfo @event) {
			Write(@event.EventHandlerType, GetTypeFlags(true));
			writer.Append(' ');
			writer.Append(@event.Name);
		}

		void Write(DmdParameterInfo parameter) {
			Write(parameter.ParameterType, GetTypeFlags(true));
			writer.Append(' ');
			writer.Append(parameter.Name);
		}

		void WriteName(DmdType type) {
			if ((object)type == null) {
				writer.Append("???");
				return;
			}

			if (!IncrementRecursionCounter()) {
				writer.Append("???");
				return;
			}

			switch (type.TypeSignatureKind) {
			case DmdTypeSignatureKind.Type:
				writer.Append(type.Name);
				break;

			case DmdTypeSignatureKind.Pointer:
				WriteName(type.GetElementType());
				writer.Append('*');
				break;

			case DmdTypeSignatureKind.ByRef:
				WriteName(type.GetElementType());
				writer.Append('&');
				break;

			case DmdTypeSignatureKind.TypeGenericParameter:
			case DmdTypeSignatureKind.MethodGenericParameter:
				writer.Append(type.Name);
				break;

			case DmdTypeSignatureKind.SZArray:
				WriteName(type.GetElementType());
				writer.Append("[]");
				break;

			case DmdTypeSignatureKind.MDArray:
				WriteName(type.GetElementType());
				writer.Append('[');
				var rank = type.GetArrayRank();
				if (rank <= 0)
					writer.Append("???");
				else if (rank == 1)
					writer.Append('*');
				else
					writer.Append(',', rank - 1);
				writer.Append(']');
				break;

			case DmdTypeSignatureKind.GenericInstance:
				WriteName(GetGenericTypeDefinition(type));
				break;

			case DmdTypeSignatureKind.FunctionPointer:
				WriteName(type.AppDomain.System_IntPtr);
				break;

			default: throw new InvalidOperationException();
			}

			DecrementRecursionCounter();
		}
	}
}
