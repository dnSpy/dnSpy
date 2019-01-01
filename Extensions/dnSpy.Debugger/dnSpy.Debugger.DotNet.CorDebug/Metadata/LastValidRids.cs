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

namespace dnSpy.Debugger.DotNet.CorDebug.Metadata {
	struct LastValidRids {
		public uint TypeDefRid;
		public uint FieldRid;
		public uint MethodRid;
		public uint ParamRid;
		public uint EventRid;
		public uint PropertyRid;
		public uint GenericParamRid;
		public uint GenericParamConstraintRid;

		public bool Equals(in LastValidRids other) =>
			TypeDefRid == other.TypeDefRid &&
			FieldRid == other.FieldRid &&
			MethodRid == other.MethodRid &&
			ParamRid == other.ParamRid &&
			EventRid == other.EventRid &&
			PropertyRid == other.PropertyRid &&
			GenericParamRid == other.GenericParamRid &&
			GenericParamConstraintRid == other.GenericParamConstraintRid;
	}
}
