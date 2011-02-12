// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Reflection;

namespace ICSharpCode.SharpDevelop.Dom.ReflectionLayer
{
	internal class ReflectionEvent : DefaultEvent
	{
		public ReflectionEvent(EventInfo eventInfo, IClass declaringType) : base(declaringType, eventInfo.Name)
		{
			this.ReturnType = ReflectionReturnType.Create(this, eventInfo.EventHandlerType, attributeProvider: eventInfo);
			
			// get modifiers
			MethodInfo methodBase = null;
			try {
				methodBase = eventInfo.GetAddMethod(true);
			} catch (Exception) {}
			
			if (methodBase == null) {
				try {
					methodBase = eventInfo.GetRemoveMethod(true);
				} catch (Exception) {}
			}
			
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
			} else {
				// assume public property, if no methodBase could be get.
				modifiers = ModifierEnum.Public;
			}
			this.Modifiers = modifiers;
			
		}
	}
}
