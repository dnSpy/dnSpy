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
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml;

namespace ICSharpCode.NRefactory.Xml
{
	/// <summary>
	/// XML element.
	/// </summary>
	public class AXmlElement : AXmlObject, IXmlNamespaceResolver
	{
		internal AXmlElement(AXmlObject parent, int startOffset, InternalElement internalObject)
			: base(parent, startOffset, internalObject)
		{
			Log.Assert(internalObject.NestedObjects[0] is InternalTag, "First child of element must be start tag");
		}
		
		/// <summary> No tags are missing anywhere within this element (recursive) </summary>
		public bool IsProperlyNested {
			get { return ((InternalElement)internalObject).IsPropertyNested; }
		}
		
		/// <summary>The start or empty-element tag for this element.</summary>
		public AXmlTag StartTag {
			get { return (AXmlTag)this.Children[0]; }
		}
		
		/// <summary>Name with namespace prefix - exactly as in source</summary>
		public string Name {
			get { return ((InternalTag)internalObject.NestedObjects[0]).Name; }
		}
		
		/// <summary>Gets whether an end tag exists for this node.</summary>
		public bool HasEndTag {
			get { return ((InternalElement)internalObject).HasEndTag; }
		}
		
		/// <summary> The end tag, if there is any. Returns null for empty elements "&lt;Element/>" and missing end tags in malformed XML.</summary>
		public AXmlTag EndTag {
			get {
				if (HasEndTag)
					return (AXmlTag)this.Children[this.Children.Count - 1];
				else
					return null;
			}
		}
		
		/// <summary>
		/// Gets the attributes.
		/// </summary>
		public IEnumerable<AXmlAttribute> Attributes {
			get {
				return ((AXmlTag)this.Children[0]).Children.OfType<AXmlAttribute>();
			}
		}
		
		/// <summary>
		/// Gets the content (all children except for the start and end tags)
		/// </summary>
		public IEnumerable<AXmlObject> Content {
			get {
				int end = this.Children.Count;
				if (HasEndTag)
					end--;
				for (int i = 1; i < end; i++) {
					yield return this.Children[i];
				}
			}
		}
		
		/// <summary> The part of name before ":" </summary>
		/// <returns> Empty string if not found </returns>
		public string Prefix {
			get { return ((InternalElement)internalObject).Prefix; }
		}
		
		/// <summary> The part of name after ":" </summary>
		/// <returns> Empty string if not found </returns>
		public string LocalName {
			get { return ((InternalElement)internalObject).LocalName; }
		}
		
		/// <summary> Resolved namespace of the name </summary>
		/// <returns> Empty string if prefix is not found </returns>
		public string Namespace {
			get {
				string prefix = this.Prefix;
				return LookupNamespace(prefix);
			}
		}
		
		/// <summary> Find the default namespace for this context </summary>
		[Obsolete("Use LookupNamespace(string.Empty) instead")]
		public string FindDefaultNamespace()
		{
			return LookupNamespace(string.Empty) ?? NoNamespace;
		}
		
		/// <summary>
		/// Recursively resolve given prefix in this context.  Prefix must have some value.
		/// </summary>
		/// <returns> Empty string if prefix is not found </returns>
		[Obsolete("Use LookupNamespace() instead")]
		public string ResolvePrefix(string prefix)
		{
			return LookupNamespace(prefix) ?? NoNamespace;
		}
		
		/// <summary>
		/// Recursively resolve given prefix in this context.
		/// </summary>
		/// <returns><c>null</c> if prefix is not found</returns>
		public string LookupNamespace(string prefix)
		{
			if (prefix == null)
				throw new ArgumentNullException("prefix");
			
			// Implicit namespaces
			if (prefix == "xml") return XmlNamespace;
			if (prefix == "xmlns") return XmlnsNamespace;
			
			string lookFor = (prefix.Length > 0 ? "xmlns:" + prefix : "xmlns");
			for (AXmlElement current = this; current != null; current = current.Parent as AXmlElement) {
				foreach (var attr in current.Attributes) {
					if (attr.Name == lookFor)
						return attr.Value;
				}
			}
			return null; // Can not find prefix
		}
		
		/// <summary>
		/// Gets the prefix that is mapped to the specified namespace URI.
		/// </summary>
		/// <returns>The prefix that is mapped to the namespace URI; null if the namespace URI is not mapped to a prefix.</returns>
		public string LookupPrefix(string namespaceName)
		{
			if (namespaceName == null)
				throw new ArgumentNullException("namespaceName");
			
			if (namespaceName == XmlNamespace)
				return "xml";
			if (namespaceName == XmlnsNamespace)
				return "xmlns";
			for (AXmlElement current = this; current != null; current = current.Parent as AXmlElement) {
				foreach (var attr in current.Attributes) {
					if (attr.Value == namespaceName) {
						if (attr.Name.StartsWith("xmlns:", StringComparison.Ordinal))
							return attr.LocalName;
						else if (attr.Name == "xmlns")
							return string.Empty;
					}
				}
			}
			return null; // Can not find prefix
		}
		
		/// <summary>
		/// Gets a collection of defined prefix-namespace mappings that are currently in scope.
		/// </summary>
		public IDictionary<string, string> GetNamespacesInScope(XmlNamespaceScope scope)
		{
			var result = new Dictionary<string, string>();
			if (scope == XmlNamespaceScope.All) {
				result["xml"] = XmlNamespace;
				//result["xmlns"] = XmlnsNamespace; xmlns should not be included in GetNamespacesInScope() results
			}
			for (AXmlElement current = this; current != null; current = current.Parent as AXmlElement) {
				foreach (var attr in current.Attributes) {
					if (attr.Name.StartsWith("xmlns:", StringComparison.Ordinal)) {
						string prefix = attr.LocalName;
						if (!result.ContainsKey(prefix)) {
							result.Add(prefix, attr.Value);
						}
					} else if (attr.Name == "xmlns" && !result.ContainsKey(string.Empty)) {
						result.Add(string.Empty, attr.Value);
					}
				}
				if (scope == XmlNamespaceScope.Local)
					break;
			}
			return result;
		}
		
		/// <summary>
		/// Get unquoted value of attribute.
		/// It looks in the no namespace (empty string).
		/// </summary>
		/// <returns>Null if not found</returns>
		public string GetAttributeValue(string localName)
		{
			return GetAttributeValue(string.Empty, localName);
		}
		
		/// <summary>
		/// Get unquoted value of attribute
		/// </summary>
		/// <param name="namespace">Namespace.  Can be no namepace (empty string), which is the default for attributes.</param>
		/// <param name="localName">Local name - text after ":"</param>
		/// <returns>Null if not found</returns>
		public string GetAttributeValue(string @namespace, string localName)
		{
			@namespace = @namespace ?? string.Empty;
			foreach (AXmlAttribute attr in this.Attributes) {
				if (attr.LocalName == localName && attr.Namespace == @namespace)
					return attr.Value;
			}
			return null;
		}
		
		/// <inheritdoc/>
		public override void AcceptVisitor(AXmlVisitor visitor)
		{
			visitor.VisitElement(this);
		}
		
		/// <inheritdoc/>
		public override string ToString()
		{
			return string.Format(CultureInfo.InvariantCulture, "[{0} '{1}' Attr:{2} Chld:{3} Nest:{4}]", base.ToString(), this.Name, this.StartTag.Children.Count, this.Children.Count, this.IsProperlyNested ? "Ok" : "Bad");
		}
	}
}
