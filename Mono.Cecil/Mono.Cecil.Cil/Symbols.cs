//
// Symbols.cs
//
// Author:
//   Jb Evain (jbevain@gmail.com)
//
// Copyright (c) 2008 - 2011 Jb Evain
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.IO;
using System.Runtime.InteropServices;
using SR = System.Reflection;

using Mono.Collections.Generic;

namespace Mono.Cecil.Cil {

	[StructLayout (LayoutKind.Sequential)]
	public struct ImageDebugDirectory {
		public int Characteristics;
		public int TimeDateStamp;
		public short MajorVersion;
		public short MinorVersion;
		public int Type;
		public int SizeOfData;
		public int AddressOfRawData;
		public int PointerToRawData;
	}

	public sealed class Scope : IVariableDefinitionProvider {

		Instruction start;
		Instruction end;

		Collection<Scope> scopes;
		Collection<VariableDefinition> variables;

		public Instruction Start {
			get { return start; }
			set { start = value; }
		}

		public Instruction End {
			get { return end; }
			set { end = value; }
		}

		public bool HasScopes {
			get { return !scopes.IsNullOrEmpty (); }
		}

		public Collection<Scope> Scopes {
			get {
				if (scopes == null)
					scopes = new Collection<Scope> ();

				return scopes;
			}
		}

		public bool HasVariables {
			get { return !variables.IsNullOrEmpty (); }
		}

		public Collection<VariableDefinition> Variables {
			get {
				if (variables == null)
					variables = new Collection<VariableDefinition> ();

				return variables;
			}
		}
	}

	public struct InstructionSymbol {

		public readonly int Offset;
		public readonly SequencePoint SequencePoint;

		public InstructionSymbol (int offset, SequencePoint sequencePoint)
		{
			this.Offset = offset;
			this.SequencePoint = sequencePoint;
		}
	}

	public sealed class MethodSymbols {

		internal int code_size;
		internal string method_name;
		internal MetadataToken method_token;
		internal MetadataToken local_var_token;
		internal Collection<VariableDefinition> variables;
		internal Collection<InstructionSymbol> instructions;

		public bool HasVariables {
			get { return !variables.IsNullOrEmpty (); }
		}

		public Collection<VariableDefinition> Variables {
			get {
				if (variables == null)
					variables = new Collection<VariableDefinition> ();

				return variables;
			}
		}

		public Collection<InstructionSymbol> Instructions {
			get {
				if (instructions == null)
					instructions = new Collection<InstructionSymbol> ();

				return instructions;
			}
		}

		public int CodeSize {
			get { return code_size; }
		}

		public string MethodName {
			get { return method_name; }
		}

		public MetadataToken MethodToken {
			get { return method_token; }
		}

		public MetadataToken LocalVarToken {
			get { return local_var_token; }
		}

		internal MethodSymbols (string methodName)
		{
			this.method_name = methodName;
		}

		public MethodSymbols (MetadataToken methodToken)
		{
			this.method_token = methodToken;
		}
	}

	public delegate Instruction InstructionMapper (int offset);

	public interface ISymbolReader : IDisposable {

		bool ProcessDebugHeader (ImageDebugDirectory directory, byte [] header);
		void Read (MethodBody body, InstructionMapper mapper);
		void Read (MethodSymbols symbols);
	}

	public interface ISymbolReaderProvider {

		ISymbolReader GetSymbolReader (ModuleDefinition module, string fileName);
		ISymbolReader GetSymbolReader (ModuleDefinition module, Stream symbolStream);
	}

	static class SymbolProvider {

		static readonly string symbol_kind = Type.GetType ("Mono.Runtime") != null ? "Mdb" : "Pdb";

		static SR.AssemblyName GetPlatformSymbolAssemblyName ()
		{
			var cecil_name = typeof (SymbolProvider).Assembly.GetName ();

			var name = new SR.AssemblyName {
				Name = "Mono.Cecil." + symbol_kind,
				Version = cecil_name.Version,
			};

			name.SetPublicKeyToken (cecil_name.GetPublicKeyToken ());

			return name;
		}

		static Type GetPlatformType (string fullname)
		{
			var type = Type.GetType (fullname);
			if (type != null)
				return type;

			var assembly_name = GetPlatformSymbolAssemblyName ();

			type = Type.GetType (fullname + ", " + assembly_name.FullName);
			if (type != null)
				return type;

			try {
				var assembly = SR.Assembly.Load (assembly_name);
				if (assembly != null)
					return assembly.GetType (fullname);
			} catch (FileNotFoundException) {
#if !CF
			} catch (FileLoadException) {
#endif
			}

			return null;
		}

		static ISymbolReaderProvider reader_provider;

		public static ISymbolReaderProvider GetPlatformReaderProvider ()
		{
			if (reader_provider != null)
				return reader_provider;

			var type = GetPlatformType (GetProviderTypeName ("ReaderProvider"));
			if (type == null)
				return null;

			return reader_provider = (ISymbolReaderProvider) Activator.CreateInstance (type);
		}

		static string GetProviderTypeName (string name)
		{
			return "Mono.Cecil." + symbol_kind + "." + symbol_kind + name;
		}

#if !READ_ONLY

		static ISymbolWriterProvider writer_provider;

		public static ISymbolWriterProvider GetPlatformWriterProvider ()
		{
			if (writer_provider != null)
				return writer_provider;

			var type = GetPlatformType (GetProviderTypeName ("WriterProvider"));
			if (type == null)
				return null;

			return writer_provider = (ISymbolWriterProvider) Activator.CreateInstance (type);
		}

#endif
	}

#if !READ_ONLY

	public interface ISymbolWriter : IDisposable {

		bool GetDebugHeader (out ImageDebugDirectory directory, out byte [] header);
		void Write (MethodBody body);
		void Write (MethodSymbols symbols);
	}

	public interface ISymbolWriterProvider {

		ISymbolWriter GetSymbolWriter (ModuleDefinition module, string fileName);
		ISymbolWriter GetSymbolWriter (ModuleDefinition module, Stream symbolStream);
	}

#endif
}
