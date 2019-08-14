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

namespace dnSpy.Contracts.Controls.ToolWindows {
	/// <summary>
	/// <see cref="IEditableValue"/> options
	/// </summary>
	[Flags]
	public enum EditableValueOptions {
		/// <summary>
		/// No bit is set
		/// </summary>
		None					= 0,

		/// <summary>
		/// Single click to edit text
		/// </summary>
		SingleClick				= 0x00000001,
	}

	/// <summary>
	/// Implemented by data that can be edited
	/// </summary>
	public interface IEditableValue : INotifyPropertyChanged {
		/// <summary>
		/// Gets the options
		/// </summary>
		EditableValueOptions Options { get; }

		/// <summary>
		/// true if the value can be edited, false if it's read-only
		/// </summary>
		bool CanEdit { get; }

		/// <summary>
		/// When true is written to this property, the edit textbox is made visible and the
		/// user can edit the value. The control will write false to it when the edit operation
		/// is completed (eg. the user hit enter or escape.)
		/// The control also writes true to this property if the user double clicks it.
		/// </summary>
		bool IsEditingValue { get; set; }

		/// <summary>
		/// Returns the text shown in the control
		/// </summary>
		/// <returns></returns>
		EditableValueTextInfo GetText();

		/// <summary>
		/// The control calls this method to write the new value
		/// </summary>
		/// <param name="text">New text</param>
		void SetText(string text);
	}

	/// <summary>
	/// Edit value flags
	/// </summary>
	[Flags]
	public enum EditValueFlags {
		/// <summary>
		/// No bit is set
		/// </summary>
		None				= 0,

		/// <summary>
		/// Select the text
		/// </summary>
		SelectText			= 0x00000001,
	}

	/// <summary>
	/// Contains the text to edit
	/// </summary>
	public readonly struct EditableValueTextInfo {
		/// <summary>
		/// Gets the text
		/// </summary>
		public string Text { get; }

		/// <summary>
		/// Flags
		/// </summary>
		public EditValueFlags Flags { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="text">Text to edit</param>
		/// <param name="flags">Flags</param>
		public EditableValueTextInfo(string text, EditValueFlags flags = EditValueFlags.SelectText) {
			Text = text ?? throw new ArgumentNullException(nameof(text));
			Flags = flags;
		}
	}

	abstract class EditableValue : IEditableValue {
		public event PropertyChangedEventHandler? PropertyChanged;
		public virtual EditableValueOptions Options => EditableValueOptions.None;
		public virtual bool CanEdit => true;

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

		public abstract EditableValueTextInfo GetText();
		public abstract void SetText(string text);
	}

	sealed class EditableValueImpl : EditableValue {
		readonly Func<EditableValueTextInfo> getText;
		readonly Action<string?> setText;
		readonly Func<bool> canEdit;
		static readonly Func<bool> defaultCanEdit = () => true;

		public override EditableValueOptions Options { get; }
		public override bool CanEdit => canEdit();

		public EditableValueImpl(Func<string?> getText, Action<string?> setText, Func<bool>? canEdit = null, EditableValueOptions options = EditableValueOptions.None) {
			if (getText is null)
				throw new ArgumentNullException(nameof(getText));
			this.getText = () => new EditableValueTextInfo(getText() ?? string.Empty);
			this.setText = setText ?? throw new ArgumentNullException(nameof(setText));
			this.canEdit = canEdit ?? defaultCanEdit;
			Options = options;
		}

		public EditableValueImpl(Func<EditableValueTextInfo> getText, Action<string?> setText, Func<bool>? canEdit = null, EditableValueOptions options = EditableValueOptions.None) {
			this.getText = getText ?? throw new ArgumentNullException(nameof(getText));
			this.setText = setText ?? throw new ArgumentNullException(nameof(setText));
			this.canEdit = canEdit ?? defaultCanEdit;
			Options = options;
		}

		public override EditableValueTextInfo GetText() => getText();
		public override void SetText(string text) => setText(text);
	}
}
