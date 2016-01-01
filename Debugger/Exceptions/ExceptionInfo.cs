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
	sealed class ExceptionInfo : IEquatable<ExceptionInfo> {
		public ExceptionType ExceptionType {
			get { return exceptionInfoKey.ExceptionType; }
		}

		public string Name {
			get { return exceptionInfoKey.Name; }
		}

		public ExceptionInfoKey Key {
			get { return exceptionInfoKey; }
		}

		public bool BreakOnFirstChance {
			get { return breakOnFirstChance; }
			set { breakOnFirstChance = value; }
		}
		bool breakOnFirstChance;

		public bool IsOtherExceptions {
			get { return isOtherExceptions; }
		}
		readonly bool isOtherExceptions;

		readonly ExceptionInfoKey exceptionInfoKey;

		public ExceptionInfo(ExceptionInfoKey key, bool breakOnFirstChance) {
			this.exceptionInfoKey = key;
			this.breakOnFirstChance = breakOnFirstChance;
			this.isOtherExceptions = false;
		}

		public ExceptionInfo(ExceptionType exceptionType, string name) {
			this.exceptionInfoKey = new ExceptionInfoKey(exceptionType, name);
			this.breakOnFirstChance = false;
			this.isOtherExceptions = true;
		}

		public ExceptionInfo(ExceptionType exceptionType, EXCEPTION_INFO info) {
			this.exceptionInfoKey = new ExceptionInfoKey(exceptionType, info.Name);
			this.breakOnFirstChance = (info.State & ExceptionState.EXCEPTION_STOP_FIRST_CHANCE) != 0;
			this.isOtherExceptions = false;
		}

		public bool Equals(ExceptionInfo other) {
			return other != null &&
				IsOtherExceptions == other.IsOtherExceptions &&
				BreakOnFirstChance == other.BreakOnFirstChance &&
				exceptionInfoKey.Equals(other.exceptionInfoKey);
		}

		public override bool Equals(object obj) {
			return Equals(obj as ExceptionInfo);
		}

		public override int GetHashCode() {
			return (BreakOnFirstChance ? int.MinValue : 0) ^ (IsOtherExceptions ? 0x40000000 : 0) ^ exceptionInfoKey.GetHashCode();
		}

		public override string ToString() {
			return Name;
		}
	}
}
