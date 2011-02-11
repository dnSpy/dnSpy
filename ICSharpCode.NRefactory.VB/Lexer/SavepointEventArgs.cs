// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;

namespace ICSharpCode.NRefactory.VB.Parser
{
	public class SavepointEventArgs : EventArgs
	{
		public Location SavepointLocation { get; private set; }
		public VBLexerMemento State { get; private set; }
		
		public SavepointEventArgs(Location savepointLocation, VBLexerMemento state)
		{
			this.SavepointLocation = savepointLocation;
			this.State = state;
		}
	}
}
