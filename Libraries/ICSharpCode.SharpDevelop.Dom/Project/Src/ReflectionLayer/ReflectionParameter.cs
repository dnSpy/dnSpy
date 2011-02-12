// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Reflection;

namespace ICSharpCode.SharpDevelop.Dom.ReflectionLayer
{
	public class ReflectionParameter : DefaultParameter
	{
		public ReflectionParameter(ParameterInfo parameterInfo, IMember member) : base(parameterInfo.Name)
		{
			Type type = parameterInfo.ParameterType;
			
			this.ReturnType = ReflectionReturnType.Create(member, type, attributeProvider: parameterInfo);
			
			if (type.IsByRef && parameterInfo.IsOut) {
				this.Modifiers = ParameterModifiers.Out;
			} else if (type.IsByRef) {
				this.Modifiers = ParameterModifiers.Ref;
			}
			
			if (parameterInfo.IsOptional) {
				this.Modifiers |= ParameterModifiers.Optional;
			}
			if (type.IsArray && type != typeof(Array)) {
				foreach (CustomAttributeData data in CustomAttributeData.GetCustomAttributes(parameterInfo)) {
					if (data.Constructor.DeclaringType.FullName == typeof(ParamArrayAttribute).FullName) {
						this.Modifiers |= ParameterModifiers.Params;
						break;
					}
				}
			}
		}
	}
}
