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

namespace dnSpy.Contracts.Debugger.Exceptions {
	/// <summary>
	/// Exception condition settings
	/// </summary>
	public readonly struct DbgExceptionConditionSettings {
		/// <summary>
		/// Exception condition type
		/// </summary>
		public DbgExceptionConditionType ConditionType { get; }

		/// <summary>
		/// Exception condition
		/// </summary>
		public string Condition { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="conditionType">Condition type</param>
		/// <param name="condition">Condition</param>
		public DbgExceptionConditionSettings(DbgExceptionConditionType conditionType, string condition) {
			ConditionType = conditionType;
			Condition = condition ?? throw new ArgumentNullException(nameof(condition));
		}
	}

	/// <summary>
	/// Exception condition type
	/// </summary>
	public enum DbgExceptionConditionType {
		/// <summary>
		/// Module name equals <see cref="DbgExceptionConditionSettings.Condition"/>
		/// </summary>
		ModuleNameEquals,

		/// <summary>
		/// Module name does not equal <see cref="DbgExceptionConditionSettings.Condition"/>
		/// </summary>
		ModuleNameNotEquals,
	}
}
