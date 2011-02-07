// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace ICSharpCode.AvalonEdit.Rendering
{
	public class MouseHoverLogic : IDisposable
	{
		UIElement target;
		
		DispatcherTimer mouseHoverTimer;
		Point mouseHoverStartPoint;
		MouseEventArgs mouseHoverLastEventArgs;
		bool mouseHovering;
		
		public MouseHoverLogic(UIElement target)
		{
			if (target == null)
				throw new ArgumentNullException("target");
			this.target = target;
			this.target.MouseLeave += MouseHoverLogicMouseLeave;
			this.target.MouseMove += MouseHoverLogicMouseMove;
		}
	
		void MouseHoverLogicMouseMove(object sender, MouseEventArgs e)
		{
			Point newPosition = e.GetPosition(this.target);
			Vector mouseMovement = mouseHoverStartPoint - newPosition;
			if (Math.Abs(mouseMovement.X) > SystemParameters.MouseHoverWidth
			    || Math.Abs(mouseMovement.Y) > SystemParameters.MouseHoverHeight)
			{
				StopHovering();
				mouseHoverStartPoint = newPosition;
				mouseHoverLastEventArgs = e;
				mouseHoverTimer = new DispatcherTimer(SystemParameters.MouseHoverTime, DispatcherPriority.Background,
				                                      OnMouseHoverTimerElapsed, this.target.Dispatcher);
				mouseHoverTimer.Start();
			}
			// do not set e.Handled - allow others to also handle MouseMove
		}
	
		void MouseHoverLogicMouseLeave(object sender, MouseEventArgs e)
		{
			StopHovering();
			// do not set e.Handled - allow others to also handle MouseLeave
		}
		
		void StopHovering()
		{
			if (mouseHoverTimer != null) {
				mouseHoverTimer.Stop();
				mouseHoverTimer = null;
			}
			if (mouseHovering) {
				mouseHovering = false;
				OnMouseHoverStopped(mouseHoverLastEventArgs);
			}
		}
		
		void OnMouseHoverTimerElapsed(object sender, EventArgs e)
		{
			mouseHoverTimer.Stop();
			mouseHoverTimer = null;
			
			mouseHovering = true;
			OnMouseHover(mouseHoverLastEventArgs);
		}
		
		public event EventHandler<MouseEventArgs> MouseHover;
		
		protected virtual void OnMouseHover(MouseEventArgs e)
		{
			if (MouseHover != null) {
				MouseHover(this, e);
			}
		}
		
		public event EventHandler<MouseEventArgs> MouseHoverStopped;
		
		protected virtual void OnMouseHoverStopped(MouseEventArgs e)
		{
			if (MouseHoverStopped != null) {
				MouseHoverStopped(this, e);
			}
		}
		
		bool disposed;
		
		public void Dispose()
		{
			if (!disposed) {
				this.target.MouseLeave -= MouseHoverLogicMouseLeave;
				this.target.MouseMove -= MouseHoverLogicMouseMove;
			}
			disposed = true;
		}
	}
}
