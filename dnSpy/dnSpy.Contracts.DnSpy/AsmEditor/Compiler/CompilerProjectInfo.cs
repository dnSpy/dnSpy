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

namespace dnSpy.Contracts.AsmEditor.Compiler {
	/// <summary>
	/// Project info passed to the compiler
	/// </summary>
	public readonly struct CompilerProjectInfo {
		/// <summary>
		/// Name of the edited assembly. It's only the simple name (eg. "MyAssembly"), and doesn't contain the public key token, version, etc
		/// </summary>
		public string AssemblyName { get; }

		/// <summary>
		/// The public key or null if none
		/// </summary>
		public byte[]? PublicKey { get; }

		/// <summary>
		/// Assembly and module references
		/// </summary>
		public CompilerMetadataReference[] AssemblyReferences { get; }

		/// <summary>
		/// Reference resolver
		/// </summary>
		public IAssemblyReferenceResolver AssemblyReferenceResolver { get; }

		/// <summary>
		/// Platform
		/// </summary>
		public TargetPlatform Platform { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="assemblyName">Name of the edited assembly. It's only the simple name (eg. "MyAssembly"), and doesn't contain the public key token, version, etc</param>
		/// <param name="publicKey">The public key or null if none</param>
		/// <param name="assemblyReferences">Assembly and module references</param>
		/// <param name="assemblyReferenceResolver">Reference resolver</param>
		/// <param name="platform">Platform</param>
		public CompilerProjectInfo(string assemblyName, byte[]? publicKey, CompilerMetadataReference[] assemblyReferences, IAssemblyReferenceResolver assemblyReferenceResolver, TargetPlatform platform) {
			AssemblyName = assemblyName ?? throw new ArgumentNullException(nameof(assemblyName));
			PublicKey = publicKey;
			AssemblyReferences = assemblyReferences ?? throw new ArgumentNullException(nameof(assemblyReferences));
			AssemblyReferenceResolver = assemblyReferenceResolver ?? throw new ArgumentNullException(nameof(assemblyReferenceResolver));
			Platform = platform;
		}
	}
}
