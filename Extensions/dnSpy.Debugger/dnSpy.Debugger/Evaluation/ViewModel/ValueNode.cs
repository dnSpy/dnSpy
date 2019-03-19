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
using System.Windows.Input;
using dnSpy.Contracts.Controls.ToolWindows;
using dnSpy.Contracts.TreeView;
using dnSpy.Debugger.Text;
using dnSpy.Debugger.UI;

namespace dnSpy.Debugger.Evaluation.ViewModel {
	abstract class ValueNode : TreeNodeData {
		public override Guid Guid => Guid.Empty;
		public override object Text => null;
		public override object ToolTip => null;

		public abstract string RootId { get; }

		// Used by XAML
		public abstract bool IsInvalid { get; protected set; }
		public abstract ICommand RefreshExpressionCommand { get; }
		public abstract string RefreshExpressionToolTip { get; }
		public abstract FormatterObject<ValueNode> NameObject { get; }
		public abstract FormatterObject<ValueNode> ValueObject { get; }
		public abstract FormatterObject<ValueNode> TypeObject { get; }
		public abstract IEditableValue NameEditableValue { get; }
		public abstract IEditValueProvider NameEditValueProvider { get; }
		public abstract IEditableValue ValueEditableValue { get; }
		public abstract IEditValueProvider ValueEditValueProvider { get; }

		// Used by formatter
		public abstract ref readonly ClassifiedTextCollection CachedName { get; }
		public abstract ref readonly ClassifiedTextCollection CachedValue { get; }
		public abstract ref readonly ClassifiedTextCollection CachedExpectedType { get; }
		public abstract ref readonly ClassifiedTextCollection CachedActualType_OrDefaultInstance { get; }
		public abstract ref readonly ClassifiedTextCollection OldCachedValue { get; }
	}
}
