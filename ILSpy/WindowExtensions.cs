
using System;
using System.Runtime.InteropServices;
using System.Windows;

namespace ICSharpCode.ILSpy
{
	// From http://stackoverflow.com/a/339635
	public static class WindowExtensions
	{
		private const int GWL_STYLE = -16,
						  WS_MAXIMIZEBOX = 0x10000,
						  WS_MINIMIZEBOX = 0x20000;

		[DllImport("user32")]
		extern private static int GetWindowLong(IntPtr hwnd, int index);

		[DllImport("user32")]
		extern private static int SetWindowLong(IntPtr hwnd, int index, int value);

		public static void HideMinimizeAndMaximizeButtons(this Window window)
		{
			IntPtr hwnd = new System.Windows.Interop.WindowInteropHelper(window).Handle;
			var currentStyle = GetWindowLong(hwnd, GWL_STYLE);

			SetWindowLong(hwnd, GWL_STYLE, (currentStyle & ~WS_MAXIMIZEBOX & ~WS_MINIMIZEBOX));
		}
	}
}
