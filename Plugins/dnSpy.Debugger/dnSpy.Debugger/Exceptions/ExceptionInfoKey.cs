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

namespace dnSpy.Debugger.Exceptions {
	sealed class ExceptionInfoKey : IEquatable<ExceptionInfoKey> {
		public ExceptionType ExceptionType { get; }
		public string Name { get; }

		public ExceptionInfoKey(ExceptionType exceptionType, string name) {
			this.ExceptionType = exceptionType;
			this.Name = name;
		}

		public bool Equals(ExceptionInfoKey other) => other != null && ExceptionType == other.ExceptionType && Name == other.Name;
		public override bool Equals(object obj) => Equals(obj as ExceptionInfoKey);
		public override int GetHashCode() => (int)ExceptionType ^ Name.GetHashCode();
		public override string ToString() => Name;
	}
}
