// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Windows;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Utils;

namespace ICSharpCode.ILSpy.AvalonEdit
{
	public static class TextEditorWeakEventManager
	{
		public sealed class MouseHover : WeakEventManagerBase<MouseHover, TextEditor>
		{
			protected override void StopListening(TextEditor source)
			{
				source.MouseHover -= DeliverEvent;
			}
			
			protected override void StartListening(TextEditor source)
			{
				source.MouseHover += DeliverEvent;
			}
		}
		
		public sealed class MouseHoverStopped : WeakEventManagerBase<MouseHoverStopped, TextEditor>
		{
			protected override void StopListening(TextEditor source)
			{
				source.MouseHoverStopped -= DeliverEvent;
			}
			
			protected override void StartListening(TextEditor source)
			{
				source.MouseHoverStopped += DeliverEvent;
			}
		}
		
		public sealed class MouseDown : WeakEventManagerBase<MouseDown, TextEditor>
		{
			protected override void StopListening(TextEditor source)
			{
				source.MouseDown -= DeliverEvent;
			}
			
			protected override void StartListening(TextEditor source)
			{
				source.MouseDown += DeliverEvent;
			}
		}
	}
}
