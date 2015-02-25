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
using System.Runtime.InteropServices;
using System.Security;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

namespace ICSharpCode.AvalonEdit.Utils
{
	/// <summary>
	/// Wrapper around Win32 functions.
	/// </summary>
	static class Win32
	{
		/// <summary>
		/// Gets the caret blink time.
		/// </summary>
		public static TimeSpan CaretBlinkTime {
			get { return TimeSpan.FromMilliseconds(SafeNativeMethods.GetCaretBlinkTime()); }
		}
		
		/// <summary>
		/// Creates an invisible Win32 caret for the specified Visual with the specified size (coordinates local to the owner visual).
		/// </summary>
		public static bool CreateCaret(Visual owner, Size size)
		{
			if (owner == null)
				throw new ArgumentNullException("owner");
			HwndSource source = PresentationSource.FromVisual(owner) as HwndSource;
			if (source != null) {
				Vector r = owner.PointToScreen(new Point(size.Width, size.Height)) - owner.PointToScreen(new Point(0, 0));
				return SafeNativeMethods.CreateCaret(source.Handle, IntPtr.Zero, (int)Math.Ceiling(r.X), (int)Math.Ceiling(r.Y));
			} else {
				return false;
			}
		}
		
		/// <summary>
		/// Sets the position of the caret previously created using <see cref="CreateCaret"/>. position is relative to the owner visual.
		/// </summary>
		public static bool SetCaretPosition(Visual owner, Point position)
		{
			if (owner == null)
				throw new ArgumentNullException("owner");
			HwndSource source = PresentationSource.FromVisual(owner) as HwndSource;
			if (source != null) {
				Point pointOnRootVisual = owner.TransformToAncestor(source.RootVisual).Transform(position);
				Point pointOnHwnd = pointOnRootVisual.TransformToDevice(source.RootVisual);
				return SafeNativeMethods.SetCaretPos((int)pointOnHwnd.X, (int)pointOnHwnd.Y);
			} else {
				return false;
			}
		}
		
		/// <summary>
		/// Destroys the caret previously created using <see cref="CreateCaret"/>.
		/// </summary>
		public static bool DestroyCaret()
		{
			return SafeNativeMethods.DestroyCaret();
		}
		
		[SuppressUnmanagedCodeSecurity]
		static class SafeNativeMethods
		{
			[DllImport("user32.dll")]
			public static extern int GetCaretBlinkTime();
			
			[DllImport("user32.dll")]
			[return: MarshalAs(UnmanagedType.Bool)]
			public static extern bool CreateCaret(IntPtr hWnd, IntPtr hBitmap, int nWidth, int nHeight);
			
			[DllImport("user32.dll")]
			[return: MarshalAs(UnmanagedType.Bool)]
			public static extern bool SetCaretPos(int x, int y);
			
			[DllImport("user32.dll")]
			[return: MarshalAs(UnmanagedType.Bool)]
			public static extern bool DestroyCaret();
		}
	}
}
