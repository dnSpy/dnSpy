// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Globalization;
using System.Linq;

using ICSharpCode.AvalonEdit.Document;

namespace ICSharpCode.AvalonEdit.Xml
{
	/// <summary>
	/// The root object of the XML document
	/// </summary>
	public class AXmlDocument: AXmlContainer
	{
		/// <summary> Parser that produced this document </summary>
		internal AXmlParser Parser { get; set; }
		
		/// <summary> Occurs when object is added to any part of the document </summary>
		public event EventHandler<NotifyCollectionChangedEventArgs> ObjectInserted;
		/// <summary> Occurs when object is removed from any part of the document </summary>
		public event EventHandler<NotifyCollectionChangedEventArgs> ObjectRemoved;
		/// <summary> Occurs before local data of any object in the document changes </summary>
		public event EventHandler<AXmlObjectEventArgs> ObjectChanging;
		/// <summary> Occurs after local data of any object in the document changed </summary>
		public event EventHandler<AXmlObjectEventArgs> ObjectChanged;
		
		internal void OnObjectInserted(int index, AXmlObject obj)
		{
			if (ObjectInserted != null)
				ObjectInserted(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, new AXmlObject[] { obj }.ToList(), index));
		}
		
		internal void OnObjectRemoved(int index, AXmlObject obj)
		{
			if (ObjectRemoved != null)
				ObjectRemoved(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, new AXmlObject[] { obj }.ToList(), index));
		}
		
		internal void OnObjectChanging(AXmlObject obj)
		{
			if (ObjectChanging != null)
				ObjectChanging(this, new AXmlObjectEventArgs() { Object = obj } );
		}
		
		internal void OnObjectChanged(AXmlObject obj)
		{
			if (ObjectChanged != null)
				ObjectChanged(this, new AXmlObjectEventArgs() { Object = obj } );
		}
		
		/// <inheritdoc/>
		public override void AcceptVisitor(IAXmlVisitor visitor)
		{
			visitor.VisitDocument(this);
		}
		
		/// <inheritdoc/>
		public override string ToString()
		{
			return string.Format(CultureInfo.InvariantCulture, "[{0} Chld:{1}]", base.ToString(), this.Children.Count);
		}
	}
}
