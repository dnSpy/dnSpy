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
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Runtime.Serialization;
using System.Windows;
using System.Windows.Media;

using ICSharpCode.AvalonEdit.Rendering;

namespace ICSharpCode.AvalonEdit.Highlighting
{
	/// <summary>
	/// A brush used for syntax highlighting. Can retrieve a real brush on-demand.
	/// </summary>
	[Serializable]
	public abstract class HighlightingBrush
	{
		/// <summary>
		/// Gets the real brush.
		/// </summary>
		/// <param name="context">The construction context. context can be null!</param>
		public abstract Brush GetBrush(ITextRunConstructionContext context);
		
		/// <summary>
		/// Gets the color of the brush.
		/// </summary>
		/// <param name="context">The construction context. context can be null!</param>
		public virtual Color? GetColor(ITextRunConstructionContext context)
		{
			SolidColorBrush scb = GetBrush(context) as SolidColorBrush;
			if (scb != null)
				return scb.Color;
			else
				return null;
		}
	}
	
	/// <summary>
	/// Highlighting brush implementation that takes a frozen brush.
	/// </summary>
	[Serializable]
	public sealed class SimpleHighlightingBrush : HighlightingBrush, ISerializable
	{
		readonly SolidColorBrush brush;
		
		internal SimpleHighlightingBrush(SolidColorBrush brush)
		{
			brush.Freeze();
			this.brush = brush;
		}
		
		/// <summary>
		/// Creates a new HighlightingBrush with the specified color.
		/// </summary>
		public SimpleHighlightingBrush(Color color) : this(new SolidColorBrush(color)) {}
		
		/// <inheritdoc/>
		public override Brush GetBrush(ITextRunConstructionContext context)
		{
			return brush;
		}

		/// <inheritdoc/>
		public override string ToString()
		{
			return brush.ToString();
		}
		
		SimpleHighlightingBrush(SerializationInfo info, StreamingContext context)
		{
			this.brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(info.GetString("color")));
			brush.Freeze();
		}
		
		void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
		{
			info.AddValue("color", brush.Color.ToString(CultureInfo.InvariantCulture));
		}
		
		/// <inheritdoc/>
		public override bool Equals(object obj)
		{
			SimpleHighlightingBrush other = obj as SimpleHighlightingBrush;
			if (other == null)
				return false;
			return this.brush.Color.Equals(other.brush.Color);
		}
		
		/// <inheritdoc/>
		public override int GetHashCode()
		{
			return brush.Color.GetHashCode();
		}
	}
	
	/// <summary>
	/// HighlightingBrush implementation that finds a brush using a resource.
	/// </summary>
	[Serializable]
	sealed class SystemColorHighlightingBrush : HighlightingBrush, ISerializable
	{
		readonly PropertyInfo property;
		
		public SystemColorHighlightingBrush(PropertyInfo property)
		{
			Debug.Assert(property.ReflectedType == typeof(SystemColors));
			Debug.Assert(typeof(Brush).IsAssignableFrom(property.PropertyType));
			this.property = property;
		}
		
		public override Brush GetBrush(ITextRunConstructionContext context)
		{
			return (Brush)property.GetValue(null, null);
		}
		
		public override string ToString()
		{
			return property.Name;
		}
		
		SystemColorHighlightingBrush(SerializationInfo info, StreamingContext context)
		{
			property = typeof(SystemColors).GetProperty(info.GetString("propertyName"));
			if (property == null)
				throw new ArgumentException("Error deserializing SystemColorHighlightingBrush");
		}
		
		void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
		{
			info.AddValue("propertyName", property.Name);
		}
		
		public override bool Equals(object obj)
		{
			SystemColorHighlightingBrush other = obj as SystemColorHighlightingBrush;
			if (other == null)
				return false;
			return object.Equals(this.property, other.property);
		}
		
		public override int GetHashCode()
		{
			return property.GetHashCode();
		}
	}
}
