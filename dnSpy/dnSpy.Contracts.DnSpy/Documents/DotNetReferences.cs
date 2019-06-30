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

using dnSpy.Contracts.Metadata;

namespace dnSpy.Contracts.Documents {
	/// <summary>
	/// A reference to a .NET method body offset
	/// </summary>
	public sealed class DotNetMethodBodyReference {
		/// <summary>
		/// The offset is in an epilog
		/// </summary>
		public const uint EPILOG = 0xFFFFFFFF;

		/// <summary>
		/// The offset is in the prolog
		/// </summary>
		public const uint PROLOG = 0xFFFFFFFE;

		/// <summary>
		/// Gets the module
		/// </summary>
		public ModuleId Module { get; }

		/// <summary>
		/// Gets the token of a method within the module
		/// </summary>
		public uint Token { get; }

		/// <summary>
		/// Gets the IL offset in method body, or one of <see cref="PROLOG"/>, <see cref="EPILOG"/>
		/// </summary>
		public uint Offset { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="module">Module</param>
		/// <param name="token">Token of method</param>
		/// <param name="offset">IL offset in method body, or one of <see cref="PROLOG"/>, <see cref="EPILOG"/></param>
		public DotNetMethodBodyReference(ModuleId module, uint token, uint offset) {
			Module = module;
			Token = token;
			Offset = offset;
		}
	}

	/// <summary>
	/// A reference to a .NET definition (type, method, field, property, event, parameter)
	/// </summary>
	public sealed class DotNetTokenReference {
		/// <summary>
		/// Gets the module
		/// </summary>
		public ModuleId Module { get; }

		/// <summary>
		/// Gets the token
		/// </summary>
		public uint Token { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="module">Module</param>
		/// <param name="token">Token</param>
		public DotNetTokenReference(ModuleId module, uint token) {
			Module = module;
			Token = token;
		}
	}
}
