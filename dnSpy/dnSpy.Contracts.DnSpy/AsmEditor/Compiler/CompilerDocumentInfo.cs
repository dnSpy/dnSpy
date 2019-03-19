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
	/// Document info
	/// </summary>
	public readonly struct CompilerDocumentInfo {
		/// <summary>
		/// All code
		/// </summary>
		public string Code { get; }

		/// <summary>
		/// Name of document
		/// </summary>
		public string Name { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="code">All code</param>
		/// <param name="name">Name of document</param>
		public CompilerDocumentInfo(string code, string name) {
			Code = code ?? throw new ArgumentNullException(nameof(name));
			Name = name ?? throw new ArgumentNullException(nameof(name));
		}
	}
}
