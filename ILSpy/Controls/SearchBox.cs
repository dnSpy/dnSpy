// Copyright (c) 2011 AlphaSierraPapa for the SharpDevelop Team
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
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace ICSharpCode.ILSpy.Controls
{
	public class SearchBox : TextBox
	{
		static SearchBox() {
			DefaultStyleKeyProperty.OverrideMetadata(
				typeof(SearchBox),
				new FrameworkPropertyMetadata(typeof(SearchBox)));
		}
		
		#region Dependency properties
		
		public static DependencyProperty WatermarkTextProperty = DependencyProperty.Register("WatermarkText", typeof(string), typeof(SearchBox));
		
		public static DependencyProperty WatermarkColorProperty = DependencyProperty.Register("WatermarkColor", typeof(Brush), typeof(SearchBox));
		
		public static DependencyProperty HasTextProperty = DependencyProperty.Register("HasText", typeof(bool), typeof(SearchBox));
		
		public static readonly DependencyProperty UpdateDelayProperty =
			DependencyProperty.Register("UpdateDelay", typeof(TimeSpan), typeof(SearchBox),
			                            new FrameworkPropertyMetadata(TimeSpan.FromMilliseconds(200)));
		
		#endregion
		
		#region Public Properties
		
		public string WatermarkText {
			get { return (string)GetValue(WatermarkTextProperty); }
			set { SetValue(WatermarkTextProperty, value); }
		}

		public Brush WatermarkColor {
			get { return (Brush)GetValue(WatermarkColorProperty); }
			set { SetValue(WatermarkColorProperty, value); }
		}
		
		public bool HasText {
			get { return (bool)GetValue(HasTextProperty); }
			private set { SetValue(HasTextProperty, value); }
		}

		public TimeSpan UpdateDelay {
			get { return (TimeSpan)GetValue(UpdateDelayProperty); }
			set { SetValue(UpdateDelayProperty, value); }
		}
		
		#endregion
		
		#region Handlers

		private void IconBorder_MouseLeftButtonUp(object obj, MouseButtonEventArgs e) {
			if (this.HasText)
				this.Text = string.Empty;
		}

		#endregion
		
		#region Overrides
		
		DispatcherTimer timer;
		
		protected override void OnTextChanged(TextChangedEventArgs e) {
			base.OnTextChanged(e);
			
			HasText = this.Text.Length > 0;
			if (timer == null) {
				timer = new DispatcherTimer();
				timer.Tick += timer_Tick;
			}
			timer.Stop();
			timer.Interval = this.UpdateDelay;
			timer.Start();
		}

		void timer_Tick(object sender, EventArgs e)
		{
			timer.Stop();
			timer = null;
			var textBinding = GetBindingExpression(TextProperty);
			if (textBinding != null) {
				textBinding.UpdateSource();
			}
		}
		
		protected override void OnLostFocus(RoutedEventArgs e)
		{
			if (!HasText) {
				Label wl = (Label)GetTemplateChild("WatermarkLabel");
				if (wl != null)
					wl.Visibility = Visibility.Visible;
			}
			
			base.OnLostFocus(e);
		}
		
		protected override void OnGotFocus(RoutedEventArgs e)
		{
			if (!HasText) {
				Label wl = (Label)GetTemplateChild("WatermarkLabel");
				if (wl != null)
					wl.Visibility = Visibility.Hidden;
			}
			
			base.OnGotFocus(e);
		}

		public override void OnApplyTemplate() {
			base.OnApplyTemplate();

			Border iconBorder = GetTemplateChild("PART_IconBorder") as Border;
			if (iconBorder != null) {
				iconBorder.MouseLeftButtonUp += IconBorder_MouseLeftButtonUp;
			}
		}
		
		protected override void OnKeyDown(KeyEventArgs e)
		{
			if (e.Key == Key.Escape && this.Text.Length > 0) {
				this.Text = string.Empty;
				e.Handled = true;
			} else {
				base.OnKeyDown(e);
			}
		}
		#endregion
	}
}
