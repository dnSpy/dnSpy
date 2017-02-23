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

namespace dnSpy.Contracts.Debugger.DotNet.CorDebug {
	/// <summary>
	/// .NET runtime in a process
	/// </summary>
	public abstract class CorDebugRuntime : DbgClrRuntime {
		/// <summary>
		/// Gets the runtime version
		/// </summary>
		public abstract CorDebugRuntimeVersion Version { get; }

		/// <summary>
		/// Gets the kind
		/// </summary>
		public CorDebugRuntimeKind Kind => Version.Kind;

		/// <summary>
		/// Path to the CLR dll (clr.dll, mscorwks.dll, mscorsvr.dll, coreclr.dll)
		/// </summary>
		public abstract string ClrFilename { get; }

		/// <summary>
		/// Path to the runtime directory
		/// </summary>
		public abstract string RuntimeDirectory { get; }
	}
}
