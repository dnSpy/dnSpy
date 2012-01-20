//
// Document.cs
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

namespace Mono.Cecil.Cil {

	public enum DocumentType {
		Other,
		Text,
	}

	public enum DocumentHashAlgorithm {
		None,
		MD5,
		SHA1,
	}

	public enum DocumentLanguage {
		Other,
		C,
		Cpp,
		CSharp,
		Basic,
		Java,
		Cobol,
		Pascal,
		Cil,
		JScript,
		Smc,
		MCpp,
		FSharp,
	}

	public enum DocumentLanguageVendor {
		Other,
		Microsoft,
	}

	public sealed class Document {

		string url;

		byte type;
		byte hash_algorithm;
		byte language;
		byte language_vendor;

		byte [] hash;

		public string Url {
			get { return url; }
			set { url = value; }
		}

		public DocumentType Type {
			get { return (DocumentType) type; }
			set { type = (byte) value; }
		}

		public DocumentHashAlgorithm HashAlgorithm {
			get { return (DocumentHashAlgorithm) hash_algorithm; }
			set { hash_algorithm = (byte) value; }
		}

		public DocumentLanguage Language {
			get { return (DocumentLanguage) language; }
			set { language = (byte) value; }
		}

		public DocumentLanguageVendor LanguageVendor {
			get { return (DocumentLanguageVendor) language_vendor; }
			set { language_vendor = (byte) value; }
		}

		public byte [] Hash {
			get { return hash; }
			set { hash = value; }
		}

		public Document (string url)
		{
			this.url = url;
			this.hash = Empty<byte>.Array;
		}
	}
}
