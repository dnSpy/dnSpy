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

namespace dnSpy.Contracts.AsmEditor.Compile {
	/// <summary>
	/// Decompiled code result, passed to <see cref="ILanguageCompiler.AddDecompiledCode(IDecompiledCodeResult)"/>
	/// </summary>
	public interface IDecompiledCodeResult {
		/// <summary>
		/// Main code
		/// </summary>
		string MainCode { get; }

		/// <summary>
		/// Other code that's not important to the user, eg. method stubs
		/// </summary>
		string HiddenCode { get; }

		/// <summary>
		/// Assembly and module references
		/// </summary>
		CompilerMetadataReference[] AssemblyReferences { get; }

		/// <summary>
		/// Reference resolver
		/// </summary>
		IAssemblyReferenceResolver AssemblyReferenceResolver { get; }

		/// <summary>
		/// Platform
		/// </summary>
		CompilePlatform Platform { get; }
	}
}
