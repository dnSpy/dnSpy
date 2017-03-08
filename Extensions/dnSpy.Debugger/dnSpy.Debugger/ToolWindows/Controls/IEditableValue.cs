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
using System.ComponentModel;

namespace dnSpy.Debugger.ToolWindows.Controls {
	interface IEditableValue : INotifyPropertyChanged {
		/// <summary>
		/// true if the value can be edited, false if it's read-only
		/// </summary>
		bool CanEdit { get; }

		/// <summary>
		/// When true is written to this property, the edit textbox is made visible and the
		/// user can edit the value. The control will write false to it when the edit operation
		/// is completed (eg. the user hit enter or escape.)
		/// </summary>
		bool IsEditingValue { get; set; }

		/// <summary>
		/// Gets the text shown in the textbox. The control will write the new value when
		/// the user hits enter.
		/// </summary>
		string Text { get; set; }
	}

	abstract class EditableValue : IEditableValue {
		public event PropertyChangedEventHandler PropertyChanged;
		public virtual bool CanEdit => true;
		public abstract string Text { get; set; }

		public bool IsEditingValue {
			get => isEditingValue;
			set {
				if (isEditingValue == value)
					return;
				isEditingValue = value;
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsEditingValue)));
			}
		}
		bool isEditingValue;
	}

	sealed class EditableValueImpl : EditableValue {
		readonly Func<string> getText;
		readonly Action<string> setText;
		readonly Func<bool> canEdit;
		static readonly Func<bool> defaultCanEdit = () => true;

		public override bool CanEdit => canEdit();

		public override string Text {
			get => getText();
			set => setText(value);
		}

		public EditableValueImpl(Func<string> getText, Action<string> setText, Func<bool> canEdit = null) {
			this.getText = getText ?? throw new ArgumentNullException(nameof(getText));
			this.setText = setText ?? throw new ArgumentNullException(nameof(setText));
			this.canEdit = canEdit ?? defaultCanEdit;
		}
	}
}
