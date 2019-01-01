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
using dnSpy.Contracts.Debugger.Text;

namespace dnSpy.Debugger.Text {
	readonly struct ClassifiedTextCollection : IEquatable<ClassifiedTextCollection> {
		public static readonly ClassifiedTextCollection Empty = default;
		public bool IsDefault => Result == null;
		public ClassifiedText[] Result { get; }

		public ClassifiedTextCollection(ClassifiedText[] result) => Result = result ?? throw new ArgumentNullException(nameof(result));

		public void WriteTo(IDbgTextWriter output) {
			foreach (var info in Result)
				output.Write(info.Color, info.Text);
		}

		public bool Equals(ClassifiedTextCollection other) {
			var a = Result;
			var b = other.Result;
			if (a == b)
				return true;
			if (a == null || b == null)
				return false;
			if (a.Length != b.Length)
				return false;
			for (int i = 0; i < a.Length; i++) {
				if (!a[i].Equals(b[i]))
					return false;
			}
			return true;
		}
	}

	readonly struct ClassifiedText : IEquatable<ClassifiedText> {
		public DbgTextColor Color { get; }
		public string Text { get; }

		public ClassifiedText(DbgTextColor color, string text) {
			Color = color;
			Text = text;
		}

		public bool Equals(ClassifiedText other) => Color == other.Color && Text == other.Text;
		public override string ToString() => $"{Color}: {Text}";
	}
}
