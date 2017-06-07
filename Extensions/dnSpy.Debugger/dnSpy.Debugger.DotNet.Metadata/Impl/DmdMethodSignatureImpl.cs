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
	sealed class DmdMethodSignatureImpl : DmdMethodSignature {
		public override DmdSignatureCallingConvention Flags { get; }
		public override int GenericParameterCount { get; }
		public override DmdType ReturnType { get; }

		readonly ReadOnlyCollection<DmdType> parameterTypes;
		readonly ReadOnlyCollection<DmdType> varArgsParameterTypes;

		public DmdMethodSignatureImpl(DmdSignatureCallingConvention flags, int genericParameterCount, DmdType returnType, IList<DmdType> parameterTypes, IList<DmdType> varArgsParameterTypes) {
			if (genericParameterCount < 0)
				throw new ArgumentOutOfRangeException(nameof(genericParameterCount));
			if (parameterTypes == null)
				throw new ArgumentNullException(nameof(parameterTypes));
			if (varArgsParameterTypes == null)
				throw new ArgumentNullException(nameof(varArgsParameterTypes));
			Flags = flags;
			GenericParameterCount = genericParameterCount;
			ReturnType = returnType ?? throw new ArgumentNullException(nameof(returnType));
			this.parameterTypes = parameterTypes.Count == 0 ? emptyTypeCollection : parameterTypes as ReadOnlyCollection<DmdType> ?? new ReadOnlyCollection<DmdType>(parameterTypes);
			this.varArgsParameterTypes = varArgsParameterTypes.Count == 0 ? emptyTypeCollection : varArgsParameterTypes as ReadOnlyCollection<DmdType> ?? new ReadOnlyCollection<DmdType>(varArgsParameterTypes);
		}
		static readonly ReadOnlyCollection<DmdType> emptyTypeCollection = new ReadOnlyCollection<DmdType>(Array.Empty<DmdType>());

		public override ReadOnlyCollection<DmdType> GetReadOnlyParameterTypes() => parameterTypes;
		public override ReadOnlyCollection<DmdType> GetReadOnlyVarArgsParameterTypes() => varArgsParameterTypes;
	}
}
