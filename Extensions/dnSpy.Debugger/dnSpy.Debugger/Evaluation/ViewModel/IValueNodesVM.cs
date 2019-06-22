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
using System.ComponentModel;
using dnSpy.Contracts.TreeView;

namespace dnSpy.Debugger.Evaluation.ViewModel {
	interface IValueNodesVM : INotifyPropertyChanged, IDisposable {
		void Show();
		void Hide();
		bool IsOpen { get; }
		bool IsReadOnly { get; }
		ITreeView TreeView { get; }
		Guid? RuntimeKindGuid { get; }
		VariablesWindowKind VariablesWindowKind { get; }

		bool CanAddRemoveExpressions { get; }
		void DeleteExpressions(string[] ids);
		void ClearAllExpressions();
		void EditExpression(string? id, string expression);
		void AddExpressions(string[] expressions, bool select = false);
		void Refresh();
	}

	static class ValueNodesVMConstants {
		public static readonly Guid GUIDOBJ_VALUENODESVM_GUID = new Guid("A148423F-B2C3-492D-9710-8573E559E957");
	}
}
