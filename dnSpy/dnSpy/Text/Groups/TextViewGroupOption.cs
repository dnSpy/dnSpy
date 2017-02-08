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
using dnSpy.Contracts.Settings.Groups;

namespace dnSpy.Text.Groups {
	sealed class TextViewGroupOption {
		public string OptionId => Definition.Name;
		public object DefaultValue => Definition.DefaultValue;

		public object Value {
			get { return theValue; }
			set {
				if (!Equals(theValue, value)) {
					theValue = value;
					owner.OptionChanged(this);
				}
			}
		}
		object theValue;

		public ContentTypeOptionDefinition Definition { get; }

		readonly TextViewOptionsGroup owner;

		public TextViewGroupOption(TextViewOptionsGroup owner, ContentTypeOptionDefinition definition) {
			if (owner == null)
				throw new ArgumentNullException(nameof(owner));
			if (definition == null)
				throw new ArgumentNullException(nameof(definition));
			this.owner = owner;
			Definition = definition;
			theValue = Definition.DefaultValue;
		}
	}
}
