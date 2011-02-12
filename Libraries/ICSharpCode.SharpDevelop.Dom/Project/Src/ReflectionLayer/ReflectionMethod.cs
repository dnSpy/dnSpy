// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Reflection;

namespace ICSharpCode.SharpDevelop.Dom.ReflectionLayer
{
	internal class ReflectionMethod : DefaultMethod
	{
		internal static void ApplySpecialsFromAttributes(DefaultMethod m)
		{
			if (m.IsStatic) {
				foreach (IAttribute a in m.Attributes) {
					string attributeName = a.AttributeType.FullyQualifiedName;
					if (attributeName == "System.Runtime.CompilerServices.ExtensionAttribute"
					    || attributeName == "Boo.Lang.ExtensionAttribute")
					{
						m.IsExtensionMethod = true;
					}
				}
			}
		}
		
		public ReflectionMethod(MethodBase methodBase, ReflectionClass declaringType)
			: base(declaringType, methodBase is ConstructorInfo ? "#ctor" : methodBase.Name)
		{
			if (methodBase is MethodInfo) {
				MethodInfo m = ((MethodInfo)methodBase);
				this.ReturnType = ReflectionReturnType.Create(this, m.ReturnType, attributeProvider: m.ReturnTypeCustomAttributes);
			} else if (methodBase is ConstructorInfo) {
				this.ReturnType = DeclaringType.DefaultReturnType;
			}
			
			foreach (ParameterInfo paramInfo in methodBase.GetParameters()) {
				this.Parameters.Add(new ReflectionParameter(paramInfo, this));
			}
			
			if (methodBase.IsGenericMethodDefinition) {
				foreach (Type g in methodBase.GetGenericArguments()) {
					this.TypeParameters.Add(new DefaultTypeParameter(this, g));
				}
				int i = 0;
				foreach (Type g in methodBase.GetGenericArguments()) {
					ReflectionClass.AddConstraintsFromType(this.TypeParameters[i++], g);
				}
			}
			
			ModifierEnum modifiers  = ModifierEnum.None;
			if (methodBase.IsStatic) {
				modifiers |= ModifierEnum.Static;
			}
			if (methodBase.IsPrivate) { // I assume that private is used most and public last (at least should be)
				modifiers |= ModifierEnum.Private;
			} else if (methodBase.IsFamily || methodBase.IsFamilyOrAssembly) {
				modifiers |= ModifierEnum.Protected;
			} else if (methodBase.IsPublic) {
				modifiers |= ModifierEnum.Public;
			} else {
				modifiers |= ModifierEnum.Internal;
			}
			
			if (methodBase.IsFinal) {
				modifiers |= ModifierEnum.Sealed;
			} else if (methodBase.IsAbstract) {
				modifiers |= ModifierEnum.Abstract;
			} else if (methodBase.IsVirtual) {
				if ((methodBase.Attributes & MethodAttributes.NewSlot) != 0)
					modifiers |= ModifierEnum.Virtual;
				else
					modifiers |= ModifierEnum.Override;
			}
			
			this.Modifiers = modifiers;
			
			ReflectionClass.AddAttributes(declaringType.ProjectContent, this.Attributes, CustomAttributeData.GetCustomAttributes(methodBase));
			ApplySpecialsFromAttributes(this);
		}
	}
}
