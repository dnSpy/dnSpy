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
using System.Diagnostics;

namespace dnSpy.Debugger.DotNet.Metadata.Impl.COMD {
	sealed class DmdEventDefCOMD : DmdEventDef {
		public override string Name { get; }
		public override DmdEventAttributes Attributes { get; }
		public override DmdType EventHandlerType { get; }

		readonly DmdComMetadataReader reader;

		public DmdEventDefCOMD(DmdComMetadataReader reader, uint rid, DmdType declaringType, DmdType reflectedType) : base(rid, declaringType, reflectedType) {
			this.reader = reader ?? throw new ArgumentNullException(nameof(reader));
			reader.Dispatcher.VerifyAccess();
			uint token = 0x14000000 + rid;
			Name = MDAPI.GetEventName(reader.MetaDataImport, token) ?? string.Empty;
			Attributes = MDAPI.GetEventAttributes(reader.MetaDataImport, token);
			var eventTypeToken = MDAPI.GetEventTypeToken(reader.MetaDataImport, token);
			EventHandlerType = reader.ResolveType((int)eventTypeToken, DeclaringType!.GetGenericArguments(), null, DmdResolveOptions.None) ?? reader.Module.AppDomain.System_Void;
		}

		T COMThread<T>(Func<T> action) => reader.Dispatcher.Invoke(action);

		protected override void GetMethods(out DmdMethodInfo? addMethod, out DmdMethodInfo? removeMethod, out DmdMethodInfo? raiseMethod, out DmdMethodInfo[]? otherMethods) {
			var info = COMThread(GetMethods_COMThread);
			addMethod = info.addMethod;
			removeMethod = info.removeMethod;
			raiseMethod = info.raiseMethod;
			otherMethods = info.otherMethods;
		}

		(DmdMethodInfo? addMethod, DmdMethodInfo? removeMethod, DmdMethodInfo? raiseMethod, DmdMethodInfo[] otherMethods) GetMethods_COMThread() {
			reader.Dispatcher.VerifyAccess();
			MDAPI.GetEventAddRemoveFireTokens(reader.MetaDataImport, (uint)MetadataToken, out uint addToken, out uint removeToken, out uint fireToken);
			var otherMethodTokens = MDAPI.GetEventOtherMethodTokens(reader.MetaDataImport, (uint)MetadataToken);
			var addMethod = Lookup_COMThread(addToken);
			var removeMethod = Lookup_COMThread(removeToken);
			var raiseMethod = Lookup_COMThread(fireToken);
			var otherMethods = otherMethodTokens.Length == 0 ? Array.Empty<DmdMethodInfo>() : new DmdMethodInfo[otherMethodTokens.Length];
			for (int i = 0; i < otherMethods.Length; i++) {
				var otherMethod = Lookup_COMThread(otherMethodTokens[i]);
				if (otherMethod is null) {
					otherMethods = Array.Empty<DmdMethodInfo>();
					break;
				}
				otherMethods[i] = otherMethod;
			}
			return (addMethod, removeMethod, raiseMethod, otherMethods);
		}

		DmdMethodInfo? Lookup_COMThread(uint token) {
			if ((token >> 24) != 0x06 || (token & 0x00FFFFFF) == 0)
				return null;
			var method = ReflectedType!.GetMethod(Module, (int)token) as DmdMethodInfo;
			Debug2.Assert(method is not null);
			return method;
		}

		protected override DmdCustomAttributeData[] CreateCustomAttributes() => reader.ReadCustomAttributes(MetadataToken);
	}
}
