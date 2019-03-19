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
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Permissions;
using System.Text;

namespace dnSpy.Debugger.DotNet.Metadata.Impl {
	readonly struct DmdDeclSecurityReader : IDisposable {
		readonly DmdDataStream reader;
		readonly DmdModule module;
		readonly IList<DmdType> genericTypeArguments;

		public static DmdCustomAttributeData[] Read(DmdModule module, DmdDataStream signature, SecurityAction action) => Read(module, signature, action, null);
		public static DmdCustomAttributeData[] Read(DmdModule module, DmdDataStream signature, SecurityAction action, IList<DmdType> genericTypeArguments) {
			using (var reader = new DmdDeclSecurityReader(module, signature, genericTypeArguments))
				return reader.Read(action);
		}

		DmdDeclSecurityReader(DmdModule module, DmdDataStream reader, IList<DmdType> genericTypeArguments) {
			this.reader = reader;
			this.module = module;
			this.genericTypeArguments = genericTypeArguments ?? Array.Empty<DmdType>();
		}

		DmdCustomAttributeData[] Read(SecurityAction action) {
			try {
				if (reader.Position >= reader.Length)
					return Array.Empty<DmdCustomAttributeData>();

				if (reader.ReadByte() == '.')
					return ReadBinaryFormat(action);
				reader.Position--;
				return ReadXmlFormat(action);
			}
			catch (TypeNameParserException) {
			}
			catch (CABlobParserException) {
			}
			catch (ResolveException) {
			}
			catch (IOException) {
			}
			return Array.Empty<DmdCustomAttributeData>();
		}

		// Reads the new (.NET 2.0+) DeclSecurity blob format
		DmdCustomAttributeData[] ReadBinaryFormat(SecurityAction action) {
			int numAttrs = (int)reader.ReadCompressedUInt32();
			var res = new DmdCustomAttributeData[numAttrs];

			IList<DmdType> genericTypeArguments = null;
			int w = 0;
			for (int i = 0; i < numAttrs; i++) {
				var name = ReadUTF8String();
				var type = DmdTypeNameParser.ParseThrow(module, name ?? string.Empty, genericTypeArguments);
				reader.ReadCompressedUInt32();// int blobLength
				int numNamedArgs = (int)reader.ReadCompressedUInt32();
				var namedArgs = DmdCustomAttributeReader.ReadNamedArguments(module, reader, type, numNamedArgs, genericTypeArguments);
				if (namedArgs == null)
					throw new IOException();
				var (ctor, ctorArguments) = GetConstructor(type, action);
				Debug.Assert((object)ctor != null);
				if ((object)ctor == null)
					continue;
				res[w++] = new DmdCustomAttributeData(ctor, ctorArguments, namedArgs, isPseudoCustomAttribute: false);
			}
			if (res.Length != w) {
				if (w == 0)
					return Array.Empty<DmdCustomAttributeData>();
				Array.Resize(ref res, w);
			}

			return res;
		}

		// Reads the old (.NET 1.x) DeclSecurity blob format
		DmdCustomAttributeData[] ReadXmlFormat(SecurityAction action) {
			reader.Position = 0;
			var xml = Encoding.Unicode.GetString(reader.ReadBytes((int)reader.Length));
			var type = module.AppDomain.GetWellKnownType(DmdWellKnownType.System_Security_Permissions_PermissionSetAttribute);
			var (ctor, ctorArguments) = GetConstructor(type, action);
			var xmlProp = type.GetProperty("XML", module.AppDomain.System_String, Array.Empty<DmdType>());
			Debug.Assert((object)ctor != null);
			Debug.Assert((object)xmlProp != null);
			if ((object)ctor == null || (object)xmlProp == null)
				return Array.Empty<DmdCustomAttributeData>();
			var namedArguments = new[] { new DmdCustomAttributeNamedArgument(xmlProp, new DmdCustomAttributeTypedArgument(module.AppDomain.System_String, xml)) };
			return new[] { new DmdCustomAttributeData(ctor, ctorArguments, namedArguments, isPseudoCustomAttribute: false) };
		}

		static (DmdConstructorInfo ctor, IList<DmdCustomAttributeTypedArgument> constructorArguments) GetConstructor(DmdType type, SecurityAction action) {
			var appDomain = type.AppDomain;
			var securityActionType = appDomain.GetWellKnownType(DmdWellKnownType.System_Security_Permissions_SecurityAction);
			var ctor = type.GetConstructor(new[] { securityActionType });
			if ((object)ctor != null) {
				var ctorArgs = new[] { new DmdCustomAttributeTypedArgument(securityActionType, (int)action) };
				return (ctor, ctorArgs);
			}

			ctor = type.GetConstructor(Array.Empty<DmdType>()) ?? type.GetConstructors().FirstOrDefault();
			return (ctor, null);
		}

		string ReadUTF8String() {
			uint len = reader.ReadCompressedUInt32();
			return len == 0 ? string.Empty : Encoding.UTF8.GetString(reader.ReadBytes((int)len));
		}

		public void Dispose() => reader?.Dispose();
	}
}
