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
using dnlib.DotNet;

namespace dnSpy.Contracts.Decompiler {
	/// <summary>
	/// Method scope
	/// </summary>
	public sealed class MethodDebugScope {
		/// <summary>
		/// Gets the span of this scope
		/// </summary>
		public ILSpan Span { get; }

		/// <summary>
		/// Gets all child scopes
		/// </summary>
		public MethodDebugScope[] Scopes { get; }

		/// <summary>
		/// Gets all new locals in the scope
		/// </summary>
		public SourceLocal[] Locals { get; }

		/// <summary>
		/// Gets all new imports in the scope
		/// </summary>
		public ImportInfo[] Imports { get; }

		/// <summary>
		/// Gets all new constants in the scope
		/// </summary>
		public MethodDebugConstant[] Constants { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="span">Scope span</param>
		/// <param name="scopes">Child scopes</param>
		/// <param name="locals">Locals</param>
		/// <param name="imports">Imports</param>
		/// <param name="constants">Constants</param>
		public MethodDebugScope(ILSpan span, MethodDebugScope[] scopes, SourceLocal[] locals, ImportInfo[] imports, MethodDebugConstant[] constants) {
			Span = span;
			Scopes = scopes ?? throw new ArgumentNullException(nameof(scopes));
			Locals = locals ?? throw new ArgumentNullException(nameof(locals));
			Imports = imports ?? throw new ArgumentNullException(nameof(imports));
			Constants = constants ?? throw new ArgumentNullException(nameof(constants));
		}
	}

	/// <summary>
	/// Import kind
	/// </summary>
	public enum ImportInfoKind {
		/// <summary>
		/// Namespace import
		/// </summary>
		Namespace,

		/// <summary>
		/// Type import
		/// </summary>
		Type,

		/// <summary>
		/// Namespace or type import
		/// </summary>
		NamespaceOrType,

		/// <summary>
		/// C#: extern alias
		/// </summary>
		Assembly,

		/// <summary>
		/// VB: XML import
		/// </summary>
		XmlNamespace,

		/// <summary>
		/// VB: token of method with imports
		/// </summary>
		MethodToken,

		/// <summary>
		/// VB: containing namespace
		/// </summary>
		CurrentNamespace,

		/// <summary>
		/// VB: root namespace
		/// </summary>
		DefaultNamespace,
	}

	/// <summary>
	/// Visual Basic import scope kind
	/// </summary>
	public enum VBImportScopeKind {
		/// <summary>
		/// Unspecified scope
		/// </summary>
		None,

		/// <summary>
		/// File scope
		/// </summary>
		File,

		/// <summary>
		/// Project scope
		/// </summary>
		Project,
	}

	/// <summary>
	/// Import info
	/// </summary>
	public readonly struct ImportInfo {
		/// <summary>
		/// Target kind
		/// </summary>
		public ImportInfoKind TargetKind { get; }

		/// <summary>
		/// Gets the VB import scope kind
		/// </summary>
		public VBImportScopeKind VBImportScopeKind { get; }

		/// <summary>
		/// Target
		/// </summary>
		public string? Target { get; }

		/// <summary>
		/// Alias
		/// </summary>
		public string? Alias { get; }

		/// <summary>
		/// Extern alias
		/// </summary>
		public string? ExternAlias { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="targetKind">Target kind</param>
		/// <param name="target">Target string</param>
		/// <param name="alias">Alias</param>
		/// <param name="externAlias">Extern alias</param>
		/// <param name="importScopeKind">VB import scope kind</param>
		public ImportInfo(ImportInfoKind targetKind, string? target = null, string? alias = null, string? externAlias = null, VBImportScopeKind importScopeKind = VBImportScopeKind.None) {
			TargetKind = targetKind;
			Target = target;
			Alias = alias;
			ExternAlias = externAlias;
			VBImportScopeKind = importScopeKind;
		}

#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
		public static ImportInfo CreateNamespace(string @namespace) => new ImportInfo(ImportInfoKind.Namespace, target: @namespace);
		public static ImportInfo CreateNamespace(string @namespace, string externAlias) => new ImportInfo(ImportInfoKind.Namespace, target: @namespace, externAlias: externAlias);
		public static ImportInfo CreateType(string type) => new ImportInfo(ImportInfoKind.Type, target: type);
		public static ImportInfo CreateNamespaceAlias(string @namespace, string alias) => new ImportInfo(ImportInfoKind.Namespace, target: @namespace, alias: alias);
		public static ImportInfo CreateTypeAlias(string type, string alias) => new ImportInfo(ImportInfoKind.Type, target: type, alias: alias);
		public static ImportInfo CreateNamespaceAlias(string @namespace, string alias, string externAlias) => new ImportInfo(ImportInfoKind.Namespace, target: @namespace, alias: alias, externAlias: externAlias);
		public static ImportInfo CreateAssembly(string externAlias) => new ImportInfo(ImportInfoKind.Assembly, externAlias: externAlias);
		public static ImportInfo CreateAssembly(string externAlias, string assembly) => new ImportInfo(ImportInfoKind.Assembly, externAlias: externAlias, target: assembly);
		public static ImportInfo CreateCurrentNamespace() => new ImportInfo(ImportInfoKind.CurrentNamespace, target: string.Empty);
		public static ImportInfo CreateNamespaceOrType(string namespaceOrType, string alias, VBImportScopeKind importScopeKind) => new ImportInfo(ImportInfoKind.NamespaceOrType, target: namespaceOrType, alias: alias, importScopeKind: importScopeKind);
		public static ImportInfo CreateXmlNamespace(string xmlNamespace, string alias, VBImportScopeKind importScopeKind) => new ImportInfo(ImportInfoKind.XmlNamespace, target: xmlNamespace, alias: alias, importScopeKind: importScopeKind);
		public static ImportInfo CreateType(string type, VBImportScopeKind importScopeKind) => new ImportInfo(ImportInfoKind.Type, target: type, importScopeKind: importScopeKind);
		public static ImportInfo CreateNamespace(string @namespace, VBImportScopeKind importScopeKind) => new ImportInfo(ImportInfoKind.Namespace, target: @namespace, importScopeKind: importScopeKind);
		public static ImportInfo CreateMethodToken(string token, VBImportScopeKind importScopeKind) => new ImportInfo(ImportInfoKind.MethodToken, target: token, importScopeKind: importScopeKind);
		public static ImportInfo CreateDefaultNamespace(string @namespace) => new ImportInfo(ImportInfoKind.DefaultNamespace, target: @namespace);
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
	}

	/// <summary>
	/// A constant value
	/// </summary>
	public readonly struct MethodDebugConstant {
		/// <summary>
		/// Gets the name of the constant
		/// </summary>
		public string Name { get; }

		/// <summary>
		/// Gets the type of the constant
		/// </summary>
		public TypeSig Type { get; }

		/// <summary>
		/// Gets the constant value
		/// </summary>
		public object Value { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="name">Name of constant</param>
		/// <param name="type">Type of constant</param>
		/// <param name="value">Constant value</param>
		public MethodDebugConstant(string name, TypeSig type, object value) {
			Name = name ?? throw new ArgumentNullException(nameof(name));
			Type = type ?? throw new ArgumentNullException(nameof(type));
			Value = value;
		}
	}
}
