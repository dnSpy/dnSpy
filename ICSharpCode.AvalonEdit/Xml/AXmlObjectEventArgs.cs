// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;

using ICSharpCode.AvalonEdit.Document;

namespace ICSharpCode.AvalonEdit.Xml
{
	/// <summary> Holds event args for event caused by <see cref="AXmlObject"/> </summary>
	public class AXmlObjectEventArgs: EventArgs
	{
		/// <summary> The object that caused the event </summary>
		public AXmlObject Object { get; set; }
	}
}
