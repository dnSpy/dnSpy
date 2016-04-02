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

using System;
using dnSpy.Contracts.Highlighting;

namespace dnSpy.Contracts.Scripting.Debugger {
	/// <summary>
	/// A value read from the debugged process
	/// </summary>
	public struct ValueResult : IEquatable<ValueResult> {
		/// <summary>
		/// The value. Only valid if <see cref="IsValid"/> is true, else it shouldn't be used
		/// </summary>
		public readonly object Value;

		/// <summary>
		/// true if <see cref="Value"/> is valid
		/// </summary>
		public readonly bool IsValid;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="value">Value</param>
		public ValueResult(object value) {
			this.Value = value;
			this.IsValid = true;
		}

		/// <summary>
		/// Write this to <paramref name="output"/>
		/// </summary>
		/// <param name="output">Destination</param>
		/// <param name="value">Owner <see cref="IDebuggerValue"/> instance</param>
		/// <param name="flags">Flags</param>
		public void Write(ISyntaxHighlightOutput output, IDebuggerValue value, TypeFormatFlags flags = TypeFormatFlags.Default) {
			value.Write(output, this, flags);
		}

		/// <summary>
		/// ToString()
		/// </summary>
		/// <param name="value">Owner <see cref="IDebuggerValue"/> instance</param>
		/// <param name="flags">Flags</param>
		/// <returns></returns>
		public string ToString(IDebuggerValue value, TypeFormatFlags flags = TypeFormatFlags.Default) {
			return value.ToString(this, flags);
		}

		/// <summary>
		/// Equals()
		/// </summary>
		/// <param name="other">Other value</param>
		/// <returns></returns>
		public bool Equals(ValueResult other) {
			if (IsValid != other.IsValid)
				return false;
			if (!IsValid)
				return true;
			if (ReferenceEquals(Value, other.Value))
				return true;
			if (Value == null || other.Value == null)
				return false;
			if (Value.GetType() != other.Value.GetType())
				return false;
			return Value.Equals(other.Value);
		}

		/// <summary>
		/// Equals()
		/// </summary>
		/// <param name="obj">Other value</param>
		/// <returns></returns>
		public override bool Equals(object obj) {
			return obj is ValueResult && Equals((ValueResult)obj);
		}

		/// <summary>
		/// GetHashCode()
		/// </summary>
		/// <returns></returns>
		public override int GetHashCode() {
			if (!IsValid)
				return 0x12345678;
			return Value == null ? 0 : Value.GetHashCode();
		}

		/// <summary>
		/// ToString()
		/// </summary>
		/// <returns></returns>
		public override string ToString() {
			if (!IsValid)
				return "<invalid>";
			if (Value == null)
				return "null";
			return Value.ToString();
		}
	}
}
