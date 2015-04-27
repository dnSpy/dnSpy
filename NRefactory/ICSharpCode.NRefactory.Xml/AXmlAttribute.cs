// Copyright (c) 2009-2013 AlphaSierraPapa for the SharpDevelop Team
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
using System.Globalization;
using ICSharpCode.NRefactory.Editor;

namespace ICSharpCode.NRefactory.Xml
{
	/// <summary>
	/// Name-value pair in a tag
	/// </summary>
	[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1711:IdentifiersShouldNotHaveIncorrectSuffix")]
	public class AXmlAttribute : AXmlObject
	{
		internal AXmlAttribute(AXmlObject parent, int startOffset, InternalAttribute internalObject)
			: base(parent, startOffset, internalObject)
		{
		}
		
		internal InternalAttribute InternalAttribute {
			get { return (InternalAttribute)internalObject; }
		}
		
		/// <summary> Name with namespace prefix - exactly as in source file </summary>
		public string Name { get { return InternalAttribute.Name; } }
		
		/// <summary> Unquoted and dereferenced value of the attribute </summary>
		public string Value { get { return InternalAttribute.Value; } }
		
		/// <summary>Gets the segment for the attribute name</summary>
		public ISegment NameSegment {
			get { return new XmlSegment(startOffset, startOffset + Name.Length); }
		}
		
		/// <summary>Gets the segment for the attribute value, including the quotes</summary>
		public ISegment ValueSegment {
			get { return new XmlSegment(startOffset + Name.Length + InternalAttribute.EqualsSignLength, this.EndOffset); }
		}
		/// <summary> The element containing this attribute </summary>
		/// <returns> Null if orphaned </returns>
		public AXmlElement ParentElement {
			get {
				AXmlTag tag = this.Parent as AXmlTag;
				if (tag != null) {
					return tag.Parent as AXmlElement;
				}
				return null;
			}
		}
		
		/// <summary> The part of name before ":"</summary>
		/// <returns> Empty string if not found </returns>
		public string Prefix {
			get {
				return GetNamespacePrefix(this.Name);
			}
		}
		
		/// <summary> The part of name after ":" </summary>
		/// <returns> Whole name if ":" not found </returns>
		public string LocalName {
			get {
				return GetLocalName(this.Name);
			}
		}
		
		/// <summary>
		/// Resolved namespace of the name.  Empty string if not found
		/// From the specification: "The namespace name for an unprefixed attribute name always has no value."
		/// </summary>
		public string Namespace {
			get {
				if (string.IsNullOrEmpty(this.Prefix)) return NoNamespace;
				
				AXmlElement elem = this.ParentElement;
				if (elem != null) {
					return elem.LookupNamespace(this.Prefix) ?? NoNamespace;
				}
				return NoNamespace; // Orphaned attribute
			}
		}
		
		/// <summary> Attribute is declaring namespace ("xmlns" or "xmlns:*") </summary>
		public bool IsNamespaceDeclaration {
			get {
				return this.Name == "xmlns" || this.Prefix == "xmlns";
			}
		}
		
		/// <inheritdoc/>
		public override void AcceptVisitor(AXmlVisitor visitor)
		{
			visitor.VisitAttribute(this);
		}
		
		/// <inheritdoc/>
		public override string ToString()
		{
			return string.Format(CultureInfo.InvariantCulture, "[{0} '{1}={2}']", base.ToString(), this.Name, this.Value);
		}
	}
}
