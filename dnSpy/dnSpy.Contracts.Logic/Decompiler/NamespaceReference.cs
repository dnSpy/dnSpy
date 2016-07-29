/*
    Copyright (C) 2014-2016 de4dot@gmail.com

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
using dnlib.DotNet;

namespace dnSpy.Contracts.Decompiler {
	/// <summary>
	/// Namespace reference
	/// </summary>
	public sealed class NamespaceReference {
		/// <summary>
		/// Gets the namespace or null
		/// </summary>
		public string Namespace { get; }

		/// <summary>
		/// Gets the assembly
		/// </summary>
		public AssemblyRef Assembly { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="assembly">Target assembly</param>
		/// <param name="namespace">Namespace</param>
		public NamespaceReference(IAssembly assembly, string @namespace) {
			if (assembly == null)
				throw new ArgumentNullException(nameof(assembly));
			Assembly = assembly.ToAssemblyRef();
			Namespace = @namespace;
		}

		/// <summary>
		/// Equals()
		/// </summary>
		/// <param name="obj"></param>
		/// <returns></returns>
		public override bool Equals(object obj) => obj is NamespaceReference && StringComparer.Ordinal.Equals(((NamespaceReference)obj).Namespace, Namespace);

		/// <summary>
		/// GetHashCode()
		/// </summary>
		/// <returns></returns>
		public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Namespace);
	}
}
