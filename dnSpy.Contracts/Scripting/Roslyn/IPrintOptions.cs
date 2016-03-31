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

namespace dnSpy.Contracts.Scripting.Roslyn {
	/// <summary>
	/// Print options (<see cref="T:Microsoft.CodeAnalysis.Scripting.Hosting.PrintOptions"/>)
	/// </summary>
	public interface IPrintOptions {// So we don't have to add Roslyn refs to this assembly
		/// <summary>
		/// Ellipsis string
		/// </summary>
		string Ellipsis { get; set; }

		/// <summary>
		/// Escape non-printable characters
		/// </summary>
		bool EscapeNonPrintableCharacters { get; set; }

		/// <summary>
		/// Maximum output length
		/// </summary>
		int MaximumOutputLength { get; set; }

		/// <summary>
		/// Member display format
		/// </summary>
		MemberDisplayFormat MemberDisplayFormat { get; set; }

		/// <summary>
		/// Number radix
		/// </summary>
		int NumberRadix { get; set; }
	}
}
