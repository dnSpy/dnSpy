//
// AssemblyNameReference.cs
//
// Author:
//   Jb Evain (jbevain@gmail.com)
//
// Copyright (c) 2008 - 2011 Jb Evain
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

using System;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Mono.Cecil {

	public class AssemblyNameReference : IMetadataScope {

		string name;
		string culture;
		Version version;
		uint attributes;
		byte [] public_key;
		byte [] public_key_token;
		AssemblyHashAlgorithm hash_algorithm;
		byte [] hash;

		internal MetadataToken token;

		string full_name;

		public string Name {
			get { return name; }
			set {
				name = value;
				full_name = null;
			}
		}

		public string Culture {
			get { return culture; }
			set {
				culture = value;
				full_name = null;
			}
		}

		public Version Version {
			get { return version; }
			set {
				 version = value;
				 full_name = null;
			}
		}

		public AssemblyAttributes Attributes {
			get { return (AssemblyAttributes) attributes; }
			set { attributes = (uint) value; }
		}

		public bool HasPublicKey {
			get { return attributes.GetAttributes ((uint) AssemblyAttributes.PublicKey); }
			set { attributes = attributes.SetAttributes ((uint) AssemblyAttributes.PublicKey, value); }
		}

		public bool IsSideBySideCompatible {
			get { return attributes.GetAttributes ((uint) AssemblyAttributes.SideBySideCompatible); }
			set { attributes = attributes.SetAttributes ((uint) AssemblyAttributes.SideBySideCompatible, value); }
		}

		public bool IsRetargetable {
			get { return attributes.GetAttributes ((uint) AssemblyAttributes.Retargetable); }
			set { attributes = attributes.SetAttributes ((uint) AssemblyAttributes.Retargetable, value); }
		}

		public byte [] PublicKey {
			get { return public_key; }
			set {
				public_key = value;
				HasPublicKey = !public_key.IsNullOrEmpty ();
				public_key_token = Empty<byte>.Array;
				full_name = null;
			}
		}

		public byte [] PublicKeyToken {
			get {
				if (public_key_token.IsNullOrEmpty () && !public_key.IsNullOrEmpty ()) {
					var hash = HashPublicKey ();
					// we need the last 8 bytes in reverse order
					byte[] local_public_key_token = new byte [8];
					Array.Copy (hash, (hash.Length - 8), local_public_key_token, 0, 8);
					Array.Reverse (local_public_key_token, 0, 8);
					public_key_token = local_public_key_token; // publish only once finished (required for thread-safety)
				}
				return public_key_token;
			}
			set {
				public_key_token = value;
				full_name = null;
			}
		}

		byte [] HashPublicKey ()
		{
			HashAlgorithm algorithm;

			switch (hash_algorithm) {
			case AssemblyHashAlgorithm.Reserved:
#if SILVERLIGHT
				throw new NotSupportedException ();
#else
				algorithm = MD5.Create ();
				break;
#endif
			default:
				// None default to SHA1
#if SILVERLIGHT
				algorithm = new SHA1Managed ();
				break;
#else
				algorithm = SHA1.Create ();
				break;
#endif
			}

			using (algorithm)
				return algorithm.ComputeHash (public_key);
		}

		public virtual MetadataScopeType MetadataScopeType {
			get { return MetadataScopeType.AssemblyNameReference; }
		}

		public string FullName {
			get {
				if (full_name != null)
					return full_name;

				const string sep = ", ";

				var builder = new StringBuilder ();
				builder.Append (name);
				if (version != null) {
					builder.Append (sep);
					builder.Append ("Version=");
					builder.Append (version.ToString ());
				}
				builder.Append (sep);
				builder.Append ("Culture=");
				builder.Append (string.IsNullOrEmpty (culture) ? "neutral" : culture);
				builder.Append (sep);
				builder.Append ("PublicKeyToken=");

				if (this.PublicKeyToken != null && public_key_token.Length > 0) {
					for (int i = 0 ; i < public_key_token.Length ; i++) {
						builder.Append (public_key_token [i].ToString ("x2"));
					}
				} else
					builder.Append ("null");

				return full_name = builder.ToString ();
			}
		}

		public static AssemblyNameReference Parse (string fullName)
		{
			if (fullName == null)
				throw new ArgumentNullException ("fullName");
			if (fullName.Length == 0)
				throw new ArgumentException ("Name can not be empty");

			var name = new AssemblyNameReference ();
			var tokens = fullName.Split (',');
			for (int i = 0; i < tokens.Length; i++) {
				var token = tokens [i].Trim ();

				if (i == 0) {
					name.Name = token;
					continue;
				}

				var parts = token.Split ('=');
				if (parts.Length != 2)
					throw new ArgumentException ("Malformed name");

				switch (parts [0].ToLowerInvariant ()) {
				case "version":
					name.Version = new Version (parts [1]);
					break;
				case "culture":
					name.Culture = parts [1];
					break;
				case "publickeytoken":
					var pk_token = parts [1];
					if (pk_token == "null")
						break;

					name.PublicKeyToken = new byte [pk_token.Length / 2];
					for (int j = 0; j < name.PublicKeyToken.Length; j++)
						name.PublicKeyToken [j] = Byte.Parse (pk_token.Substring (j * 2, 2), NumberStyles.HexNumber);

					break;
				}
			}

			return name;
		}

		public AssemblyHashAlgorithm HashAlgorithm {
			get { return hash_algorithm; }
			set { hash_algorithm = value; }
		}

		public virtual byte [] Hash {
			get { return hash; }
			set { hash = value; }
		}

		public MetadataToken MetadataToken {
			get { return token; }
			set { token = value; }
		}

		internal AssemblyNameReference ()
		{
		}

		public AssemblyNameReference (string name, Version version)
		{
			if (name == null)
				throw new ArgumentNullException ("name");

			this.name = name;
			this.version = version;
			this.hash_algorithm = AssemblyHashAlgorithm.None;
			this.token = new MetadataToken (TokenType.AssemblyRef);
		}

		public override string ToString ()
		{
			return this.FullName;
		}
	}
}
