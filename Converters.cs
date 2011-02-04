// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Markup;
using System.Windows.Data;
using System.Globalization;

namespace ICSharpCode.TreeView
{
	public class CollapsedWhenFalse : MarkupExtension, IValueConverter
	{
		public static CollapsedWhenFalse Instance = new CollapsedWhenFalse();

		public override object ProvideValue(IServiceProvider serviceProvider)
		{
			return Instance;
		}

		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return (bool)value ? Visibility.Visible : Visibility.Collapsed;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}
