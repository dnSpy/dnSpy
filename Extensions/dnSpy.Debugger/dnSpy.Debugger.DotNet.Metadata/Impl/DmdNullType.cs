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
using System.Runtime.InteropServices;

namespace dnSpy.Debugger.DotNet.Metadata.Impl {
	sealed class DmdNullType : DmdType {
		public override DmdTypeSignatureKind TypeSignatureKind => DmdTypeSignatureKind.Type;
		public override DmdTypeScope TypeScope => new DmdTypeScope(Module);
		public override DmdMethodBase DeclaringMethod => null;
		public override Guid GUID => Guid.Empty;
		public override DmdModule Module { get; }
		public override DmdAssembly Assembly => Module.Assembly;
		public override string FullName => Name;
		public override string Namespace => null;
		public override string AssemblyQualifiedName => DmdAssembly.CreateQualifiedName(Assembly.FullName, FullName);
		public override DmdType BaseType => null;
		public override StructLayoutAttribute StructLayoutAttribute => null;
		public override DmdGenericParameterAttributes GenericParameterAttributes => throw new InvalidOperationException();
		public override DmdTypeAttributes Attributes => 0;

		public DmdNullType(DmdModule module) => Module = module ?? throw new ArgumentNullException(nameof(module));

		public override DmdType Resolve(bool throwOnError) => this;
		public override DmdType MakePointerType() => throw new NotImplementedException();//TODO:
		public override DmdType MakeByRefType() => throw new NotImplementedException();//TODO:
		public override DmdType MakeArrayType() => throw new NotImplementedException();//TODO:
		public override DmdType MakeArrayType(int rank, int[] sizes, int[] lowerBounds) => throw new NotImplementedException();//TODO:
		public override DmdType MakeGenericType(params DmdType[] typeArguments) => throw new InvalidOperationException();
		public override DmdConstructorInfo GetConstructor(DmdBindingFlags bindingAttr, DmdCallingConventions callConvention, DmdType[] types, DmdParameterModifier[] modifiers) => null;
		public override DmdConstructorInfo[] GetConstructors(DmdBindingFlags bindingAttr) => Array.Empty<DmdConstructorInfo>();
		public override DmdMethodInfo GetMethod(string name, DmdBindingFlags bindingAttr, DmdCallingConventions callConvention, DmdType[] types, DmdParameterModifier[] modifiers) => null;
		public override DmdMethodInfo[] GetMethods(DmdBindingFlags bindingAttr) => Array.Empty<DmdMethodInfo>();
		public override DmdFieldInfo GetField(string name, DmdBindingFlags bindingAttr) => null;
		public override DmdFieldInfo[] GetFields(DmdBindingFlags bindingAttr) => Array.Empty<DmdFieldInfo>();
		public override DmdType GetInterface(string name, bool ignoreCase) => null;
		public override DmdType[] GetInterfaces() => Array.Empty<DmdType>();
		public override DmdEventInfo GetEvent(string name, DmdBindingFlags bindingAttr) => null;
		public override DmdEventInfo[] GetEvents(DmdBindingFlags bindingAttr) => Array.Empty<DmdEventInfo>();
		public override DmdPropertyInfo GetProperty(string name, DmdBindingFlags bindingAttr, DmdType returnType, DmdType[] types, DmdParameterModifier[] modifiers) => null;
		public override DmdPropertyInfo[] GetProperties(DmdBindingFlags bindingAttr) => Array.Empty<DmdPropertyInfo>();
		public override DmdType[] GetNestedTypes(DmdBindingFlags bindingAttr) => Array.Empty<DmdType>();
		public override DmdType GetNestedType(string name, DmdBindingFlags bindingAttr) => null;
		public override DmdMemberInfo[] GetMember(string name, DmdMemberTypes type, DmdBindingFlags bindingAttr) {
			switch (type) {
			case DmdMemberTypes.Constructor: return Array.Empty<DmdConstructorInfo>();
			case DmdMemberTypes.Event: return Array.Empty<DmdEventInfo>();
			case DmdMemberTypes.Field: return Array.Empty<DmdFieldInfo>();
			case DmdMemberTypes.Method: return Array.Empty<DmdMethodInfo>();
			case DmdMemberTypes.Property: return Array.Empty<DmdPropertyInfo>();
			case DmdMemberTypes.TypeInfo: return Array.Empty<DmdType>();
			case DmdMemberTypes.NestedType: return Array.Empty<DmdType>();
			default: return Array.Empty<DmdMemberInfo>();
			}
		}
		public override DmdMemberInfo[] GetMembers(DmdBindingFlags bindingAttr) => Array.Empty<DmdMemberInfo>();
		public override DmdMemberInfo[] GetDefaultMembers() => Array.Empty<DmdMemberInfo>();
		public override int GetArrayRank() => throw new InvalidOperationException();
		public override int[] GetArraySizes() => throw new InvalidOperationException();
		public override int[] GetArrayLowerBounds() => throw new InvalidOperationException();
		public override bool IsGenericType => false;
		public override bool IsGenericTypeDefinition => false;
		public override int GenericParameterPosition => throw new InvalidOperationException();
		public override DmdType[] GetGenericParameterConstraints() => throw new InvalidOperationException();
		public override DmdMethodSignature GetFunctionPointerMethodSignature() => throw new InvalidOperationException();
		public override bool IsCOMObject => false;
		public override bool HasElementType => false;
		public override DmdType GetElementType() => null;
		public override DmdType[] GetGenericArguments() => Array.Empty<DmdType>();
		public override DmdType GetGenericTypeDefinition() => throw new InvalidOperationException();
		public override DmdType[] GetRequiredCustomModifiers() => Array.Empty<DmdType>();
		public override DmdType[] GetOptionalCustomModifiers() => Array.Empty<DmdType>();
		public override DmdType[] GetCustomModifiers() => Array.Empty<DmdType>();
		public override string[] GetEnumNames() => throw new ArgumentException();
		public override string Name => "<Module>";
		public override int MetadataToken => 0x02000001;
		public override bool IsMetadataReference => false;
		public override IList<DmdCustomAttributeData> GetCustomAttributesData() => throw new NotImplementedException();//TODO:
		public override bool IsDefined(string attributeTypeFullName, bool inherit) => throw new NotImplementedException();//TODO:
		public override bool IsDefined(DmdType attributeType, bool inherit) => throw new NotImplementedException();//TODO:
		public override string ToString() => FullName;
	}
}
