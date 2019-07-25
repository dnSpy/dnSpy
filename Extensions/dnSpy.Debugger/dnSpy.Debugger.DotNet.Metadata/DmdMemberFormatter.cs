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
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text;

namespace dnSpy.Debugger.DotNet.Metadata {
	struct DmdMemberFormatter : IDisposable {
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
			writer = ObjectPools.AllocStringBuilder();
			recursionCounter = 0;
		}

		public void Dispose() => ObjectPools.FreeNoToString(ref writer!);

		bool IncrementRecursionCounter() {
			if (recursionCounter >= MAX_RECURSION_COUNT)
				return false;
			recursionCounter++;
			return true;
		}
		void DecrementRecursionCounter() => recursionCounter--;

		static bool IsGenericTypeDefinition(DmdType type) {
			if (!type.IsMetadataReference)
				return type.IsGenericTypeDefinition;
			// It's a TypeRef, make sure it won't throw if it can't resolve the type
			var resolvedType = type.ResolveNoThrow();
			if (!(resolvedType is null))
				return resolvedType.IsGenericTypeDefinition;
			// Guess based on name
			return type is Impl.DmdTypeRef && type.MetadataName!.LastIndexOf('`') >= 0;
		}

		static bool ContainsGenericParameters(DmdType type) {
			if (!type.IsMetadataReference)
				return type.ContainsGenericParameters;
			// It's a TypeRef, make sure it won't throw if it can't resolve the type
			var resolvedType = type.ResolveNoThrow();
			if (!(resolvedType is null))
				return resolvedType.ContainsGenericParameters;
			if (type is Impl.DmdTypeRef)
				return type.MetadataName!.LastIndexOf('`') >= 0;
			return type.ContainsGenericParameters;
		}

		static void WriteAssemblyFullName(StringBuilder sb, DmdType type) {
			if (!type.IsMetadataReference) {
				if (type.TypeSignatureKind != DmdTypeSignatureKind.GenericInstance) {
					type.Assembly.GetName().FormatFullNameTo(sb);
					return;
				}

				// Won't throw
				type = type.GetGenericTypeDefinition();
			}

			var nonNested = Impl.DmdTypeUtilities.GetNonNestedType(type);
			if (!(nonNested is null)) {
				var typeScope = nonNested.TypeScope;
				switch (typeScope.Kind) {
				default:
				case DmdTypeScopeKind.Invalid:
					sb.Append("???");
					return;

				case DmdTypeScopeKind.Module:
					((DmdModule)typeScope.Data!).Assembly.GetName().FormatFullNameTo(sb);
					return;

				case DmdTypeScopeKind.ModuleRef:
					((IDmdAssemblyName)typeScope.Data2!).FormatFullNameTo(sb);
					return;

				case DmdTypeScopeKind.AssemblyRef:
					((IDmdAssemblyName)typeScope.Data!).FormatFullNameTo(sb);
					return;
				}
			}
		}

		static DmdType? GetGenericTypeDefinition(DmdType type) {
			if (!type.IsMetadataReference)
				return type.GetGenericTypeDefinition();

			var resolvedType = type.ResolveNoThrow();
			if (!(resolvedType is null))
				return resolvedType.GetGenericTypeDefinition();

			if (type is Impl.DmdGenericInstanceTypeRef)
				return type.GetGenericTypeDefinition();
			if (type.MetadataName!.LastIndexOf('`') >= 0)
				return type;
			return null;
		}

		static ReadOnlyCollection<DmdType> GetGenericArguments(DmdType type) {
			if (!type.IsMetadataReference)
				return type.GetGenericArguments();

			var resolvedType = type.ResolveNoThrow();
			if (!(resolvedType is null))
				return resolvedType.GetGenericArguments();

			if (type is Impl.DmdGenericInstanceTypeRef)
				return type.GetGenericArguments();
			return ReadOnlyCollectionHelpers.Empty<DmdType>();
		}

		static IList<DmdType> GetGenericArguments(DmdMethodBase method) {
			if (method.GetMethodSignature().GenericParameterCount == 0)
				return Array.Empty<DmdType>();
			if (!method.IsMetadataReference)
				return method.GetGenericArguments();

			var resolvedMethod = method.ResolveMethodBaseNoThrow();
			if (!(resolvedMethod is null))
				return resolvedMethod.GetGenericArguments();

			return Array.Empty<DmdType>();
		}

		public static string? FormatFullName(DmdType type) => Format(type, serializable: true);

		public static string? FormatAssemblyQualifiedName(DmdType type) {
			var t = type;
			while (t.GetElementType() is DmdType elementType)
				t = elementType;
			if (!IsGenericTypeDefinition(t) && ContainsGenericParameters(t))
				return null;
			using (var formatter = new DmdMemberFormatter(GlobalFlags.Serializable)) {
				formatter.Write(type);
				formatter.writer.Append(", ");
				WriteAssemblyFullName(formatter.writer, type);
				return formatter.writer.ToString();
			}
		}

		public static string Format(DmdMemberInfo member, bool serializable = false) {
			using (var formatter = new DmdMemberFormatter(serializable ? GlobalFlags.Serializable : GlobalFlags.None))
				return formatter.FormatCore(member);
		}

		public static string? Format(DmdType type, bool serializable = false) {
			if (serializable) {
				var t = type;
				while (t.GetElementType() is DmdType elementType)
					t = elementType;
				if (!IsGenericTypeDefinition(t) && ContainsGenericParameters(t))
					return null;
			}
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

		public static string Format(DmdMethodSignature methodSignature, bool serializable = false) {
			using (var formatter = new DmdMemberFormatter(serializable ? GlobalFlags.Serializable : GlobalFlags.None))
				return formatter.FormatCore(methodSignature);
		}

		public static string FormatName(DmdType type) {
			if (type.MetadataName is string name && name.IndexOfAny(escapeChars) < 0)
				return name;
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

		string FormatCore(DmdMethodSignature methodSignature) {
			Write(methodSignature);
			return writer.ToString();
		}

		string FormatNameCore(DmdType type) {
			WriteName(type, GetTypeFlags(shortTypeNames: false) | TypeFlags.FnPtrIsIntPtr);
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

		void Write(DmdType type) => Write(type, GetTypeFlags(false) | TypeFlags.FnPtrIsIntPtr);

		[Flags]
		enum TypeFlags {
			None						= 0,
			ShortSpecialNames			= 0x00000001,
			NoDeclaringTypeNames		= 0x00000002,
			NoGenericDefParams			= 0x00000004,
			MethodGenericArgumentType	= 0x00000008,
			FnPtrIsIntPtr				= 0x00000010,
		}

		void WriteIdentifier(string? id) {
			if (id is null)
				id = string.Empty;
			if (id.IndexOfAny(escapeChars) < 0)
				writer.Append(id);
			else {
				int start = 0;
				for (int i = 0; i < id.Length; i++) {
					var c = id[i];
					switch (c) {
					// coreclr: IsTypeNameReservedChar()
					case ',':
					case '[':
					case ']':
					case '&':
					case '*':
					case '+':
					case '\\':
						writer.Append(id, start, i - start);
						writer.Append('\\');
						writer.Append(c);
						start = i + 1;
						break;
					}
				}
				if (start != id.Length) {
					if (start == 0)
						writer.Append(id);
					else
						writer.Append(id, start, id.Length - start);
				}
			}
		}
		// coreclr: IsTypeNameReservedChar()
		static readonly char[] escapeChars = new[] { ',', '[', ']', '&', '*', '+', '\\' };

		static bool IsShortNameType(DmdType type) => type.IsPrimitive || type == type.AppDomain.System_Void || type == type.AppDomain.System_TypedReference;

		void Write(DmdType? type, TypeFlags flags) {
			if (type is null) {
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
				if (!type.IsNested && type.MetadataNamespace is string ns && ns.Length > 0) {
					if ((globalFlags & GlobalFlags.Serializable) != 0 ||
						((flags & TypeFlags.MethodGenericArgumentType) == 0 && ((flags & TypeFlags.ShortSpecialNames) == 0 || !IsShortNameType(type)))) {
						WriteIdentifier(ns);
						writer.Append('.');
					}
				}
				WriteIdentifier(type.MetadataName);
				if ((flags & TypeFlags.NoGenericDefParams) == 0 && (globalFlags & GlobalFlags.Serializable) == 0)
					WriteTypeGenericArguments(GetGenericArguments(type), flags & ~TypeFlags.NoGenericDefParams);
				break;

			case DmdTypeSignatureKind.Pointer:
				Write(type.GetElementType(), flags);
				writer.Append('*');
				break;

			case DmdTypeSignatureKind.ByRef:
				Write(type.GetElementType(), flags);
				writer.Append('&');
				break;

			case DmdTypeSignatureKind.TypeGenericParameter:
			case DmdTypeSignatureKind.MethodGenericParameter:
				WriteIdentifier(type.MetadataName);
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
				if ((flags & TypeFlags.MethodGenericArgumentType) == 0)
					WriteTypeGenericArguments(GetGenericArguments(type), flags);
				break;

			case DmdTypeSignatureKind.FunctionPointer:
				if ((flags & TypeFlags.FnPtrIsIntPtr) != 0)
					Write(type.AppDomain.System_IntPtr, flags);
				else
					writer.Append("(fnptr)");
				break;

			default: throw new InvalidOperationException();
			}

			DecrementRecursionCounter();
		}

		void WriteTypeGenericArguments(IList<DmdType> genericArguments, TypeFlags flags) => WriteGenericArguments(genericArguments, flags & ~(TypeFlags.ShortSpecialNames | TypeFlags.NoDeclaringTypeNames));
		void WriteMethodGenericArguments(IList<DmdType> genericArguments, TypeFlags flags) => WriteGenericArguments(genericArguments, flags | TypeFlags.MethodGenericArgumentType);

		void WriteGenericArguments(IList<DmdType> genericArguments, TypeFlags flags) {
			if (genericArguments.Count == 0)
				return;
			writer.Append('[');
			for (int i = 0; i < genericArguments.Count; i++) {
				if (i > 0) {
					// No whitespace is added
					writer.Append(',');
				}
				if ((globalFlags & GlobalFlags.Serializable) != 0)
					writer.Append('[');
				var gaType = genericArguments[i];
				Write(gaType, flags);
				if ((globalFlags & GlobalFlags.Serializable) != 0) {
					writer.Append(", ");
					WriteAssemblyFullName(writer, gaType);
					writer.Append(']');
				}
			}
			writer.Append(']');
		}

		void WriteParameters(IList<DmdType> parameters, TypeFlags flags) {
			for (int i = 0; i < parameters.Count; i++) {
				if (i > 0)
					writer.Append(", ");
				int origLen = writer.Length;
				var type = parameters[i];
				FormatTypeName(type, flags);

				if (type.IsByRef && (globalFlags & GlobalFlags.Serializable) == 0) {
					while (writer.Length > origLen && writer[writer.Length - 1] == '&')
						writer.Length--;
					writer.Append(" ByRef");
				}
			}
		}

		void Write(DmdFieldInfo field) {
			FormatTypeName(field.FieldType, GetTypeFlags(true) | TypeFlags.FnPtrIsIntPtr);
			writer.Append(' ');
			writer.Append(field.Name);
		}

		void Write(DmdMethodBase method) => WriteMethod(method.Name, method.GetMethodSignature(), GetGenericArguments(method), isMethod: true);
		void Write(DmdPropertyInfo property) => WriteMethod(property.Name, property.GetMethodSignature(), genericArguments: null, isMethod: false);
		void Write(DmdMethodSignature methodSignature) => WriteMethod(null, methodSignature, genericArguments: null, isMethod: true);

		void WriteMethod(string? name, DmdMethodSignature sig, IList<DmdType>? genericArguments, bool isMethod) {
			var flags = GetTypeFlags(true) | TypeFlags.FnPtrIsIntPtr;
			FormatTypeName(sig.ReturnType, flags);
			writer.Append(' ');
			writer.Append(name);
			if (!(genericArguments is null))
				WriteMethodGenericArguments(genericArguments, flags);
			if (isMethod || sig.GetParameterTypes().Count != 0 || sig.GetVarArgsParameterTypes().Count != 0) {
				if (!isMethod)
					writer.Append(' ');
				writer.Append(isMethod ? '(' : '[');
				WriteParameters(sig.GetParameterTypes(), flags);
				if ((sig.Flags & DmdSignatureCallingConvention.Mask) == DmdSignatureCallingConvention.VarArg) {
					if (sig.GetParameterTypes().Count > 0)
						writer.Append(", ");
					writer.Append("...");
				}
				writer.Append(isMethod ? ')' : ']');
			}
		}

		void FormatTypeName(DmdType type, TypeFlags flags) {
			if ((globalFlags & GlobalFlags.Serializable) != 0)
				Write(type, flags);
			else {
				var rootType = type;
				while (rootType.GetElementType() is DmdType elementType)
					rootType = elementType;
				if (rootType.IsNested)
					WriteName(type, flags);
				else
					Write(type, flags | TypeFlags.ShortSpecialNames);
			}
		}

		void Write(DmdEventInfo @event) {
			FormatTypeName(@event.EventHandlerType, GetTypeFlags(true) | TypeFlags.FnPtrIsIntPtr);
			writer.Append(' ');
			writer.Append(@event.Name);
		}

		void Write(DmdParameterInfo parameter) {
			FormatTypeName(parameter.ParameterType, GetTypeFlags(true) | TypeFlags.FnPtrIsIntPtr);
			writer.Append(' ');
			writer.Append(parameter.Name);
		}

		void WriteName(DmdType? type, TypeFlags flags) {
			if (type is null) {
				writer.Append("???");
				return;
			}

			if (!IncrementRecursionCounter()) {
				writer.Append("???");
				return;
			}

			switch (type.TypeSignatureKind) {
			case DmdTypeSignatureKind.Type:
				WriteIdentifier(type.MetadataName);
				break;

			case DmdTypeSignatureKind.Pointer:
				WriteName(type.GetElementType(), flags);
				writer.Append('*');
				break;

			case DmdTypeSignatureKind.ByRef:
				WriteName(type.GetElementType(), flags);
				writer.Append('&');
				break;

			case DmdTypeSignatureKind.TypeGenericParameter:
			case DmdTypeSignatureKind.MethodGenericParameter:
				WriteIdentifier(type.MetadataName);
				break;

			case DmdTypeSignatureKind.SZArray:
				WriteName(type.GetElementType(), flags);
				writer.Append("[]");
				break;

			case DmdTypeSignatureKind.MDArray:
				WriteName(type.GetElementType(), flags);
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
				WriteName(GetGenericTypeDefinition(type), flags);
				break;

			case DmdTypeSignatureKind.FunctionPointer:
				if ((flags & TypeFlags.FnPtrIsIntPtr) != 0)
					WriteName(type.AppDomain.System_IntPtr, flags);
				else
					writer.Append("(fnptr)");
				break;

			default: throw new InvalidOperationException();
			}

			DecrementRecursionCounter();
		}
	}
}
