/*
    Copyright (C) 2014-2015 de4dot@gmail.com

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
using System.Linq;
using dndbg.Engine.COM.MetaData;
using dnlib.DotNet;

namespace dndbg.Engine {
	static class Utils {
		public static bool IsDebuggee32Bit {
			get { return IntPtr.Size == 4; }// Debugger and debuggee must both be 32-bit or both 64-bit
		}

		public static int DebuggeeIntPtrSize {
			get { return IntPtr.Size; }
		}

		public static bool IsSystemNullable(this CorType type) {
			TokenAndName hasValueInfo, valueInfo;
			return IsSystemNullable(type, out hasValueInfo, out valueInfo);
		}

		public static bool IsSystemNullable(this CorType type, out TokenAndName hasValueInfo, out TokenAndName valueInfo) {
			hasValueInfo = new TokenAndName();
			valueInfo = new TokenAndName();
			if (type == null)
				return false;
			var cls = type.Class;
			if (cls == null)
				return false;
			var mod = cls.Module;
			if (mod == null)
				return false;
			//TODO: verify that module is the corlib
			if (type.TypeParameters.Count() != 1)
				return false;
			var mdi = mod.GetMetaDataInterface<IMetaDataImport>();
			if (MetaDataUtils.GetTypeDefFullName(mdi, cls.Token) != "System.Nullable`1")
				return false;
			var fields = MetaDataUtils.GetFields(mdi, cls.Token);
			if (fields.Count != 2)
				return false;
			if (fields[0].Name != "hasValue")
				return false;
			if (fields[1].Name != "value")
				return false;

			hasValueInfo = fields[0];
			valueInfo = fields[1];
			return true;
		}

		public static bool IsSystemNullable(this GenericInstSig gis) {
			if (gis == null)
				return false;
			if (gis.GenericArguments.Count != 1)
				return false;
			var type = gis.GenericType as ValueTypeSig;
			if (type == null)
				return false;
			var tdr = type.TypeDefOrRef;
			if (tdr == null || tdr.DeclaringType != null || tdr.FullName != "System.Nullable`1")
				return false;
			var td = tdr.ResolveTypeDef();
			if (td != null) {
				if (td.Fields.Count != 2)
					return false;
				if (td.Fields[0].Name != "hasValue")
					return false;
				if (td.Fields[1].Name != "value")
					return false;
			}

			return true;
		}
	}
}
