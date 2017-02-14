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

using System.Collections.Generic;

namespace dnSpy.Contracts.Text.Editor {
	/// <summary>
	/// <see cref="ILogEditor"/> options
	/// </summary>
	public sealed class LogEditorOptions : CommonTextEditorOptions {
		/// <summary>
		/// Extra text view roles
		/// </summary>
		public List<string> ExtraRoles { get; } = new List<string>();

		/// <summary>
		/// Constructor
		/// </summary>
		public LogEditorOptions() => EnableUndoHistory = false;

		/// <summary>
		/// Clones this
		/// </summary>
		/// <returns></returns>
		public new LogEditorOptions Clone() => CopyTo(new LogEditorOptions());

		LogEditorOptions CopyTo(LogEditorOptions other) {
			base.CopyTo(other);
			other.ExtraRoles.Clear();
			other.ExtraRoles.AddRange(ExtraRoles);
			return other;
		}
	}
}
