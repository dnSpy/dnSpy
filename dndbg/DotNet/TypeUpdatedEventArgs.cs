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
using dnlib.DotNet;

namespace dndbg.DotNet {
	public sealed class TypeUpdatedEventArgs : EventArgs {
		/// <summary>
		/// true if the type has been loaded (the <c>LoadClass</c> event has been received), false
		/// if the type can still get new members (fields, methods, properties, events, nested types)
		/// </summary>
		public readonly bool Loaded;

		/// <summary>
		/// true if the type was created and no new members can be added. false if it got updated,
		/// eg. a member (field, type, event, property) could've gotten added or updated or a
		/// nested type got added to <see cref="TypeDef.NestedTypes"/>.
		/// </summary>
		public readonly bool Created;

		/// <summary>
		/// The type
		/// </summary>
		public readonly TypeDef TypeDef;

		public TypeUpdatedEventArgs(TypeDef type, bool created, bool loaded) {
			this.Created = created;
			this.TypeDef = type;
			this.Loaded = loaded;
		}
	}
}
