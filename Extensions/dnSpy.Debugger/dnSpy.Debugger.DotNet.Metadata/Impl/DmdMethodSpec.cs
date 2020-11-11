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

namespace dnSpy.Debugger.DotNet.Metadata.Impl {
	sealed class DmdMethodSpec : DmdMethodInfoBase {
		public override string Name => genericMethodDefinition.Name;
		public override DmdType? DeclaringType => genericMethodDefinition.DeclaringType;
		public override DmdType? ReflectedType => genericMethodDefinition.ReflectedType;
		public override int MetadataToken => genericMethodDefinition.MetadataToken;
		public override DmdMethodImplAttributes MethodImplementationFlags => genericMethodDefinition.MethodImplementationFlags;
		public override DmdMethodAttributes Attributes => genericMethodDefinition.Attributes;
		public override uint RVA => genericMethodDefinition.RVA;
		public override bool IsMetadataReference => false;
		public override bool IsGenericMethodDefinition => false;
		public override bool IsGenericMethod => true;

		readonly DmdMethodDef genericMethodDefinition;
		readonly DmdMethodSignature methodSignature;
		readonly ReadOnlyCollection<DmdType> genericArguments;

		public DmdMethodSpec(DmdMethodDef genericMethodDefinition, IList<DmdType> genericArguments) {
			this.genericMethodDefinition = genericMethodDefinition ?? throw new ArgumentNullException(nameof(genericMethodDefinition));
			methodSignature = genericMethodDefinition.GetMethodSignature(genericArguments);
			this.genericArguments = ReadOnlyCollectionHelpers.Create(genericArguments);
		}

		public override DmdMethodInfo? Resolve(bool throwOnError) => this;
		public override DmdMethodSignature GetMethodSignature() => methodSignature;
		internal override DmdMethodInfo? GetParentDefinition() => genericMethodDefinition.GetParentDefinition();
		public override ReadOnlyCollection<DmdType> GetGenericArguments() => genericArguments;
		public override DmdMethodInfo GetGenericMethodDefinition() {
			var method = genericMethodDefinition;
			if ((object?)method.ReflectedType == method.DeclaringType)
				return method;
			return method.DeclaringType!.GetMethod(method.Module, method.MetadataToken) as DmdMethodInfo ?? throw new InvalidOperationException();
		}
		public override DmdMethodInfo MakeGenericMethod(IList<DmdType> typeArguments) => AppDomain.MakeGenericMethod(this, typeArguments);
		public override DmdMethodBody? GetMethodBody() => genericMethodDefinition.GetMethodBody(genericArguments);
		internal override DmdMethodBody? GetMethodBody(IList<DmdType> genericMethodArguments) => genericMethodDefinition.GetMethodBody(genericMethodArguments);
		public override ReadOnlyCollection<DmdCustomAttributeData> GetCustomAttributesData() => genericMethodDefinition.GetCustomAttributesData();
		public override ReadOnlyCollection<DmdCustomAttributeData> GetSecurityAttributesData() => genericMethodDefinition.GetSecurityAttributesData();

		public override DmdParameterInfo ReturnParameter {
			get {
				if (__parameters_DONT_USE is null)
					InitializeParameters();
				return __returnParameter_DONT_USE!;
			}
		}

		public override ReadOnlyCollection<DmdParameterInfo> GetParameters() {
			if (__parameters_DONT_USE is null)
				InitializeParameters();
			return __parameters_DONT_USE!;
		}

		void InitializeParameters() {
			if (__parameters_DONT_USE is not null)
				return;

			var newRP = new DmdCreatedParameterInfo(this, genericMethodDefinition.ReturnParameter, methodSignature.ReturnType);
			var defParameters = genericMethodDefinition.GetParameters();
			var paramTypes = methodSignature.GetParameterTypes();
			var parameters = defParameters.Count == 0 ? Array.Empty<DmdParameterInfo>() : new DmdParameterInfo[Math.Min(paramTypes.Count, defParameters.Count)];
			for (int i = 0; i < parameters.Length; i++)
				parameters[i] = new DmdCreatedParameterInfo(this, defParameters[i], paramTypes[i]);

			lock (LockObject) {
				if (__parameters_DONT_USE is null) {
					__returnParameter_DONT_USE = newRP;
					__parameters_DONT_USE = ReadOnlyCollectionHelpers.Create(parameters);
				}
			}
		}
		volatile ReadOnlyCollection<DmdParameterInfo>? __parameters_DONT_USE;
		volatile DmdParameterInfo? __returnParameter_DONT_USE;
	}
}
