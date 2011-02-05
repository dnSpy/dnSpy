// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT X11 license (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;
using System.Threading;

using Mono.Cecil;
using Mono.Collections.Generic;

namespace ICSharpCode.ILSpy.Disassembler
{
	/// <summary>
	/// Disassembles type and member definitions.
	/// </summary>
	public class ReflectionDisassembler
	{
		ITextOutput output;
		CancellationToken cancellationToken;
		MethodBodyDisassembler methodBodyDisassembler;
		
		public ReflectionDisassembler(ITextOutput output, bool detectControlStructure, CancellationToken cancellationToken)
		{
			if (output == null)
				throw new ArgumentNullException("output");
			this.output = output;
			this.cancellationToken = cancellationToken;
			this.methodBodyDisassembler = new MethodBodyDisassembler(output, detectControlStructure, cancellationToken);
		}
		
		EnumNameCollection<MethodAttributes> methodAttributeFlags = new EnumNameCollection<MethodAttributes>() {
			{ MethodAttributes.Static, "static" },
			{ MethodAttributes.Final, "final" },
			{ MethodAttributes.Virtual, "virtual" },
			{ MethodAttributes.HideBySig, "hidebysig" },
			{ MethodAttributes.Abstract, "abstract" },
			{ MethodAttributes.SpecialName, "specialname" },
			{ MethodAttributes.PInvokeImpl, "pinvokeimpl" },
			{ MethodAttributes.UnmanagedExport, "export" },
			{ MethodAttributes.RTSpecialName, "rtspecialname" },
			{ MethodAttributes.RequireSecObject, "requiresecobj" },
			{ MethodAttributes.NewSlot, "newslot" }
		};
		
		EnumNameCollection<MethodAttributes> methodVisibility = new EnumNameCollection<MethodAttributes>() {
			{ MethodAttributes.Private, "private" },
			{ MethodAttributes.FamANDAssem, "famandassem" },
			{ MethodAttributes.Assembly, "assembly" },
			{ MethodAttributes.Family, "family" },
			{ MethodAttributes.FamORAssem, "famorassem" },
			{ MethodAttributes.Public, "public" },
		};
		
		EnumNameCollection<MethodCallingConvention> callingConvention = new EnumNameCollection<MethodCallingConvention>() {
			{ MethodCallingConvention.C, "unmanaged cdecl" },
			{ MethodCallingConvention.StdCall, "unmanaged stdcall" },
			{ MethodCallingConvention.ThisCall, "unmanaged thiscall" },
			{ MethodCallingConvention.FastCall, "unmanaged fastcall" },
			{ MethodCallingConvention.VarArg, "vararg" },
			{ MethodCallingConvention.Generic, "generic" },
		};
		
		EnumNameCollection<MethodImplAttributes> methodCodeType = new EnumNameCollection<MethodImplAttributes>() {
			{ MethodImplAttributes.IL, "cil" },
			{ MethodImplAttributes.Native, "native" },
			{ MethodImplAttributes.OPTIL, "optil" },
			{ MethodImplAttributes.Runtime, "runtime" },
		};
		
		EnumNameCollection<MethodImplAttributes> methodImpl = new EnumNameCollection<MethodImplAttributes>() {
			{ MethodImplAttributes.Synchronized, "synchronized" },
		};
		
		public void DisassembleMethod(MethodDefinition method)
		{
			//    .method public hidebysig  specialname
			//               instance default class [mscorlib]System.IO.TextWriter get_BaseWriter ()  cil managed
			//
			
			// write method header
			output.WriteDefinition(".method ", method);
			
			//emit flags
			WriteEnum(method.Attributes & MethodAttributes.MemberAccessMask, methodVisibility);
			WriteFlags(method.Attributes & ~MethodAttributes.MemberAccessMask, methodAttributeFlags);
			
			output.WriteLine();
			output.Indent();
			
			if (method.HasThis)
				output.Write("instance ");
			
			//call convention
			WriteEnum(method.CallingConvention & (MethodCallingConvention)0x1f, callingConvention);
			
			
			//return type
			method.ReturnType.WriteTo(output, false, true);
			output.Write(' ');
			output.Write(DisassemblerHelpers.Escape(method.Name));
			
			//( params )
			output.Write(" (");
			if (method.HasParameters) {
				output.WriteLine();
				output.Indent();
				WriteParameters(method.Parameters);
				output.Unindent();
			}
			output.Write(") ");
			//cil managed
			WriteEnum(method.ImplAttributes & MethodImplAttributes.CodeTypeMask, methodCodeType);
			if ((method.ImplAttributes & MethodImplAttributes.ManagedMask) == MethodImplAttributes.Managed)
				output.Write("managed ");
			else
				output.Write("unmanaged ");
			WriteFlags(method.ImplAttributes & ~(MethodImplAttributes.CodeTypeMask | MethodImplAttributes.ManagedMask), methodImpl);
			
			output.WriteLine();
			output.Unindent();
			OpenBlock();
			WriteAttributes(method.CustomAttributes);
			
			if (method.HasBody)
				methodBodyDisassembler.Disassemble(method.Body);
			
			CloseBlock();
		}
		
		void WriteParameters(Collection<ParameterDefinition> parameters)
		{
			for (int i = 0; i < parameters.Count; i++) {
				var p = parameters[i];
				p.ParameterType.WriteTo(output);
				output.Write(' ');
				output.WriteDefinition(DisassemblerHelpers.Escape(p.Name), p);
				if (i < parameters.Count - 1)
					output.Write(',');
				output.WriteLine();
			}
		}
		
		void WriteAttributes(Collection<CustomAttribute> attributes)
		{
			foreach (CustomAttribute a in attributes) {
				output.Write(".custom");
				a.Constructor.WriteTo(output);
				byte[] blob = a.GetBlob();
				if (blob != null) {
					output.Write(" = ");
					WriteBlob(blob);
				}
				output.WriteLine();
			}
		}
		
		void WriteBlob(byte[] blob)
		{
			output.Write("(");
			output.Indent();

			for (int i = 0; i < blob.Length; i++) {
				if (i % 16 == 0 && i < blob.Length - 1) {
					output.WriteLine();
				} else {
					output.Write(' ');
				}
				output.Write(blob[i].ToString("x2"));
			}
			
			output.WriteLine();
			output.Unindent();
			output.Write(")");
		}
		
		void OpenBlock()
		{
			output.WriteLine("{");
			output.Indent();
		}
		
		void CloseBlock()
		{
			output.Unindent();
			output.WriteLine("}");
		}
		
		void WriteFlags<T>(T flags, EnumNameCollection<T> flagNames) where T : struct
		{
			long val = Convert.ToInt64(flags);
			long tested = 0;
			foreach (var pair in flagNames) {
				tested |= pair.Key;
				if ((val & pair.Key) != 0) {
					output.Write(pair.Value);
					output.Write(' ');
				}
			}
			if ((val & ~tested) != 0)
				output.Write("flag({0}) ", val & ~tested);
		}
		
		void WriteEnum<T>(T enumValue, EnumNameCollection<T> enumNames) where T : struct
		{
			long val = Convert.ToInt64(enumValue);
			foreach (var pair in enumNames) {
				if (pair.Key == val) {
					output.Write(pair.Value);
					output.Write(' ');
					return;
				}
			}
			if (val != 0) {
				output.Write("flag({0})", val);
				output.Write(' ');
			}
			
		}
		
		sealed class EnumNameCollection<T> : IEnumerable<KeyValuePair<long, string>> where T : struct
		{
			List<KeyValuePair<long, string>> names = new List<KeyValuePair<long, string>>();
			
			public void Add(T flag, string name)
			{
				this.names.Add(new KeyValuePair<long, string>(Convert.ToInt64(flag), name));
			}
			
			public IEnumerator<KeyValuePair<long, string>> GetEnumerator()
			{
				return names.GetEnumerator();
			}
			
			System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
			{
				return names.GetEnumerator();
			}
		}
	}
}
