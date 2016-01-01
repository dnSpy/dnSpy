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

using dnlib.DotNet;
using dnlib.DotNet.Emit;
using dnSpy.Contracts.Files.TreeView.Resources;

namespace dnSpy.Contracts.Files.TreeView {
	/// <summary>
	/// Filters <see cref="IFileTreeNodeData"/> instances
	/// </summary>
	public interface IFileTreeNodeFilter {
		/// <summary>
		/// Returns a filter result. Called if it's a <see cref="IAssemblyFileNode"/>
		/// </summary>
		/// <param name="asm">Assembly</param>
		/// <returns></returns>
		FileTreeNodeFilterResult GetResult(AssemblyDef asm);

		/// <summary>
		/// Returns a filter result. Called if it's a <see cref="IModuleFileNode"/>
		/// </summary>
		/// <param name="mod">Module</param>
		/// <returns></returns>
		FileTreeNodeFilterResult GetResult(ModuleDef mod);

		/// <summary>
		/// Returns a filter result. Called if it's a <see cref="IDnSpyFileNode"/> but not a
		/// <see cref="IAssemblyFileNode"/> or a <see cref="IModuleFileNode"/>.
		/// </summary>
		/// <param name="file">File</param>
		/// <returns></returns>
		FileTreeNodeFilterResult GetResult(IDnSpyFile file);

		/// <summary>
		/// Returns a filter result. Called if it's a <see cref="INamespaceNode"/>
		/// </summary>
		/// <param name="ns">Namespace</param>
		/// <param name="owner">Owner file</param>
		/// <returns></returns>
		FileTreeNodeFilterResult GetResult(string ns, IDnSpyFile owner);

		/// <summary>
		/// Returns a filter result. Called if it's a <see cref="ITypeNode"/>
		/// </summary>
		/// <param name="type">Type</param>
		/// <returns></returns>
		FileTreeNodeFilterResult GetResult(TypeDef type);

		/// <summary>
		/// Returns a filter result. Called if it's a <see cref="IFieldNode"/>
		/// </summary>
		/// <param name="field">Field</param>
		/// <returns></returns>
		FileTreeNodeFilterResult GetResult(FieldDef field);

		/// <summary>
		/// Returns a filter result. Called if it's a <see cref="IMethodNode"/>
		/// </summary>
		/// <param name="method">Method</param>
		/// <returns></returns>
		FileTreeNodeFilterResult GetResult(MethodDef method);

		/// <summary>
		/// Returns a filter result. Called if it's a <see cref="IPropertyNode"/>
		/// </summary>
		/// <param name="prop">Property</param>
		/// <returns></returns>
		FileTreeNodeFilterResult GetResult(PropertyDef prop);

		/// <summary>
		/// Returns a filter result. Called if it's a <see cref="IEventNode"/>
		/// </summary>
		/// <param name="evt">Event</param>
		/// <returns></returns>
		FileTreeNodeFilterResult GetResult(EventDef evt);

		/// <summary>
		/// Returns a filter result for a method's body
		/// </summary>
		/// <param name="method">Method</param>
		/// <returns></returns>
		FileTreeNodeFilterResult GetResultBody(MethodDef method);

		/// <summary>
		/// Returns a filter result for a method's <see cref="ParamDef"/>s
		/// </summary>
		/// <param name="method">Method</param>
		/// <returns></returns>
		FileTreeNodeFilterResult GetResultParamDefs(MethodDef method);

		/// <summary>
		/// Returns a filter result for a method's <see cref="ParamDef"/>
		/// </summary>
		/// <param name="method">Method</param>
		/// <param name="param">Parameter</param>
		/// <returns></returns>
		FileTreeNodeFilterResult GetResult(MethodDef method, ParamDef param);

		/// <summary>
		/// Returns a filter result for a method's <see cref="Local"/>s
		/// </summary>
		/// <param name="method">Method</param>
		/// <returns></returns>
		FileTreeNodeFilterResult GetResultLocals(MethodDef method);

		/// <summary>
		/// Returns a filter result for a method's <see cref="Local"/>
		/// </summary>
		/// <param name="method">Method</param>
		/// <param name="local">Local</param>
		/// <returns></returns>
		FileTreeNodeFilterResult GetResult(MethodDef method, Local local);

		/// <summary>
		/// Returns a filter result. Called if it's a <see cref="IAssemblyReferenceNode"/>
		/// </summary>
		/// <param name="asmRef">Assembly reference</param>
		/// <returns></returns>
		FileTreeNodeFilterResult GetResult(AssemblyRef asmRef);

		/// <summary>
		/// Returns a filter result. Called if it's a <see cref="IModuleReferenceNode"/>
		/// </summary>
		/// <param name="modRef">Module reference</param>
		/// <returns></returns>
		FileTreeNodeFilterResult GetResult(ModuleRef modRef);

		/// <summary>
		/// Returns a filter result. The input can be null.
		/// </summary>
		/// <param name="node">Node, can be null</param>
		/// <returns></returns>
		FileTreeNodeFilterResult GetResult(IBaseTypeNode node);

		/// <summary>
		/// Returns a filter result. The input can be null.
		/// </summary>
		/// <param name="node">Node, can be null</param>
		/// <returns></returns>
		FileTreeNodeFilterResult GetResult(IBaseTypeFolderNode node);

		/// <summary>
		/// Returns a filter result. The input can be null.
		/// </summary>
		/// <param name="node">Node, can be null</param>
		/// <returns></returns>
		FileTreeNodeFilterResult GetResult(IDerivedTypeNode node);

		/// <summary>
		/// Returns a filter result. The input can be null.
		/// </summary>
		/// <param name="node">Node, can be null</param>
		/// <returns></returns>
		FileTreeNodeFilterResult GetResult(IDerivedTypesFolderNode node);

		/// <summary>
		/// Returns a filter result. The input can be null.
		/// </summary>
		/// <param name="node">Node, can be null</param>
		/// <returns></returns>
		FileTreeNodeFilterResult GetResult(IReferencesFolderNode node);

		/// <summary>
		/// Returns a filter result. The input can be null.
		/// </summary>
		/// <param name="node">Node, can be null</param>
		/// <returns></returns>
		FileTreeNodeFilterResult GetResult(IResourcesFolderNode node);

		/// <summary>
		/// Returns a filter result. The input can be null.
		/// </summary>
		/// <param name="node">Node, can be null</param>
		/// <returns></returns>
		FileTreeNodeFilterResult GetResult(IResourceNode node);

		/// <summary>
		/// Returns a filter result. The input can be null.
		/// </summary>
		/// <param name="node">Node, can be null</param>
		/// <returns></returns>
		FileTreeNodeFilterResult GetResult(IResourceElementNode node);

		/// <summary>
		/// Returns a filter result if it's any other <see cref="IFileTreeNodeData"/> instance
		/// </summary>
		/// <param name="node">Node, can't be null</param>
		/// <returns></returns>
		FileTreeNodeFilterResult GetResult(IFileTreeNodeData node);
	}
}
