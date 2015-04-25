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

namespace ICSharpCode.NRefactory.Xml
{
	internal abstract class InternalObject
	{
		public int StartRelativeToParent;
		public int Length;
		/// <summary>Length that was touched to parsed this object.</summary>
		public int LengthTouched;
		public InternalSyntaxError[] SyntaxErrors;
		public InternalObject[] NestedObjects;
		
		public InternalObject SetStartRelativeToParent(int newStartRelativeToParent)
		{
			if (newStartRelativeToParent == StartRelativeToParent)
				return this;
			InternalObject obj = (InternalObject)MemberwiseClone();
			obj.StartRelativeToParent = newStartRelativeToParent;
			return obj;
		}
		
		public abstract AXmlObject CreatePublicObject(AXmlObject parent, int parentStartOffset);
	}
	
	sealed class InternalDocument : InternalObject
	{
		public override AXmlObject CreatePublicObject(AXmlObject parent, int parentStartOffset)
		{
			return new AXmlDocument(parent, (parent != null ? parentStartOffset + StartRelativeToParent : parentStartOffset), this);
		}
	}
	
	sealed class InternalText : InternalObject
	{
		public TextType Type;
		public bool ContainsOnlyWhitespace;
		public string Value;
		
		public override AXmlObject CreatePublicObject(AXmlObject parent, int parentStartOffset)
		{
			return new AXmlText(parent, (parent != null ? parentStartOffset + StartRelativeToParent : parentStartOffset), this);
		}
		
		public override string ToString()
		{
			return "Text: " + this.Value.Replace("\n", "\\n").Replace("\r", "\\r");
		}
	}
	
	sealed class InternalTag : InternalObject
	{
		public string OpeningBracket;
		public int RelativeNameStart;
		public string Name;
		public string ClosingBracket;
		
		/// <summary> True if tag starts with "&lt;" </summary>
		public bool IsStartOrEmptyTag       { get { return OpeningBracket == "<"; } }
		/// <summary> True if tag starts with "&lt;" and ends with "&gt;" </summary>
		public bool IsStartTag              { get { return OpeningBracket == "<" && ClosingBracket == ">"; } }
		/// <summary> True if tag starts with "&lt;" and does not end with "&gt;" </summary>
		public bool IsEmptyTag              { get { return OpeningBracket == "<" && ClosingBracket != ">" ; } }
		/// <summary> True if tag starts with "&lt;/" </summary>
		public bool IsEndTag                { get { return OpeningBracket == "</"; } }
		/// <summary> True if tag starts with "&lt;?" </summary>
		public bool IsProcessingInstruction { get { return OpeningBracket == "<?"; } }
		/// <summary> True if tag starts with "&lt;!--" </summary>
		public bool IsComment               { get { return OpeningBracket == "<!--"; } }
		/// <summary> True if tag starts with "&lt;![CDATA[" </summary>
		public bool IsCData                 { get { return OpeningBracket == "<![CDATA["; } }
		/// <summary> True if tag starts with one of the DTD starts </summary>
		public bool IsDocumentType          { get { return AXmlTag.DtdNames.Contains(OpeningBracket); } }
		/// <summary> True if tag starts with "&lt;!" </summary>
		public bool IsUnknownBang           { get { return OpeningBracket == "<!"; } }
		
		public override AXmlObject CreatePublicObject(AXmlObject parent, int parentStartOffset)
		{
			return new AXmlTag(parent, (parent != null ? parentStartOffset + StartRelativeToParent : parentStartOffset), this);
		}
		
		public override string ToString()
		{
			return "Tag: " + OpeningBracket + Name + ClosingBracket;
		}
		
		public InternalTag AddSyntaxError(string description)
		{
			if (this.SyntaxErrors != null && this.SyntaxErrors.Length > 0)
				return this; // don't add error if there already is one
			InternalTag tag = (InternalTag)MemberwiseClone();
			tag.SyntaxErrors = new InternalSyntaxError[] { new InternalSyntaxError(0, Length, description) };
			return tag;
		}
	}
	
	struct InternalSyntaxError
	{
		public readonly int RelativeStart;
		public readonly int RelativeEnd;
		public readonly string Description;
		
		public InternalSyntaxError(int relativeStart, int relativeEnd, string description)
		{
			this.RelativeStart = relativeStart;
			this.RelativeEnd = relativeEnd;
			this.Description = description;
		}
	}
	
	sealed class InternalAttribute : InternalObject
	{
		public string Name;
		public int EqualsSignLength; // length of equals sign including the surrounding whitespace
		public string Value;
		
		public override AXmlObject CreatePublicObject(AXmlObject parent, int parentStartOffset)
		{
			return new AXmlAttribute(parent, (parent != null ? parentStartOffset + StartRelativeToParent : parentStartOffset), this);
		}
		
		public override string ToString()
		{
			return "Attribute: " + Name + "='" + Value + "'";
		}
	}
	
	sealed class InternalElement : InternalObject
	{
		public bool HasEndTag;
		public bool IsPropertyNested;
		public readonly string Name;
		
		public InternalElement(InternalTag tag)
		{
			this.Name = tag.Name;
		}
		
		public string Prefix {
			get { return AXmlObject.GetNamespacePrefix(Name); }
		}
		
		public string LocalName {
			get { return AXmlObject.GetLocalName(Name); }
		}
		
		public override AXmlObject CreatePublicObject(AXmlObject parent, int parentStartOffset)
		{
			return new AXmlElement(parent, (parent != null ? parentStartOffset + StartRelativeToParent : parentStartOffset), this);
		}
		
		public override string ToString()
		{
			return "Element: " + NestedObjects[0].ToString();
		}
	}
}
