/*
    Copyright (C) 2014-2017 de4dot@gmail.com

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

namespace dnSpy.Contracts.Decompiler {
	/// <summary>
	/// Builds <see cref="MethodDebugInfo"/> instances
	/// </summary>
	public sealed class MethodDebugInfoBuilder {
		readonly MethodDef method;
		readonly List<SourceStatement> statements;

		/// <summary>
		/// Gets the scope builder
		/// </summary>
		public MethodDebugScopeBuilder Scope { get; }

		/// <summary>
		/// Start of method (eg. position of the first character of the modifier or return type)
		/// </summary>
		public int? StartPosition { get; set; }

		/// <summary>
		/// End of method (eg. after the last brace)
		/// </summary>
		public int? EndPosition { get; set; }

		readonly int decompilerOptionsVersion;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="decompilerOptionsVersion">Decompiler options version number. This version number should get incremented when the options change.</param>
		/// <param name="method">Method</param>
		public MethodDebugInfoBuilder(int decompilerOptionsVersion, MethodDef method) {
			this.decompilerOptionsVersion = decompilerOptionsVersion;
			this.method = method ?? throw new ArgumentNullException(nameof(method));
			statements = new List<SourceStatement>();
			Scope = new MethodDebugScopeBuilder();
			Scope.Span = BinSpan.FromBounds(0, (uint)method.Body.GetCodeSize());
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="decompilerOptionsVersion">Decompiler options version number. This version number should get incremented when the options change.</param>
		/// <param name="method">Method</param>
		/// <param name="locals">Locals</param>
		public MethodDebugInfoBuilder(int decompilerOptionsVersion, MethodDef method, SourceLocal[] locals)
			: this(decompilerOptionsVersion, method) => Scope.Locals.AddRange(locals);

		/// <summary>
		/// Adds a <see cref="SourceStatement"/>
		/// </summary>
		/// <param name="statement">Statement</param>
		public void Add(SourceStatement statement) => statements.Add(statement);

		/// <summary>
		/// Creates a <see cref="MethodDebugInfo"/>
		/// </summary>
		/// <returns></returns>
		public MethodDebugInfo Create() {
			TextSpan? methodSpan;
			if (StartPosition != null && EndPosition != null && StartPosition.Value <= EndPosition.Value)
				methodSpan = TextSpan.FromBounds(StartPosition.Value, EndPosition.Value);
			else
				methodSpan = null;
			return new MethodDebugInfo(decompilerOptionsVersion, method, statements.ToArray(), Scope.ToScope(), methodSpan);
		}
	}
}
