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
using System.Collections.Generic;
using System.Text;
using dnSpy.Debugger.DotNet.Metadata;

namespace dnSpy.Debugger.DotNet.Evaluation.Engine.Interpreter {
	struct TypeName {
		public string Namespace;
		public string Name;
		public string Extra;

		public TypeName(string @namespace, string name) {
			Namespace = @namespace;
			Name = name;
			Extra = null;
		}

		public TypeName(string @namespace, string name, string extra) {
			Namespace = @namespace;
			Name = name;
			Extra = extra;
		}

		internal static TypeName Create(DmdType type) {
			if (type.TypeSignatureKind == DmdTypeSignatureKind.Type) {
				var declType = type.DeclaringType;
				if ((object)declType == null)
					return new TypeName(type.MetadataNamespace, type.MetadataName);

				if ((object)declType.DeclaringType == null)
					return new TypeName(declType.MetadataNamespace, declType.MetadataName, type.Name);

				var list = new List<DmdType>();
				for (;;) {
					if ((object)type.DeclaringType == null)
						break;
					list.Add(type);
					type = type.DeclaringType;
				}
				var sb = new StringBuilder();
				for (int i = list.Count - 1; i >= 0; i--) {
					if (i != list.Count - 1)
						sb.Append('+');
					sb.Append(list[i].MetadataName);
				}
				return new TypeName(type.MetadataNamespace, type.MetadataName, sb.ToString());
			}

			return new TypeName(null, string.Empty);
		}

		public override string ToString() {
			if (Namespace == null) {
				if (Extra == null)
					return Name;
				return Name + "+" + Extra;
			}
			if (Extra == null)
				return Namespace + "." + Name;
			return Namespace + "." + Name + "+" + Extra;
		}
	}

	sealed class TypeNameEqualityComparer : IEqualityComparer<TypeName> {
		public static readonly TypeNameEqualityComparer Instance = new TypeNameEqualityComparer();
		TypeNameEqualityComparer() { }

		public bool Equals(TypeName x, TypeName y) =>
			x.Name == y.Name &&
			x.Namespace == y.Namespace &&
			x.Extra == y.Extra;

		public int GetHashCode(TypeName obj) =>
			StringComparer.Ordinal.GetHashCode(obj.Namespace ?? string.Empty) ^
			StringComparer.Ordinal.GetHashCode(obj.Name ?? string.Empty) ^
			StringComparer.Ordinal.GetHashCode(obj.Extra ?? string.Empty);
	}
}
