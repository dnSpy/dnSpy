// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

using ICSharpCode.AvalonEdit.Rendering;
using ICSharpCode.AvalonEdit.Utils;

namespace ICSharpCode.AvalonEdit.Editing
{
	sealed class CaretLayer : Layer
	{
		bool isVisible;
		Rect caretRectangle;
		
		DispatcherTimer caretBlinkTimer = new DispatcherTimer();
		bool blink;
		
		public CaretLayer(TextView textView) : base(textView, KnownLayer.Caret)
		{
			this.IsHitTestVisible = false;
			caretBlinkTimer.Tick += new EventHandler(caretBlinkTimer_Tick);
		}

		void caretBlinkTimer_Tick(object sender, EventArgs e)
		{
			blink = !blink;
			InvalidateVisual();
		}
		
		public void Show(Rect caretRectangle)
		{
			this.caretRectangle = caretRectangle;
			this.isVisible = true;
			StartBlinkAnimation();
			InvalidateVisual();
		}
		
		public void Hide()
		{
			if (isVisible) {
				isVisible = false;
				StopBlinkAnimation();
				InvalidateVisual();
			}
		}
		
		void StartBlinkAnimation()
		{
			TimeSpan blinkTime = Win32.CaretBlinkTime;
			blink = true; // the caret should visible initially
			// This is important if blinking is disabled (system reports a negative blinkTime)
			if (blinkTime.TotalMilliseconds > 0) {
				caretBlinkTimer.Interval = blinkTime;
				caretBlinkTimer.Start();
			}
		}
		
		void StopBlinkAnimation()
		{
			caretBlinkTimer.Stop();
		}
		
		internal Brush CaretBrush;
		
		protected override void OnRender(DrawingContext drawingContext)
		{
			base.OnRender(drawingContext);
			if (isVisible && blink) {
				Brush caretBrush = this.CaretBrush;
				if (caretBrush == null)
					caretBrush = (Brush)textView.GetValue(TextBlock.ForegroundProperty);
				Rect r = new Rect(caretRectangle.X - textView.HorizontalOffset,
				                  caretRectangle.Y - textView.VerticalOffset,
				                  caretRectangle.Width,
				                  caretRectangle.Height);
				drawingContext.DrawRectangle(caretBrush, null, PixelSnapHelpers.Round(r, PixelSnapHelpers.GetPixelSize(this)));
			}
		}
	}
}
