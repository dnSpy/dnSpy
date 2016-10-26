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
using dnSpy.Contracts.Documents.TreeView.Resources;

namespace dnSpy.Contracts.Documents.TreeView {
	/// <summary>
	/// Filters <see cref="DocumentTreeNodeData"/> instances
	/// </summary>
	public interface IDocumentTreeNodeFilter {
		/// <summary>
		/// Returns a filter result. Called if it's a <see cref="AssemblyDocumentNode"/>
		/// </summary>
		/// <param name="asm">Assembly</param>
		/// <returns></returns>
		DocumentTreeNodeFilterResult GetResult(AssemblyDef asm);

		/// <summary>
		/// Returns a filter result. Called if it's a <see cref="ModuleDocumentNode"/>
		/// </summary>
		/// <param name="mod">Module</param>
		/// <returns></returns>
		DocumentTreeNodeFilterResult GetResult(ModuleDef mod);

		/// <summary>
		/// Returns a filter result. Called if it's a <see cref="DsDocumentNode"/> but not a
		/// <see cref="AssemblyDocumentNode"/> or a <see cref="ModuleDocumentNode"/>.
		/// </summary>
		/// <param name="document">Document</param>
		/// <returns></returns>
		DocumentTreeNodeFilterResult GetResult(IDsDocument document);

		/// <summary>
		/// Returns a filter result. Called if it's a <see cref="NamespaceNode"/>
		/// </summary>
		/// <param name="ns">Namespace</param>
		/// <param name="owner">Owner document</param>
		/// <returns></returns>
		DocumentTreeNodeFilterResult GetResult(string ns, IDsDocument owner);

		/// <summary>
		/// Returns a filter result. Called if it's a <see cref="TypeNode"/>
		/// </summary>
		/// <param name="type">Type</param>
		/// <returns></returns>
		DocumentTreeNodeFilterResult GetResult(TypeDef type);

		/// <summary>
		/// Returns a filter result. Called if it's a <see cref="FieldNode"/>
		/// </summary>
		/// <param name="field">Field</param>
		/// <returns></returns>
		DocumentTreeNodeFilterResult GetResult(FieldDef field);

		/// <summary>
		/// Returns a filter result. Called if it's a <see cref="MethodNode"/>
		/// </summary>
		/// <param name="method">Method</param>
		/// <returns></returns>
		DocumentTreeNodeFilterResult GetResult(MethodDef method);

		/// <summary>
		/// Returns a filter result. Called if it's a <see cref="PropertyNode"/>
		/// </summary>
		/// <param name="prop">Property</param>
		/// <returns></returns>
		DocumentTreeNodeFilterResult GetResult(PropertyDef prop);

		/// <summary>
		/// Returns a filter result. Called if it's a <see cref="EventNode"/>
		/// </summary>
		/// <param name="evt">Event</param>
		/// <returns></returns>
		DocumentTreeNodeFilterResult GetResult(EventDef evt);

		/// <summary>
		/// Returns a filter result for a method's body
		/// </summary>
		/// <param name="method">Method</param>
		/// <returns></returns>
		DocumentTreeNodeFilterResult GetResultBody(MethodDef method);

		/// <summary>
		/// Returns a filter result for a method's <see cref="ParamDef"/>s
		/// </summary>
		/// <param name="method">Method</param>
		/// <returns></returns>
		DocumentTreeNodeFilterResult GetResultParamDefs(MethodDef method);

		/// <summary>
		/// Returns a filter result for a method's <see cref="ParamDef"/>
		/// </summary>
		/// <param name="method">Method</param>
		/// <param name="param">Parameter</param>
		/// <returns></returns>
		DocumentTreeNodeFilterResult GetResult(MethodDef method, ParamDef param);

		/// <summary>
		/// Returns a filter result for a method's <see cref="Local"/>s
		/// </summary>
		/// <param name="method">Method</param>
		/// <returns></returns>
		DocumentTreeNodeFilterResult GetResultLocals(MethodDef method);

		/// <summary>
		/// Returns a filter result for a method's <see cref="Local"/>
		/// </summary>
		/// <param name="method">Method</param>
		/// <param name="local">Local</param>
		/// <returns></returns>
		DocumentTreeNodeFilterResult GetResult(MethodDef method, Local local);

		/// <summary>
		/// Returns a filter result. Called if it's a <see cref="AssemblyReferenceNode"/>
		/// </summary>
		/// <param name="asmRef">Assembly reference</param>
		/// <returns></returns>
		DocumentTreeNodeFilterResult GetResult(AssemblyRef asmRef);

		/// <summary>
		/// Returns a filter result. Called if it's a <see cref="ModuleReferenceNode"/>
		/// </summary>
		/// <param name="modRef">Module reference</param>
		/// <returns></returns>
		DocumentTreeNodeFilterResult GetResult(ModuleRef modRef);

		/// <summary>
		/// Returns a filter result. The input can be null.
		/// </summary>
		/// <param name="node">Node, can be null</param>
		/// <returns></returns>
		DocumentTreeNodeFilterResult GetResult(BaseTypeNode node);

		/// <summary>
		/// Returns a filter result. The input can be null.
		/// </summary>
		/// <param name="node">Node, can be null</param>
		/// <returns></returns>
		DocumentTreeNodeFilterResult GetResult(BaseTypeFolderNode node);

		/// <summary>
		/// Returns a filter result. The input can be null.
		/// </summary>
		/// <param name="node">Node, can be null</param>
		/// <returns></returns>
		DocumentTreeNodeFilterResult GetResult(DerivedTypeNode node);

		/// <summary>
		/// Returns a filter result. The input can be null.
		/// </summary>
		/// <param name="node">Node, can be null</param>
		/// <returns></returns>
		DocumentTreeNodeFilterResult GetResult(DerivedTypesFolderNode node);

		/// <summary>
		/// Returns a filter result. The input can be null.
		/// </summary>
		/// <param name="node">Node, can be null</param>
		/// <returns></returns>
		DocumentTreeNodeFilterResult GetResult(ReferencesFolderNode node);

		/// <summary>
		/// Returns a filter result. The input can be null.
		/// </summary>
		/// <param name="node">Node, can be null</param>
		/// <returns></returns>
		DocumentTreeNodeFilterResult GetResult(ResourcesFolderNode node);

		/// <summary>
		/// Returns a filter result. The input can be null.
		/// </summary>
		/// <param name="node">Node, can be null</param>
		/// <returns></returns>
		DocumentTreeNodeFilterResult GetResult(ResourceNode node);

		/// <summary>
		/// Returns a filter result. The input can be null.
		/// </summary>
		/// <param name="node">Node, can be null</param>
		/// <returns></returns>
		DocumentTreeNodeFilterResult GetResult(ResourceElementNode node);

		/// <summary>
		/// Returns a filter result if it's any other <see cref="DocumentTreeNodeData"/> instance
		/// </summary>
		/// <param name="node">Node, can't be null</param>
		/// <returns></returns>
		DocumentTreeNodeFilterResult GetResultOther(DocumentTreeNodeData node);

		/// <summary>
		/// Returns a filter result
		/// </summary>
		/// <param name="hca">Object with custom attributes</param>
		/// <returns></returns>
		DocumentTreeNodeFilterResult GetResultAttributes(IHasCustomAttribute hca);
	}
}
