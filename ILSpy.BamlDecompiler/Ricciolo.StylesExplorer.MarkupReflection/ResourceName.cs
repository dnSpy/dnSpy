// Copyright (c) Cristian Civera (cristian@aspitalia.com)
// This code is distributed under the MS-PL (for details please see \doc\MS-PL.txt)

using System;
using System.Collections.Generic;
using System.Text;

namespace Ricciolo.StylesExplorer.MarkupReflection
{
	public class ResourceName
	{
		private string name;

		public ResourceName(string name)
		{
			this.name = name;
		}

		public override string ToString()
		{
			return this.Name;
		}

		public string Name
		{
			get
			{
				return this.name;
			}
		}
	}
}
