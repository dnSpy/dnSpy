//
// Document.cs
//
// Author:
//   Jb Evain (jbevain@gmail.com)
//
// (C) 2006 Jb Evain
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

namespace Mono.Cecil.Cil {

	public class Document {

		string m_url;

		DocumentType m_type;
		DocumentHashAlgorithm m_hashAlgorithm;
		DocumentLanguage m_language;
		DocumentLanguageVendor m_languageVendor;

		byte [] m_hash;

		public string Url {
			get { return m_url; }
			set { m_url = value; }
		}

		public DocumentType Type {
			get { return m_type; }
			set { m_type = value; }
		}

		public DocumentHashAlgorithm HashAlgorithm {
			get { return m_hashAlgorithm; }
			set { m_hashAlgorithm = value; }
		}

		public DocumentLanguage Language {
			get { return m_language; }
			set { m_language = value; }
		}

		public DocumentLanguageVendor LanguageVendor {
			get { return m_languageVendor; }
			set { m_languageVendor = value; }
		}

		public byte [] Hash {
			get { return m_hash; }
			set { m_hash = value; }
		}

		public Document (string url)
		{
			m_url = url;
			m_hash = new byte [0];
		}
	}
}
