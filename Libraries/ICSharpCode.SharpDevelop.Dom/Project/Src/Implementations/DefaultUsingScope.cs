// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;

namespace ICSharpCode.SharpDevelop.Dom
{
	public class DefaultUsingScope : AbstractFreezable, IUsingScope
	{
		DomRegion region;
		IUsingScope parent;
		IList<IUsing> usings;
		IList<IUsingScope> childScopes;
		string namespaceName = "";
		
		protected override void FreezeInternal()
		{
			base.FreezeInternal();
			usings = FreezeList(usings);
			childScopes = FreezeList(childScopes);
		}
		
		public DomRegion Region {
			get { return region; }
			set {
				CheckBeforeMutation();
				region = value;
			}
		}
		
		public IUsingScope Parent {
			get { return parent; }
			set {
				CheckBeforeMutation();
				parent = value;
			}
		}
		
		public virtual IList<IUsing> Usings {
			get {
				if (usings == null)
					usings = new List<IUsing>();
				return usings;
			}
		}
		
		public virtual IList<IUsingScope> ChildScopes {
			get {
				if (childScopes == null)
					childScopes = new List<IUsingScope>();
				return childScopes;
			}
		}
		
		public string NamespaceName {
			get { return namespaceName; }
			set {
				if (value == null)
					throw new ArgumentNullException("NamespaceName");
				CheckBeforeMutation();
				namespaceName = value;
			}
		}
	}
}
