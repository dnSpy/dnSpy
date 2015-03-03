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
using System.Windows;

namespace ICSharpCode.AvalonEdit.Rendering
{
	/// <summary>
	/// An enumeration of well-known layers.
	/// </summary>
	public enum KnownLayer
	{
		/// <summary>
		/// This layer is in the background.
		/// There is no UIElement to represent this layer, it is directly drawn in the TextView.
		/// It is not possible to replace the background layer or insert new layers below it.
		/// </summary>
		/// <remarks>This layer is below the Selection layer.</remarks>
		Background,
		/// <summary>
		/// This layer contains the selection rectangle.
		/// </summary>
		/// <remarks>This layer is between the Background and the Text layers.</remarks>
		Selection,
		/// <summary>
		/// This layer contains the text and inline UI elements.
		/// </summary>
		/// <remarks>This layer is between the Selection and the Caret layers.</remarks>
		Text,
		/// <summary>
		/// This layer contains the blinking caret.
		/// </summary>
		/// <remarks>This layer is above the Text layer.</remarks>
		Caret
	}
	
	/// <summary>
	/// Specifies where a new layer is inserted, in relation to an old layer.
	/// </summary>
	public enum LayerInsertionPosition
	{
		/// <summary>
		/// The new layer is inserted below the specified layer.
		/// </summary>
		Below,
		/// <summary>
		/// The new layer replaces the specified layer. The old layer is removed
		/// from the <see cref="TextView.Layers"/> collection.
		/// </summary>
		Replace,
		/// <summary>
		/// The new layer is inserted above the specified layer.
		/// </summary>
		Above
	}
	
	sealed class LayerPosition : IComparable<LayerPosition>
	{
		internal static readonly DependencyProperty LayerPositionProperty =
			DependencyProperty.RegisterAttached("LayerPosition", typeof(LayerPosition), typeof(LayerPosition));
		
		public static void SetLayerPosition(UIElement layer, LayerPosition value)
		{
			layer.SetValue(LayerPositionProperty, value);
		}
		
		public static LayerPosition GetLayerPosition(UIElement layer)
		{
			return (LayerPosition)layer.GetValue(LayerPositionProperty);
		}
		
		internal readonly KnownLayer KnownLayer;
		internal readonly LayerInsertionPosition Position;
		
		public LayerPosition(KnownLayer knownLayer, LayerInsertionPosition position)
		{
			this.KnownLayer = knownLayer;
			this.Position = position;
		}
		
		public int CompareTo(LayerPosition other)
		{
			int r = this.KnownLayer.CompareTo(other.KnownLayer);
			if (r != 0)
				return r;
			else
				return this.Position.CompareTo(other.Position);
		}
	}
}
