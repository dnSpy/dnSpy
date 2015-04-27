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
using System.Linq;
using System.Xml;
using ICSharpCode.NRefactory.Editor;
using ICSharpCode.NRefactory.Utils;

namespace ICSharpCode.NRefactory.Xml
{
	/// <summary>
	/// XML object. Base class for all nodes in the XML document.
	/// </summary>
	public abstract class AXmlObject : ISegment
	{
		/// <summary> Empty string.  The namespace used if there is no "xmlns" specified </summary>
		internal static readonly string NoNamespace = string.Empty;
		
		/// <summary> Namespace for "xml:" prefix: "http://www.w3.org/XML/1998/namespace" </summary>
		public static readonly string XmlNamespace = "http://www.w3.org/XML/1998/namespace";
		
		/// <summary> Namesapce for "xmlns:" prefix: "http://www.w3.org/2000/xmlns/" </summary>
		public static readonly string XmlnsNamespace = "http://www.w3.org/2000/xmlns/";
		
		readonly AXmlObject parent;
		internal readonly int startOffset;
		internal readonly InternalObject internalObject;
		IList<AXmlObject> children;
		
		internal AXmlObject(AXmlObject parent, int startOffset, InternalObject internalObject)
		{
			this.parent = parent;
			this.startOffset = startOffset;
			this.internalObject = internalObject;
		}
		
		/// <summary>
		/// Creates an XML reader that reads from this document.
		/// </summary>
		/// <remarks>
		/// The reader will ignore comments and processing instructions; and will not have line information.
		/// </remarks>
		public XmlReader CreateReader()
		{
			return new AXmlReader(CreateIteratorForReader());
		}
		
		/// <summary>
		/// Creates an XML reader that reads from this document.
		/// </summary>
		/// <param name="settings">Reader settings.
		/// Currently, only <c>IgnoreComments</c> is supported.</param>
		/// <remarks>
		/// The reader will not have line information.
		/// </remarks>
		public XmlReader CreateReader(XmlReaderSettings settings)
		{
			return new AXmlReader(CreateIteratorForReader(), settings);
		}
		
		/// <summary>
		/// Creates an XML reader that reads from this document.
		/// </summary>
		/// <param name="settings">Reader settings.
		/// Currently, only <c>IgnoreComments</c> is supported.</param>
		/// <param name="document">
		/// The document that was used to parse the XML. It is used to convert offsets to line information.
		/// </param>
		public XmlReader CreateReader(XmlReaderSettings settings, IDocument document)
		{
			if (document == null)
				throw new ArgumentNullException("document");
			return new AXmlReader(CreateIteratorForReader(), settings, document.GetLocation);
		}
		
		/// <summary>
		/// Creates an XML reader that reads from this document.
		/// </summary>
		/// <param name="settings">Reader settings.
		/// Currently, only <c>IgnoreComments</c> is supported.</param>
		/// <param name="offsetToTextLocation">
		/// A function for converting offsets to line information.
		/// </param>
		public XmlReader CreateReader(XmlReaderSettings settings, Func<int, TextLocation> offsetToTextLocation)
		{
			return new AXmlReader(CreateIteratorForReader(), settings, offsetToTextLocation);
		}
		
		internal virtual ObjectIterator CreateIteratorForReader()
		{
			return new ObjectIterator(new[] { internalObject }, startOffset);
		}
		
		/// <summary>
		/// Gets the parent node.
		/// </summary>
		public AXmlObject Parent {
			get { return parent; }
		}
		
		/// <summary>
		/// Gets the list of child objects.
		/// </summary>
		public IList<AXmlObject> Children {
			get {
				var result = LazyInit.VolatileRead(ref this.children);
				if (result != null) {
					return result;
				} else {
					if (internalObject.NestedObjects != null) {
						var array = new AXmlObject[internalObject.NestedObjects.Length];
						for (int i = 0; i < array.Length; i++) {
							array[i] = internalObject.NestedObjects[i].CreatePublicObject(this, startOffset);
						}
						result = Array.AsReadOnly(array);
					} else {
						result = EmptyList<AXmlObject>.Instance;
					}
					return LazyInit.GetOrSet(ref this.children, result);
				}
			}
		}
		
		/// <summary>
		/// Gets a child fully containg the given offset.
		/// Goes recursively down the tree.
		/// Special case if at the end of attribute or text
		/// </summary>
		public AXmlObject GetChildAtOffset(int offset)
		{
			foreach(AXmlObject child in this.Children) {
				if (offset == child.EndOffset && (child is AXmlAttribute || child is AXmlText))
					return child;
				if (child.StartOffset < offset && offset < child.EndOffset) {
					return child.GetChildAtOffset(offset);
				}
			}
			return this; // No children at offset
		}
		
		/// <summary>
		/// The error that occured in the context of this node (excluding nested nodes)
		/// </summary>
		public IEnumerable<SyntaxError> MySyntaxErrors {
			get {
				if (internalObject.SyntaxErrors != null) {
					return internalObject.SyntaxErrors.Select(e => new SyntaxError(startOffset + e.RelativeStart, startOffset + e.RelativeEnd, e.Description));
				} else {
					return EmptyList<SyntaxError>.Instance;
				}
			}
		}
		
		/// <summary>
		/// The error that occured in the context of this node and all nested nodes.
		/// It has O(n) cost.
		/// </summary>
		public IEnumerable<SyntaxError> SyntaxErrors {
			get {
				return TreeTraversal.PreOrder(this, n => n.Children).SelectMany(obj => obj.MySyntaxErrors);
			}
		}
		
		/// <summary> Get all ancestors of this node </summary>
		public IEnumerable<AXmlObject> Ancestors {
			get {
				AXmlObject curr = this.Parent;
				while(curr != null) {
					yield return curr;
					curr = curr.Parent;
				}
			}
		}
		
		#region Helper methods
		
		/// <summary> The part of name before ":" </summary>
		/// <returns> Empty string if not found </returns>
		internal static string GetNamespacePrefix(string name)
		{
			if (string.IsNullOrEmpty(name)) return string.Empty;
			int colonIndex = name.IndexOf(':');
			if (colonIndex != -1) {
				return name.Substring(0, colonIndex);
			} else {
				return string.Empty;
			}
		}
		
		/// <summary> The part of name after ":" </summary>
		/// <returns> Whole name if ":" not found </returns>
		internal static string GetLocalName(string name)
		{
			if (string.IsNullOrEmpty(name)) return string.Empty;
			int colonIndex = name.IndexOf(':');
			if (colonIndex != -1) {
				return name.Remove(0, colonIndex + 1);
			} else {
				return name ?? string.Empty;
			}
		}
		
		#endregion
		
		/// <summary> Call appropriate visit method on the given visitor </summary>
		public abstract void AcceptVisitor(AXmlVisitor visitor);
		
		/// <summary>
		/// Gets the start offset of the segment.
		/// </summary>
		public int StartOffset {
			get { return startOffset; }
		}
		
		int ISegment.Offset {
			get { return startOffset; }
		}
		
		/// <inheritdoc/>
		public int Length {
			get { return internalObject.Length; }
		}
		
		/// <inheritdoc/>
		public int EndOffset {
			get { return startOffset + internalObject.Length; }
		}
	}
}
