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
using System.Collections.Generic;
using dnlib.DotNet;

namespace dnSpy.Decompiler.Shared {
	/// <summary>
	/// Builds <see cref="MethodDebugInfo"/> instances
	/// </summary>
	public sealed class MethodDebugInfoBuilder {
		readonly MethodDef method;
		readonly List<SourceStatement> statements;
		readonly SourceLocal[] locals;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="method">Method</param>
		/// <param name="locals">Locals or null</param>
		public MethodDebugInfoBuilder(MethodDef method, SourceLocal[] locals = null) {
			if (method == null)
				throw new ArgumentNullException(nameof(method));
			this.method = method;
			this.statements = new List<SourceStatement>();
			this.locals = locals ?? Array.Empty<SourceLocal>();
		}

		/// <summary>
		/// Adds a <see cref="SourceStatement"/>
		/// </summary>
		/// <param name="statement">Statement</param>
		public void Add(SourceStatement statement) => statements.Add(statement);

		/// <summary>
		/// Creates a <see cref="MethodDebugInfo"/>
		/// </summary>
		/// <returns></returns>
		public MethodDebugInfo Create() => new MethodDebugInfo(method, statements.ToArray(), locals);
	}
}
