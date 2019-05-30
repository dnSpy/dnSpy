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
using System.Runtime.InteropServices;

namespace dnSpy.Debugger.DotNet.Metadata.Impl {
	sealed class DmdFunctionPointerType : DmdTypeBase {
		public override DmdAppDomain AppDomain { get; }
		public override DmdTypeSignatureKind TypeSignatureKind => DmdTypeSignatureKind.FunctionPointer;
		public override DmdTypeScope TypeScope => methodSignature.ReturnType.TypeScope;
		public override DmdModule Module => methodSignature.ReturnType.Module;
		public override string? MetadataNamespace => null;
		public override string? MetadataName => null;
		public override DmdType? BaseType => null;
		public override StructLayoutAttribute? StructLayoutAttribute => null;
		public override DmdTypeAttributes Attributes => DmdTypeAttributes.NotPublic | DmdTypeAttributes.AutoLayout | DmdTypeAttributes.Class | DmdTypeAttributes.AnsiClass;
		public override DmdType? DeclaringType => null;
		public override int MetadataToken => 0x02000000;
		public override bool IsMetadataReference => false;
		internal override bool HasTypeEquivalence => methodSignature.HasTypeEquivalence;

		readonly DmdMethodSignature methodSignature;

		public DmdFunctionPointerType(DmdAppDomain appDomain, DmdMethodSignature methodSignature, IList<DmdCustomModifier>? customModifiers) : base(customModifiers) {
			AppDomain = appDomain ?? throw new ArgumentNullException(nameof(appDomain));
			this.methodSignature = methodSignature ?? throw new ArgumentNullException(nameof(methodSignature));
			IsFullyResolved = ((DmdTypeBase)methodSignature.ReturnType).IsFullyResolved &&
					DmdTypeUtilities.IsFullyResolved(methodSignature.GetParameterTypes()) &&
					DmdTypeUtilities.IsFullyResolved(methodSignature.GetVarArgsParameterTypes());
		}

		public override DmdType WithCustomModifiers(IList<DmdCustomModifier>? customModifiers) => AppDomain.MakeFunctionPointerType(methodSignature.Flags, methodSignature.GenericParameterCount, methodSignature.ReturnType, methodSignature.GetParameterTypes(), methodSignature.GetVarArgsParameterTypes(), customModifiers);
		public override DmdType WithoutCustomModifiers() => GetCustomModifiers().Count == 0 ? this : AppDomain.MakeFunctionPointerType(methodSignature.Flags, methodSignature.GenericParameterCount, methodSignature.ReturnType, methodSignature.GetParameterTypes(), methodSignature.GetVarArgsParameterTypes(), null);

		public override DmdMethodSignature GetFunctionPointerMethodSignature() => methodSignature;

		protected override DmdType? ResolveNoThrowCore() => this;
		public override bool IsFullyResolved { get; }
		public override DmdTypeBase? FullResolve() {
			if (IsFullyResolved)
				return this;
			var returnType = ((DmdTypeBase)methodSignature.ReturnType).FullResolve();
			if (returnType is null)
				return null;
			var parameterTypes = DmdTypeUtilities.FullResolve(methodSignature.GetParameterTypes());
			if (parameterTypes is null)
				return null;
			var varArgsParameterTypes = DmdTypeUtilities.FullResolve(methodSignature.GetVarArgsParameterTypes());
			if (varArgsParameterTypes is null)
				return null;
			return (DmdTypeBase)returnType.AppDomain.MakeFunctionPointerType(methodSignature.Flags, methodSignature.GenericParameterCount, returnType, parameterTypes, varArgsParameterTypes, GetCustomModifiers());
		}

		public override DmdType[]? ReadDeclaredInterfaces() => null;
		public override ReadOnlyCollection<DmdType> NestedTypes => ReadOnlyCollectionHelpers.Empty<DmdType>();
	}
}
