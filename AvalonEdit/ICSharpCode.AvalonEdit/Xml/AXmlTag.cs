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
	/// Represents any markup starting with "&lt;" and (hopefully) ending with ">"
	/// </summary>
	public class AXmlTag: AXmlContainer
	{
		/// <summary> These identify the start of DTD elements </summary>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes", Justification="ReadOnlyCollection is immutable")]
		public static readonly ReadOnlyCollection<string> DtdNames = new ReadOnlyCollection<string>(
			new string[] {"<!DOCTYPE", "<!NOTATION", "<!ELEMENT", "<!ATTLIST", "<!ENTITY" } );
		
		/// <summary> Opening bracket - usually "&lt;" </summary>
		public string OpeningBracket { get; internal set; }
		/// <summary> Name following the opening bracket </summary>
		public string Name { get; internal set; }
		/// <summary> Opening bracket - usually "&gt;" </summary>
		public string ClosingBracket { get; internal set; }
		
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
		public bool IsDocumentType          { get { return DtdNames.Contains(OpeningBracket); } }
		/// <summary> True if tag starts with "&lt;!" </summary>
		public bool IsUnknownBang           { get { return OpeningBracket == "<!"; } }
		
		#region Helpper methods
		
		AXmlAttributeCollection attributes;
		
		/// <summary> Gets attributes of the tag (if applicable) </summary>
		public AXmlAttributeCollection Attributes {
			get {
				if (attributes == null) {
					attributes = new AXmlAttributeCollection(this.Children);
				}
				return attributes;
			}
		}
		
		#endregion
		
		internal override void DebugCheckConsistency(bool checkParentPointers)
		{
			Assert(OpeningBracket != null, "Null OpeningBracket");
			Assert(Name != null, "Null Name");
			Assert(ClosingBracket != null, "Null ClosingBracket");
			base.DebugCheckConsistency(checkParentPointers);
		}
		
		/// <inheritdoc/>
		public override void AcceptVisitor(IAXmlVisitor visitor)
		{
			visitor.VisitTag(this);
		}
		
		/// <inheritdoc/>
		internal override bool UpdateDataFrom(AXmlObject source)
		{
			if (!base.UpdateDataFrom(source)) return false;
			AXmlTag src = (AXmlTag)source;
			if (this.OpeningBracket != src.OpeningBracket ||
				this.Name != src.Name ||
				this.ClosingBracket != src.ClosingBracket)
			{
				OnChanging();
				this.OpeningBracket = src.OpeningBracket;
				this.Name = src.Name;
				this.ClosingBracket = src.ClosingBracket;
				OnChanged();
				return true;
			} else {
				return false;
			}
		}
		
		/// <inheritdoc/>
		public override string ToString()
		{
			return string.Format(CultureInfo.InvariantCulture, "[{0} '{1}{2}{3}' Attr:{4}]", base.ToString(), this.OpeningBracket, this.Name, this.ClosingBracket, this.Children.Count);
		}
	}
}
