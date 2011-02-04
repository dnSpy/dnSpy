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
	/// Whitespace or character data
	/// </summary>
	public class AXmlText: AXmlObject
	{
		/// <summary> The context in which the text occured </summary>
		internal TextType Type { get; set; }
		/// <summary> The text exactly as in source </summary>
		public string EscapedValue { get; set; }
		/// <summary> The text with all entity references resloved </summary>
		public string Value { get; set; }
		/// <summary> True if the text contains only whitespace characters </summary>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1702:CompoundWordsShouldBeCasedCorrectly", MessageId = "Whitespace",
		                                                 Justification = "System.Xml also uses 'Whitespace'")]
		public bool ContainsOnlyWhitespace { get; set; }
		
		/// <inheritdoc/>
		public override void AcceptVisitor(IAXmlVisitor visitor)
		{
			visitor.VisitText(this);
		}
		
		/// <inheritdoc/>
		internal override bool UpdateDataFrom(AXmlObject source)
		{
			if (!base.UpdateDataFrom(source)) return false;
			AXmlText src = (AXmlText)source;
			if (this.EscapedValue != src.EscapedValue ||
			    this.Value != src.Value)
			{
				OnChanging();
				this.EscapedValue = src.EscapedValue;
				this.Value = src.Value;
				OnChanged();
				return true;
			} else {
				return false;
			}
		}
		
		/// <inheritdoc/>
		public override string ToString()
		{
			return string.Format(CultureInfo.InvariantCulture, "[{0} Text.Length={1}]", base.ToString(), this.EscapedValue.Length);
		}
	}
}
