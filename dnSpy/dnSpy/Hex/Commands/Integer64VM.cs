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
using dnSpy.Contracts.MVVM;
using dnSpy.Contracts.Utilities;

namespace dnSpy.Hex.Commands {
	/// <summary>
	/// 64-bit integer, from -2^63 to 2^64-1
	/// </summary>
	sealed class Integer64VM : DataFieldVM<ulong> {
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="onUpdated">Called when value gets updated</param>
		public Integer64VM(Action<DataFieldVM> onUpdated)
			: this(0, onUpdated) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="value">Initial value</param>
		/// <param name="onUpdated">Called when value gets updated</param>
		public Integer64VM(ulong value, Action<DataFieldVM> onUpdated)
			: base(onUpdated) {
			SetValueFromConstructor(value);
		}

		/// <inheritdoc/>
		protected override string OnNewValue(ulong value) => SimpleTypeConverter.ToString(value, ulong.MinValue, ulong.MaxValue, null);

		/// <inheritdoc/>
		protected override string ConvertToValue(out ulong value) {
			string error;
			long v = SimpleTypeConverter.ParseInt64(StringValue, long.MinValue, long.MaxValue, out error);
			if (error == null) {
				value = (ulong)v;
				return null;
			}
			value = SimpleTypeConverter.ParseUInt64(StringValue, ulong.MinValue, ulong.MaxValue, out error);
			return error;
		}
	}
}
