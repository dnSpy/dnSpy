// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Runtime.Serialization;
using System.Security.Permissions;
using System.Windows;

namespace ICSharpCode.AvalonEdit.Highlighting.Xshd
{
	/// <summary>
	/// A color in an Xshd file.
	/// </summary>
	[Serializable]
	public class XshdColor : XshdElement, ISerializable
	{
		/// <summary>
		/// Gets/sets the name.
		/// </summary>
		public string Name { get; set; }
		
		/// <summary>
		/// Gets/sets the foreground brush.
		/// </summary>
		public HighlightingBrush Foreground { get; set; }
		
		/// <summary>
		/// Gets/sets the background brush.
		/// </summary>
		public HighlightingBrush Background { get; set; }
		
		/// <summary>
		/// Gets/sets the font weight.
		/// </summary>
		public FontWeight? FontWeight { get; set; }
		
		/// <summary>
		/// Gets/sets the font style.
		/// </summary>
		public FontStyle? FontStyle { get; set; }
		
		/// <summary>
		/// Gets/Sets the example text that demonstrates where the color is used.
		/// </summary>
		public string ExampleText { get; set; }
		
		/// <summary>
		/// Creates a new XshdColor instance.
		/// </summary>
		public XshdColor()
		{
		}
		
		/// <summary>
		/// Deserializes an XshdColor.
		/// </summary>
		protected XshdColor(SerializationInfo info, StreamingContext context)
		{
			if (info == null)
				throw new ArgumentNullException("info");
			this.Name = info.GetString("Name");
			this.Foreground = (HighlightingBrush)info.GetValue("Foreground", typeof(HighlightingBrush));
			this.Background = (HighlightingBrush)info.GetValue("Background", typeof(HighlightingBrush));
			if (info.GetBoolean("HasWeight"))
				this.FontWeight = System.Windows.FontWeight.FromOpenTypeWeight(info.GetInt32("Weight"));
			if (info.GetBoolean("HasStyle"))
				this.FontStyle = (FontStyle?)new FontStyleConverter().ConvertFromInvariantString(info.GetString("Style"));
			this.ExampleText = info.GetString("ExampleText");
		}
		
		/// <summary>
		/// Serializes this XshdColor instance.
		/// </summary>
		#if DOTNET4
		[System.Security.SecurityCritical]
		#else
		[SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.SerializationFormatter)]
		#endif
		public virtual void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			if (info == null)
				throw new ArgumentNullException("info");
			info.AddValue("Name", this.Name);
			info.AddValue("Foreground", this.Foreground);
			info.AddValue("Background", this.Background);
			info.AddValue("HasWeight", this.FontWeight.HasValue);
			if (this.FontWeight.HasValue)
				info.AddValue("Weight", this.FontWeight.Value.ToOpenTypeWeight());
			info.AddValue("HasStyle", this.FontStyle.HasValue);
			if (this.FontStyle.HasValue)
				info.AddValue("Style", this.FontStyle.Value.ToString());
			info.AddValue("ExampleText", this.ExampleText);
		}
		
		/// <inheritdoc/>
		public override object AcceptVisitor(IXshdVisitor visitor)
		{
			return visitor.VisitColor(this);
		}
	}
}
