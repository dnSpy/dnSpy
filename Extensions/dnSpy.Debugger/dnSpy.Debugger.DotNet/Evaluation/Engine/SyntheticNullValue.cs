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
using dnSpy.Contracts.Debugger.DotNet.Evaluation;
using dnSpy.Contracts.Debugger.Evaluation;
using dnSpy.Debugger.DotNet.Metadata;

namespace dnSpy.Debugger.DotNet.Evaluation.Engine {
	sealed class SyntheticNullValue : DbgDotNetValue {
		public override DmdType Type { get; }
		public override bool IsReference => true;
		public override bool IsNullReference => true;
		public override bool IsBox => false;
		public override bool IsArray => false;

		public SyntheticNullValue(DmdType type) =>
			Type = type ?? throw new ArgumentNullException(nameof(type));

		public override DbgDotNetRawValue GetRawValue() => new DbgDotNetRawValue(DbgSimpleValueType.Other, null);

		public override ulong? GetReferenceAddress() => null;
		public override DbgDotNetValue Dereference() => null;
		public override DbgDotNetValue Unbox() => null;

		public override bool GetArrayCount(out uint elementCount) {
			elementCount = 0;
			return false;
		}

		public override bool GetArrayInfo(out uint elementCount, out DbgDotNetArrayDimensionInfo[] dimensionInfos) {
			elementCount = 0;
			dimensionInfos = null;
			return false;
		}

		public override DbgDotNetValue GetArrayElementAt(uint index) => null;
		public override DbgRawAddressValue? GetRawAddressValue(bool onlyDataAddress) => null;

		public override void Dispose() { }
	}
}
