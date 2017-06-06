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

using System.Collections.ObjectModel;
using System.Linq;

namespace dnSpy.Debugger.DotNet.Metadata {
	/// <summary>
	/// .NET method signature
	/// </summary>
	public abstract class DmdMethodSignature {
		/// <summary>
		/// Gets the flags
		/// </summary>
		public abstract DmdSignatureCallingConvention Flags { get; }

		/// <summary>
		/// true if it's a generic method signature
		/// </summary>
		public bool IsGeneric => (Flags & DmdSignatureCallingConvention.Generic) != 0;

		/// <summary>
		/// true if 'this' is a hidden parameter
		/// </summary>
		public bool HasThis => (Flags & DmdSignatureCallingConvention.HasThis) != 0;

		/// <summary>
		/// true if 'this' is an explicit parameter
		/// </summary>
		public bool ExplicitThis => (Flags & DmdSignatureCallingConvention.ExplicitThis) != 0;

		/// <summary>
		/// Generic parameter count
		/// </summary>
		public abstract int GenericParameterCount { get; }

		/// <summary>
		/// Gets the return type
		/// </summary>
		public abstract DmdType ReturnType { get; }

		/// <summary>
		/// Gets the parameter types, see also <see cref="GetVarArgsParameterTypes"/>
		/// </summary>
		/// <returns></returns>
		public DmdType[] GetParameterTypes() => GetReadOnlyParameterTypes().ToArray();

		/// <summary>
		/// Gets the parameter types, see also <see cref="GetReadOnlyVarArgsParameterTypes"/>
		/// </summary>
		/// <returns></returns>
		public abstract ReadOnlyCollection<DmdType> GetReadOnlyParameterTypes();

		/// <summary>
		/// Gets the var args parameter types
		/// </summary>
		/// <returns></returns>
		public DmdType[] GetVarArgsParameterTypes() => GetReadOnlyVarArgsParameterTypes().ToArray();

		/// <summary>
		/// Gets the var args parameter types
		/// </summary>
		/// <returns></returns>
		public abstract ReadOnlyCollection<DmdType> GetReadOnlyVarArgsParameterTypes();
	}
}
