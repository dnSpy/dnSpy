// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

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
	sealed class SimpleHighlightingBrush : HighlightingBrush, ISerializable
	{
		readonly SolidColorBrush brush;
		
		public SimpleHighlightingBrush(SolidColorBrush brush)
		{
			brush.Freeze();
			this.brush = brush;
		}
		
		public SimpleHighlightingBrush(Color color) : this(new SolidColorBrush(color)) {}
		
		public override Brush GetBrush(ITextRunConstructionContext context)
		{
			return brush;
		}
		
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
	}
}
