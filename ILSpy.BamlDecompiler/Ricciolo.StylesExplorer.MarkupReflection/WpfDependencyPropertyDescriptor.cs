// Copyright (c) Cristian Civera (cristian@aspitalia.com)
// This code is distributed under the MS-PL (for details please see \doc\MS-PL.txt)

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace Ricciolo.StylesExplorer.MarkupReflection
{
	public class WpfDependencyPropertyDescriptor : MarshalByRefObject, IDependencyPropertyDescriptor
	{
		private readonly DependencyPropertyDescriptor _propertyDescriptor;

		public WpfDependencyPropertyDescriptor(DependencyPropertyDescriptor propertyDescriptor)
		{
			if (propertyDescriptor == null) throw new ArgumentNullException("propertyDescriptor");
			_propertyDescriptor = propertyDescriptor;
		}

		#region IDependencyPropertyDescriptor Members

		public bool IsAttached
		{
			get { return _propertyDescriptor.IsAttached; }
		}

		#endregion

		public override object InitializeLifetimeService()
		{
			return null;
		}
	}
}
