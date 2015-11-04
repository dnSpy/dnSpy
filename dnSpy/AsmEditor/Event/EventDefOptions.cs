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

using System.Collections.Generic;
using dnlib.DotNet;
using ICSharpCode.ILSpy;

namespace dnSpy.AsmEditor.Event {
	sealed class EventDefOptions {
		public EventAttributes Attributes;
		public UTF8String Name;
		public ITypeDefOrRef EventType;
		public MethodDef AddMethod;
		public MethodDef InvokeMethod;
		public MethodDef RemoveMethod;
		public List<MethodDef> OtherMethods = new List<MethodDef>();
		public List<CustomAttribute> CustomAttributes = new List<CustomAttribute>();

		public EventDefOptions() {
		}

		public EventDefOptions(EventDef evt) {
			this.Attributes = evt.Attributes;
			this.Name = evt.Name;
			this.EventType = evt.EventType;
			this.AddMethod = evt.AddMethod;
			this.InvokeMethod = evt.InvokeMethod;
			this.RemoveMethod = evt.RemoveMethod;
			this.OtherMethods.AddRange(evt.OtherMethods);
			this.CustomAttributes.AddRange(evt.CustomAttributes);
		}

		public EventDef CopyTo(EventDef evt) {
			evt.Attributes = this.Attributes;
			evt.Name = this.Name ?? UTF8String.Empty;
			evt.EventType = this.EventType;
			evt.AddMethod = this.AddMethod;
			evt.InvokeMethod = this.InvokeMethod;
			evt.RemoveMethod = this.RemoveMethod;
			evt.OtherMethods.Clear();
			evt.OtherMethods.AddRange(this.OtherMethods);
			evt.CustomAttributes.Clear();
			evt.CustomAttributes.AddRange(CustomAttributes);
			return evt;
		}

		public EventDef CreateEventDef(ModuleDef ownerModule) {
			return ownerModule.UpdateRowId(CopyTo(new EventDefUser()));
		}

		public static EventDefOptions Create(UTF8String name, ITypeDefOrRef eventType) {
			return new EventDefOptions {
				Attributes = 0,
				Name = name,
				EventType = eventType,
			};
		}
	}
}
