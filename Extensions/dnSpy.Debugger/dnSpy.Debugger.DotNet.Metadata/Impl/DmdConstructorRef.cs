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
using System.Threading;

namespace dnSpy.Debugger.DotNet.Metadata.Impl {
	sealed class DmdConstructorRef : DmdConstructorInfoBase {
		public override DmdAppDomain AppDomain => declaringTypeRef.AppDomain;
		public override string Name { get; }
		public override DmdType DeclaringType => __resolvedConstructor_DONT_USE?.DeclaringType ?? declaringTypeRef;
		public override DmdType ReflectedType => DeclaringType;
		public override bool IsMetadataReference => true;
		public override int MetadataToken => ResolvedConstructor.MetadataToken;
		public override DmdMethodImplAttributes MethodImplementationFlags => ResolvedConstructor.MethodImplementationFlags;
		public override DmdMethodAttributes Attributes => ResolvedConstructor.Attributes;
		public override uint RVA => ResolvedConstructor.RVA;
		public override bool IsGenericMethodDefinition => methodSignature.GenericParameterCount != 0;
		public override bool IsGenericMethod => methodSignature.GenericParameterCount != 0;

		DmdConstructorDef ResolvedConstructor => GetResolvedConstructor(throwOnError: true);
		DmdConstructorDef GetResolvedConstructor(bool throwOnError) {
			if ((object)__resolvedConstructor_DONT_USE != null)
				return __resolvedConstructor_DONT_USE;

			DmdConstructorDef newResolvedCtor = null;
			var declType = declaringTypeRef.Resolve(throwOnError);
			if ((object)declType != null) {
				var nonGenericInstDeclType = declType.IsGenericType ? declType.GetGenericTypeDefinition() : declType;
				var nonGenericInstDeclTypeMethod = nonGenericInstDeclType?.GetMethod(Name, rawMethodSignature, throwOnError: false) as DmdConstructorDef;
				if ((object)nonGenericInstDeclTypeMethod != null) {
					newResolvedCtor = (object)nonGenericInstDeclTypeMethod.DeclaringType == declType ?
						nonGenericInstDeclTypeMethod :
						declType.GetMethod(nonGenericInstDeclTypeMethod.Module, nonGenericInstDeclTypeMethod.MetadataToken) as DmdConstructorDef;
					Debug.Assert((object)newResolvedCtor != null);
				}
			}
			if ((object)newResolvedCtor != null) {
				Interlocked.CompareExchange(ref __resolvedConstructor_DONT_USE, newResolvedCtor, null);
				Debug.Assert(DmdMemberInfoEqualityComparer.DefaultMember.Equals(__resolvedConstructor_DONT_USE.ReflectedType, declaringTypeRef));
				return __resolvedConstructor_DONT_USE;
			}
			if (throwOnError)
				throw new MethodResolveException(this);
			return null;
		}
		volatile DmdConstructorDef __resolvedConstructor_DONT_USE;

		readonly DmdType declaringTypeRef;
		readonly DmdMethodSignature rawMethodSignature;
		readonly DmdMethodSignature methodSignature;

		public DmdConstructorRef(DmdType declaringTypeRef, string name, DmdMethodSignature rawMethodSignature, DmdMethodSignature methodSignature) {
			this.declaringTypeRef = declaringTypeRef ?? throw new ArgumentNullException(nameof(declaringTypeRef));
			Name = name ?? throw new ArgumentNullException(nameof(name));
			this.rawMethodSignature = rawMethodSignature ?? throw new ArgumentNullException(nameof(rawMethodSignature));
			this.methodSignature = methodSignature ?? throw new ArgumentNullException(nameof(methodSignature));
		}

		public override DmdConstructorInfo Resolve(bool throwOnError) => GetResolvedConstructor(throwOnError);
		public override ReadOnlyCollection<DmdParameterInfo> GetParameters() => ResolvedConstructor.GetParameters();
		public override ReadOnlyCollection<DmdType> GetGenericArguments() => methodSignature.GenericParameterCount == 0 ? ReadOnlyCollectionHelpers.Empty<DmdType>() : ResolvedConstructor.GetGenericArguments();
		public override DmdMethodBody GetMethodBody() => ResolvedConstructor.GetMethodBody();
		internal override DmdMethodBody GetMethodBody(IList<DmdType> genericMethodArguments) => ResolvedConstructor.GetMethodBody(genericMethodArguments);
		public override DmdMethodSignature GetMethodSignature() => methodSignature;
		public override ReadOnlyCollection<DmdCustomAttributeData> GetCustomAttributesData() => ResolvedConstructor.GetCustomAttributesData();
		public override ReadOnlyCollection<DmdCustomAttributeData> GetSecurityAttributesData() => ResolvedConstructor.GetSecurityAttributesData();
	}
}
