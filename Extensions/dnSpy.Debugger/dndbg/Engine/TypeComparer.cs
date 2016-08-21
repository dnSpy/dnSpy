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

using dnlib.DotNet;

namespace dndbg.Engine {
	struct TypeComparer {
		public bool Equals(TypeSig ts1, TypeSig ts2) {
			if (ts1 == ts2)
				return true;
			if (ts1 == null || ts2 == null)
				return false;
			if (ts1.ElementType != ts2.ElementType)
				return false;

			switch (ts1.ElementType) {
			case ElementType.Void:
			case ElementType.Boolean:
			case ElementType.Char:
			case ElementType.I1:
			case ElementType.U1:
			case ElementType.I2:
			case ElementType.U2:
			case ElementType.I4:
			case ElementType.U4:
			case ElementType.I8:
			case ElementType.U8:
			case ElementType.R4:
			case ElementType.R8:
			case ElementType.String:
			case ElementType.TypedByRef:
			case ElementType.I:
			case ElementType.U:
			case ElementType.Object:
			case ElementType.Sentinel:
			case ElementType.End:
			case ElementType.R:
			case ElementType.Internal:
			default:
				return true;

			case ElementType.Ptr:
			case ElementType.ByRef:
			case ElementType.SZArray:
			case ElementType.Pinned:
				return Equals(ts1.Next, ts2.Next);

			case ElementType.Var:
			case ElementType.MVar:
				return ((GenericSig)ts1).Number == ((GenericSig)ts2).Number;

			case ElementType.Class:
			case ElementType.ValueType:
				return Equals(((ClassOrValueTypeSig)ts1).TypeDefOrRef, ((ClassOrValueTypeSig)ts2).TypeDefOrRef);

			case ElementType.Array:
				var ary1 = (ArraySig)ts1;
				var ary2 = (ArraySig)ts2;
				if (ary1.Rank != ary2.Rank)
					return false;
				if (ary1.Sizes.Count != ary2.Sizes.Count)
					return false;
				if (ary1.LowerBounds.Count != ary2.LowerBounds.Count)
					return false;
				for (int i = 0; i < ary1.Sizes.Count; i++) {
					if (ary1.Sizes[i] != ary2.Sizes[i])
						return false;
				}
				for (int i = 0; i < ary1.LowerBounds.Count; i++) {
					if (ary1.LowerBounds[i] != ary2.LowerBounds[i])
						return false;
				}
				return Equals(ts1.Next, ts2.Next);

			case ElementType.GenericInst:
				var gis1 = (GenericInstSig)ts1;
				var gis2 = (GenericInstSig)ts2;
				if (gis1.GenericArguments.Count != gis2.GenericArguments.Count)
					return false;
				for (int i = 0; i < gis1.GenericArguments.Count; i++) {
					if (!Equals(gis1.GenericArguments[i], gis2.GenericArguments[i]))
						return false;
				}
				return Equals(gis1.GenericType, gis2.GenericType);

			case ElementType.FnPtr:
				return true;//TODO: Compare func sig

			case ElementType.CModReqd:
			case ElementType.CModOpt:
				var mod1 = (ModifierSig)ts1;
				var mod2 = (ModifierSig)ts2;
				return Equals(mod1.Modifier, mod2.Modifier) && Equals(ts1.Next, ts2.Next);

			case ElementType.ValueArray:
			case ElementType.Module:
				return Equals(ts1.Next, ts2.Next);
			}
		}

		static bool Equals(ITypeDefOrRef tdr1, ITypeDefOrRef tdr2) {
			var mdip1 = tdr1 as IMetaDataImportProvider;
			var mdip2 = tdr2 as IMetaDataImportProvider;
			return mdip1 != null && mdip2 != null &&
				mdip1.MetaDataImport == mdip2.MetaDataImport &&
				mdip1.MDToken == mdip2.MDToken;
		}
	}
}
