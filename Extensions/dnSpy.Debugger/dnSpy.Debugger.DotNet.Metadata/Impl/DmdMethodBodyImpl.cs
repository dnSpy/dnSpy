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
	sealed class DmdMethodBodyImpl : DmdMethodBody {
		public override int LocalSignatureMetadataToken { get; }
		public override int MaxStackSize { get; }
		public override bool InitLocals { get; }
		public override ReadOnlyCollection<DmdLocalVariableInfo> LocalVariables { get; }
		public override ReadOnlyCollection<DmdExceptionHandlingClause> ExceptionHandlingClauses { get; }
		public override ReadOnlyCollection<DmdType> GenericTypeArguments { get; }
		public override ReadOnlyCollection<DmdType> GenericMethodArguments { get; }

		readonly byte[] ilBytes;

		public DmdMethodBodyImpl(int localSignatureMetadataToken, int maxStackSize, bool initLocals, DmdLocalVariableInfo[] localVariables, DmdExceptionHandlingClause[] exceptionHandlingClauses, IList<DmdType> genericTypeArguments, IList<DmdType> genericMethodArguments, byte[] ilBytes) {
			LocalSignatureMetadataToken = localSignatureMetadataToken;
			MaxStackSize = maxStackSize;
			InitLocals = initLocals;
			LocalVariables = ReadOnlyCollectionHelpers.Create(localVariables);
			ExceptionHandlingClauses = ReadOnlyCollectionHelpers.Create(exceptionHandlingClauses);
			GenericTypeArguments = ReadOnlyCollectionHelpers.Create(genericTypeArguments);
			GenericMethodArguments = ReadOnlyCollectionHelpers.Create(genericMethodArguments);
			this.ilBytes = ilBytes ?? throw new ArgumentNullException(nameof(ilBytes));
		}

		public override byte[] GetILAsByteArray() => ilBytes;
	}
}
