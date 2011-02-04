// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Windows;
using System.Windows.Markup;

namespace ICSharpCode.ILSpy
{
	/// <summary>
	/// ExtensionMethods that help with WPF.
	/// </summary>
	public static class ExtensionMethods
	{
		/// <summary>
		/// Sets the value of a dependency property on <paramref name="targetObject"/> using a markup extension.
		/// </summary>
		/// <remarks>This method does not support markup extensions like x:Static that depend on
		/// having a XAML file as context.</remarks>
		public static void SetValueToExtension(this DependencyObject targetObject, DependencyProperty property, MarkupExtension markupExtension)
		{
			// This method was copied from ICSharpCode.Core.Presentation
			
			if (targetObject == null)
				throw new ArgumentNullException("targetObject");
			if (property == null)
				throw new ArgumentNullException("property");
			if (markupExtension == null)
				throw new ArgumentNullException("markupExtension");
			
			var serviceProvider = new SetValueToExtensionServiceProvider(targetObject, property);
			targetObject.SetValue(property, markupExtension.ProvideValue(serviceProvider));
		}
		
		sealed class SetValueToExtensionServiceProvider : IServiceProvider, IProvideValueTarget
		{
			// This class  was copied from ICSharpCode.Core.Presentation
			
			readonly DependencyObject targetObject;
			readonly DependencyProperty targetProperty;
			
			public SetValueToExtensionServiceProvider(DependencyObject targetObject, DependencyProperty property)
			{
				this.targetObject = targetObject;
				this.targetProperty = property;
			}
			
			public object GetService(Type serviceType)
			{
				if (serviceType == typeof(IProvideValueTarget))
					return this;
				else
					return null;
			}
			
			public object TargetObject {
				get { return targetObject; }
			}
			
			public object TargetProperty {
				get { return targetProperty; }
			}
		}
	}
}
