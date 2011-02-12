// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;

namespace ICSharpCode.SharpDevelop.Dom.VBNet
{
	/// <summary>
	/// Description of VBNetCompilationUnit.
	/// </summary>
	public class VBNetCompilationUnit : DefaultCompilationUnit, IVBNetOptionProvider
	{
		IVBNetOptionProvider projectOptionProvider;
		
		public VBNetCompilationUnit(IProjectContent projectContent)
			: base(projectContent)
		{
			if (projectContent.Project is IVBNetOptionProvider)
				projectOptionProvider = projectContent.Project as IVBNetOptionProvider;
			else {
				infer = false;
				strict = false;
				@explicit = true;
				compare = CompareKind.Binary;
			}
		}
		
		bool? infer, strict, @explicit;
		CompareKind? compare;
		
		public bool? OptionInfer {
			get { return infer ?? projectOptionProvider.OptionInfer; }
			set { infer = value; }
		}
		
		public bool? OptionStrict {
			get { return strict ?? projectOptionProvider.OptionStrict; }
			set { strict = value; }
		}
		
		public bool? OptionExplicit {
			get { return @explicit ?? projectOptionProvider.OptionExplicit; }
			set { @explicit = value; }
		}
		
		public CompareKind? OptionCompare {
			get { return compare ?? projectOptionProvider.OptionCompare; }
			set { compare = value; }
		}
	}
}
