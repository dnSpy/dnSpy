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
using dnSpy.Contracts.Debugger.Evaluation;
using dnSpy.Contracts.Decompiler;

namespace dnSpy.Contracts.Debugger.DotNet.Code {
	/// <summary>
	/// Returns the decompiler that should be used by code that needs to use a decompiler to format methods.
	/// The decompiler gets updated when the <see cref="DbgLanguage"/> gets changed.
	/// </summary>
	public abstract class DbgDotNetDecompilerService {
		/// <summary>
		/// Gets the decompiler
		/// </summary>
		public abstract IDecompiler Decompiler { get; }

		/// <summary>
		/// Raised after <see cref="Decompiler"/> is changed
		/// </summary>
		public abstract event EventHandler<EventArgs>? DecompilerChanged;
	}
}
