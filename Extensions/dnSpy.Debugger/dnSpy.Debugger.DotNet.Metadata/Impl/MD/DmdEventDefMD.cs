/*
    Copyright (C) 2014-2018 de4dot@gmail.com

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
using dnlib.DotNet;
using dnlib.DotNet.MD;

namespace dnSpy.Debugger.DotNet.Metadata.Impl.MD {
	sealed class DmdEventDefMD : DmdEventDef {
		public override string Name { get; }
		public override DmdEventAttributes Attributes { get; }
		public override DmdType EventHandlerType { get; }

		readonly DmdEcma335MetadataReader reader;

		public DmdEventDefMD(DmdEcma335MetadataReader reader, uint rid, DmdType declaringType, DmdType reflectedType) : base(rid, declaringType, reflectedType) {
			this.reader = reader ?? throw new ArgumentNullException(nameof(reader));
			var row = reader.TablesStream.ReadEventRow(rid);
			Name = reader.StringsStream.ReadNoNull(row.Name);
			Attributes = (DmdEventAttributes)row.EventFlags;
			if (!CodedToken.TypeDefOrRef.Decode(row.EventType, out uint token))
				token = uint.MaxValue;
			EventHandlerType = reader.ResolveType((int)token, DeclaringType.GetGenericArguments(), null, DmdResolveOptions.None) ?? reader.Module.AppDomain.System_Void;
		}

		protected override void GetMethods(out DmdMethodInfo addMethod, out DmdMethodInfo removeMethod, out DmdMethodInfo raiseMethod, out DmdMethodInfo[] otherMethods) {
			addMethod = null;
			removeMethod = null;
			raiseMethod = null;
			List<DmdMethodInfo> otherMethodsList = null;

			var ridList = reader.Metadata.GetMethodSemanticsRidList(Table.Event, Rid);
			for (uint i = 0; i < ridList.Length; i++) {
				var row = reader.TablesStream.ReadMethodSemanticsRow(ridList[i]);
				var method = ReflectedType.GetMethod(Module, 0x06000000 + (int)row.Method) as DmdMethodInfo;
				if ((object)method == null)
					continue;

				switch ((MethodSemanticsAttributes)row.Semantic) {
				case MethodSemanticsAttributes.AddOn:
					if ((object)addMethod == null)
						addMethod = method;
					break;

				case MethodSemanticsAttributes.RemoveOn:
					if ((object)removeMethod == null)
						removeMethod = method;
					break;

				case MethodSemanticsAttributes.Fire:
					if ((object)raiseMethod == null)
						raiseMethod = method;
					break;

				case MethodSemanticsAttributes.Other:
					if (otherMethodsList == null)
						otherMethodsList = new List<DmdMethodInfo>();
					otherMethodsList.Add(method);
					break;
				}
			}

			otherMethods = otherMethodsList?.ToArray();
		}

		protected override DmdCustomAttributeData[] CreateCustomAttributes() => reader.ReadCustomAttributes(MetadataToken);
	}
}
