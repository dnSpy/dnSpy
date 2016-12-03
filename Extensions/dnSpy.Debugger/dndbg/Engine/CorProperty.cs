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

using System;
using dndbg.COM.MetaData;
using dnlib.DotNet;

namespace dndbg.Engine {
	public sealed class CorProperty : IEquatable<CorProperty> {
		/// <summary>
		/// Gets the module or null
		/// </summary>
		public CorModule Module => Class.Module;

		/// <summary>
		/// Gets the class
		/// </summary>
		public CorClass Class { get; }

		/// <summary>
		/// Gets the token
		/// </summary>
		public uint Token { get; }

		public CorFunction GetMethod {
			get {
				var mod = Module;
				uint mdGetter, mdSetter;
				MDAPI.GetPropertyGetterSetter(mod?.GetMetaDataInterface<IMetaDataImport>(), Token, out mdGetter, out mdSetter);
				return mod?.GetFunctionFromToken(mdGetter);
			}
		}

		public CorFunction SetMethod {
			get {
				var mod = Module;
				uint mdGetter, mdSetter;
				MDAPI.GetPropertyGetterSetter(mod?.GetMetaDataInterface<IMetaDataImport>(), Token, out mdGetter, out mdSetter);
				return mod?.GetFunctionFromToken(mdSetter);
			}
		}

		public CorProperty(CorClass cls, uint token) {
			Class = cls;
			Token = token;
		}

		public CorFunction[] GetOtherMethods() {
			var mod = Module;
			var tokens = MDAPI.GetPropertyOtherMethodTokens(mod?.GetMetaDataInterface<IMetaDataImport>(), Token);
			if (tokens.Length == 0)
				return Array.Empty<CorFunction>();
			var funcs = new CorFunction[tokens.Length];
			for (int i = 0; i < tokens.Length; i++)
				funcs[i] = mod.GetFunctionFromToken(tokens[i]);
			return funcs;
		}

		public PropertyAttributes GetAttributes() =>
			MDAPI.GetPropertyAttributes(Module?.GetMetaDataInterface<IMetaDataImport>(), Token);
		public string GetName() =>
			MDAPI.GetPropertyName(Module?.GetMetaDataInterface<IMetaDataImport>(), Token);
		public PropertySig GetPropertySig() =>
			MetaDataUtils.ReadPropertySig(Module?.GetMetaDataInterface<IMetaDataImport>(), Token);

		public static bool operator ==(CorProperty a, CorProperty b) {
			if (ReferenceEquals(a, b))
				return true;
			if (ReferenceEquals(a, null) || ReferenceEquals(b, null))
				return false;
			return a.Equals(b);
		}

		public static bool operator !=(CorProperty a, CorProperty b) => !(a == b);

		public bool Equals(CorProperty other) {
			return !ReferenceEquals(other, null) &&
				Token == other.Token &&
				Class == other.Class;
		}

		public override bool Equals(object obj) => Equals(obj as CorProperty);
		public override int GetHashCode() => (int)Token ^ Class.GetHashCode();

		public T Write<T>(T output, TypePrinterFlags flags) where T : ITypeOutput {
			new TypePrinter(output, flags).Write(this);
			return output;
		}

		public string ToString(TypePrinterFlags flags) => Write(new StringBuilderTypeOutput(), flags).ToString();
		public override string ToString() => ToString(TypePrinterFlags.Default);
	}
}
