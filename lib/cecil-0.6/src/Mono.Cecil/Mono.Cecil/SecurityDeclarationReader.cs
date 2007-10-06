//
// SecurityDeclarationReader.cs
//
// Author:
//	Sebastien Pouliot <sebastien@ximian.com>
//
// Copyright (C) 2005 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

namespace Mono.Cecil {

	using System;
	using System.IO;
	using System.Reflection;
	using System.Security;
	using SSP = System.Security.Permissions;
	using System.Text;

	using Mono.Cecil.Metadata;
	using Mono.Cecil.Signatures;
	using Mono.Xml;

	internal class SecurityDeclarationReader {

		private SecurityParser m_parser;
		private SignatureReader sr;

		public SecurityDeclarationReader (MetadataRoot root, ReflectionReader reader)
		{
			sr = new SignatureReader (root, reader);
		}

		public SecurityParser Parser {
			get {
				if (m_parser == null)
					m_parser = new SecurityParser ();
				return m_parser;
			}
		}

		public SecurityDeclaration FromByteArray (SecurityAction action, byte [] declaration)
		{
			return FromByteArray (action, declaration, false);
		}

		static bool IsEmptyDeclaration (byte [] declaration)
		{
			return declaration == null || declaration.Length == 0 ||
				(declaration.Length == 1 && declaration [0] == 0);
		}

		public SecurityDeclaration FromByteArray (SecurityAction action, byte [] declaration, bool resolve)
		{
			SecurityDeclaration dec = new SecurityDeclaration (action);
#if !CF_1_0 && !CF_2_0
			dec.PermissionSet = new PermissionSet (SSP.PermissionState.None);

			if (IsEmptyDeclaration (declaration))
				return dec;

			if (declaration[0] == 0x2e) {
				// new binary format introduced in 2.0
				int pos = 1;
				int start;
				int numattr = Utilities.ReadCompressedInteger (declaration, pos, out start);
				if (numattr == 0)
					return dec;

				BinaryReader br = new BinaryReader (new MemoryStream (declaration));
				for (int i = 0; i < numattr; i++) {
					pos = start;
					SSP.SecurityAttribute sa = CreateSecurityAttribute (action, br, declaration, pos, out start, resolve);
					if (sa == null) {
						dec.Resolved = false;
						dec.Blob = declaration;
						return dec;
					}

					IPermission p = sa.CreatePermission ();
					dec.PermissionSet.AddPermission (p);
				}
			} else {
				Parser.LoadXml (Encoding.Unicode.GetString (declaration));
				try {
					dec.PermissionSet.FromXml (Parser.ToXml ());
					dec.PermissionSet.ToXml ();
				} catch {
					dec.Resolved = false;
					dec.Blob = declaration;
				}
			}
#endif
			return dec;
		}

#if !CF_1_0 && !CF_2_0
		private SSP.SecurityAttribute CreateSecurityAttribute (SecurityAction action, BinaryReader br, byte [] permset, int pos, out int start, bool resolve)
		{
			string cname = SignatureReader.ReadUTF8String (permset, pos, out start);
			Type secattr = null;

			// note: the SecurityAction parameter isn't important to generate the XML
			SSP.SecurityAttribute sa = null;
			try {
				secattr = Type.GetType (cname, false);
				if (secattr == null)
					return null;

				sa = Activator.CreateInstance (secattr, new object [] {(SSP.SecurityAction) action}) as SSP.SecurityAttribute;
			} catch {}

			if (sa == null)
				return null;

			// encoded length of all parameters (we don't need the value - except the updated pos)
			Utilities.ReadCompressedInteger (permset, start, out pos);
			int numparams = Utilities.ReadCompressedInteger (permset, pos, out start);
			if (numparams == 0)
				return sa;

			br.BaseStream.Position = start;
			for (int j = 0; j < numparams; j++) {
				bool read = false;
				CustomAttrib.NamedArg na = sr.ReadNamedArg (permset, br, ref read, resolve);
				if (!read)
					return null;

				if (na.Field) {
					FieldInfo fi = secattr.GetField (na.FieldOrPropName);
					fi.SetValue (sa, na.FixedArg.Elems[0].Value);
				} else if (na.Property) {
					PropertyInfo pi = secattr.GetProperty (na.FieldOrPropName);
					pi.SetValue (sa, na.FixedArg.Elems[0].Value, null);
				}
			}

			return sa;
		}
#endif
	}
}
