//
// ImportContext.cs
//
// Author:
//   Jb Evain (jbevain@gmail.com)
//
// (C) 2006 Evaluant RC S.A.
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

	public class ImportContext {

		GenericContext m_genContext;
		IImporter m_importer;

		public GenericContext GenericContext {
			get { return m_genContext; }
		}

		public ImportContext (IImporter importer)
		{
			m_genContext = new GenericContext ();
			m_importer = importer;
		}

		public ImportContext (IImporter importer, IGenericParameterProvider provider)
		{
			m_importer = importer;
			m_genContext = new GenericContext (provider);
		}

		public TypeReference Import (TypeReference type)
		{
			return m_importer.ImportTypeReference (type, this);
		}

		public MethodReference Import (MethodReference meth)
		{
			return m_importer.ImportMethodReference (meth, this);
		}

		public FieldReference Import (FieldReference field)
		{
			return m_importer.ImportFieldReference (field, this);
		}
	}
}
