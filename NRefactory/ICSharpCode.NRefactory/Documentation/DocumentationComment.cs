// Copyright (c) 2010-2013 AlphaSierraPapa for the SharpDevelop Team
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this
// software and associated documentation files (the "Software"), to deal in the Software
// without restriction, including without limitation the rights to use, copy, modify, merge,
// publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons
// to whom the Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or
// substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
// PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE
// FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
// OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.

using System;
using ICSharpCode.NRefactory.Editor;
using ICSharpCode.NRefactory.TypeSystem;

namespace ICSharpCode.NRefactory.Documentation
{
	/// <summary>
	/// Represents a documentation comment.
	/// </summary>
	public class DocumentationComment
	{
		ITextSource xml;
		protected readonly ITypeResolveContext context;
		
		/// <summary>
		/// Gets the XML code for this documentation comment.
		/// </summary>
		public ITextSource Xml {
			get { return xml; }
		}
		
		/// <summary>
		/// Creates a new DocumentationComment.
		/// </summary>
		/// <param name="xml">The XML text.</param>
		/// <param name="context">Context for resolving cref attributes.</param>
		public DocumentationComment(ITextSource xml, ITypeResolveContext context)
		{
			if (xml == null)
				throw new ArgumentNullException("xml");
			if (context == null)
				throw new ArgumentNullException("context");
			this.xml = xml;
			this.context = context;
		}
		
		/// <summary>
		/// Creates a new DocumentationComment.
		/// </summary>
		/// <param name="xml">The XML text.</param>
		/// <param name="context">Context for resolving cref attributes.</param>
		public DocumentationComment(string xml, ITypeResolveContext context)
		{
			if (xml == null)
				throw new ArgumentNullException("xml");
			if (context == null)
				throw new ArgumentNullException("context");
			this.xml = new StringTextSource(xml);
			this.context = context;
		}
		
		/// <summary>
		/// Resolves the given cref value to an entity.
		/// Returns null if the entity is not found, or if the cref attribute is syntactically invalid.
		/// </summary>
		public virtual IEntity ResolveCref(string cref)
		{
			try {
				return IdStringProvider.FindEntity(cref, context);
			} catch (ReflectionNameParseException) {
				return null;
			}
		}
		
		public override string ToString ()
		{
			return Xml.Text;
		}
		
		public static implicit operator string (DocumentationComment documentationComment)
		{
			if (documentationComment != null)
				return documentationComment.ToString ();
			return null;
		}
	}
}
