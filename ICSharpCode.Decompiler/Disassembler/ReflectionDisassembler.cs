// Copyright (c) 2011 AlphaSierraPapa for the SharpDevelop Team
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
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using ICSharpCode.NRefactory;
using dnlib.DotNet;
using dnlib.DotNet.Emit;

namespace ICSharpCode.Decompiler.Disassembler
{
	public class DisassemblerOptions
	{
		public DisassemblerOptions(CancellationToken cancellationToken)
		{
			this.CancellationToken = cancellationToken;
		}

		public readonly CancellationToken CancellationToken;

		/// <summary>
		/// null if we shouldn't add opcode documentation. It returns null if no doc was found
		/// </summary>
		public Func<OpCode, string> GetOpCodeDocumentation;

		/// <summary>
		/// null if we shouldn't add XML doc comments.
		/// </summary>
		public Func<IMemberRef, IEnumerable<string>> GetXmlDocComments;
	}

	/// <summary>
	/// Disassembles type and member definitions.
	/// </summary>
	public sealed class ReflectionDisassembler
	{
		ITextOutput output;
		readonly DisassemblerOptions options;
		bool isInType; // whether we are currently disassembling a whole type (-> defaultCollapsed for foldings)
		MethodBodyDisassembler methodBodyDisassembler;
		IMemberDef currentMember;
		
		public ReflectionDisassembler(ITextOutput output, bool detectControlStructure, DisassemblerOptions options)
		{
			if (output == null)
				throw new ArgumentNullException("output");
			this.output = output;
			this.options = options;
			this.methodBodyDisassembler = new MethodBodyDisassembler(output, detectControlStructure, options);
		}
		
		#region Disassemble Method
		EnumNameCollection<MethodAttributes> methodAttributeFlags = new EnumNameCollection<MethodAttributes>() {
			{ MethodAttributes.Final, "final" },
			{ MethodAttributes.HideBySig, "hidebysig" },
			{ MethodAttributes.SpecialName, "specialname" },
			{ MethodAttributes.PInvokeImpl, null }, // handled separately
			{ MethodAttributes.UnmanagedExport, "export" },
			{ MethodAttributes.RTSpecialName, "rtspecialname" },
			{ MethodAttributes.RequireSecObject, "reqsecobj" },
			{ MethodAttributes.NewSlot, "newslot" },
			{ MethodAttributes.CheckAccessOnOverride, "strict" },
			{ MethodAttributes.Abstract, "abstract" },
			{ MethodAttributes.Virtual, "virtual" },
			{ MethodAttributes.Static, "static" },
			{ MethodAttributes.HasSecurity, null }, // ?? also invisible in ILDasm
		};
		
		EnumNameCollection<MethodAttributes> methodVisibility = new EnumNameCollection<MethodAttributes>() {
			{ MethodAttributes.Private, "private" },
			{ MethodAttributes.FamANDAssem, "famandassem" },
			{ MethodAttributes.Assembly, "assembly" },
			{ MethodAttributes.Family, "family" },
			{ MethodAttributes.FamORAssem, "famorassem" },
			{ MethodAttributes.Public, "public" },
		};
		
		EnumNameCollection<CallingConvention> callingConvention = new EnumNameCollection<CallingConvention>() {
			{ CallingConvention.C, "unmanaged cdecl" },
			{ CallingConvention.StdCall, "unmanaged stdcall" },
			{ CallingConvention.ThisCall, "unmanaged thiscall" },
			{ CallingConvention.FastCall, "unmanaged fastcall" },
			{ CallingConvention.VarArg, "vararg" },
			{ CallingConvention.NativeVarArg, "nativevararg" },
			{ CallingConvention.Generic, null },
		};
		
		EnumNameCollection<MethodImplAttributes> methodCodeType = new EnumNameCollection<MethodImplAttributes>() {
			{ MethodImplAttributes.IL, "cil" },
			{ MethodImplAttributes.Native, "native" },
			{ MethodImplAttributes.OPTIL, "optil" },
			{ MethodImplAttributes.Runtime, "runtime" },
		};
		
		EnumNameCollection<MethodImplAttributes> methodImpl = new EnumNameCollection<MethodImplAttributes>() {
			{ MethodImplAttributes.Synchronized, "synchronized" },
			{ MethodImplAttributes.NoInlining, "noinlining" },
			{ MethodImplAttributes.NoOptimization, "nooptimization" },
			{ MethodImplAttributes.PreserveSig, "preservesig" },
			{ MethodImplAttributes.InternalCall, "internalcall" },
			{ MethodImplAttributes.ForwardRef, "forwardref" },
		};

		void WriteXmlDocComment(IMemberDef mr) {
			if (options.GetXmlDocComments == null)
				return;
			foreach (var line in options.GetXmlDocComments(mr)) {
				output.Write("///", TextTokenType.XmlDocTag);
				output.WriteXmlDoc(line);
				output.WriteLine();
			}
		}
		
		public void DisassembleMethod(MethodDef method)
		{
			// set current member
			currentMember = method;
			
			// write method header
			WriteXmlDocComment(method);
			output.WriteDefinition(".method", method, TextTokenType.ILDirective, false);
			output.WriteSpace();
			DisassembleMethodInternal(method);
		}
		
		void DisassembleMethodInternal(MethodDef method)
		{
			//    .method public hidebysig  specialname
			//               instance default class [mscorlib]System.IO.TextWriter get_BaseWriter ()  cil managed
			//
			
			//emit flags
			WriteEnum(method.Attributes & MethodAttributes.MemberAccessMask, methodVisibility);
			WriteFlags(method.Attributes & ~MethodAttributes.MemberAccessMask, methodAttributeFlags);
			if (method.IsCompilerControlled) {
				output.Write("privatescope", TextTokenType.Keyword);
				output.WriteSpace();
			}
			
			if ((method.Attributes & MethodAttributes.PinvokeImpl) == MethodAttributes.PinvokeImpl) {
				output.Write("pinvokeimpl", TextTokenType.Keyword);
				if (method.HasImplMap) {
					ImplMap info = method.ImplMap;
					output.Write('(', TextTokenType.Operator);
					output.Write("\"" + NRefactory.CSharp.TextWriterTokenWriter.ConvertString(info.Module == null ? string.Empty : info.Module.Name.String) + "\"", TextTokenType.String);

					if (!string.IsNullOrEmpty(info.Name) && info.Name != method.Name) {
						output.WriteSpace();
						output.Write("as", TextTokenType.Keyword);
						output.WriteSpace();
						output.Write("\"" + NRefactory.CSharp.TextWriterTokenWriter.ConvertString(info.Name) + "\"", TextTokenType.String);
					}

					if (info.IsNoMangle) {
						output.WriteSpace();
						output.Write("nomangle", TextTokenType.Keyword);
					}

					if (info.IsCharSetAnsi) {
						output.WriteSpace();
						output.Write("ansi", TextTokenType.Keyword);
					}
					else if (info.IsCharSetAuto) {
						output.WriteSpace();
						output.Write("autochar", TextTokenType.Keyword);
					}
					else if (info.IsCharSetUnicode) {
						output.WriteSpace();
						output.Write("unicode", TextTokenType.Keyword);
					}

					if (info.SupportsLastError) {
						output.WriteSpace();
						output.Write("lasterr", TextTokenType.Keyword);
					}

					if (info.IsCallConvCdecl) {
						output.WriteSpace();
						output.Write("cdecl", TextTokenType.Keyword);
					}
					else if (info.IsCallConvFastcall) {
						output.WriteSpace();
						output.Write("fastcall", TextTokenType.Keyword);
					}
					else if (info.IsCallConvStdcall) {
						output.WriteSpace();
						output.Write("stdcall", TextTokenType.Keyword);
					}
					else if (info.IsCallConvThiscall) {
						output.WriteSpace();
						output.Write("thiscall", TextTokenType.Keyword);
					}
					else if (info.IsCallConvWinapi) {
						output.WriteSpace();
						output.Write("winapi", TextTokenType.Keyword);
					}

					output.Write(')', TextTokenType.Operator);
				}
				output.WriteSpace();
			}
			
			output.WriteLine();
			output.Indent();
			if (method.ExplicitThis) {
				output.Write("instance", TextTokenType.Keyword);
				output.WriteSpace();
				output.Write("explicit", TextTokenType.Keyword);
				output.WriteSpace();
			} else if (method.HasThis) {
				output.Write("instance", TextTokenType.Keyword);
				output.WriteSpace();
			}
			
			//call convention
			WriteEnum(method.CallingConvention & (CallingConvention)0x1f, callingConvention);
			
			//return type
			method.ReturnType.WriteTo(output);
			output.WriteSpace();
			if (method.Parameters.ReturnParameter.HasParamDef && method.Parameters.ReturnParameter.ParamDef.HasMarshalType) {
				WriteMarshalInfo(method.Parameters.ReturnParameter.ParamDef.MarshalType);
			}
			
			if (method.IsCompilerControlled) {
				output.Write(DisassemblerHelpers.Escape(method.Name + "$PST" + method.MDToken.ToInt32().ToString("X8")), TextTokenHelper.GetTextTokenType(method));
			} else {
				output.Write(DisassemblerHelpers.Escape(method.Name), TextTokenHelper.GetTextTokenType(method));
			}
			
			WriteTypeParameters(output, method);
			
			//( params )
			output.WriteSpace();
			output.Write('(', TextTokenType.Operator);
			if (method.Parameters.GetNumberOfNormalParameters() > 0) {
				output.WriteLine();
				output.Indent();
				WriteParameters(method.Parameters);
				output.Unindent();
			}
			output.Write(')', TextTokenType.Operator);
			output.WriteSpace();
			//cil managed
			WriteEnum(method.ImplAttributes & MethodImplAttributes.CodeTypeMask, methodCodeType);
			if ((method.ImplAttributes & MethodImplAttributes.ManagedMask) == MethodImplAttributes.Managed)
				output.Write("managed", TextTokenType.Keyword);
			else
				output.Write("unmanaged", TextTokenType.Keyword);
			output.WriteSpace();
			WriteFlags(method.ImplAttributes & ~(MethodImplAttributes.CodeTypeMask | MethodImplAttributes.ManagedMask), methodImpl);
			
			output.Unindent();
			OpenBlock(defaultCollapsed: isInType);
			WriteAttributes(method.CustomAttributes);
			if (method.HasOverrides) {
				foreach (var methodOverride in method.Overrides) {
					output.Write(".override", TextTokenType.ILDirective);
					output.WriteSpace();
					output.Write("method", TextTokenType.Keyword);
					output.WriteSpace();
					methodOverride.MethodDeclaration.WriteMethodTo(output);
					output.WriteLine();
				}
			}
			foreach (var p in method.Parameters) {
				if (p.IsHiddenThisParameter)
					continue;
				WriteParameterAttributes(p);
			}
			WriteSecurityDeclarations(method);
			
			if (method.HasBody) {
				// create IL code mappings - used in debugger
				MemberMapping debugSymbols = new MemberMapping(method);
				methodBodyDisassembler.Disassemble(method, debugSymbols);
				output.AddDebugSymbols(debugSymbols);
			}
			
			CloseBlock("end of method " + DisassemblerHelpers.Escape(method.DeclaringType.Name) + "::" + DisassemblerHelpers.Escape(method.Name));
		}
		
		#region Write Security Declarations
		void WriteSecurityDeclarations(IHasDeclSecurity secDeclProvider)
		{
			if (!secDeclProvider.HasDeclSecurities)
				return;
			foreach (var secdecl in secDeclProvider.DeclSecurities) {
				output.Write(".permissionset", TextTokenType.ILDirective);
				output.WriteSpace();
				switch (secdecl.Action) {
					case SecurityAction.Request:
					output.Write("request", TextTokenType.Keyword);
						break;
					case SecurityAction.Demand:
						output.Write("demand", TextTokenType.Keyword);
						break;
					case SecurityAction.Assert:
						output.Write("assert", TextTokenType.Keyword);
						break;
					case SecurityAction.Deny:
						output.Write("deny", TextTokenType.Keyword);
						break;
					case SecurityAction.PermitOnly:
						output.Write("permitonly", TextTokenType.Keyword);
						break;
					case SecurityAction.LinkDemand:
						output.Write("linkcheck", TextTokenType.Keyword);
						break;
					case SecurityAction.InheritDemand:
						output.Write("inheritcheck", TextTokenType.Keyword);
						break;
					case SecurityAction.RequestMinimum:
						output.Write("reqmin", TextTokenType.Keyword);
						break;
					case SecurityAction.RequestOptional:
						output.Write("reqopt", TextTokenType.Keyword);
						break;
					case SecurityAction.RequestRefuse:
						output.Write("reqrefuse", TextTokenType.Keyword);
						break;
					case SecurityAction.PreJitGrant:
						output.Write("prejitgrant", TextTokenType.Keyword);
						break;
					case SecurityAction.PreJitDeny:
						output.Write("prejitdeny", TextTokenType.Keyword);
						break;
					case SecurityAction.NonCasDemand:
						output.Write("noncasdemand", TextTokenType.Keyword);
						break;
					case SecurityAction.NonCasLinkDemand:
						output.Write("noncaslinkdemand", TextTokenType.Keyword);
						break;
					case SecurityAction.NonCasInheritance:
						output.Write("noncasinheritance", TextTokenType.Keyword);
						break;
					default:
						output.Write(secdecl.Action.ToString(), TextTokenType.Keyword);
						break;
				}
				output.WriteSpace();
				output.Write('=', TextTokenType.Operator);
				output.WriteSpace();
				output.WriteLineLeftBrace();
				output.Indent();
				for (int i = 0; i < secdecl.SecurityAttributes.Count; i++) {
					SecurityAttribute sa = secdecl.SecurityAttributes[i];
					if (sa.AttributeType != null && sa.AttributeType.Scope == sa.AttributeType.Module) {
						output.Write("class", TextTokenType.Keyword);
						output.WriteSpace();
						output.Write(DisassemblerHelpers.Escape(GetAssemblyQualifiedName(sa.AttributeType)), TextTokenType.Text);
					} else {
						sa.AttributeType.WriteTo(output, ILNameSyntax.TypeName);
					}
					output.WriteSpace();
					output.Write('=', TextTokenType.Operator);
					output.WriteSpace();
					output.WriteLeftBrace();
					if (sa.HasNamedArguments) {
						output.WriteLine();
						output.Indent();
						
						var attrType = sa.AttributeType.ResolveTypeDef();
						foreach (var na in sa.Fields) {
							output.Write("field", TextTokenType.Keyword);
							output.WriteSpace();
							WriteSecurityDeclarationArgument(attrType, na);
							output.WriteLine();
						}
						
						foreach (var na in sa.Properties) {
							output.Write("property", TextTokenType.Keyword);
							output.WriteSpace();
							WriteSecurityDeclarationArgument(attrType, na);
							output.WriteLine();
						}
						
						output.Unindent();
					}
					output.WriteRightBrace();
					
					if (i + 1< secdecl.SecurityAttributes.Count)
						output.Write(',', TextTokenType.Operator);
					output.WriteLine();
				}
				output.Unindent();
				output.WriteLineRightBrace();
			}
		}
		
		void WriteSecurityDeclarationArgument(TypeDef attrType, CANamedArgument na)
		{
			object reference = null;
			if (attrType != null) {
				if (na.IsField)
					reference = attrType.FindField(na.Name, new FieldSig(na.Type));
				else
					reference = attrType.FindProperty(na.Name, PropertySig.CreateInstance(na.Type));
			}

			TypeSig type = na.Argument.Type;
			if (type != null && (type.ElementType == ElementType.Class || type.ElementType == ElementType.ValueType)) {
				output.Write("enum", TextTokenType.Keyword);
				output.WriteSpace();
				if (type.Scope != type.Module) {
					output.Write("class", TextTokenType.Keyword);
					output.WriteSpace();
					output.Write(DisassemblerHelpers.Escape(GetAssemblyQualifiedName(type)), TextTokenType.Text);
				} else {
					type.WriteTo(output, ILNameSyntax.TypeName);
				}
			} else {
				type.WriteTo(output);
			}
			output.WriteSpace();
			output.WriteReference(DisassemblerHelpers.Escape(na.Name), reference, na.IsField ? TextTokenType.InstanceField : TextTokenType.InstanceProperty);
			output.WriteSpace();
			output.Write('=', TextTokenType.Operator);
			output.WriteSpace();
			if (na.Argument.Value is UTF8String) {
				// secdecls use special syntax for strings
				output.Write("string", TextTokenType.Keyword);
				output.Write('(', TextTokenType.Operator);
				output.Write(string.Format("'{0}'", NRefactory.CSharp.TextWriterTokenWriter.ConvertString((UTF8String)na.Argument.Value).Replace("'", "\'")), TextTokenType.String);
				output.Write(')', TextTokenType.Operator);
			} else {
				WriteConstant(na.Argument.Value);
			}
		}
		
		string GetAssemblyQualifiedName(IType type)
		{
			IAssembly anr = type.Scope as IAssembly;
			if (anr == null) {
				ModuleDef md = type.Scope as ModuleDef;
				if (md != null) {
					anr = md.Assembly;
				}
			}
			if (anr != null) {
				return type.FullName + ", " + anr.FullName;
			} else {
				return type.FullName;
			}
		}
		#endregion
		
		#region WriteMarshalInfo
		void WriteMarshalInfo(MarshalType marshalInfo)
		{
			output.Write("marshal", TextTokenType.Keyword);
			output.Write('(', TextTokenType.Operator);
			if (marshalInfo != null)
				WriteNativeType(marshalInfo.NativeType, marshalInfo);
			output.Write(')', TextTokenType.Operator);
			output.WriteSpace();
		}
		
		void WriteNativeType(NativeType nativeType, MarshalType marshalInfo = null)
		{
			switch (nativeType) {
				case NativeType.NotInitialized:
					break;
				case NativeType.Boolean:
					output.Write("bool", TextTokenType.Keyword);
					break;
				case NativeType.I1:
					output.Write("int8", TextTokenType.Keyword);
					break;
				case NativeType.U1:
					output.Write("unsigned", TextTokenType.Keyword);
					output.WriteSpace();
					output.Write("int8", TextTokenType.Keyword);
					break;
				case NativeType.I2:
					output.Write("int16", TextTokenType.Keyword);
					break;
				case NativeType.U2:
					output.Write("unsigned", TextTokenType.Keyword);
					output.WriteSpace();
					output.Write("int16", TextTokenType.Keyword);
					break;
				case NativeType.I4:
					output.Write("int32", TextTokenType.Keyword);
					break;
				case NativeType.U4:
					output.Write("unsigned", TextTokenType.Keyword);
					output.WriteSpace();
					output.Write("int32", TextTokenType.Keyword);
					break;
				case NativeType.I8:
					output.Write("int64", TextTokenType.Keyword);
					break;
				case NativeType.U8:
					output.Write("unsigned", TextTokenType.Keyword);
					output.WriteSpace();
					output.Write("int64", TextTokenType.Keyword);
					break;
				case NativeType.R4:
					output.Write("float32", TextTokenType.Keyword);
					break;
				case NativeType.R8:
					output.Write("float64", TextTokenType.Keyword);
					break;
				case NativeType.LPStr:
					output.Write("lpstr", TextTokenType.Keyword);
					break;
				case NativeType.Int:
					output.Write("int", TextTokenType.Keyword);
					break;
				case NativeType.UInt:
					output.Write("unsigned", TextTokenType.Keyword);
					output.WriteSpace();
					output.Write("int", TextTokenType.Keyword);
					break;
				case NativeType.Func:
					output.Write("method", TextTokenType.Keyword);
					break;
				case NativeType.Array:
					ArrayMarshalType ami = marshalInfo as ArrayMarshalType;
					if (ami == null)
						goto default;
					if (ami.ElementType != NativeType.Max)
						WriteNativeType(ami.ElementType);
					output.Write('[', TextTokenType.Operator);
					if (ami.Flags == 0) {
						output.Write(ami.Size.ToString(), TextTokenType.Number);
					} else {
						if (ami.Size >= 0)
							output.Write(ami.Size.ToString(), TextTokenType.Number);
						output.WriteSpace();
						output.Write('+', TextTokenType.Operator);
						output.WriteSpace();
						output.Write(ami.ParamNumber.ToString(), TextTokenType.Number);
					}
					output.Write(']', TextTokenType.Operator);
					break;
				case NativeType.Currency:
					output.Write("currency", TextTokenType.Keyword);
					break;
				case NativeType.BStr:
					output.Write("bstr", TextTokenType.Keyword);
					break;
				case NativeType.LPWStr:
					output.Write("lpwstr", TextTokenType.Keyword);
					break;
				case NativeType.LPTStr:
					output.Write("lptstr", TextTokenType.Keyword);
					break;
				case NativeType.FixedSysString:
					var fsmi = marshalInfo as FixedSysStringMarshalType;
					output.Write("fixed", TextTokenType.Keyword);
					output.WriteSpace();
					output.Write("sysstring", TextTokenType.Keyword);
					if (fsmi != null && fsmi.IsSizeValid) {
						output.Write('[', TextTokenType.Operator);
						output.Write(string.Format("{0}", fsmi.Size), TextTokenType.Number);
						output.Write(']', TextTokenType.Operator);
					}
					break;
				case NativeType.IUnknown:
				case NativeType.IDispatch:
				case NativeType.IntF:
					if (nativeType == NativeType.IUnknown)
						output.Write("iunknown", TextTokenType.Keyword);
					else if (nativeType == NativeType.IDispatch)
						output.Write("idispatch", TextTokenType.Keyword);
					else if (nativeType == NativeType.IntF)
						output.Write("interface", TextTokenType.Keyword);
					else
						throw new InvalidOperationException();
					var imti = marshalInfo as InterfaceMarshalType;
					if (imti != null && imti.IsIidParamIndexValid) {
						output.Write('(', TextTokenType.Operator);
						output.Write("iidparam", TextTokenType.Keyword);
						output.WriteSpace();
						output.Write('=', TextTokenType.Operator);
						output.WriteSpace();
						output.Write(imti.IidParamIndex.ToString(), TextTokenType.Number);
						output.Write(')', TextTokenType.Operator);
					}
					break;
				case NativeType.Struct:
					output.Write("struct", TextTokenType.Keyword);
					break;
				case NativeType.SafeArray:
					output.Write("safearray", TextTokenType.Keyword);
					output.WriteSpace();
					SafeArrayMarshalType sami = marshalInfo as SafeArrayMarshalType;
					if (sami != null && sami.IsVariantTypeValid) {
						switch (sami.VariantType & VariantType.TypeMask) {
							case VariantType.None:
								break;
							case VariantType.Null:
								output.Write("null", TextTokenType.Keyword);
								break;
							case VariantType.I2:
								output.Write("int16", TextTokenType.Keyword);
								break;
							case VariantType.I4:
								output.Write("int32", TextTokenType.Keyword);
								break;
							case VariantType.R4:
								output.Write("float32", TextTokenType.Keyword);
								break;
							case VariantType.R8:
								output.Write("float64", TextTokenType.Keyword);
								break;
							case VariantType.CY:
								output.Write("currency", TextTokenType.Keyword);
								break;
							case VariantType.Date:
								output.Write("date", TextTokenType.Keyword);
								break;
							case VariantType.BStr:
								output.Write("bstr", TextTokenType.Keyword);
								break;
							case VariantType.Dispatch:
								output.Write("idispatch", TextTokenType.Keyword);
								break;
							case VariantType.Error:
								output.Write("error", TextTokenType.Keyword);
								break;
							case VariantType.Bool:
								output.Write("bool", TextTokenType.Keyword);
								break;
							case VariantType.Variant:
								output.Write("variant", TextTokenType.Keyword);
								break;
							case VariantType.Unknown:
								output.Write("iunknown", TextTokenType.Keyword);
								break;
							case VariantType.Decimal:
								output.Write("decimal", TextTokenType.Keyword);
								break;
							case VariantType.I1:
								output.Write("int8", TextTokenType.Keyword);
								break;
							case VariantType.UI1:
								output.Write("unsigned", TextTokenType.Keyword);
								output.WriteSpace();
								output.Write("int8", TextTokenType.Keyword);
								break;
							case VariantType.UI2:
								output.Write("unsigned", TextTokenType.Keyword);
								output.WriteSpace();
								output.Write("int16", TextTokenType.Keyword);
								break;
							case VariantType.UI4:
								output.Write("unsigned", TextTokenType.Keyword);
								output.WriteSpace();
								output.Write("int32", TextTokenType.Keyword);
								break;
							case VariantType.I8:
								output.Write("int64", TextTokenType.Keyword);
								break;
							case VariantType.UI8:
								output.Write("unsigned", TextTokenType.Keyword);
								output.WriteSpace();
								output.Write("int64", TextTokenType.Keyword);
								break;
							case VariantType.Int:
								output.Write("int", TextTokenType.Keyword);
								break;
							case VariantType.UInt:
								output.Write("unsigned", TextTokenType.Keyword);
								output.WriteSpace();
								output.Write("int", TextTokenType.Keyword);
								break;
							case VariantType.Void:
								output.Write("void", TextTokenType.Keyword);
								break;
							case VariantType.HResult:
								output.Write("hresult", TextTokenType.Keyword);
								break;
							case VariantType.Ptr:
								output.Write("*", TextTokenType.Operator);
								break;
							case VariantType.SafeArray:
								output.Write("safearray", TextTokenType.Keyword);
								break;
							case VariantType.CArray:
								output.Write("carray", TextTokenType.Keyword);
								break;
							case VariantType.UserDefined:
								output.Write("userdefined", TextTokenType.Keyword);
								break;
							case VariantType.LPStr:
								output.Write("lpstr", TextTokenType.Keyword);
								break;
							case VariantType.LPWStr:
								output.Write("lpwstr", TextTokenType.Keyword);
								break;
							case VariantType.Record:
								output.Write("record", TextTokenType.Keyword);
								break;
							case VariantType.FileTime:
								output.Write("filetime", TextTokenType.Keyword);
								break;
							case VariantType.Blob:
								output.Write("blob", TextTokenType.Keyword);
								break;
							case VariantType.Stream:
								output.Write("stream", TextTokenType.Keyword);
								break;
							case VariantType.Storage:
								output.Write("storage", TextTokenType.Keyword);
								break;
							case VariantType.StreamedObject:
								output.Write("streamed_object", TextTokenType.Keyword);
								break;
							case VariantType.StoredObject:
								output.Write("stored_object", TextTokenType.Keyword);
								break;
							case VariantType.BlobObject:
								output.Write("blob_object", TextTokenType.Keyword);
								break;
							case VariantType.CF:
								output.Write("cf", TextTokenType.Keyword);
								break;
							case VariantType.CLSID:
								output.Write("clsid", TextTokenType.Keyword);
								break;
							case VariantType.IntPtr:
							case VariantType.UIntPtr:
							case VariantType.VersionedStream:
							case VariantType.BStrBlob:
							default:
								output.Write((sami.VariantType & VariantType.TypeMask).ToString(), TextTokenType.Keyword);
								break;
						}
						if ((sami.VariantType & VariantType.ByRef) != 0)
							output.Write("&", TextTokenType.Operator);
						if ((sami.VariantType & VariantType.Array) != 0)
							output.Write("[]", TextTokenType.Operator);
						if ((sami.VariantType & VariantType.Vector) != 0) {
							output.WriteSpace();
							output.Write("vector", TextTokenType.Keyword);
						}
						if (sami.IsUserDefinedSubTypeValid) {
							output.Write(',', TextTokenType.Operator);
							output.WriteSpace();
							output.Write("\"" + NRefactory.CSharp.TextWriterTokenWriter.ConvertString(sami.UserDefinedSubType.FullName) + "\"", TextTokenType.String);
						}
					}
					break;
				case NativeType.FixedArray:
					output.Write("fixed", TextTokenType.Keyword);
					output.WriteSpace();
					output.Write("array", TextTokenType.Keyword);
					FixedArrayMarshalType fami = marshalInfo as FixedArrayMarshalType;
					if (fami != null) {
						if (fami.IsSizeValid) {
							output.Write('[', TextTokenType.Operator);
							output.Write(fami.Size.ToString(), TextTokenType.Number);
							output.Write(']', TextTokenType.Operator);
						}
						if (fami.IsElementTypeValid) {
							output.WriteSpace();
							WriteNativeType(fami.ElementType);
						}
					}
					break;
				case NativeType.ByValStr:
					output.Write("byvalstr", TextTokenType.Keyword);
					break;
				case NativeType.ANSIBStr:
					output.Write("ansi", TextTokenType.Keyword);
					output.WriteSpace();
					output.Write("bstr", TextTokenType.Keyword);
					break;
				case NativeType.TBStr:
					output.Write("tbstr", TextTokenType.Keyword);
					break;
				case NativeType.VariantBool:
					output.Write("variant", TextTokenType.Keyword);
					output.WriteSpace();
					output.Write("bool", TextTokenType.Keyword);
					break;
				case NativeType.ASAny:
					output.Write("as", TextTokenType.Keyword);
					output.WriteSpace();
					output.Write("any", TextTokenType.Keyword);
					break;
				case NativeType.LPStruct:
					output.Write("lpstruct", TextTokenType.Keyword);
					break;
				case NativeType.CustomMarshaler:
					CustomMarshalType cmi = marshalInfo as CustomMarshalType;
					if (cmi == null)
						goto default;
					output.Write("custom", TextTokenType.Keyword);
					output.Write('(', TextTokenType.Operator);
					output.Write(string.Format("\"{0}\"", NRefactory.CSharp.TextWriterTokenWriter.ConvertString(cmi.CustomMarshaler == null ? string.Empty : cmi.CustomMarshaler.FullName)), TextTokenType.String);
					output.Write(',', TextTokenType.Operator);
					output.WriteSpace();
					output.Write(string.Format("\"{0}\"", NRefactory.CSharp.TextWriterTokenWriter.ConvertString(cmi.Cookie)), TextTokenType.String);
					if (!UTF8String.IsNullOrEmpty(cmi.Guid) || !UTF8String.IsNullOrEmpty(cmi.NativeTypeName)) {
						output.Write(',', TextTokenType.Operator);
						output.WriteSpace();
						output.Write(string.Format("\"{0}\"", NRefactory.CSharp.TextWriterTokenWriter.ConvertString(cmi.Guid)), TextTokenType.String);
						output.Write(',', TextTokenType.Operator);
						output.WriteSpace();
						output.Write(string.Format("\"{0}\"", NRefactory.CSharp.TextWriterTokenWriter.ConvertString(cmi.NativeTypeName)), TextTokenType.String);
					}
					output.Write(')', TextTokenType.Operator);
					break;
				case NativeType.Error:
					output.Write("error", TextTokenType.Keyword);
					break;
				case NativeType.Void:
					output.Write("void", TextTokenType.Keyword);
					break;
				case NativeType.SysChar:
					output.Write("syschar", TextTokenType.Keyword);
					break;
				case NativeType.Variant:
					output.Write("variant", TextTokenType.Keyword);
					break;
				case NativeType.Decimal:
					output.Write("decimal", TextTokenType.Keyword);
					break;
				case NativeType.Date:
					output.Write("date", TextTokenType.Keyword);
					break;
				case NativeType.ObjectRef:
					output.Write("objectref", TextTokenType.Keyword);
					break;
				case NativeType.NestedStruct:
					output.Write("nested", TextTokenType.Keyword);
					output.WriteSpace();
					output.Write("struct", TextTokenType.Keyword);
					break;
				case NativeType.Ptr:
				case NativeType.IInspectable:
				case NativeType.HString:
				default:
					output.Write(nativeType.ToString(), TextTokenType.Keyword);
					break;
			}
		}
		#endregion
		
		void WriteParameters(IList<Parameter> parameters)
		{
			for (int i = 0; i < parameters.Count; i++) {
				var p = parameters[i];
				if (p.IsHiddenThisParameter)
					continue;
				var paramDef = p.ParamDef;
				if (paramDef != null) {
					if (paramDef.IsIn) {
						output.Write('[', TextTokenType.Operator);
						output.Write("in", TextTokenType.Keyword);
						output.Write(']', TextTokenType.Operator);
						output.WriteSpace();
					}
					if (paramDef.IsOut) {
						output.Write('[', TextTokenType.Operator);
						output.Write("out", TextTokenType.Keyword);
						output.Write(']', TextTokenType.Operator);
						output.WriteSpace();
					}
					if (paramDef.IsOptional) {
						output.Write('[', TextTokenType.Operator);
						output.Write("opt", TextTokenType.Keyword);
						output.Write(']', TextTokenType.Operator);
						output.WriteSpace();
					}
				}
				p.Type.WriteTo(output);
				output.WriteSpace();
				if (paramDef != null && paramDef.MarshalType != null) {
					WriteMarshalInfo(paramDef.MarshalType);
				}
				output.WriteDefinition(DisassemblerHelpers.Escape(p.Name), p, TextTokenType.Parameter);
				if (i < parameters.Count - 1)
					output.Write(',', TextTokenType.Operator);
				output.WriteLine();
			}
		}
		
		bool HasParameterAttributes(Parameter p)
		{
			return p.ParamDef != null && (p.ParamDef.HasConstant || p.ParamDef.HasCustomAttributes);
		}
		
		void WriteParameterAttributes(Parameter p)
		{
			if (!HasParameterAttributes(p))
				return;
			output.Write(".param", TextTokenType.ILDirective);
			output.WriteSpace();
			output.Write('[', TextTokenType.Operator);
			output.Write(string.Format("{0}", p.MethodSigIndex + 1), TextTokenType.Number);
			output.Write(']', TextTokenType.Operator);
			if (p.HasParamDef && p.ParamDef.HasConstant) {
				output.WriteSpace();
				output.Write('=', TextTokenType.Operator);
				output.WriteSpace();
				WriteConstant(p.ParamDef.Constant.Value);
			}
			output.WriteLine();
			if (p.HasParamDef)
				WriteAttributes(p.ParamDef.CustomAttributes);
		}
		
		void WriteConstant(object constant)
		{
			if (constant == null) {
				output.Write("nullref", TextTokenType.Keyword);
			} else {
				string typeName = DisassemblerHelpers.PrimitiveTypeName(constant.GetType().FullName);
				if (typeName != null && typeName != "string") {
					DisassemblerHelpers.WriteKeyword(output, typeName);
					output.Write('(', TextTokenType.Operator);
					float? cf = constant as float?;
					double? cd = constant as double?;
					if (cf.HasValue && (float.IsNaN(cf.Value) || float.IsInfinity(cf.Value))) {
						output.Write(string.Format("0x{0:x8}", BitConverter.ToInt32(BitConverter.GetBytes(cf.Value), 0)), TextTokenType.Number);
					} else if (cd.HasValue && (double.IsNaN(cd.Value) || double.IsInfinity(cd.Value))) {
						output.Write(string.Format("0x{0:x16}", BitConverter.DoubleToInt64Bits(cd.Value)), TextTokenType.Number);
					} else {
						DisassemblerHelpers.WriteOperand(output, constant);
					}
					output.Write(')', TextTokenType.Operator);
				} else {
					DisassemblerHelpers.WriteOperand(output, constant);
				}
			}
		}
		#endregion
		
		#region Disassemble Field
		EnumNameCollection<FieldAttributes> fieldVisibility = new EnumNameCollection<FieldAttributes>() {
			{ FieldAttributes.Private, "private" },
			{ FieldAttributes.FamANDAssem, "famandassem" },
			{ FieldAttributes.Assembly, "assembly" },
			{ FieldAttributes.Family, "family" },
			{ FieldAttributes.FamORAssem, "famorassem" },
			{ FieldAttributes.Public, "public" },
		};
		
		EnumNameCollection<FieldAttributes> fieldAttributes = new EnumNameCollection<FieldAttributes>() {
			{ FieldAttributes.Static, "static" },
			{ FieldAttributes.Literal, "literal" },
			{ FieldAttributes.InitOnly, "initonly" },
			{ FieldAttributes.SpecialName, "specialname" },
			{ FieldAttributes.RTSpecialName, "rtspecialname" },
			{ FieldAttributes.NotSerialized, "notserialized" },
		};
		
		public void DisassembleField(FieldDef field)
		{
			WriteXmlDocComment(field);
			output.WriteDefinition(".field", field, TextTokenType.ILDirective, false);
			output.WriteSpace();
			if (field.HasLayoutInfo && field.FieldOffset.HasValue) {
				output.Write('[', TextTokenType.Operator);
				output.Write(string.Format("{0}", field.FieldOffset), TextTokenType.Number);
				output.Write(']', TextTokenType.Operator);
				output.WriteSpace();
			}
			WriteEnum(field.Attributes & FieldAttributes.FieldAccessMask, fieldVisibility);
			const FieldAttributes hasXAttributes = FieldAttributes.HasDefault | FieldAttributes.HasFieldMarshal | FieldAttributes.HasFieldRVA;
			WriteFlags(field.Attributes & ~(FieldAttributes.FieldAccessMask | hasXAttributes), fieldAttributes);
			if (field.HasMarshalType) {
				WriteMarshalInfo(field.MarshalType);
			}
			field.FieldType.WriteTo(output);
			output.WriteSpace();
			output.Write(DisassemblerHelpers.Escape(field.Name), TextTokenHelper.GetTextTokenType(field));
			if ((field.Attributes & FieldAttributes.HasFieldRVA) == FieldAttributes.HasFieldRVA) {
				output.WriteSpace();
				output.Write("at", TextTokenType.Keyword);
				output.WriteSpace();
				output.Write(string.Format("I_{0:x8}", (uint)field.RVA), TextTokenType.Text);
				uint fieldSize;
				if (field.GetFieldSize(out fieldSize)) {
					output.WriteSpace();
					output.Write(string.Format("// {0} (0x{0:x4}) bytes", fieldSize), TextTokenType.Comment);
				}
			}
			if (field.HasConstant) {
				output.WriteSpace();
				output.Write('=', TextTokenType.Operator);
				output.WriteSpace();
				WriteConstant(field.Constant.Value);
			}
			output.WriteLine();
			if (field.HasCustomAttributes) {
				output.MarkFoldStart();
				WriteAttributes(field.CustomAttributes);
				output.MarkFoldEnd();
			}
		}
		#endregion
		
		#region Disassemble Property
		EnumNameCollection<PropertyAttributes> propertyAttributes = new EnumNameCollection<PropertyAttributes>() {
			{ PropertyAttributes.SpecialName, "specialname" },
			{ PropertyAttributes.RTSpecialName, "rtspecialname" },
			{ PropertyAttributes.HasDefault, "hasdefault" },
		};
		
		public void DisassembleProperty(PropertyDef property, bool full = true)
		{
			// set current member
			currentMember = property;
			
			WriteXmlDocComment(property);
			output.WriteDefinition(".property", property, TextTokenType.ILDirective, false);
			output.WriteSpace();
			WriteFlags(property.Attributes, propertyAttributes);
			if (property.PropertySig != null && property.PropertySig.HasThis) {
				output.Write("instance", TextTokenType.Keyword);
				output.WriteSpace();
			}
			property.PropertySig.GetRetType().WriteTo(output);
			output.WriteSpace();
			output.Write(DisassemblerHelpers.Escape(property.Name), TextTokenHelper.GetTextTokenType(property));
			
			output.Write('(', TextTokenType.Operator);
			var parameters = new List<Parameter>(property.GetParameters());
			if (parameters.GetNumberOfNormalParameters() > 0) {
				output.WriteLine();
				output.Indent();
				WriteParameters(parameters);
				output.Unindent();
			}
			output.Write(')', TextTokenType.Operator);

			if (full) {
				OpenBlock(false);
				WriteAttributes(property.CustomAttributes);
				WriteNestedMethod(".get", property.GetMethod);
				WriteNestedMethod(".set", property.SetMethod);

				foreach (var method in property.OtherMethods) {
					WriteNestedMethod(".other", method);
				}
				CloseBlock();
			}
			else {
				output.WriteSpace();
				output.WriteLeftBrace();

				if (property.GetMethods.Count > 0) {
					output.WriteSpace();
					output.Write(".get", TextTokenType.Keyword);
					output.Write(';', TextTokenType.Operator);
				}

				if (property.SetMethods.Count > 0) {
					output.WriteSpace();
					output.Write(".set", TextTokenType.Keyword);
					output.Write(';', TextTokenType.Operator);
				}

				output.WriteSpace();
				output.WriteRightBrace();
			}
		}
		
		void WriteNestedMethod(string keyword, MethodDef method)
		{
			if (method == null)
				return;
			
			output.Write(keyword, TextTokenType.ILDirective);
			output.WriteSpace();
			method.WriteMethodTo(output);
			output.WriteLine();
		}
		#endregion
		
		#region Disassemble Event
		EnumNameCollection<EventAttributes> eventAttributes = new EnumNameCollection<EventAttributes>() {
			{ EventAttributes.SpecialName, "specialname" },
			{ EventAttributes.RTSpecialName, "rtspecialname" },
		};
		
		public void DisassembleEvent(EventDef ev, bool full = true)
		{
			// set current member
			currentMember = ev;
			
			WriteXmlDocComment(ev);
			output.WriteDefinition(".event", ev, TextTokenType.ILDirective, false);
			output.WriteSpace();
			WriteFlags(ev.Attributes, eventAttributes);
			ev.EventType.WriteTo(output, ILNameSyntax.TypeName);
			output.WriteSpace();
			output.Write(DisassemblerHelpers.Escape(ev.Name), TextTokenHelper.GetTextTokenType(ev));

			if (full) {
				OpenBlock(false);
				WriteAttributes(ev.CustomAttributes);
				WriteNestedMethod(".addon", ev.AddMethod);
				WriteNestedMethod(".removeon", ev.RemoveMethod);
				WriteNestedMethod(".fire", ev.InvokeMethod);
				foreach (var method in ev.OtherMethods) {
					WriteNestedMethod(".other", method);
				}
				CloseBlock();
			}
			else {
				output.WriteSpace();
				output.WriteLeftBrace();

				if (ev.AddMethod != null) {
					output.WriteSpace();
					output.Write(".addon", TextTokenType.Keyword);
					output.Write(';', TextTokenType.Operator);
				}

				if (ev.RemoveMethod != null) {
					output.WriteSpace();
					output.Write(".removeon", TextTokenType.Keyword);
					output.Write(';', TextTokenType.Operator);
				}

				if (ev.InvokeMethod != null) {
					output.WriteSpace();
					output.Write(".fire", TextTokenType.Keyword);
					output.Write(';', TextTokenType.Operator);
				}

				output.WriteSpace();
				output.WriteRightBrace();
			}
		}
		#endregion
		
		#region Disassemble Type
		EnumNameCollection<TypeAttributes> typeVisibility = new EnumNameCollection<TypeAttributes>() {
			{ TypeAttributes.Public, "public" },
			{ TypeAttributes.NotPublic, "private" },
			{ TypeAttributes.NestedPublic, "nested public" },
			{ TypeAttributes.NestedPrivate, "nested private" },
			{ TypeAttributes.NestedAssembly, "nested assembly" },
			{ TypeAttributes.NestedFamily, "nested family" },
			{ TypeAttributes.NestedFamANDAssem, "nested famandassem" },
			{ TypeAttributes.NestedFamORAssem, "nested famorassem" },
		};
		
		EnumNameCollection<TypeAttributes> typeLayout = new EnumNameCollection<TypeAttributes>() {
			{ TypeAttributes.AutoLayout, "auto" },
			{ TypeAttributes.SequentialLayout, "sequential" },
			{ TypeAttributes.ExplicitLayout, "explicit" },
		};
		
		EnumNameCollection<TypeAttributes> typeStringFormat = new EnumNameCollection<TypeAttributes>() {
			{ TypeAttributes.AutoClass, "auto" },
			{ TypeAttributes.AnsiClass, "ansi" },
			{ TypeAttributes.UnicodeClass, "unicode" },
		};
		
		EnumNameCollection<TypeAttributes> typeAttributes = new EnumNameCollection<TypeAttributes>() {
			{ TypeAttributes.Abstract, "abstract" },
			{ TypeAttributes.Sealed, "sealed" },
			{ TypeAttributes.SpecialName, "specialname" },
			{ TypeAttributes.Import, "import" },
			{ TypeAttributes.Serializable, "serializable" },
			{ TypeAttributes.WindowsRuntime, "windowsruntime" },
			{ TypeAttributes.BeforeFieldInit, "beforefieldinit" },
			{ TypeAttributes.HasSecurity, null },
		};
		
		public void DisassembleType(TypeDef type)
		{
			// start writing IL
			WriteXmlDocComment(type);
			output.WriteDefinition(".class", type, TextTokenType.ILDirective, false);
			output.WriteSpace();
			
			if ((type.Attributes & TypeAttributes.ClassSemanticMask) == TypeAttributes.Interface) {
				output.Write("interface", TextTokenType.Keyword);
				output.WriteSpace();
			}
			WriteEnum(type.Attributes & TypeAttributes.VisibilityMask, typeVisibility);
			WriteEnum(type.Attributes & TypeAttributes.LayoutMask, typeLayout);
			WriteEnum(type.Attributes & TypeAttributes.StringFormatMask, typeStringFormat);
			const TypeAttributes masks = TypeAttributes.ClassSemanticMask | TypeAttributes.VisibilityMask | TypeAttributes.LayoutMask | TypeAttributes.StringFormatMask;
			WriteFlags(type.Attributes & ~masks, typeAttributes);
			
			output.Write(DisassemblerHelpers.Escape(type.DeclaringType != null ? type.Name.String : type.FullName), TextTokenHelper.GetTextTokenType(type));
			WriteTypeParameters(output, type);
			output.MarkFoldStart(defaultCollapsed: isInType);
			output.WriteLine();
			
			if (type.BaseType != null) {
				output.Indent();
				output.Write("extends", TextTokenType.Keyword);
				output.WriteSpace();
				type.BaseType.WriteTo(output, ILNameSyntax.TypeName);
				output.WriteLine();
				output.Unindent();
			}
			if (type.HasInterfaces) {
				output.Indent();
				for (int index = 0; index < type.Interfaces.Count; index++) {
					if (index > 0)
						output.WriteLine(",", TextTokenType.Operator);
					if (index == 0) {
						output.Write("implements", TextTokenType.Keyword);
						output.WriteSpace();
					}
					else
						output.Write("           ", TextTokenType.Text);
					type.Interfaces[index].Interface.WriteTo(output, ILNameSyntax.TypeName);
				}
				output.WriteLine();
				output.Unindent();
			}
			
			output.WriteLineLeftBrace();
			output.Indent();
			bool oldIsInType = isInType;
			isInType = true;
			WriteAttributes(type.CustomAttributes);
			WriteSecurityDeclarations(type);
			if (type.HasClassLayout) {
				output.Write(".pack", TextTokenType.ILDirective);
				output.WriteSpace();
				output.WriteLine(string.Format("{0}", type.PackingSize), TextTokenType.Number);
				output.Write(".size", TextTokenType.ILDirective);
				output.WriteSpace();
				output.WriteLine(string.Format("{0}", type.ClassSize), TextTokenType.Number);
				output.WriteLine();
			}
			if (type.HasNestedTypes) {
				output.WriteLine("// Nested Types", TextTokenType.Comment);
				foreach (var nestedType in type.NestedTypes) {
					options.CancellationToken.ThrowIfCancellationRequested();
					DisassembleType(nestedType);
					output.WriteLine();
				}
				output.WriteLine();
			}
			if (type.HasFields) {
				output.WriteLine("// Fields", TextTokenType.Comment);
				foreach (var field in type.Fields) {
					options.CancellationToken.ThrowIfCancellationRequested();
					DisassembleField(field);
				}
				output.WriteLine();
			}
			if (type.HasMethods) {
				output.WriteLine("// Methods", TextTokenType.Comment);
				foreach (var m in type.Methods) {
					options.CancellationToken.ThrowIfCancellationRequested();
					DisassembleMethod(m);
					output.WriteLine();
				}
			}
			if (type.HasEvents) {
				output.WriteLine("// Events", TextTokenType.Comment);
				foreach (var ev in type.Events) {
					options.CancellationToken.ThrowIfCancellationRequested();
					DisassembleEvent(ev);
					output.WriteLine();
				}
				output.WriteLine();
			}
			if (type.HasProperties) {
				output.WriteLine("// Properties", TextTokenType.Comment);
				foreach (var prop in type.Properties) {
					options.CancellationToken.ThrowIfCancellationRequested();
					DisassembleProperty(prop);
				}
				output.WriteLine();
			}
			CloseBlock("end of class " + (type.DeclaringType != null ? type.Name.String : type.FullName));
			isInType = oldIsInType;
		}
		
		void WriteTypeParameters(ITextOutput output, ITypeOrMethodDef p)
		{
			if (p.HasGenericParameters) {
				output.Write('<', TextTokenType.Operator);
				for (int i = 0; i < p.GenericParameters.Count; i++) {
					if (i > 0) {
						output.Write(',', TextTokenType.Operator);
						output.WriteSpace();
					}
					GenericParam gp = p.GenericParameters[i];
					if (gp.HasReferenceTypeConstraint) {
						output.Write("class", TextTokenType.Keyword);
						output.WriteSpace();
					} else if (gp.HasNotNullableValueTypeConstraint) {
						output.Write("valuetype", TextTokenType.Keyword);
						output.WriteSpace();
					}
					if (gp.HasDefaultConstructorConstraint) {
						output.Write(".ctor", TextTokenType.Keyword);
						output.WriteSpace();
					}
					if (gp.HasGenericParamConstraints) {
						output.Write('(', TextTokenType.Operator);
						for (int j = 0; j < gp.GenericParamConstraints.Count; j++) {
							if (j > 0) {
								output.Write(',', TextTokenType.Operator);
								output.WriteSpace();
							}
							gp.GenericParamConstraints[j].Constraint.WriteTo(output, ILNameSyntax.TypeName);
						}
						output.Write(')', TextTokenType.Operator);
						output.WriteSpace();
					}
					if (gp.IsContravariant) {
						output.Write('-', TextTokenType.Operator);
					} else if (gp.IsCovariant) {
						output.Write('+', TextTokenType.Operator);
					}
					output.Write(DisassemblerHelpers.Escape(gp.Name), TextTokenHelper.GetTextTokenType(gp));
				}
				output.Write('>', TextTokenType.Operator);
			}
		}
		#endregion

		#region Helper methods
		void WriteAttributes(CustomAttributeCollection attributes)
		{
			foreach (CustomAttribute a in attributes) {
				output.Write(".custom", TextTokenType.ILDirective);
				output.WriteSpace();
				a.Constructor.WriteMethodTo(output);
				byte[] blob = a.GetBlob();
				if (blob != null) {
					output.WriteSpace();
					output.Write('=', TextTokenType.Operator);
					output.WriteSpace();
					WriteBlob(blob);
				}
				output.WriteLine();
			}
		}
		
		void WriteBlob(byte[] blob)
		{
			output.Write('(', TextTokenType.Operator);
			output.Indent();

			for (int i = 0; i < blob.Length; i++) {
				if (i % 16 == 0 && i < blob.Length - 1) {
					output.WriteLine();
				} else {
					output.WriteSpace();
				}
				output.Write(blob[i].ToString("x2"), TextTokenType.Number);
			}
			
			output.WriteLine();
			output.Unindent();
			output.Write(')', TextTokenType.Operator);
		}
		
		void OpenBlock(bool defaultCollapsed)
		{
			output.MarkFoldStart(defaultCollapsed: defaultCollapsed);
			output.WriteLine();
			output.WriteLineLeftBrace();
			output.Indent();
		}
		
		void CloseBlock(string comment = null)
		{
			output.Unindent();
			output.WriteRightBrace();
			if (comment != null) {
				output.WriteSpace();
				output.Write("// " + comment, TextTokenType.Comment);
			}
			output.MarkFoldEnd();
			output.WriteLine();
		}
		
		void WriteFlags<T>(T flags, EnumNameCollection<T> flagNames) where T : struct
		{
			long val = Convert.ToInt64(flags);
			long tested = 0;
			foreach (var pair in flagNames) {
				tested |= pair.Key;
				if ((val & pair.Key) != 0 && pair.Value != null) {
					foreach (var kv in pair.Value.Split(' ')) {
						output.Write(kv, TextTokenType.Keyword);
						output.WriteSpace();
					}
				}
			}
			if ((val & ~tested) != 0) {
				output.Write(string.Format("flag({0:x4})", val & ~tested), TextTokenType.Keyword);
				output.WriteSpace();
			}
		}
		
		void WriteEnum<T>(T enumValue, EnumNameCollection<T> enumNames) where T : struct
		{
			long val = Convert.ToInt64(enumValue);
			foreach (var pair in enumNames) {
				if (pair.Key == val) {
					if (pair.Value != null) {
						foreach (var kv in pair.Value.Split(' ')) {
							output.Write(kv, TextTokenType.Keyword);
							output.WriteSpace();
						}
					}
					return;
				}
			}
			if (val != 0) {
				output.Write(string.Format("flag({0:x4})", val), TextTokenType.Keyword);
				output.WriteSpace();
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
		#endregion
		
		public void DisassembleNamespace(string nameSpace, IEnumerable<TypeDef> types)
		{
			if (!string.IsNullOrEmpty(nameSpace)) {
				output.Write(".namespace", TextTokenType.ILDirective);
				output.WriteSpace();
				output.Write(DisassemblerHelpers.Escape(nameSpace), TextTokenType.NamespacePart);
				OpenBlock(false);
			}
			bool oldIsInType = isInType;
			isInType = true;
			foreach (TypeDef td in types) {
				options.CancellationToken.ThrowIfCancellationRequested();
				DisassembleType(td);
				output.WriteLine();
			}
			if (!string.IsNullOrEmpty(nameSpace)) {
				CloseBlock();
				isInType = oldIsInType;
			}
		}
		
		public void WriteAssemblyHeader(AssemblyDef asm)
		{
			output.Write(".assembly", TextTokenType.ILDirective);
			output.WriteSpace();
			if (asm.IsContentTypeWindowsRuntime) {
				output.Write("windowsruntime", TextTokenType.Keyword);
				output.WriteSpace();
			}
			output.Write(DisassemblerHelpers.Escape(asm.Name), TextTokenType.Text);
			OpenBlock(false);
			WriteAttributes(asm.CustomAttributes);
			WriteSecurityDeclarations(asm);
			if (asm.PublicKey != null && !asm.PublicKey.IsNullOrEmpty) {
				output.Write(".publickey", TextTokenType.ILDirective);
				output.WriteSpace();
				output.Write('=', TextTokenType.Operator);
				output.WriteSpace();
				WriteBlob(asm.PublicKey.Data);
				output.WriteLine();
			}
			if (asm.HashAlgorithm != AssemblyHashAlgorithm.None) {
				output.Write(".hash", TextTokenType.ILDirective);
				output.WriteSpace();
				output.Write("algorithm", TextTokenType.Keyword);
				output.WriteSpace();
				output.Write(string.Format("0x{0:x8}", (uint)asm.HashAlgorithm), TextTokenType.Number);
				if (asm.HashAlgorithm == AssemblyHashAlgorithm.SHA1) {
					output.WriteSpace();
					output.Write("// SHA1", TextTokenType.Comment);
				}
				output.WriteLine();
			}
			Version v = asm.Version;
			if (v != null) {
				output.Write(".ver", TextTokenType.ILDirective);
				output.WriteSpace();
				output.Write(string.Format("{0}", v.Major), TextTokenType.Number);
				output.Write(':', TextTokenType.Operator);
				output.Write(string.Format("{0}", v.Minor), TextTokenType.Number);
				output.Write(':', TextTokenType.Operator);
				output.Write(string.Format("{0}", v.Build), TextTokenType.Number);
				output.Write(':', TextTokenType.Operator);
				output.WriteLine(string.Format("{0}", v.Revision), TextTokenType.Number);
			}
			CloseBlock();
		}
		
		public void WriteAssemblyReferences(ModuleDefMD module)
		{
			if (module == null)
				return;
			foreach (var mref in module.GetModuleRefs()) {
				output.Write(".module", TextTokenType.ILDirective);
				output.WriteSpace();
				output.Write("extern", TextTokenType.Keyword);
				output.WriteSpace();
				output.WriteLine(DisassemblerHelpers.Escape(mref.Name), TextTokenType.Text);
			}
			foreach (var aref in module.GetAssemblyRefs()) {
				output.Write(".assembly", TextTokenType.ILDirective);
				output.WriteSpace();
				output.Write("extern", TextTokenType.Keyword);
				output.WriteSpace();
				if (aref.IsContentTypeWindowsRuntime) {
					output.Write("windowsruntime", TextTokenType.Keyword);
					output.WriteSpace();
				}
				output.Write(DisassemblerHelpers.Escape(aref.Name), TextTokenType.Text);
				OpenBlock(false);
				if (!PublicKeyBase.IsNullOrEmpty2(aref.PublicKeyOrToken)) {
					output.Write(".publickeytoken", TextTokenType.ILDirective);
					output.WriteSpace();
					output.Write('=', TextTokenType.Operator);
					output.WriteSpace();
					WriteBlob(aref.PublicKeyOrToken.Token.Data);
					output.WriteLine();
				}
				if (aref.Version != null) {
					output.Write(".ver", TextTokenType.ILDirective);
					output.WriteSpace();
					output.Write(string.Format("{0}", aref.Version.Major), TextTokenType.Number);
					output.Write(':', TextTokenType.Operator);
					output.Write(string.Format("{0}", aref.Version.Minor), TextTokenType.Number);
					output.Write(':', TextTokenType.Operator);
					output.Write(string.Format("{0}", aref.Version.Build), TextTokenType.Number);
					output.Write(':', TextTokenType.Operator);
					output.WriteLine(string.Format("{0}", aref.Version.Revision), TextTokenType.Number);
				}
				CloseBlock();
			}
		}
		
		public void WriteModuleHeader(ModuleDef module)
		{
			if (module.HasExportedTypes) {
				foreach (ExportedType exportedType in module.ExportedTypes) {
					output.Write(".class", TextTokenType.ILDirective);
					output.WriteSpace();
					output.Write("extern", TextTokenType.Keyword);
					output.WriteSpace();
					if (exportedType.IsForwarder) {
						output.Write("forwarder", TextTokenType.Keyword);
						output.WriteSpace();
					}
					output.Write(exportedType.DeclaringType != null ? exportedType.TypeName.String : exportedType.FullName, TextTokenHelper.GetTextTokenType(exportedType));
					OpenBlock(false);
					if (exportedType.DeclaringType != null) {
						output.Write(".class", TextTokenType.ILDirective);
						output.WriteSpace();
						output.Write("extern", TextTokenType.Keyword);
						output.WriteSpace();
						output.WriteLine(DisassemblerHelpers.Escape(exportedType.DeclaringType.FullName), TextTokenHelper.GetTextTokenType(exportedType.DeclaringType));
					}
					else {
						output.Write(".assembly", TextTokenType.ILDirective);
						output.WriteSpace();
						output.Write("extern", TextTokenType.Keyword);
						output.WriteSpace();
						output.WriteLine(DisassemblerHelpers.Escape(exportedType.Scope.GetScopeName()), TextTokenType.Text);
					}
					CloseBlock();
				}
			}
			
			output.Write(".module", TextTokenType.ILDirective);
			output.WriteSpace();
			output.WriteLine(module.Name, TextTokenType.Text);
			if (module.Mvid.HasValue)
				output.WriteLine(string.Format("// MVID: {0}", module.Mvid.Value.ToString("B").ToUpperInvariant()), TextTokenType.Comment);
			// TODO: imagebase, file alignment, stackreserve, subsystem
			output.Write(".corflags", TextTokenType.ILDirective);
			output.WriteSpace();
			output.Write(string.Format("0x{0:x}", module.Cor20HeaderFlags), TextTokenType.Number);
			output.WriteSpace();
			output.WriteLine(string.Format("// {0}", module.Cor20HeaderFlags.ToString()), TextTokenType.Comment);
			
			WriteAttributes(module.CustomAttributes);
		}
		
		public void WriteModuleContents(ModuleDef module)
		{
			foreach (TypeDef td in module.Types) {
				DisassembleType(td);
				output.WriteLine();
			}
		}
	}
}
