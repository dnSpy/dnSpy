// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Reflection;

namespace Debugger.MetaData
{
	public class DebugParameterInfo : System.Reflection.ParameterInfo
	{
		ValueGetter getter;
		MemberInfo member;
		string name;
		Type parameterType;
		int position;
		
		/// <inheritdoc/>
		public override MemberInfo Member {
			get { return member; }
		}
		
		/// <inheritdoc/>
		public override string Name {
			get { return name; }
		}
		
		/// <inheritdoc/>
		public override Type ParameterType {
			get { return parameterType; }
		}
		
		/// <inheritdoc/>
		public override int Position {
			get { return position; }
		}
		
		public DebugParameterInfo(MemberInfo member, string name, Type parameterType, int position, ValueGetter getter)
		{
			this.member = member;
			this.name = name;
			this.parameterType = parameterType;
			this.position = position;
			this.getter = getter;
		}
		
		public Value GetValue(StackFrame context)
		{
			return getter(context);
		}
			
		//		public virtual ParameterAttributes Attributes { get; }
		//		public virtual object DefaultValue { get; }		
		//		public virtual object RawDefaultValue { get; }
		//		
		//		public virtual object[] GetCustomAttributes(bool inherit);
		//		public virtual object[] GetCustomAttributes(Type attributeType, bool inherit);
		//		public virtual Type[] GetOptionalCustomModifiers();
		//		public virtual Type[] GetRequiredCustomModifiers();
		//		public virtual bool IsDefined(Type attributeType, bool inherit);
		
		/// <inheritdoc/>
		public override string ToString()
		{
			return this.ParameterType + " " + this.Name;
		}
	}
}
