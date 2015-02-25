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
using System.Windows.Input;
using System.Windows.Threading;

namespace ICSharpCode.AvalonEdit.Rendering
{
	/// <summary>
	/// Encapsulates and adds MouseHover support to UIElements.
	/// </summary>
	public class MouseHoverLogic : IDisposable
	{
		UIElement target;
		
		DispatcherTimer mouseHoverTimer;
		Point mouseHoverStartPoint;
		MouseEventArgs mouseHoverLastEventArgs;
		bool mouseHovering;
		
		/// <summary>
		/// Creates a new instance and attaches itself to the <paramref name="target" /> UIElement.
		/// </summary>
		public MouseHoverLogic(UIElement target)
		{
			if (target == null)
				throw new ArgumentNullException("target");
			this.target = target;
			this.target.MouseLeave += MouseHoverLogicMouseLeave;
			this.target.MouseMove += MouseHoverLogicMouseMove;
			this.target.MouseEnter += MouseHoverLogicMouseEnter;
		}
		
		void MouseHoverLogicMouseMove(object sender, MouseEventArgs e)
		{
			Vector mouseMovement = mouseHoverStartPoint - e.GetPosition(this.target);
			if (Math.Abs(mouseMovement.X) > SystemParameters.MouseHoverWidth
			    || Math.Abs(mouseMovement.Y) > SystemParameters.MouseHoverHeight)
			{
				StartHovering(e);
			}
			// do not set e.Handled - allow others to also handle MouseMove
		}
		
		void MouseHoverLogicMouseEnter(object sender, MouseEventArgs e)
		{
			StartHovering(e);
			// do not set e.Handled - allow others to also handle MouseEnter
		}
		
		void StartHovering(MouseEventArgs e)
		{
			StopHovering();
			mouseHoverStartPoint = e.GetPosition(this.target);
			mouseHoverLastEventArgs = e;
			mouseHoverTimer = new DispatcherTimer(SystemParameters.MouseHoverTime, DispatcherPriority.Background, OnMouseHoverTimerElapsed, this.target.Dispatcher);
			mouseHoverTimer.Start();
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
		
		/// <summary>
		/// Occurs when the mouse starts hovering over a certain location.
		/// </summary>
		public event EventHandler<MouseEventArgs> MouseHover;
		
		/// <summary>
		/// Raises the <see cref="MouseHover"/> event.
		/// </summary>
		protected virtual void OnMouseHover(MouseEventArgs e)
		{
			if (MouseHover != null) {
				MouseHover(this, e);
			}
		}
		
		/// <summary>
		/// Occurs when the mouse stops hovering over a certain location.
		/// </summary>
		public event EventHandler<MouseEventArgs> MouseHoverStopped;
		
		/// <summary>
		/// Raises the <see cref="MouseHoverStopped"/> event.
		/// </summary>
		protected virtual void OnMouseHoverStopped(MouseEventArgs e)
		{
			if (MouseHoverStopped != null) {
				MouseHoverStopped(this, e);
			}
		}
		
		bool disposed;
		
		/// <summary>
		/// Removes the MouseHover support from the target UIElement.
		/// </summary>
		public void Dispose()
		{
			if (!disposed) {
				this.target.MouseLeave -= MouseHoverLogicMouseLeave;
				this.target.MouseMove -= MouseHoverLogicMouseMove;
				this.target.MouseEnter -= MouseHoverLogicMouseEnter;
			}
			disposed = true;
		}
	}
}
