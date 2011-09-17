// Copyright (c) AlphaSierraPapa for the SharpDevelop Team
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this
// software and associated documentation files (the "Software"), to deal in the Software
// without restriction, including without limitation the rights to use, copy, modify, merge,
// publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons
// to whom the Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or
// substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
// PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE
// FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
// OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.

using System;
using System.Collections.Generic;
using ICSharpCode.NRefactory.Utils;

namespace ICSharpCode.NRefactory.TypeSystem.Implementation
{
	/// <summary>
	/// Type definition that represents a partial class with multiple parts.
	/// </summary>
	[Serializable]
	public class CompoundTypeDefinition : DefaultTypeDefinition
	{
		IList<ITypeDefinition> parts;
		
		private CompoundTypeDefinition(ITypeDefinition declaringTypeDefinition, string name)
			: base(declaringTypeDefinition, name)
		{
		}
		
		private CompoundTypeDefinition(IParsedFile parsedFile, string ns, string name)
			: base(parsedFile, ns, name)
		{
		}
		
		private CompoundTypeDefinition(IProjectContent projectContent, string ns, string name)
			: base(projectContent, ns, name)
		{
		}
		
		protected override void FreezeInternal()
		{
			parts = FreezeList(parts);
			base.FreezeInternal();
		}
		
		public override IList<ITypeDefinition> GetParts()
		{
			return parts;
		}
		
		public override string Documentation {
			get { return parts[0].Documentation; }
		}
		
		public static ITypeDefinition Create(IList<ITypeDefinition> parts)
		{
			if (parts == null || parts.Count == 0)
				throw new ArgumentException("parts");
			
			ITypeDefinition mainPart = parts[0];
			for (int i = 1; i < parts.Count; i++) {
				if (PreferAsMainPart(parts[i], mainPart))
					mainPart = parts[i];
			}
			if (parts.Count == 1) {
				((DefaultTypeDefinition)mainPart).SetCompoundTypeDefinition(mainPart);
				return mainPart;
			}
			
			CompoundTypeDefinition compound;
			if (mainPart.DeclaringTypeDefinition != null) {
				throw new NotImplementedException("nested compound types not implemented");
			} else {
				if (mainPart.ParsedFile != null)
					compound = new CompoundTypeDefinition(mainPart.ParsedFile, mainPart.Namespace, mainPart.Name);
				else
					compound = new CompoundTypeDefinition(mainPart.ProjectContent, mainPart.Namespace, mainPart.Name);
			}
			compound.parts = parts;
			compound.Region = mainPart.Region;
			compound.BodyRegion = mainPart.BodyRegion;
			compound.TypeParameters.AddRange(mainPart.TypeParameters);
			compound.IsSynthetic = mainPart.IsSynthetic;
			compound.Accessibility = mainPart.Accessibility;
			
			bool allPartsFrozen = true;
			foreach (DefaultTypeDefinition part in parts) {
				compound.BaseTypes.AddRange(part.BaseTypes);
				compound.Attributes.AddRange(part.Attributes);
				compound.NestedTypes.AddRange(part.NestedTypes);
				compound.Methods.AddRange(part.Methods);
				compound.Properties.AddRange(part.Properties);
				compound.Events.AddRange(part.Events);
				compound.Fields.AddRange(part.Fields);
				
				if (part.IsAbstract)
					compound.IsAbstract = true;
				if (part.IsSealed)
					compound.IsSealed = true;
				if (part.IsShadowing)
					compound.IsShadowing = true;
				if (part.HasExtensionMethods)
					compound.HasExtensionMethods = true;
				if (part.AddDefaultConstructorIfRequired)
					compound.AddDefaultConstructorIfRequired = true;
				
				// internal is the default, so use another part's accessibility until we find a non-internal accessibility
				if (compound.Accessibility == Accessibility.Internal)
					compound.Accessibility = part.Accessibility;
				
				allPartsFrozen &= part.IsFrozen;
			}
			
			if (allPartsFrozen) {
				// If all parts are frozen, also freeze the compound typedef.
				compound.Freeze();
			}
			// Publish the compound class via part.compoundTypeDefinition only after it has been frozen.
			foreach (DefaultTypeDefinition part in parts) {
				part.SetCompoundTypeDefinition(compound);
			}
			
			return compound;
		}
		
		/// <summary>
		/// Gets whether part1 should be preferred as main part over part2.
		/// </summary>
		static bool PreferAsMainPart(ITypeDefinition part1, ITypeDefinition part2)
		{
			if (part1.IsSynthetic != part2.IsSynthetic)
				return part2.IsSynthetic; // prefer non-synthetic part
			string file1 = part1.Region.FileName;
			string file2 = part2.Region.FileName;
			if ((file1 != null) != (file2 != null))
				return file1 != null; // prefer part with file name
			if (file1 != null && file2 != null) {
				return file1.Length < file2.Length; // prefer shorter file name (file without Designer suffix)
			}
			return false;
		}
	}
}
