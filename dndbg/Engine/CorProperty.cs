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
		public CorModule Module {
			get { return cls.Module; }
		}

		/// <summary>
		/// Gets the class
		/// </summary>
		public CorClass Class {
			get { return cls; }
		}
		readonly CorClass cls;

		/// <summary>
		/// Gets the token
		/// </summary>
		public uint Token {
			get { return token; }
		}
		readonly uint token;

		public CorFunction GetMethod {
			get {
				var mod = Module;
				var mdi = mod == null ? null : mod.GetMetaDataInterface<IMetaDataImport>();
				uint mdGetter, mdSetter;
				MDAPI.GetPropertyGetterSetter(mdi, token, out mdGetter, out mdSetter);
				return mod == null ? null : mod.GetFunctionFromToken(mdGetter);
			}
		}

		public CorFunction SetMethod {
			get {
				var mod = Module;
				var mdi = mod == null ? null : mod.GetMetaDataInterface<IMetaDataImport>();
				uint mdGetter, mdSetter;
				MDAPI.GetPropertyGetterSetter(mdi, token, out mdGetter, out mdSetter);
				return mod == null ? null : mod.GetFunctionFromToken(mdSetter);
			}
		}

		public CorProperty(CorClass cls, uint token) {
			this.cls = cls;
			this.token = token;
		}

		public CorFunction[] GetOtherMethods() {
			var mod = Module;
			var mdi = mod == null ? null : mod.GetMetaDataInterface<IMetaDataImport>();
			var tokens = MDAPI.GetPropertyOtherMethodTokens(mdi, token);
			if (tokens.Length == 0)
				return emptyCorFunctions;
			var funcs = new CorFunction[0];
			for (int i = 0; i < tokens.Length; i++)
				funcs[i] = mod.GetFunctionFromToken(tokens[i]);
			return funcs;
		}
		static readonly CorFunction[] emptyCorFunctions = new CorFunction[0];

		public PropertyAttributes GetAttributes() {
			var mod = Module;
			var mdi = mod == null ? null : mod.GetMetaDataInterface<IMetaDataImport>();
			return MDAPI.GetPropertyAttributes(mdi, Token);
		}

		public string GetName() {
			var mod = Module;
			var mdi = mod == null ? null : mod.GetMetaDataInterface<IMetaDataImport>();
			return MDAPI.GetPropertyName(mdi, Token);
		}

		public PropertySig GetPropertySig() {
			var mod = Module;
			var mdi = mod == null ? null : mod.GetMetaDataInterface<IMetaDataImport>();
			return MetaDataUtils.ReadPropertySig(mdi, Token);
		}

		public static bool operator ==(CorProperty a, CorProperty b) {
			if (ReferenceEquals(a, b))
				return true;
			if (ReferenceEquals(a, null) || ReferenceEquals(b, null))
				return false;
			return a.Equals(b);
		}

		public static bool operator !=(CorProperty a, CorProperty b) {
			return !(a == b);
		}

		public bool Equals(CorProperty other) {
			return !ReferenceEquals(other, null) &&
				Token == other.Token &&
				Class == other.Class;
		}

		public override bool Equals(object obj) {
			return Equals(obj as CorProperty);
		}

		public override int GetHashCode() {
			return (int)Token ^ cls.GetHashCode();
		}

		public T Write<T>(T output, TypePrinterFlags flags) where T : ITypeOutput {
			new TypePrinter(output, flags).Write(this);
			return output;
		}

		public string ToString(TypePrinterFlags flags) {
			return Write(new StringBuilderTypeOutput(), flags).ToString();
		}

		public override string ToString() {
			return ToString(TypePrinterFlags.Default);
		}
	}
}
