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

namespace dnSpy.Contracts.Decompiler {
	/// <summary>
	/// <see cref="MethodDebugScope"/> builder
	/// </summary>
	public sealed class MethodDebugScopeBuilder {
		/// <summary>
		/// Gets the span of this scope
		/// </summary>
		public ILSpan Span { get; set; }

		/// <summary>
		/// Gets all child scopes
		/// </summary>
		public List<MethodDebugScopeBuilder> Scopes {
			get {
				if (scopes is null)
					scopes = new List<MethodDebugScopeBuilder>();
				return scopes;
			}
		}
		List<MethodDebugScopeBuilder>? scopes;

		/// <summary>
		/// Gets all new locals in the scope
		/// </summary>
		public List<SourceLocal> Locals {
			get {
				if (locals is null)
					locals = new List<SourceLocal>();
				return locals;
			}
		}
		List<SourceLocal>? locals;

		/// <summary>
		/// Gets all new imports in the scope
		/// </summary>
		public List<ImportInfo> Imports {
			get {
				if (imports is null)
					imports = new List<ImportInfo>();
				return imports;
			}
		}
		List<ImportInfo>? imports;

		/// <summary>
		/// Gets all new constants in the scope
		/// </summary>
		public List<MethodDebugConstant> Constants {
			get {
				if (constants is null)
					constants = new List<MethodDebugConstant>();
				return constants;
			}
		}
		List<MethodDebugConstant>? constants;

		/// <summary>
		/// Constructor
		/// </summary>
		public MethodDebugScopeBuilder() {
		}

		/// <summary>
		/// Creates a new <see cref="MethodDebugScope"/> instance
		/// </summary>
		/// <returns></returns>
		public MethodDebugScope ToScope() =>
			new MethodDebugScope(
				Span,
				scopes is null ? Array.Empty<MethodDebugScope>() : ToScopes(scopes),
				locals is null || locals.Count == 0 ? Array.Empty<SourceLocal>() : locals.ToArray(),
				imports is null || imports.Count == 0 ? Array.Empty<ImportInfo>() : imports.ToArray(),
				constants is null || constants.Count == 0 ? Array.Empty<MethodDebugConstant>() : constants.ToArray());

		static MethodDebugScope[] ToScopes(List<MethodDebugScopeBuilder> scopes) {
			var res = new MethodDebugScope[scopes.Count];
			for (int i = 0; i < res.Length; i++)
				res[i] = scopes[i].ToScope();
			return res;
		}
	}
}
