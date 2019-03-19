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
using System.Collections.Generic;
using dnSpy.Contracts.Debugger.DotNet.Evaluation;

namespace dnSpy.Roslyn.Debugger.ValueNodes {
	sealed class DbgDotNetValueNodeInfo : IDisposable {
		public string Expression {
			get => expression;
			set => expression = value ?? throw new ArgumentNullException(nameof(value));
		}

		public DbgDotNetValue DisplayValue => displayValue;
		public DbgDotNetValue ProxyValue => proxyValue;
		public DbgDotNetValue Value => proxyValue ?? displayValue;

		string expression;

		public DbgDotNetValueNodeInfo(DbgDotNetValue value, string expression) {
			this.expression = expression ?? throw new ArgumentNullException(nameof(expression));
			displayValue = value ?? throw new ArgumentNullException(nameof(value));
			proxyValue = null;
			otherValues = null;
		}

		DbgDotNetValue displayValue;
		DbgDotNetValue proxyValue;
		object otherValues;

		void AddValue(DbgDotNetValue value) {
			if (value == null)
				return;
			if (otherValues == null)
				otherValues = value;
			else {
				var list = otherValues as List<DbgDotNetValue>;
				if (list == null)
					otherValues = list = new List<DbgDotNetValue> { (DbgDotNetValue)otherValues };
				list.Add(value);
			}
		}

		public void SetDisplayValue(DbgDotNetValue value) {
			if (value == null)
				throw new ArgumentNullException(nameof(value));
			AddValue(displayValue);
			displayValue = value;
		}

		public void SetProxyValue(DbgDotNetValue value) {
			if (value == null)
				throw new ArgumentNullException(nameof(value));
			AddValue(proxyValue);
			proxyValue = value;
		}

		public void Dispose() {
			(otherValues as DbgDotNetValue)?.Dispose();
			if (otherValues is List<DbgDotNetValue> list) {
				foreach (var value in list)
					value.Dispose();
			}
			proxyValue?.Dispose();
			displayValue?.Dispose();
		}
	}
}
