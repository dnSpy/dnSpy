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

namespace dnSpy.Debugger.DotNet.Metadata.Impl {
	abstract class DmdTypeBase : DmdType {
		protected static readonly ReadOnlyCollection<DmdType> emptyReadOnlyCollection = new ReadOnlyCollection<DmdType>(Array.Empty<DmdType>());
		public override DmdMethodBase DeclaringMethod => null;
		public sealed override Guid GUID => throw new NotImplementedException();//TODO:
		public sealed override DmdAssembly Assembly => Module.Assembly;
		public sealed override string FullName => throw new NotImplementedException();//TODO:
		public sealed override string AssemblyQualifiedName => throw new NotImplementedException();//TODO:
		public sealed override bool IsCOMObject => throw new NotImplementedException();//TODO:
		public sealed override bool HasElementType => (object)GetElementType() != null;
		public override DmdGenericParameterAttributes GenericParameterAttributes => throw new InvalidOperationException();
		public override bool IsGenericType => false;
		public override bool IsGenericTypeDefinition => false;
		public override int GenericParameterPosition => throw new InvalidOperationException();
		public override ReadOnlyCollection<DmdType> GetReadOnlyGenericArguments() => emptyReadOnlyCollection;
		public override DmdType GetGenericTypeDefinition() => throw new InvalidOperationException();
		public override ReadOnlyCollection<DmdType> GetReadOnlyGenericParameterConstraints() => throw new InvalidOperationException();
		public override DmdMethodSignature GetFunctionPointerMethodSignature() => throw new InvalidOperationException();
		public override DmdType GetElementType() => null;
		public override int GetArrayRank() => throw new ArgumentException();
		public override ReadOnlyCollection<int> GetReadOnlyArraySizes() => throw new ArgumentException();
		public override ReadOnlyCollection<int> GetReadOnlyArrayLowerBounds() => throw new ArgumentException();
		public sealed override DmdType MakePointerType() => throw new NotImplementedException();//TODO:
		public sealed override DmdType MakeByRefType() => throw new NotImplementedException();//TODO:
		public sealed override DmdType MakeArrayType() => throw new NotImplementedException();//TODO:
		public sealed override DmdType MakeArrayType(int rank, IList<int> sizes, IList<int> lowerBounds) => throw new NotImplementedException();//TODO:
		public sealed override DmdType MakeGenericType(IList<DmdType> typeArguments) => throw new NotImplementedException();//TODO:
		public sealed override DmdConstructorInfo GetConstructor(DmdBindingFlags bindingAttr, DmdCallingConventions callConvention, IList<DmdType> types, IList<DmdParameterModifier> modifiers) => throw new NotImplementedException();//TODO:
		public sealed override DmdConstructorInfo[] GetConstructors(DmdBindingFlags bindingAttr) => throw new NotImplementedException();//TODO:
		public sealed override DmdMethodInfo GetMethod(string name, DmdBindingFlags bindingAttr, DmdCallingConventions callConvention, IList<DmdType> types, IList<DmdParameterModifier> modifiers) => throw new NotImplementedException();//TODO:
		public sealed override DmdMethodInfo[] GetMethods(DmdBindingFlags bindingAttr) => throw new NotImplementedException();//TODO:
		public sealed override DmdFieldInfo GetField(string name, DmdBindingFlags bindingAttr) => throw new NotImplementedException();//TODO:
		public sealed override DmdFieldInfo[] GetFields(DmdBindingFlags bindingAttr) => throw new NotImplementedException();//TODO:
		public sealed override DmdType GetInterface(string name, bool ignoreCase) => throw new NotImplementedException();//TODO:
		public sealed override ReadOnlyCollection<DmdType> GetReadOnlyInterfaces() => throw new NotImplementedException();//TODO:
		public sealed override DmdEventInfo GetEvent(string name, DmdBindingFlags bindingAttr) => throw new NotImplementedException();//TODO:
		public sealed override DmdEventInfo[] GetEvents(DmdBindingFlags bindingAttr) => throw new NotImplementedException();//TODO:
		public sealed override DmdPropertyInfo GetProperty(string name, DmdBindingFlags bindingAttr, DmdType returnType, IList<DmdType> types, IList<DmdParameterModifier> modifiers) => throw new NotImplementedException();//TODO:
		public sealed override DmdPropertyInfo[] GetProperties(DmdBindingFlags bindingAttr) => throw new NotImplementedException();//TODO:
		public sealed override DmdType[] GetNestedTypes(DmdBindingFlags bindingAttr) => throw new NotImplementedException();//TODO:
		public sealed override DmdType GetNestedType(string name, DmdBindingFlags bindingAttr) => throw new NotImplementedException();//TODO:
		public sealed override DmdMemberInfo[] GetMember(string name, DmdMemberTypes type, DmdBindingFlags bindingAttr) => throw new NotImplementedException();//TODO:
		public sealed override DmdMemberInfo[] GetMembers(DmdBindingFlags bindingAttr) => throw new NotImplementedException();//TODO:
		public sealed override DmdMemberInfo[] GetDefaultMembers() => throw new NotImplementedException();//TODO:
		public sealed override string[] GetEnumNames() => throw new NotImplementedException();//TODO:
		public sealed override IList<DmdCustomAttributeData> GetCustomAttributesData() => throw new NotImplementedException();//TODO:
		public sealed override ReadOnlyCollection<DmdCustomModifier> GetCustomModifiers() => throw new NotImplementedException();//TODO:
		public sealed override bool IsDefined(string attributeTypeFullName, bool inherit) => throw new NotImplementedException();//TODO:
		public sealed override bool IsDefined(DmdType attributeType, bool inherit) => throw new NotImplementedException();//TODO:
		public sealed override string ToString() => throw new NotImplementedException();//TODO:
	}
}
