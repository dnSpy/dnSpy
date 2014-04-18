// Copyright (c) AlphaSierraPapa for the SharpDevelop Team
// This code is distributed under the MS-PL (for details please see \doc\MS-PL.txt)

using System;
using System.Collections.Generic;

namespace Ricciolo.StylesExplorer.MarkupReflection
{
	public class KeyMapping
	{
		List<object> staticResources;
		
		public List<object> StaticResources {
			get { return staticResources; }
		}
		
		public bool HasStaticResource(int identifier)
		{
			return staticResources != null && staticResources.Count > identifier;
		}
		
		public string KeyString { get; set; }
		public bool Shared { get; set; }
		public bool SharedSet { get; set; }
		
		public int Position { get; set; }
		
		public KeyMapping()
		{
			this.staticResources = new List<object>();
			this.Position = -1;
		}
		
		public KeyMapping(string key)
		{
			this.KeyString = key;
			this.staticResources = new List<object>();
			this.Position = -1;
		}
		
		public override string ToString()
		{
			return '"' + KeyString + '"';
		}

	}
}
