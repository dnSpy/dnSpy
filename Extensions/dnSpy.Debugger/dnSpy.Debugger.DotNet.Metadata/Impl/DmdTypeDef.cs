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
	abstract class DmdTypeDef : DmdTypeBase {
		public override DmdTypeSignatureKind TypeSignatureKind => DmdTypeSignatureKind.Type;
		public override DmdTypeScope TypeScope => new DmdTypeScope(Module);
		public override bool IsMetadataReference => false;
		public override bool IsGenericType => GetOrCreateGenericParameters().Length != 0;
		public override bool IsGenericTypeDefinition => GetOrCreateGenericParameters().Length != 0;
		public override int MetadataToken => (int)(0x02000000 + rid);
		public override StructLayoutAttribute StructLayoutAttribute => throw new NotImplementedException();//TODO:
		public override DmdType DeclaringType => throw new NotImplementedException();//TODO:

		public override DmdType BaseType {
			get {
				if (!baseTypeInitd) {
					lock (LockObject) {
						if (!baseTypeInitd) {
							baseTypeInitd = true;
							baseType = Module.ResolveType(GetBaseTypeToken(), GetOrCreateGenericParameters(), null, throwOnError: false);
						}
					}
				}
				return baseType;
			}
		}
		DmdType baseType;
		bool baseTypeInitd;

		protected abstract int GetBaseTypeToken();

		protected abstract DmdType[] CreateGenericParameters_NoLock();
		DmdType[] GetOrCreateGenericParameters() {
			if (__genericParameters_DONT_USE != null)
				return __genericParameters_DONT_USE;
			lock (LockObject) {
				if (__genericParameters_DONT_USE != null)
					return __genericParameters_DONT_USE;
				__genericParameters_DONT_USE = CreateGenericParameters_NoLock() ?? Array.Empty<DmdType>();
				return __genericParameters_DONT_USE;
			}
		}
		DmdType[] __genericParameters_DONT_USE;

		protected uint Rid => rid;
		readonly uint rid;

		protected DmdTypeDef(uint rid) => this.rid = rid;

		public override DmdType Resolve(bool throwOnError) => this;
		public override DmdType[] GetGenericArguments() => GetOrCreateGenericParameters().CloneArray();
		public override DmdType GetGenericTypeDefinition() => IsGenericType ? this : throw new InvalidOperationException();
	}
}
