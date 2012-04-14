// Copyright (c) AlphaSierraPapa for the SharpDevelop Team
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

namespace ICSharpCode.NRefactory.Xml
{
	/// <summary>
	/// XmlReader implementation that reads from an <see cref="AXmlDocument"/>.
	/// </summary>
	sealed class AXmlReader : XmlReader, IXmlLineInfo
	{
		readonly ObjectIterator objectIterator;
		readonly Func<int, TextLocation> offsetToTextLocation;
		readonly XmlNameTable nameTable = new NameTable();
		ReadState readState = ReadState.Initial;
		XmlNodeType elementNodeType = XmlNodeType.None;
		IList<InternalAttribute> attributes;
		int attributeIndex = -1;
		bool inAttributeValue;
		
		internal AXmlReader(InternalObject[] objects, int startPosition = 0, Func<int, TextLocation> offsetToTextLocation = null)
		{
			this.offsetToTextLocation = offsetToTextLocation;
			objectIterator = new ObjectIterator(objects, startPosition);
			objectIterator.StopAtElementEnd = true;
		}
		
		public override void ResolveEntity()
		{
			throw new NotSupportedException();
		}
		
		public override ReadState ReadState {
			get { return readState; }
		}
		
		public override bool ReadAttributeValue()
		{
			if (attributeIndex >= 0 && !inAttributeValue) {
				inAttributeValue = true;
				return true;
			}
			return false;
		}
		
		public override bool Read()
		{
			switch (readState) {
				case ReadState.Initial:
					readState = ReadState.Interactive;
					return ReadCurrentPosition();
				case ReadState.Interactive:
					objectIterator.MoveInto();
					return ReadCurrentPosition();
				default:
					return false;
			}
		}
		
		bool ReadCurrentPosition()
		{
			attributes = null;
			attributeIndex = -1;
			inAttributeValue = false;
			while (true) {
				var obj = objectIterator.CurrentObject;
				if (obj == null) {
					readState = ReadState.EndOfFile;
					elementNodeType = XmlNodeType.None;
					return false;
				} else if (objectIterator.IsAtElementEnd) {
					elementNodeType = XmlNodeType.EndElement;
					return true;
				} else if (obj is InternalElement) {
					// element start
					elementNodeType = XmlNodeType.Element;
					InternalTag startTag = ((InternalTag)obj.NestedObjects[0]);
					if (startTag.NestedObjects != null)
						attributes = startTag.NestedObjects.OfType<InternalAttribute>().ToList();
					return true;
				} else if (obj is InternalText) {
					InternalText text = (InternalText)obj;
					if (text.ContainsOnlyWhitespace) {
						elementNodeType = XmlNodeType.Whitespace;
					} else {
						elementNodeType = XmlNodeType.Text;
					}
					return true;
				} else if (obj is InternalTag) {
					// start/end tags can be skipped as the parent InternalElement already handles them,
					// TODO all other tags (xml decl, comments, ...)
				} else {
					throw new NotSupportedException();
				}
				objectIterator.MoveInto();
			}
		}
		
		public override void Skip()
		{
			if (readState == ReadState.Interactive) {
				MoveToElement();
				objectIterator.MoveNext();
				ReadCurrentPosition();
			}
		}
		
		public override string Prefix {
			get {
				if (readState != ReadState.Interactive)
					return string.Empty;
				if (attributeIndex >= 0) {
					if (inAttributeValue)
						return string.Empty;
					return nameTable.Add(AXmlObject.GetNamespacePrefix(attributes[attributeIndex].Name));
				}
				InternalElement element = objectIterator.CurrentObject as InternalElement;
				return element != null ? nameTable.Add(element.Prefix) : string.Empty;
			}
		}
		
		public override string NamespaceURI {
			get {
				if (readState != ReadState.Interactive)
					return string.Empty;
				return LookupNamespace(this.Prefix);
			}
		}
		
		public override string LocalName {
			get {
				if (readState != ReadState.Interactive)
					return string.Empty;
				if (attributeIndex >= 0) {
					if (inAttributeValue)
						return string.Empty;
					return nameTable.Add(AXmlObject.GetLocalName(attributes[attributeIndex].Name));
				}
				InternalElement element = objectIterator.CurrentObject as InternalElement;
				return element != null ? nameTable.Add(element.LocalName) : string.Empty;
			}
		}
		
		public override bool IsEmptyElement {
			get {
				if (readState != ReadState.Interactive)
					return false;
				InternalElement element = objectIterator.CurrentObject as InternalElement;
				return element != null && element.NestedObjects.Length == 1;
			}
		}
		
		public override string Value {
			get {
				if (readState != ReadState.Interactive)
					return string.Empty;
				if (attributeIndex >= 0)
					return attributes[attributeIndex].Value;
				InternalText text = objectIterator.CurrentObject as InternalText;
				return text != null ? text.Value : string.Empty;
			}
		}
		
		public override bool HasValue {
			get {
				return !string.IsNullOrEmpty (Value);
			}
		}
		
		public override XmlNodeType NodeType {
			get {
				if (attributeIndex >= 0)
					return inAttributeValue ? XmlNodeType.Text : XmlNodeType.Attribute;
				else
					return elementNodeType;
			}
		}
		
		public override XmlNameTable NameTable {
			get { return nameTable; }
		}
		
		public override bool MoveToFirstAttribute()
		{
			return DoMoveToAttribute(0);
		}
		
		public override bool MoveToNextAttribute()
		{
			return DoMoveToAttribute(attributeIndex + 1);
		}
		
		public override void MoveToAttribute(int i)
		{
			if (!DoMoveToAttribute(i))
				throw new ArgumentOutOfRangeException("i");
		}
		
		bool DoMoveToAttribute(int i)
		{
			if (i >= 0 && i < this.AttributeCount) {
				attributeIndex = i;
				inAttributeValue = false;
				return true;
			}
			return false;
		}
		
		public override bool MoveToElement()
		{
			if (attributeIndex >= 0) {
				attributeIndex = -1;
				inAttributeValue = false;
				return true;
			}
			return false;
		}
		
		int GetAttributeIndex(string name)
		{
			if (attributes == null)
				return -1;
			for (int i = 0; i < attributes.Count; i++) {
				if (attributes[i].Name == name)
					return i;
			}
			return -1;
		}
		
		int GetAttributeIndex(string name, string ns)
		{
			if (attributes == null)
				return -1;
			for (int i = 0; i < attributes.Count; i++) {
				if (AXmlObject.GetLocalName(attributes[i].Name) == name && LookupNamespace(AXmlObject.GetNamespacePrefix(attributes[i].Name)) == ns)
					return i;
			}
			return -1;
		}
		
		public override bool MoveToAttribute(string name, string ns)
		{
			return DoMoveToAttribute(GetAttributeIndex(name, ns));
		}
		
		public override bool MoveToAttribute(string name)
		{
			return DoMoveToAttribute(GetAttributeIndex(name));
		}
		
		public override string LookupNamespace(string prefix)
		{
			return string.Empty; // TODO implement namespace lookup
		}
		
		public override string GetAttribute(int i)
		{
			if (attributes == null || i < 0 || i >= attributes.Count)
				return null;
			return attributes[i].Value;
		}
		
		public override string GetAttribute(string name, string namespaceURI)
		{
			return GetAttribute(GetAttributeIndex(name, namespaceURI));
		}
		
		public override string GetAttribute(string name)
		{
			return GetAttribute(GetAttributeIndex(name));
		}
		
		public override bool EOF {
			get { return readState == ReadState.EndOfFile; }
		}
		
		public override int Depth {
			get { return objectIterator.Depth; }
		}
		
		public override void Close()
		{
			readState = ReadState.Closed;
		}
		
		public override string BaseURI {
			get { return string.Empty; }
		}
		
		public override int AttributeCount {
			get { return attributes != null ? attributes.Count : 0; }
		}
		
		int CurrentPosition {
			get {
				if (attributeIndex < 0)
					return objectIterator.CurrentPosition;
				else
					return objectIterator.CurrentPosition + attributes[attributeIndex].StartRelativeToParent;
			}
		}
		
		public int LineNumber {
			get {
				if (offsetToTextLocation != null)
					return offsetToTextLocation(CurrentPosition).Line;
				else
					return 0;
			}
		}
		
		public int LinePosition {
			get {
				if (offsetToTextLocation != null)
					return offsetToTextLocation(CurrentPosition).Column - 1;
				else
					return 0;
			}
		}
		
		bool IXmlLineInfo.HasLineInfo()
		{
			return offsetToTextLocation != null;
		}
	}
}
