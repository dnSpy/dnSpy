// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Xml;
using System.Xml.Linq;

using Mono.Cecil;

namespace ICSharpCode.ILSpy.Debugger.Services
{
	/// <summary>
	/// Extension methods used in ILSpy debugger.
	/// </summary>
	static class ExtensionMethods
	{
		/// <summary>
		/// Raises the event.
		/// Does nothing if eventHandler is null.
		/// Because the event handler is passed as parameter, it is only fetched from the event field one time.
		/// This makes
		/// <code>MyEvent.RaiseEvent(x,y);</code>
		/// thread-safe
		/// whereas
		/// <code>if (MyEvent != null) MyEvent(x,y);</code>
		/// would not be safe.
		/// </summary>
		/// <remarks>Using this method is only thread-safe under the Microsoft .NET memory model,
		/// not under the less strict memory model in the CLI specification.</remarks>
		public static void RaiseEvent(this EventHandler eventHandler, object sender, EventArgs e)
		{
			if (eventHandler != null) {
				eventHandler(sender, e);
			}
		}
		
		/// <summary>
		/// Raises the event.
		/// Does nothing if eventHandler is null.
		/// Because the event handler is passed as parameter, it is only fetched from the event field one time.
		/// This makes
		/// <code>MyEvent.RaiseEvent(x,y);</code>
		/// thread-safe
		/// whereas
		/// <code>if (MyEvent != null) MyEvent(x,y);</code>
		/// would not be safe.
		/// </summary>
		public static void RaiseEvent<T>(this EventHandler<T> eventHandler, object sender, T e) where T : EventArgs
		{
			if (eventHandler != null) {
				eventHandler(sender, e);
			}
		}
		
		/// <summary>
		/// Runs an action for all elements in the input.
		/// </summary>
		public static void ForEach<T>(this IEnumerable<T> input, Action<T> action)
		{
			if (input == null)
				throw new ArgumentNullException("input");
			foreach (T element in input) {
				action(element);
			}
		}
		
		/// <summary>
		/// Adds all <paramref name="elements"/> to <paramref name="list"/>.
		/// </summary>
		public static void AddRange<T>(this ICollection<T> list, IEnumerable<T> elements)
		{
			foreach (T o in elements)
				list.Add(o);
		}
		
		public static ReadOnlyCollection<T> AsReadOnly<T>(this IList<T> arr)
		{
			return new ReadOnlyCollection<T>(arr);
		}
		
		/// <summary>
		/// Converts a recursive data structure into a flat list.
		/// </summary>
		/// <param name="input">The root elements of the recursive data structure.</param>
		/// <param name="recursion">The function that gets the children of an element.</param>
		/// <returns>Iterator that enumerates the tree structure in preorder.</returns>
		public static IEnumerable<T> Flatten<T>(this IEnumerable<T> input, Func<T, IEnumerable<T>> recursion)
		{
			Stack<IEnumerator<T>> stack = new Stack<IEnumerator<T>>();
			try {
				stack.Push(input.GetEnumerator());
				while (stack.Count > 0) {
					while (stack.Peek().MoveNext()) {
						T element = stack.Peek().Current;
						yield return element;
						IEnumerable<T> children = recursion(element);
						if (children != null) {
							stack.Push(children.GetEnumerator());
						}
					}
					stack.Pop().Dispose();
				}
			} finally {
				while (stack.Count > 0) {
					stack.Pop().Dispose();
				}
			}
		}
		
		/// <summary>
		/// Creates an array containing a part of the array (similar to string.Substring).
		/// </summary>
		public static T[] Splice<T>(this T[] array, int startIndex)
		{
			if (array == null)
				throw new ArgumentNullException("array");
			return Splice(array, startIndex, array.Length - startIndex);
		}
		
		/// <summary>
		/// Creates an array containing a part of the array (similar to string.Substring).
		/// </summary>
		public static T[] Splice<T>(this T[] array, int startIndex, int length)
		{
			if (array == null)
				throw new ArgumentNullException("array");
			if (startIndex < 0 || startIndex > array.Length)
				throw new ArgumentOutOfRangeException("startIndex", startIndex, "Value must be between 0 and " + array.Length);
			if (length < 0 || length > array.Length - startIndex)
				throw new ArgumentOutOfRangeException("length", length, "Value must be between 0 and " + (array.Length - startIndex));
			T[] result = new T[length];
			Array.Copy(array, startIndex, result, 0, length);
			return result;
		}
		
		#region System.Drawing <-> WPF conversions
		public static System.Drawing.Point ToSystemDrawing(this Point p)
		{
			return new System.Drawing.Point((int)p.X, (int)p.Y);
		}
		
		public static System.Drawing.Size ToSystemDrawing(this Size s)
		{
			return new System.Drawing.Size((int)s.Width, (int)s.Height);
		}
		
		public static System.Drawing.Rectangle ToSystemDrawing(this Rect r)
		{
			return new System.Drawing.Rectangle(r.TopLeft.ToSystemDrawing(), r.Size.ToSystemDrawing());
		}
		
		public static System.Drawing.Color ToSystemDrawing(this System.Windows.Media.Color c)
		{
			return System.Drawing.Color.FromArgb(c.A, c.R, c.G, c.B);
		}
		
		public static Point ToWpf(this System.Drawing.Point p)
		{
			return new Point(p.X, p.Y);
		}
		
		public static Size ToWpf(this System.Drawing.Size s)
		{
			return new Size(s.Width, s.Height);
		}
		
		public static Rect ToWpf(this System.Drawing.Rectangle rect)
		{
			return new Rect(rect.Location.ToWpf(), rect.Size.ToWpf());
		}
		
		public static System.Windows.Media.Color ToWpf(this System.Drawing.Color c)
		{
			return System.Windows.Media.Color.FromArgb(c.A, c.R, c.G, c.B);
		}
		#endregion
		
		#region DPI independence
		public static Rect TransformToDevice(this Rect rect, Visual visual)
		{
			Matrix matrix = PresentationSource.FromVisual(visual).CompositionTarget.TransformToDevice;
			return Rect.Transform(rect, matrix);
		}
		
		public static Rect TransformFromDevice(this Rect rect, Visual visual)
		{
			Matrix matrix = PresentationSource.FromVisual(visual).CompositionTarget.TransformFromDevice;
			return Rect.Transform(rect, matrix);
		}
		
		public static Size TransformToDevice(this Size size, Visual visual)
		{
			Matrix matrix = PresentationSource.FromVisual(visual).CompositionTarget.TransformToDevice;
			return new Size(size.Width * matrix.M11, size.Height * matrix.M22);
		}
		
		public static Size TransformFromDevice(this Size size, Visual visual)
		{
			Matrix matrix = PresentationSource.FromVisual(visual).CompositionTarget.TransformFromDevice;
			return new Size(size.Width * matrix.M11, size.Height * matrix.M22);
		}
		
		public static Point TransformToDevice(this Point point, Visual visual)
		{
			Matrix matrix = PresentationSource.FromVisual(visual).CompositionTarget.TransformToDevice;
			return new Point(point.X * matrix.M11, point.Y * matrix.M22);
		}
		
		public static Point TransformFromDevice(this Point point, Visual visual)
		{
			Matrix matrix = PresentationSource.FromVisual(visual).CompositionTarget.TransformFromDevice;
			return new Point(point.X * matrix.M11, point.Y * matrix.M22);
		}
		#endregion
		
		/// <summary>
		/// Removes <param name="stringToRemove" /> from the start of this string.
		/// Throws ArgumentException if this string does not start with <param name="stringToRemove" />.
		/// </summary>
		public static string RemoveStart(this string s, string stringToRemove)
		{
			if (s == null)
				return null;
			if (string.IsNullOrEmpty(stringToRemove))
				return s;
			if (!s.StartsWith(stringToRemove))
				throw new ArgumentException(string.Format("{0} does not start with {1}", s, stringToRemove));
			return s.Substring(stringToRemove.Length);
		}
		
		/// <summary>
		/// Removes <param name="stringToRemove" /> from the end of this string.
		/// Throws ArgumentException if this string does not end with <param name="stringToRemove" />.
		/// </summary>
		public static string RemoveEnd(this string s, string stringToRemove)
		{
			if (s == null)
				return null;
			if (string.IsNullOrEmpty(stringToRemove))
				return s;
			if (!s.EndsWith(stringToRemove))
				throw new ArgumentException(string.Format("{0} does not end with {1}", s, stringToRemove));
			return s.Substring(0, s.Length - stringToRemove.Length);
		}
		
		/// <summary>
		/// Takes at most <param name="length" /> first characters from string.
		/// String can be null.
		/// </summary>
		public static string TakeStart(this string s, int length)
		{
			if (string.IsNullOrEmpty(s) || length >= s.Length)
				return s;
			return s.Substring(0, length);
		}

		/// <summary>
		/// Takes at most <param name="length" /> first characters from string, and appends '...' if string is longer.
		/// String can be null.
		/// </summary>
		public static string TakeStartEllipsis(this string s, int length)
		{
			if (string.IsNullOrEmpty(s) || length >= s.Length)
				return s;
			return s.Substring(0, length) + "...";
		}

		
		public static string Replace(this string original, string pattern, string replacement, StringComparison comparisonType)
		{
			if (original == null)
				throw new ArgumentNullException("original");
			if (pattern == null)
				throw new ArgumentNullException("pattern");
			if (pattern.Length == 0)
				throw new ArgumentException("String cannot be of zero length.", "pattern");
			if (comparisonType != StringComparison.Ordinal && comparisonType != StringComparison.OrdinalIgnoreCase)
				throw new NotSupportedException("Currently only ordinal comparisons are implemented.");
			
			StringBuilder result = new StringBuilder(original.Length);
			int currentPos = 0;
			int nextMatch = original.IndexOf(pattern, comparisonType);
			while (nextMatch >= 0) {
				result.Append(original, currentPos, nextMatch - currentPos);
				// The following line restricts this method to ordinal comparisons:
				// for non-ordinal comparisons, the match length might be different than the pattern length.
				currentPos = nextMatch + pattern.Length;
				result.Append(replacement);
				
				nextMatch = original.IndexOf(pattern, currentPos, comparisonType);
			}
			
			result.Append(original, currentPos, original.Length - currentPos);
			return result.ToString();
		}
		
		public static byte[] GetBytesWithPreamble(this Encoding encoding, string text)
		{
			byte[] encodedText = encoding.GetBytes(text);
			byte[] bom = encoding.GetPreamble();
			if (bom != null && bom.Length > 0) {
				byte[] result = new byte[bom.Length + encodedText.Length];
				bom.CopyTo(result, 0);
				encodedText.CopyTo(result, bom.Length);
				return result;
			} else {
				return encodedText;
			}
		}
		
		/// <summary>
		/// Returns the index of the first element for which <paramref name="predicate"/> returns true.
		/// If none of the items in the list fits the <paramref name="predicate"/>, -1 is returned.
		/// </summary>
		public static int FindIndex<T>(this IList<T> list, Func<T, bool> predicate)
		{
			for (int i = 0; i < list.Count; i++) {
				if (predicate(list[i]))
					return i;
			}
			
			return -1;
		}
		
		/// <summary>
		/// Adds item to the list if the item is not null.
		/// </summary>
		public static void AddIfNotNull<T>(this IList<T> list, T itemToAdd)
		{
			if (itemToAdd != null)
				list.Add(itemToAdd);
		}
		
		public static void WriteTo(this Stream sourceStream, Stream targetStream)
		{
			byte[] buffer = new byte[4096];
			int bytes;
			while ((bytes = sourceStream.Read(buffer, 0, buffer.Length)) > 0)
				targetStream.Write(buffer, 0, bytes);
		}
	}
	
	/// <summary>
	/// Scrolling related helpers.
	/// </summary>
	public static class ScrollUtils
	{
		/// <summary>
		/// Searches VisualTree of given object for a ScrollViewer.
		/// </summary>
		public static ScrollViewer GetScrollViewer(this DependencyObject o)
		{
			var scrollViewer = o as ScrollViewer;
			if (scrollViewer != null)	{
				return scrollViewer;
			}

			for (int i = 0; i < VisualTreeHelper.GetChildrenCount(o); i++)
			{
				var child = VisualTreeHelper.GetChild(o, i);
				var result = GetScrollViewer(child);
				if (result != null) {
					return result;
				}
			}
			return null;
		}
		
		/// <summary>
		/// Scrolls ScrollViewer up by given offset.
		/// </summary>
		/// <param name="offset">Offset by which to scroll up. Should be positive.</param>
		public static void ScrollUp(this ScrollViewer scrollViewer, double offset)
		{
			ScrollUtils.ScrollByVerticalOffset(scrollViewer, -offset);
		}
		
		/// <summary>
		/// Scrolls ScrollViewer down by given offset.
		/// </summary>
		/// <param name="offset">Offset by which to scroll down. Should be positive.</param>
		public static void ScrollDown(this ScrollViewer scrollViewer, double offset)
		{
			ScrollUtils.ScrollByVerticalOffset(scrollViewer, offset);
		}
		
		/// <summary>
		/// Scrolls ScrollViewer by given vertical offset.
		/// </summary>
		public static void ScrollByVerticalOffset(this ScrollViewer scrollViewer, double offset)
		{
			scrollViewer.ScrollToVerticalOffset(scrollViewer.VerticalOffset + offset);
		}
		
		/// <summary>
		/// Gets the member by token.
		/// </summary>
		/// <param name="type">Type.</param>
		/// <param name="memberToken">Member metadata token.</param>
		/// <returns></returns>
		public static MemberReference GetMemberByToken(this TypeDefinition type, int memberToken)
		{
			if (type.HasProperties) {
				foreach (var member in type.Properties) {
					if (member.MetadataToken.ToInt32() == memberToken)
						return member;
					if (member.GetMethod != null && member.GetMethod.MetadataToken.ToInt32() == memberToken)
						return member;
					if (member.SetMethod != null && member.SetMethod.MetadataToken.ToInt32() == memberToken)
						return member;
				}
			}
			
			if (type.HasEvents) {
				foreach (var member in type.Events) {
					if (member.MetadataToken.ToInt32() == memberToken)
						return member;
					if (member.AddMethod != null && member.AddMethod.MetadataToken.ToInt32() == memberToken)
						return member;
					if (member.RemoveMethod != null && member.RemoveMethod.MetadataToken.ToInt32() == memberToken)
						return member;
					if (member.InvokeMethod != null && member.InvokeMethod.MetadataToken.ToInt32() == memberToken)
						return member;
				}
			}
			if (type.HasMethods) {
				foreach (var member in type.Methods) {
					if (member.MetadataToken.ToInt32() == memberToken)
						return member;
				}
			}
			
			if (type.HasFields) {
				foreach (var member in type.Fields) {
					if (member.MetadataToken.ToInt32() == memberToken)
						return member;
				}
			}
			
			return null;
		}
	}
}
