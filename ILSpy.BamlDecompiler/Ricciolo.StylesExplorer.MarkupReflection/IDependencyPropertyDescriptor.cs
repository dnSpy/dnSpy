// Copyright (c) Cristian Civera (cristian@aspitalia.com)
// This code is distributed under the MS-PL (for details please see \doc\MS-PL.txt)

using System;
using System.Collections.Generic;
using System.Text;

namespace Ricciolo.StylesExplorer.MarkupReflection
{
	public interface IDependencyPropertyDescriptor
	{
		bool IsAttached { get; }
	}
	
	public class UnresolvableDependencyPropertyDescriptor : IDependencyPropertyDescriptor
	{
		public bool IsAttached {
			get {
				return false;
			}
		}
	}
}
