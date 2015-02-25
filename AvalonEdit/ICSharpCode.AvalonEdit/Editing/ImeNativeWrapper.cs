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
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.TextFormatting;

using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Rendering;
using ICSharpCode.AvalonEdit.Utils;
using Draw = System.Drawing;

namespace ICSharpCode.AvalonEdit.Editing
{
	/// <summary>
	/// Native API required for IME support.
	/// </summary>
	static class ImeNativeWrapper
	{
		[StructLayout(LayoutKind.Sequential)]
		struct CompositionForm
		{
			public int dwStyle;
			public POINT ptCurrentPos;
			public RECT rcArea;
		}
		
		[StructLayout(LayoutKind.Sequential)]
		struct POINT
		{
			public int x;
			public int y;
		}
		
		[StructLayout(LayoutKind.Sequential)]
		struct RECT
		{
			public int left;
			public int top;
			public int right;
			public int bottom;
		}
		
		[StructLayout(LayoutKind.Sequential, CharSet=CharSet.Unicode)]
		struct LOGFONT
		{
			public int lfHeight;
			public int lfWidth;
			public int lfEscapement;
			public int lfOrientation;
			public int lfWeight;
			public byte lfItalic;
			public byte lfUnderline;
			public byte lfStrikeOut;
			public byte lfCharSet;
			public byte lfOutPrecision;
			public byte lfClipPrecision;
			public byte lfQuality;
			public byte lfPitchAndFamily;
			[MarshalAs(UnmanagedType.ByValTStr, SizeConst=32)] public string lfFaceName;
		}
		
		const int CPS_CANCEL = 0x4;
		const int NI_COMPOSITIONSTR = 0x15;
		const int GCS_COMPSTR = 0x0008;
		
		public const int WM_IME_COMPOSITION = 0x10F;
		public const int WM_IME_SETCONTEXT = 0x281;
		public const int WM_INPUTLANGCHANGE = 0x51;
		
		[DllImport("imm32.dll")]
		public static extern IntPtr ImmAssociateContext(IntPtr hWnd, IntPtr hIMC);
		[DllImport("imm32.dll")]
		internal static extern IntPtr ImmGetContext(IntPtr hWnd);
		[DllImport("imm32.dll")]
		internal static extern IntPtr ImmGetDefaultIMEWnd(IntPtr hWnd);
		[DllImport("imm32.dll")]
		[return: MarshalAs(UnmanagedType.Bool)]
		internal static extern bool ImmReleaseContext(IntPtr hWnd, IntPtr hIMC);
		[DllImport("imm32.dll")]
		[return: MarshalAs(UnmanagedType.Bool)]
		static extern bool ImmNotifyIME(IntPtr hIMC, int dwAction, int dwIndex, int dwValue = 0);
		[DllImport("imm32.dll")]
		[return: MarshalAs(UnmanagedType.Bool)]
		static extern bool ImmSetCompositionWindow(IntPtr hIMC, ref CompositionForm form);
		[DllImport("imm32.dll", CharSet = CharSet.Unicode)]
		[return: MarshalAs(UnmanagedType.Bool)]
		static extern bool ImmSetCompositionFont(IntPtr hIMC, ref LOGFONT font);
		[DllImport("imm32.dll")]
		[return: MarshalAs(UnmanagedType.Bool)]
		static extern bool ImmGetCompositionFont(IntPtr hIMC, out LOGFONT font);
		
		[DllImport("msctf.dll")]
		static extern int TF_CreateThreadMgr(out ITfThreadMgr threadMgr);
		
		[ThreadStatic] static bool textFrameworkThreadMgrInitialized;
		[ThreadStatic] static ITfThreadMgr textFrameworkThreadMgr;
		
		public static ITfThreadMgr GetTextFrameworkThreadManager()
		{
			if (!textFrameworkThreadMgrInitialized) {
				textFrameworkThreadMgrInitialized = true;
				TF_CreateThreadMgr(out textFrameworkThreadMgr);
			}
			return textFrameworkThreadMgr;
		}
		
		public static bool NotifyIme(IntPtr hIMC)
		{
			return ImmNotifyIME(hIMC, NI_COMPOSITIONSTR, CPS_CANCEL);
		}
		
		public static bool SetCompositionWindow(HwndSource source, IntPtr hIMC, TextArea textArea)
		{
			if (textArea == null)
				throw new ArgumentNullException("textArea");
			Rect textViewBounds = textArea.TextView.GetBounds(source);
			Rect characterBounds = textArea.TextView.GetCharacterBounds(textArea.Caret.Position, source);
			CompositionForm form = new CompositionForm();
			form.dwStyle = 0x0020;
			form.ptCurrentPos.x = (int)Math.Max(characterBounds.Left, textViewBounds.Left);
			form.ptCurrentPos.y = (int)Math.Max(characterBounds.Top, textViewBounds.Top);
			form.rcArea.left = (int)textViewBounds.Left;
			form.rcArea.top = (int)textViewBounds.Top;
			form.rcArea.right = (int)textViewBounds.Right;
			form.rcArea.bottom = (int)textViewBounds.Bottom;
			return ImmSetCompositionWindow(hIMC, ref form);
		}
		
		public static bool SetCompositionFont(HwndSource source, IntPtr hIMC, TextArea textArea)
		{
			if (textArea == null)
				throw new ArgumentNullException("textArea");
			LOGFONT lf = new LOGFONT();
			Rect characterBounds = textArea.TextView.GetCharacterBounds(textArea.Caret.Position, source);
			lf.lfFaceName = textArea.FontFamily.Source;
			lf.lfHeight = (int)characterBounds.Height;
			return ImmSetCompositionFont(hIMC, ref lf);
		}
		
		static Rect GetBounds(this TextView textView, HwndSource source)
		{
			// this may happen during layout changes in AvalonDock, so we just return an empty rectangle
			// in those cases. It should be refreshed immediately.
			if (source.RootVisual == null || !source.RootVisual.IsAncestorOf(textView))
				return EMPTY_RECT;
			Rect displayRect = new Rect(0, 0, textView.ActualWidth, textView.ActualHeight);
			return textView
				.TransformToAncestor(source.RootVisual).TransformBounds(displayRect) // rect on root visual
				.TransformToDevice(source.RootVisual); // rect on HWND
		}
		
		static readonly Rect EMPTY_RECT = new Rect(0, 0, 0, 0);
		
		static Rect GetCharacterBounds(this TextView textView, TextViewPosition pos, HwndSource source)
		{
			VisualLine vl = textView.GetVisualLine(pos.Line);
			if (vl == null)
				return EMPTY_RECT;
			// this may happen during layout changes in AvalonDock, so we just return an empty rectangle
			// in those cases. It should be refreshed immediately.
			if (source.RootVisual == null || !source.RootVisual.IsAncestorOf(textView))
				return EMPTY_RECT;
			TextLine line = vl.GetTextLine(pos.VisualColumn, pos.IsAtEndOfLine);
			Rect displayRect;
			// calculate the display rect for the current character
			if (pos.VisualColumn < vl.VisualLengthWithEndOfLineMarker) {
				displayRect = line.GetTextBounds(pos.VisualColumn, 1).First().Rectangle;
				displayRect.Offset(0, vl.GetTextLineVisualYPosition(line, VisualYPosition.LineTop));
			} else {
				// if we are in virtual space, we just use one wide-space as character width
				displayRect = new Rect(vl.GetVisualPosition(pos.VisualColumn, VisualYPosition.TextTop),
				                       new Size(textView.WideSpaceWidth, textView.DefaultLineHeight));
			}
			// adjust to current scrolling
			displayRect.Offset(-textView.ScrollOffset);
			return textView
				.TransformToAncestor(source.RootVisual).TransformBounds(displayRect) // rect on root visual
				.TransformToDevice(source.RootVisual); // rect on HWND
		}
	}
	
	[ComImport, Guid("aa80e801-2021-11d2-93e0-0060b067b86e"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
	interface ITfThreadMgr
	{
		void Activate(out int clientId);
		void Deactivate();
		void CreateDocumentMgr(out IntPtr docMgr);
		void EnumDocumentMgrs(out IntPtr enumDocMgrs);
		void GetFocus(out IntPtr docMgr);
		void SetFocus(IntPtr docMgr);
		void AssociateFocus(IntPtr hwnd, IntPtr newDocMgr, out IntPtr prevDocMgr);
		void IsThreadFocus([MarshalAs(UnmanagedType.Bool)] out bool isFocus);
		void GetFunctionProvider(ref Guid classId, out IntPtr funcProvider);
		void EnumFunctionProviders(out IntPtr enumProviders);
		void GetGlobalCompartment(out IntPtr compartmentMgr);
	}
}
