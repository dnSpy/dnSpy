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
using System.Runtime.InteropServices;

namespace dnSpy.Debugger.DotNet.Metadata.Impl {
	sealed class DmdFunctionPointerType : DmdTypeBase {
		public override DmdTypeSignatureKind TypeSignatureKind => DmdTypeSignatureKind.FunctionPointer;
		public override DmdTypeScope TypeScope => methodSignature.ReturnType.TypeScope;
		public override DmdModule Module => methodSignature.ReturnType.Module;
		public override string Namespace => null;
		public override DmdType BaseType => null;
		public override StructLayoutAttribute StructLayoutAttribute => null;
		public override DmdTypeAttributes Attributes => DmdTypeAttributes.NotPublic | DmdTypeAttributes.AutoLayout | DmdTypeAttributes.Class | DmdTypeAttributes.AnsiClass;
		public override string Name => DmdMemberFormatter.FormatName(this);
		public override DmdType DeclaringType => null;
		public override int MetadataToken => 0x02000000;
		public override bool IsMetadataReference => false;

		readonly DmdMethodSignature methodSignature;

		public DmdFunctionPointerType(DmdMethodSignature methodSignature) {
			this.methodSignature = methodSignature ?? throw new ArgumentNullException(nameof(methodSignature));
			IsFullyResolved = ((DmdTypeBase)methodSignature.ReturnType).IsFullyResolved &&
					DmdTypeUtilities.IsFullyResolved(methodSignature.GetReadOnlyParameterTypes()) &&
					DmdTypeUtilities.IsFullyResolved(methodSignature.GetReadOnlyVarArgsParameterTypes());
		}

		public override DmdMethodSignature GetFunctionPointerMethodSignature() => methodSignature;

		protected override DmdType ResolveNoThrowCore() => this;
		public override bool IsFullyResolved { get; }
		public override DmdTypeBase FullResolve() {
			if (IsFullyResolved)
				return this;
			var returnType = ((DmdTypeBase)methodSignature.ReturnType).FullResolve();
			if ((object)returnType == null)
				return null;
			var parameterTypes = DmdTypeUtilities.FullResolve(methodSignature.GetReadOnlyParameterTypes());
			if (parameterTypes == null)
				return null;
			var varArgsParameterTypes = DmdTypeUtilities.FullResolve(methodSignature.GetReadOnlyVarArgsParameterTypes());
			if (varArgsParameterTypes == null)
				return null;
			return (DmdTypeBase)returnType.AppDomain.MakeFunctionPointerType(methodSignature.Flags, methodSignature.GenericParameterCount, returnType, parameterTypes, varArgsParameterTypes);
		}
	}
}
