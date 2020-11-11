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
using System.Threading;

namespace dnSpy.Debugger.DotNet.Metadata.Impl {
	sealed class DmdMethodSpecRef : DmdMethodInfoBase {
		public override DmdAppDomain AppDomain => genericMethodRef.AppDomain;
		public override string Name => genericMethodRef.Name;
		public override DmdType? DeclaringType => genericMethodRef.DeclaringType;
		public override DmdType? ReflectedType => genericMethodRef.ReflectedType;
		public override int MetadataToken => genericMethodRef.MetadataToken;
		public override DmdMethodImplAttributes MethodImplementationFlags => genericMethodRef.MethodImplementationFlags;
		public override DmdMethodAttributes Attributes => genericMethodRef.Attributes;
		public override uint RVA => genericMethodRef.RVA;
		public override bool IsMetadataReference => true;
		public override bool IsGenericMethodDefinition => false;
		public override bool IsGenericMethod => true;

		readonly DmdMethodRef genericMethodRef;
		readonly DmdMethodSignature methodSignature;
		readonly ReadOnlyCollection<DmdType> genericArguments;

		public DmdMethodSpecRef(DmdMethodRef genericMethodRef, IList<DmdType> genericArguments) {
			this.genericMethodRef = genericMethodRef ?? throw new ArgumentNullException(nameof(genericMethodRef));
			methodSignature = genericMethodRef.GetMethodSignature(genericArguments);
			this.genericArguments = ReadOnlyCollectionHelpers.Create(genericArguments);
		}

		public override DmdMethodInfo? Resolve(bool throwOnError) {
			if (__resolvedMethod_DONT_USE is not null)
				return __resolvedMethod_DONT_USE;

			var genericMethodDef = (DmdMethodDef?)genericMethodRef.Resolve(throwOnError);
			if (genericMethodDef is not null) {
				var newResolvedMethod = (DmdMethodSpec)AppDomain.MakeGenericMethod(genericMethodDef, genericArguments, DmdMakeTypeOptions.None);
				if (newResolvedMethod is not null) {
					Interlocked.CompareExchange(ref __resolvedMethod_DONT_USE, newResolvedMethod, null);
					return __resolvedMethod_DONT_USE!;
				}
			}
			if (throwOnError)
				throw new MethodResolveException(this);
			return null;
		}
		volatile DmdMethodSpec? __resolvedMethod_DONT_USE;

		public override DmdMethodSignature GetMethodSignature() => methodSignature;
		internal override DmdMethodInfo? GetParentDefinition() => genericMethodRef.GetParentDefinition();
		public override ReadOnlyCollection<DmdType> GetGenericArguments() => genericArguments;
		public override DmdMethodInfo GetGenericMethodDefinition() => Resolve(throwOnError: true)!.GetGenericMethodDefinition();
		public override DmdMethodInfo MakeGenericMethod(IList<DmdType> typeArguments) => AppDomain.MakeGenericMethod(this, typeArguments);
		public override DmdMethodBody? GetMethodBody() => genericMethodRef.GetMethodBody(genericArguments);
		internal override DmdMethodBody? GetMethodBody(IList<DmdType> genericMethodArguments) => genericMethodRef.GetMethodBody(genericMethodArguments);
		public override ReadOnlyCollection<DmdCustomAttributeData> GetCustomAttributesData() => genericMethodRef.GetCustomAttributesData();
		public override ReadOnlyCollection<DmdCustomAttributeData> GetSecurityAttributesData() => genericMethodRef.GetSecurityAttributesData();
		public override DmdParameterInfo ReturnParameter => Resolve(throwOnError: true)!.ReturnParameter;
		public override ReadOnlyCollection<DmdParameterInfo> GetParameters() => Resolve(throwOnError: true)!.GetParameters();
	}
}
