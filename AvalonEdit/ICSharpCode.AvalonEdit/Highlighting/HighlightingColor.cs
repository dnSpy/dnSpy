// Copyright (c) 2014 AlphaSierraPapa for the SharpDevelop Team
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
using System.Runtime.Serialization;
using System.Security.Permissions;
using System.Text;
using System.Windows;
using System.Windows.Media;
using ICSharpCode.AvalonEdit.Utils;

namespace ICSharpCode.AvalonEdit.Highlighting
{
	/// <summary>
	/// A highlighting color is a set of font properties and foreground and background color.
	/// </summary>
	[Serializable]
	public class HighlightingColor : ISerializable, IFreezable, ICloneable, IEquatable<HighlightingColor>
	{
		internal static readonly HighlightingColor Empty = FreezableHelper.FreezeAndReturn(new HighlightingColor());
		
		string name;
		FontWeight? fontWeight;
		FontStyle? fontStyle;
		bool? underline;
		HighlightingBrush foreground;
		HighlightingBrush background;
		bool frozen;
		
		/// <summary>
		/// Gets/Sets the name of the color.
		/// </summary>
		public string Name {
			get {
				return name;
			}
			set {
				if (frozen)
					throw new InvalidOperationException();
				name = value;
			}
		}
		
		/// <summary>
		/// Gets/sets the font weight. Null if the highlighting color does not change the font weight.
		/// </summary>
		public FontWeight? FontWeight {
			get {
				return fontWeight;
			}
			set {
				if (frozen)
					throw new InvalidOperationException();
				fontWeight = value;
			}
		}
		
		/// <summary>
		/// Gets/sets the font style. Null if the highlighting color does not change the font style.
		/// </summary>
		public FontStyle? FontStyle {
			get {
				return fontStyle;
			}
			set {
				if (frozen)
					throw new InvalidOperationException();
				fontStyle = value;
			}
		}
		
		/// <summary>
		///  Gets/sets the underline flag. Null if the underline status does not change the font style.
		/// </summary>
		public bool? Underline {
			get {
				return underline;
			}
			set {
				if (frozen)
					throw new InvalidOperationException();
				underline = value;
			}
		}
		
		/// <summary>
		/// Gets/sets the foreground color applied by the highlighting.
		/// </summary>
		public HighlightingBrush Foreground {
			get {
				return foreground;
			}
			set {
				if (frozen)
					throw new InvalidOperationException();
				foreground = value;
			}
		}
		
		/// <summary>
		/// Gets/sets the background color applied by the highlighting.
		/// </summary>
		public HighlightingBrush Background {
			get {
				return background;
			}
			set {
				if (frozen)
					throw new InvalidOperationException();
				background = value;
			}
		}
		
		/// <summary>
		/// Creates a new HighlightingColor instance.
		/// </summary>
		public HighlightingColor()
		{
		}
		
		/// <summary>
		/// Deserializes a HighlightingColor.
		/// </summary>
		protected HighlightingColor(SerializationInfo info, StreamingContext context)
		{
			if (info == null)
				throw new ArgumentNullException("info");
			this.Name = info.GetString("Name");
			if (info.GetBoolean("HasWeight"))
				this.FontWeight = System.Windows.FontWeight.FromOpenTypeWeight(info.GetInt32("Weight"));
			if (info.GetBoolean("HasStyle"))
				this.FontStyle = (FontStyle?)new FontStyleConverter().ConvertFromInvariantString(info.GetString("Style"));
			if (info.GetBoolean("HasUnderline"))
				this.Underline = info.GetBoolean("Underline");
			this.Foreground = (HighlightingBrush)info.GetValue("Foreground", typeof(HighlightingBrush));
			this.Background = (HighlightingBrush)info.GetValue("Background", typeof(HighlightingBrush));
		}
		
		/// <summary>
		/// Serializes this HighlightingColor instance.
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
			info.AddValue("HasWeight", this.FontWeight.HasValue);
			if (this.FontWeight.HasValue)
				info.AddValue("Weight", this.FontWeight.Value.ToOpenTypeWeight());
			info.AddValue("HasStyle", this.FontStyle.HasValue);
			if (this.FontStyle.HasValue)
				info.AddValue("Style", this.FontStyle.Value.ToString());
			info.AddValue("HasUnderline", this.Underline.HasValue);
			if (this.Underline.HasValue)
				info.AddValue("Underline", this.Underline.Value);
			info.AddValue("Foreground", this.Foreground);
			info.AddValue("Background", this.Background);
		}
		
		/// <summary>
		/// Gets CSS code for the color.
		/// </summary>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1308:NormalizeStringsToUppercase", Justification = "CSS usually uses lowercase, and all possible values are English-only")]
		public virtual string ToCss()
		{
			StringBuilder b = new StringBuilder();
			if (Foreground != null) {
				Color? c = Foreground.GetColor(null);
				if (c != null) {
					b.AppendFormat(CultureInfo.InvariantCulture, "color: #{0:x2}{1:x2}{2:x2}; ", c.Value.R, c.Value.G, c.Value.B);
				}
			}
			if (FontWeight != null) {
				b.Append("font-weight: ");
				b.Append(FontWeight.Value.ToString().ToLowerInvariant());
				b.Append("; ");
			}
			if (FontStyle != null) {
				b.Append("font-style: ");
				b.Append(FontStyle.Value.ToString().ToLowerInvariant());
				b.Append("; ");
			}
			if (Underline != null)
			{
				b.Append("text-decoration: ");
				b.Append(Underline.Value ? "underline" : "none");
				b.Append("; ");
			}
			return b.ToString();
		}
		
		/// <inheritdoc/>
		public override string ToString()
		{
			return "[" + GetType().Name + " " + (string.IsNullOrEmpty(this.Name) ? ToCss() : this.Name) + "]";
		}
		
		/// <summary>
		/// Prevent further changes to this highlighting color.
		/// </summary>
		public virtual void Freeze()
		{
			frozen = true;
		}
		
		/// <summary>
		/// Gets whether this HighlightingColor instance is frozen.
		/// </summary>
		public bool IsFrozen {
			get { return frozen; }
		}
		
		/// <summary>
		/// Clones this highlighting color.
		/// If this color is frozen, the clone will be unfrozen.
		/// </summary>
		public virtual HighlightingColor Clone()
		{
			HighlightingColor c = (HighlightingColor)MemberwiseClone();
			c.frozen = false;
			return c;
		}
		
		object ICloneable.Clone()
		{
			return Clone();
		}
		
		/// <inheritdoc/>
		public override sealed bool Equals(object obj)
		{
			return Equals(obj as HighlightingColor);
		}
		
		/// <inheritdoc/>
		public virtual bool Equals(HighlightingColor other)
		{
			if (other == null)
				return false;
			return this.name == other.name && this.fontWeight == other.fontWeight
				&& this.fontStyle == other.fontStyle && this.underline == other.underline
				&& object.Equals(this.foreground, other.foreground) && object.Equals(this.background, other.background);
		}
		
		/// <inheritdoc/>
		public override int GetHashCode()
		{
			int hashCode = 0;
			unchecked {
				if (name != null)
					hashCode += 1000000007 * name.GetHashCode();
				hashCode += 1000000009 * fontWeight.GetHashCode();
				hashCode += 1000000021 * fontStyle.GetHashCode();
				if (foreground != null)
					hashCode += 1000000033 * foreground.GetHashCode();
				if (background != null)
					hashCode += 1000000087 * background.GetHashCode();
			}
			return hashCode;
		}
		
		/// <summary>
		/// Overwrites the properties in this HighlightingColor with those from the given color;
		/// but maintains the current values where the properties of the given color return <c>null</c>.
		/// </summary>
		public void MergeWith(HighlightingColor color)
		{
			FreezableHelper.ThrowIfFrozen(this);
			if (color.fontWeight != null)
				this.fontWeight = color.fontWeight;
			if (color.fontStyle != null)
				this.fontStyle = color.fontStyle;
			if (color.foreground != null)
				this.foreground = color.foreground;
			if (color.background != null)
				this.background = color.background;
			if (color.underline != null)
				this.underline = color.underline;
		}
		
		internal bool IsEmptyForMerge {
			get {
				return fontWeight == null && fontStyle == null && underline == null
					&& foreground == null && background == null;
			}
		}
	}
}
