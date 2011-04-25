//
// CryptoService.cs
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
using System.IO;
using System.Reflection;
using System.Security.Cryptography;

#if !READ_ONLY

#if !SILVERLIGHT && !CF
using System.Runtime.Serialization;
using Mono.Security.Cryptography;
#endif

using Mono.Cecil.PE;

namespace Mono.Cecil {

	// Most of this code has been adapted
	// from Jeroen Frijters' fantastic work
	// in IKVM.Reflection.Emit. Thanks!

	static class CryptoService {

#if !SILVERLIGHT && !CF
		public static void StrongName (Stream stream, ImageWriter writer, StrongNameKeyPair key_pair)
		{
			int strong_name_pointer;

			var strong_name = CreateStrongName (key_pair, HashStream (stream, writer, out strong_name_pointer));
			PatchStrongName (stream, strong_name_pointer, strong_name);
		}

		static void PatchStrongName (Stream stream, int strong_name_pointer, byte [] strong_name)
		{
			stream.Seek (strong_name_pointer, SeekOrigin.Begin);
			stream.Write (strong_name, 0, strong_name.Length);
		}

		static byte [] CreateStrongName (StrongNameKeyPair key_pair, byte [] hash)
		{
			const string hash_algo = "SHA1";

			using (var rsa = key_pair.CreateRSA ()) {
				var formatter = new RSAPKCS1SignatureFormatter (rsa);
				formatter.SetHashAlgorithm (hash_algo);

				byte [] signature = formatter.CreateSignature (hash);
				Array.Reverse (signature);

				return signature;
			}
		}

		static byte [] HashStream (Stream stream, ImageWriter writer, out int strong_name_pointer)
		{
			const int buffer_size = 8192;

			var text = writer.text;
			var header_size = (int) writer.GetHeaderSize ();
			var text_section_pointer = (int) text.PointerToRawData;
			var strong_name_directory = writer.GetStrongNameSignatureDirectory ();

			if (strong_name_directory.Size == 0)
				throw new InvalidOperationException ();

			strong_name_pointer = (int) (text_section_pointer
				+ (strong_name_directory.VirtualAddress - text.VirtualAddress));
			var strong_name_length = (int) strong_name_directory.Size;

			var sha1 = new SHA1Managed ();
			var buffer = new byte [buffer_size];
			using (var crypto_stream = new CryptoStream (Stream.Null, sha1, CryptoStreamMode.Write)) {

				stream.Seek (0, SeekOrigin.Begin);
				CopyStreamChunk (stream, crypto_stream, buffer, header_size);

				stream.Seek (text_section_pointer, SeekOrigin.Begin);
				CopyStreamChunk (stream, crypto_stream, buffer, (int) strong_name_pointer - text_section_pointer);

				stream.Seek (strong_name_length, SeekOrigin.Current);
				CopyStreamChunk (stream, crypto_stream, buffer, (int) (stream.Length - (strong_name_pointer + strong_name_length)));
			}

			return sha1.Hash;
		}
#endif
		static void CopyStreamChunk (Stream stream, Stream dest_stream, byte [] buffer, int length)
		{
			while (length > 0) {
				int read = stream.Read (buffer, 0, System.Math.Min (buffer.Length, length));
				dest_stream.Write (buffer, 0, read);
				length -= read;
			}
		}

		public static byte [] ComputeHash (string file)
		{
			if (!File.Exists (file))
				return Empty<byte>.Array;

			const int buffer_size = 8192;

			var sha1 = new SHA1Managed ();

			using (var stream = new FileStream (file, FileMode.Open, FileAccess.Read, FileShare.Read)) {

				var buffer = new byte [buffer_size];

				using (var crypto_stream = new CryptoStream (Stream.Null, sha1, CryptoStreamMode.Write))
					CopyStreamChunk (stream, crypto_stream, buffer, (int) stream.Length);
			}

			return sha1.Hash;
		}
	}

#if !SILVERLIGHT && !CF
	static partial class Mixin {

		public static RSA CreateRSA (this StrongNameKeyPair key_pair)
		{
			byte [] key;
			string key_container;

			if (!TryGetKeyContainer (key_pair, out key, out key_container))
				return CryptoConvert.FromCapiKeyBlob (key);

			var parameters = new CspParameters {
				Flags = CspProviderFlags.UseMachineKeyStore,
				KeyContainerName = key_container,
				KeyNumber = 2,
			};

			return new RSACryptoServiceProvider (parameters);
		}

		static bool TryGetKeyContainer (ISerializable key_pair, out byte [] key, out string key_container)
		{
			var info = new SerializationInfo (typeof (StrongNameKeyPair), new FormatterConverter ());
			key_pair.GetObjectData (info, new StreamingContext ());

			key = (byte []) info.GetValue ("_keyPairArray", typeof (byte []));
			key_container = info.GetString ("_keyPairContainer");
			return key_container != null;
		}
	}
#endif
}

#endif
