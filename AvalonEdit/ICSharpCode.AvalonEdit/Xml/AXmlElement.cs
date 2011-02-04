// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Globalization;
using System.Linq;

using ICSharpCode.AvalonEdit.Document;

namespace ICSharpCode.AvalonEdit.Xml
{
	/// <summary>
	/// Logical grouping of other nodes together.
	/// </summary>
	public class AXmlElement: AXmlContainer
	{
		/// <summary> No tags are missing anywhere within this element (recursive) </summary>
		public bool IsProperlyNested { get; set; }
		/// <returns> True in wellformed XML </returns>
		public bool HasStartOrEmptyTag { get; set; }
		/// <returns> True in wellformed XML </returns>
		public bool HasEndTag { get; set; }
		
		/// <inheritdoc/>
		internal override bool UpdateDataFrom(AXmlObject source)
		{
			if (!base.UpdateDataFrom(source)) return false;
			AXmlElement src = (AXmlElement)source;
			// Clear the cache for this - quite expensive
			attributesAndElements = null;
			if (this.IsProperlyNested != src.IsProperlyNested ||
				this.HasStartOrEmptyTag != src.HasStartOrEmptyTag ||
				this.HasEndTag != src.HasEndTag)
			{
				OnChanging();
				this.IsProperlyNested = src.IsProperlyNested;
				this.HasStartOrEmptyTag = src.HasStartOrEmptyTag;
				this.HasEndTag = src.HasEndTag;
				OnChanged();
				return true;
			} else {
				return false;
			}
		}
		
		/// <summary> The start or empty-element tag if there is any </summary>
		internal AXmlTag StartTag {
			get {
				Assert(HasStartOrEmptyTag, "Does not have a start tag");
				return (AXmlTag)this.Children[0];
			}
		}
		
		/// <summary> The end tag if there is any </summary>
		internal AXmlTag EndTag {
			get {
				Assert(HasEndTag, "Does not have an end tag");
				return (AXmlTag)this.Children[this.Children.Count - 1];
			}
		}
		
		internal override void DebugCheckConsistency(bool checkParentPointers)
		{
			DebugAssert(Children.Count > 0, "No children");
			base.DebugCheckConsistency(checkParentPointers);
		}
		
		#region Helpper methods
		
		/// <summary> Gets attributes of the element </summary>
		/// <remarks>
		/// Warning: this is a cenvenience method to access the attributes of the start tag.
		/// However, since the start tag might be moved/replaced, this property might return 
		/// different values over time.
		/// </remarks>
		public AXmlAttributeCollection Attributes {
			get {
				if (this.HasStartOrEmptyTag) {
					return this.StartTag.Attributes;
				} else {
					return AXmlAttributeCollection.Empty;
				}
			}
		}
		
		ObservableCollection<AXmlObject> attributesAndElements;
		
		/// <summary> Gets both attributes and elements.  Expensive, avoid use. </summary>
		/// <remarks> Warning: the collection will regenerate after each update </remarks>
		public ObservableCollection<AXmlObject> AttributesAndElements {
			get {
				if (attributesAndElements == null) {
					if (this.HasStartOrEmptyTag) {
						attributesAndElements = new MergedCollection<AXmlObject, ObservableCollection<AXmlObject>> (
							// New wrapper with RawObject types
							new FilteredCollection<AXmlObject, AXmlObjectCollection<AXmlObject>>(this.StartTag.Children, x => x is AXmlAttribute),
							new FilteredCollection<AXmlObject, AXmlObjectCollection<AXmlObject>>(this.Children, x => x is AXmlElement)
						);
					} else {
						attributesAndElements = new FilteredCollection<AXmlObject, AXmlObjectCollection<AXmlObject>>(this.Children, x => x is AXmlElement);
					}
				}
				return attributesAndElements;
			}
		}
		
		/// <summary> Name with namespace prefix - exactly as in source </summary>
		public string Name {
			get {
				if (this.HasStartOrEmptyTag) {
					return this.StartTag.Name;
				} else {
					return this.EndTag.Name;
				}
			}
		}
		
		/// <summary> The part of name before ":" </summary>
		/// <returns> Empty string if not found </returns>
		public string Prefix {
			get {
				return GetNamespacePrefix(this.Name);
			}
		}
		
		/// <summary> The part of name after ":" </summary>
		/// <returns> Empty string if not found </returns>
		public string LocalName {
			get {
				return GetLocalName(this.Name);
			}
		}
		
		/// <summary> Resolved namespace of the name </summary>
		/// <returns> Empty string if prefix is not found </returns>
		public string Namespace {
			get {
				string prefix = this.Prefix;
				if (string.IsNullOrEmpty(prefix)) {
					return FindDefaultNamespace();
				} else {
					return ResolvePrefix(prefix);
				}
			}
		}
		
		/// <summary> Find the defualt namespace for this context </summary>
		public string FindDefaultNamespace()
		{
			AXmlElement current = this;
			while(current != null) {
				string namesapce = current.GetAttributeValue(NoNamespace, "xmlns");
				if (namesapce != null) return namesapce;
				current = current.Parent as AXmlElement;
			}
			return string.Empty; // No namesapce
		}
		
		/// <summary>
		/// Recursively resolve given prefix in this context.  Prefix must have some value.
		/// </summary>
		/// <returns> Empty string if prefix is not found </returns>
		public string ResolvePrefix(string prefix)
		{
			if (string.IsNullOrEmpty(prefix)) throw new ArgumentException("No prefix given", "prefix");
			
			// Implicit namesapces
			if (prefix == "xml") return XmlNamespace;
			if (prefix == "xmlns") return XmlnsNamespace;
			
			AXmlElement current = this;
			while(current != null) {
				string namesapce = current.GetAttributeValue(XmlnsNamespace, prefix);
				if (namesapce != null) return namesapce;
				current = current.Parent as AXmlElement;
			}
			return NoNamespace; // Can not find prefix
		}
		
		/// <summary>
		/// Get unquoted value of attribute.
		/// It looks in the no namespace (empty string).
		/// </summary>
		/// <returns>Null if not found</returns>
		public string GetAttributeValue(string localName)
		{
			return GetAttributeValue(NoNamespace, localName);
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
			foreach(AXmlAttribute attr in this.Attributes.GetByLocalName(localName)) {
				DebugAssert(attr.LocalName == localName, "Bad hashtable");
				if (attr.Namespace == @namespace) {
					return attr.Value;
				}
			}
			return null;
		}
		
		#endregion
		
		/// <inheritdoc/>
		public override void AcceptVisitor(IAXmlVisitor visitor)
		{
			visitor.VisitElement(this);
		}
		
		/// <inheritdoc/>
		public override string ToString()
		{
			return string.Format(CultureInfo.InvariantCulture, "[{0} '{1}' Attr:{2} Chld:{3} Nest:{4}]", base.ToString(), this.Name, this.HasStartOrEmptyTag ? this.StartTag.Children.Count : 0, this.Children.Count, this.IsProperlyNested ? "Ok" : "Bad");
		}
	}
}
