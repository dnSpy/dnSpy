// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Reflection;

namespace ICSharpCode.SharpDevelop.Dom.ReflectionLayer
{
	internal class ReflectionProperty : DefaultProperty
	{
		public ReflectionProperty(PropertyInfo propertyInfo, IClass declaringType) : base(declaringType, propertyInfo.Name)
		{
			this.ReturnType = ReflectionReturnType.Create(this, propertyInfo.PropertyType, attributeProvider: propertyInfo);
			
			CanGet = propertyInfo.CanRead;
			CanSet = propertyInfo.CanWrite;
			
			ParameterInfo[] parameterInfo = propertyInfo.GetIndexParameters();
			if (parameterInfo != null && parameterInfo.Length > 0) {
				// check if this property is an indexer (=default member of parent class)
				foreach (MemberInfo memberInfo in propertyInfo.DeclaringType.GetDefaultMembers()) {
					if (memberInfo == propertyInfo) {
						this.IsIndexer = true;
						break;
					}
				}
				// there are only few properties with parameters, so we can load them immediately
				foreach (ParameterInfo info in parameterInfo) {
					this.Parameters.Add(new ReflectionParameter(info, this));
				}
			}
			
			MethodInfo getterMethod = null;
			try {
				getterMethod = propertyInfo.GetGetMethod(true);
			} catch (Exception) {}
			
			MethodInfo setterMethod = null;
			try {
				setterMethod = propertyInfo.GetSetMethod(true);
			} catch (Exception) {}
			
			MethodInfo methodBase = getterMethod ?? setterMethod;
			
			ModifierEnum modifiers  = ModifierEnum.None;
			if (methodBase != null) {
				if (methodBase.IsStatic) {
					modifiers |= ModifierEnum.Static;
				}
				
				if (methodBase.IsAssembly) {
					modifiers |= ModifierEnum.Internal;
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
					modifiers |= ModifierEnum.Virtual;
				}
			} else { // assume public property, if no methodBase could be get.
				modifiers = ModifierEnum.Public;
			}
			this.Modifiers = modifiers;
			if (getterMethod != null) {
				ModifierEnum getterModifier = GetAccessorModifier(getterMethod);
				if (getterModifier == ModifierEnum.Private) {
					this.CanGet = false;
				} else {
					if (getterModifier != (modifiers & ModifierEnum.VisibilityMask))
						this.GetterModifiers = getterModifier;
				}
			}
			if (setterMethod != null) {
				ModifierEnum setterModifier = GetAccessorModifier(setterMethod);
				if (setterModifier == ModifierEnum.Private) {
					this.CanSet = false;
				} else {
					if (setterModifier != (modifiers & ModifierEnum.VisibilityMask))
						this.SetterModifiers = setterModifier;
				}
			}
		}
		
		static ModifierEnum GetAccessorModifier(MethodInfo accessor)
		{
			if (accessor.IsPublic) {
				return ModifierEnum.Public;
			} else if (accessor.IsFamily || accessor.IsFamilyOrAssembly) {
				return ModifierEnum.Protected;
			} else {
				return ModifierEnum.Private; // or internal, we don't care about that difference
			}
		}
	}
}
