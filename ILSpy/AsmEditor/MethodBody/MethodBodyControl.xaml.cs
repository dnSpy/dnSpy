/*
    Copyright (C) 2014-2015 de4dot@gmail.com

    This file is part of dnSpy

    dnSpy is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    dnSpy is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with dnSpy.  If not, see <http://www.gnu.org/licenses/>.
*/

using System.Windows;
using System.Windows.Controls;

namespace ICSharpCode.ILSpy.AsmEditor.MethodBody
{
	/// <summary>
	/// Interaction logic for MethodBodyControl.xaml
	/// </summary>
	public partial class MethodBodyControl : UserControl
	{
		LocalsListHelper localsListHelper;
		InstructionsListHelper instructionsListHelper;
		ExceptionHandlersListHelper exceptionHandlersListHelper;

		public MethodBodyControl()
		{
			InitializeComponent();
			DataContextChanged += MethodBodyControl_DataContextChanged;
			Loaded += MethodBodyControl_Loaded;
		}

		void MethodBodyControl_Loaded(object sender, RoutedEventArgs e)
		{
			Loaded -= MethodBodyControl_Loaded;
			SetFocusToControl();
		}

		void SetFocusToControl()
		{
			var data = DataContext as MethodBodyVM;
			if (data == null)
				return;

			if (data.IsCilBody)
				instructionsListView.Focus();
			else
				rvaTextBox.Focus();
		}

		void MethodBodyControl_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
		{
			var data = DataContext as MethodBodyVM;
			if (data == null)
				return;

			var ownerWindow = Window.GetWindow(this);
			localsListHelper = new LocalsListHelper(localsListView, ownerWindow);
			instructionsListHelper = new InstructionsListHelper(instructionsListView, ownerWindow);
			exceptionHandlersListHelper = new ExceptionHandlersListHelper(ehListView, ownerWindow);

			localsListHelper.OnDataContextChanged(data);
			instructionsListHelper.OnDataContextChanged(data);
			exceptionHandlersListHelper.OnDataContextChanged(data);
		}
	}
}
