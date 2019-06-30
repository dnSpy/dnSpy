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

namespace dnSpy.Contracts.Debugger.DotNet.Evaluation.ExpressionCompiler {
	/// <summary>
	/// A reference to module metadata used by .NET expression compilers
	/// </summary>
	public abstract class DbgModuleReference : DbgObject {
		/// <summary>
		/// Gets the address of the .NET metadata (BSJB header)
		/// </summary>
		public abstract IntPtr MetadataAddress { get; }

		/// <summary>
		/// Gets the size of the metadata
		/// </summary>
		public abstract uint MetadataSize { get; }

		/// <summary>
		/// Gets the module version id
		/// </summary>
		public abstract Guid ModuleVersionId { get; }

		/// <summary>
		/// Gets the module generation id
		/// </summary>
		public abstract Guid GenerationId { get; }
	}
}
