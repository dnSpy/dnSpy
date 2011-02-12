// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Diagnostics;

namespace ICSharpCode.SharpDevelop.Dom
{
	/// <summary>
	/// A class made up of multiple partial classes.
	/// 
	/// CompoundClass is immutable, it freezes the underlying DefaultClass in the constructor.
	/// The constructor also freezes all parts to ensure that the methods/properties/fields/events of a
	/// CompoundClass never change.
	/// When you want to build add or remove parts from a CompoundClass, you need to create a new
	/// CompoundClass instance with the new parts.
	/// </summary>
	public sealed class CompoundClass : DefaultClass
	{
		/// <summary>
		/// The parts this class is based on.
		/// </summary>
		readonly ReadOnlyCollection<IClass> parts;
		
		/// <summary>
		/// Gets the parts this class is based on.
		/// </summary>
		public ReadOnlyCollection<IClass> Parts {
			get {
				return parts;
			}
		}
		
		/// <summary>
		/// Creates a new CompoundClass with the specified parts.
		/// </summary>
		public static CompoundClass Create(IEnumerable<IClass> parts)
		{
			// Ensure that the list of parts does not change.
			var p = parts.ToList();
			foreach (IClass c in p) {
				c.Freeze();
			}
			return new CompoundClass(p);
		}
		
		private CompoundClass(List<IClass> parts) : base(new DefaultCompilationUnit(parts[0].ProjectContent), parts[0].FullyQualifiedName)
		{
			this.CompilationUnit.Classes.Add(this);
			
			this.parts = parts.AsReadOnly();
			
			UpdateInformationFromParts();
			this.CompilationUnit.Freeze();
			Debug.Assert(this.IsFrozen);
		}
		
		/// <summary>
		/// Calculate information from class parts (Modifier, Base classes, Type parameters etc.)
		/// </summary>
		void UpdateInformationFromParts()
		{
			// Common for all parts:
			this.ClassType = parts[0].ClassType;
			
			ModifierEnum modifier = ModifierEnum.None;
			const ModifierEnum defaultClassVisibility = ModifierEnum.Internal;
			
			this.BaseTypes.Clear();
			this.InnerClasses.Clear();
			this.Attributes.Clear();
			this.Methods.Clear();
			this.Properties.Clear();
			this.Events.Clear();
			this.Fields.Clear();
			
			string shortestFileName = null;
			
			foreach (IClass part in parts) {
				if (!string.IsNullOrEmpty(part.CompilationUnit.FileName)) {
					if (shortestFileName == null || part.CompilationUnit.FileName.Length < shortestFileName.Length) {
						shortestFileName = part.CompilationUnit.FileName;
						this.Region = part.Region;
					}
				}
				
				if ((part.Modifiers & ModifierEnum.VisibilityMask) != defaultClassVisibility) {
					modifier |= part.Modifiers;
				} else {
					modifier |= part.Modifiers &~ ModifierEnum.VisibilityMask;
				}
				foreach (IReturnType rt in part.BaseTypes) {
					if (!rt.IsDefaultReturnType || rt.FullyQualifiedName != "System.Object") {
						this.BaseTypes.Add(rt);
					}
				}
				this.InnerClasses.AddRange(part.InnerClasses);
				this.Attributes.AddRange(part.Attributes);
				this.Methods.AddRange(part.Methods);
				this.Properties.AddRange(part.Properties);
				this.Events.AddRange(part.Events);
				this.Fields.AddRange(part.Fields);
				
				this.AddDefaultConstructorIfRequired |= part.AddDefaultConstructorIfRequired;
			}
			this.CompilationUnit.FileName = shortestFileName;
			if ((modifier & ModifierEnum.VisibilityMask) == ModifierEnum.None) {
				modifier |= defaultClassVisibility;
			}
			this.Modifiers = modifier;
		}
		
		/// <summary>
		/// Type parameters are the same on all parts.
		/// </summary>
		public override IList<ITypeParameter> TypeParameters {
			get {
				// Locking for the time of getting the reference to the sub-list is sufficient:
				// Classes used for parts never change, instead the whole part is replaced with
				// a new IClass instance.
				return parts[0].TypeParameters;
			}
			set {
				throw new NotSupportedException();
			}
		}
	}
}
