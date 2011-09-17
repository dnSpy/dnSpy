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
using System.Diagnostics.Contracts;

namespace ICSharpCode.NRefactory.TypeSystem
{
	/// <summary>
	/// Represents a class, enum, interface, struct, delegate or VB module.
	/// Also used to represent a part of a partial class.
	/// </summary>
	#if WITH_CONTRACTS
	[ContractClass(typeof(ITypeDefinitionContract))]
	#endif
	public interface ITypeDefinition : IType, IEntity
	{
		IList<ITypeReference> BaseTypes { get; }
		IList<ITypeParameter> TypeParameters { get; }
		
		/// <summary>
		/// If this is a compound class (combination of class parts), this method retrieves all individual class parts.
		/// Otherwise, a list containing <c>this</c> is returned.
		/// </summary>
		IList<ITypeDefinition> GetParts();
		
		IList<ITypeDefinition> NestedTypes { get; }
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
