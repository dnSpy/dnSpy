// Copyright (c) 2010 AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;

namespace ICSharpCode.NRefactory.TypeSystem
{
	/// <summary>
	/// Represents a class, enum, interface, struct, delegate or VB module.
	/// </summary>
	#if WITH_CONTRACTS
	[ContractClass(typeof(ITypeDefinitionContract))]
	#endif
	public interface ITypeDefinition : IType, IEntity
	{
		ClassType ClassType { get; }
		
		IList<ITypeReference> BaseTypes { get; }
		IList<ITypeParameter> TypeParameters { get; }
		
		/// <summary>
		/// If this is a partial class, gets the compound class containing information from all parts.
		/// If this is not a partial class, a reference to this class is returned.
		/// 
		/// This method will always retrieve the latest version of the class, which might not contain this class as a part.
		/// </summary>
		ITypeDefinition GetCompoundClass();
		
		/// <summary>
		/// If this is a compound class (combination of class parts), this method retrieves all individual class parts.
		/// Otherwise, a list containing <c>this</c> is returned.
		/// </summary>
		IList<ITypeDefinition> GetParts();
		
		IList<ITypeDefinition> InnerClasses { get; }
		IList<IField> Fields { get; }
		IList<IProperty> Properties { get; }
		IList<IMethod> Methods { get; }
		IList<IEvent> Events { get; }
		
		/// <summary>
		/// Gets all members declared in this class. This is the union of Fields,Properties,Methods and Events.
		/// </summary>
		IEnumerable<IMember> Members { get; }
		
		/// <summary>
		/// Gets whether this type contains extension methods.
		/// </summary>
		/// <remarks>This property is used to speed up the search for extension methods.</remarks>
		bool HasExtensionMethods { get; }
	}
	
	#if WITH_CONTRACTS
	[ContractClassFor(typeof(ITypeDefinition))]
	abstract class ITypeDefinitionContract : ITypeContract, ITypeDefinition
	{
		ClassType ITypeDefinition.ClassType {
			get { return default(ClassType); }
		}
		
		IList<ITypeReference> ITypeDefinition.BaseTypes {
			get {
				Contract.Ensures(Contract.Result<IList<ITypeReference>>() != null);
				return null;
			}
		}
		
		IList<ITypeParameter> ITypeDefinition.TypeParameters {
			get {
				Contract.Ensures(Contract.Result<IList<ITypeParameter>>() != null);
				return null;
			}
		}
		
		IList<ITypeDefinition> ITypeDefinition.InnerClasses {
			get {
				Contract.Ensures(Contract.Result<IList<ITypeDefinition>>() != null);
				return null;
			}
		}
		
		IList<IField> ITypeDefinition.Fields {
			get {
				Contract.Ensures(Contract.Result<IList<IField>>() != null);
				return null;
			}
		}
		
		IList<IProperty> ITypeDefinition.Properties {
			get {
				Contract.Ensures(Contract.Result<IList<IProperty>>() != null);
				return null;
			}
		}
		
		IList<IMethod> ITypeDefinition.Methods {
			get {
				Contract.Ensures(Contract.Result<IList<IMethod>>() != null);
				return null;
			}
		}
		
		IList<IEvent> ITypeDefinition.Events {
			get {
				Contract.Ensures(Contract.Result<IList<IEvent>>() != null);
				return null;
			}
		}
		
		IEnumerable<IMember> ITypeDefinition.Members {
			get {
				Contract.Ensures(Contract.Result<IEnumerable<IMember>>() != null);
				return null;
			}
		}
		
		ITypeDefinition ITypeDefinition.GetCompoundClass()
		{
			Contract.Ensures(Contract.Result<ITypeDefinition>() != null);
			return null;
		}
		
		IList<ITypeDefinition> ITypeDefinition.GetParts()
		{
			Contract.Ensures(Contract.Result<IList<ITypeDefinition>>() != null);
			return null;
		}
		
		bool ITypeDefinition.HasExtensionMethods {
			get { return default(bool); }
		}
		
		#region IEntity
		EntityType IEntity.EntityType {
			get { return EntityType.None; }
		}
		
		DomRegion IEntity.Region {
			get { return DomRegion.Empty; }
		}
		
		DomRegion IEntity.BodyRegion {
			get { return DomRegion.Empty; }
		}
		
		ITypeDefinition IEntity.DeclaringTypeDefinition {
			get { return null; }
		}
		
		IList<IAttribute> IEntity.Attributes {
			get { return null; }
		}
		
		string IEntity.Documentation {
			get { return null; }
		}
		
		bool IEntity.IsStatic {
			get { return false; }
		}
		
		Accessibility IEntity.Accessibility {
			get { return Accessibility.None; }
		}
		
		bool IEntity.IsAbstract {
			get { return false; }
		}
		
		bool IEntity.IsSealed {
			get { return false; }
		}
		
		bool IEntity.IsShadowing {
			get { return false; }
		}
		
		bool IEntity.IsSynthetic {
			get { return false; }
		}
		
		IProjectContent IEntity.ProjectContent {
			get { return null; }
		}
		
		bool IFreezable.IsFrozen {
			get { return false; }
		}
		
		void IFreezable.Freeze()
		{
		}
		#endregion
	}
	#endif
}
