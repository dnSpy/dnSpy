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
using dndbg.COM.CorDebug;
using dndbg.COM.MetaData;

namespace dndbg.Engine {
	sealed class CorType : COMObject<ICorDebugType>, IEquatable<CorType?> {
		public CorElementType ElementType => elemType;
		readonly CorElementType elemType;

		public uint Rank => rank;
		readonly uint rank;

		public CorType? FirstTypeParameter {
			get {
				int hr = obj.GetFirstTypeParameter(out var type);
				return hr < 0 || type is null ? null : new CorType(type);
			}
		}

		public IEnumerable<CorType> TypeParameters {
			get {
				int hr = obj.EnumerateTypeParameters(out var typeEnum);
				if (hr < 0)
					yield break;
				for (;;) {
					hr = typeEnum.Next(1, out var type, out uint count);
					if (hr != 0 || type is null)
						break;
					yield return new CorType(type);
				}
			}
		}

		public bool HasClass => ElementType == CorElementType.Class || ElementType == CorElementType.ValueType;

		public CorClass? Class {
			get {
				int hr = obj.GetClass(out var cls);
				return hr < 0 || cls is null ? null : new CorClass(cls);
			}
		}

		internal IMetaDataImport? MetaDataImport => GetMetaDataImport(out uint token);

		internal IMetaDataImport? GetMetaDataImport(out uint token) {
			var cls = Class;
			var mdi = cls?.Module?.GetMetaDataInterface<IMetaDataImport>();
			token = cls?.Token ?? 0;
			return mdi;
		}

		public CorType(ICorDebugType type)
			: base(type) {
			int hr = type.GetType(out elemType);
			if (hr < 0)
				elemType = 0;

			hr = type.GetRank(out rank);
			if (hr < 0)
				rank = 0;
		}

		public CorValue? GetStaticFieldValue(uint token, CorFrame frame, out int hr) {
			hr = obj.GetStaticFieldValue(token, frame?.RawObject, out var value);
			return hr < 0 || value is null ? null : new CorValue(value);
		}

		public bool Equals(CorType? other) => !(other is null) && RawObject == other.RawObject;
		public override bool Equals(object? obj) => Equals(obj as CorType);
		public override int GetHashCode() => RawObject.GetHashCode();
	}
}
