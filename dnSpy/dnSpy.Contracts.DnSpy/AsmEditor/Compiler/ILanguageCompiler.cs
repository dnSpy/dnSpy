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
using System.Threading;
using System.Threading.Tasks;
using dnlib.DotNet;

namespace dnSpy.Contracts.AsmEditor.Compiler {
	/// <summary>
	/// Compiles source code
	/// </summary>
	public interface ILanguageCompiler : IDisposable {
		/// <summary>
		/// Gets the file extension, including the period, eg. ".cs"
		/// </summary>
		string FileExtension { get; }

		/// <summary>
		/// Assembly references that must be included when compiling the code, even if the
		/// references aren't part of the edited assembly. This is usually empty unless the
		/// language uses types from certain language specific assemblies, eg. Visual Basic
		/// usually needs <c>Microsoft.VisualBasic</c>.
		/// </summary>
		/// <param name="editedModule">The module the user is editing</param>
		/// <returns></returns>
		IEnumerable<string> GetRequiredAssemblyReferences(ModuleDef editedModule);

		/// <summary>
		/// Initializes the project
		/// </summary>
		/// <param name="projectInfo">Project info</param>
		void InitializeProject(CompilerProjectInfo projectInfo);

		/// <summary>
		/// Compiles the code
		/// </summary>
		/// <param name="cancellationToken">Cancellation token</param>
		/// <returns></returns>
		Task<CompilationResult> CompileAsync(CancellationToken cancellationToken);

		/// <summary>
		/// Adds more documents
		/// </summary>
		/// <param name="documents">Documents to add to the compilation</param>
		/// <returns></returns>
		ICodeDocument[] AddDocuments(CompilerDocumentInfo[] documents);

		/// <summary>
		/// Adds new metadata references
		/// </summary>
		/// <param name="metadataReferences">Metadata references</param>
		/// <returns></returns>
		bool AddMetadataReferences(CompilerMetadataReference[] metadataReferences);
	}
}
