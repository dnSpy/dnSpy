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

namespace dnSpy.Debugger.Evaluation.ViewModel {
	abstract class ValueNodesVMFactory {
		public abstract IValueNodesVM Create(ValueNodesVMOptions options);
	}

	enum ShowMessageBoxButtons {
		YesNo,
		OK,
	}
	delegate bool ShowMessageBox(string message, ShowMessageBoxButtons buttons);

	enum VariablesWindowKind {
		None,
		Locals,
		Autos,
		Watch,
	}

	sealed class ValueNodesVMOptions {
#pragma warning disable CS8618 // Non-nullable field is uninitialized.
		public ValueNodesProvider NodesProvider { get; set; }

		/// <summary>
		/// Eg. <see cref="Contracts.Text.ContentTypes.LocalsWindow"/>
		/// </summary>
		public string WindowContentType { get; set; }

		/// <summary>
		/// Eg. <see cref="Contracts.Text.Classification.PredefinedTextClassifierTags.LocalsWindowName"/>
		/// </summary>
		public string NameColumnName { get; set; }

		/// <summary>
		/// Eg. <see cref="Contracts.Text.Classification.PredefinedTextClassifierTags.LocalsWindowValue"/>
		/// </summary>
		public string ValueColumnName { get; set; }

		/// <summary>
		/// Eg. <see cref="Contracts.Text.Classification.PredefinedTextClassifierTags.LocalsWindowType"/>
		/// </summary>
		public string TypeColumnName { get; set; }

		public VariablesWindowKind VariablesWindowKind { get; set; }
		public Guid VariablesWindowGuid { get; set; }

		public ShowMessageBox ShowMessageBox { get; set; }

		public Guid TreeViewGuid { get; set; }
#pragma warning restore CS8618 // Non-nullable field is uninitialized.
	}
}
