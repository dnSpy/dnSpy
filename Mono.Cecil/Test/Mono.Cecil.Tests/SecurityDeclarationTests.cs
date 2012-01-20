using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

using Mono.Cecil;
using Mono.Cecil.Metadata;
using Mono.Cecil.PE;

using NUnit.Framework;

namespace Mono.Cecil.Tests {

	[TestFixture]
	public class SecurityDeclarationTests : BaseTestFixture {

		[TestModule ("decsec-xml.dll")]
		public void XmlSecurityDeclaration (ModuleDefinition module)
		{
			var type = module.GetType ("SubLibrary");

			Assert.IsTrue (type.HasSecurityDeclarations);

			Assert.AreEqual (1, type.SecurityDeclarations.Count);

			var declaration = type.SecurityDeclarations [0];
			Assert.AreEqual (SecurityAction.Deny, declaration.Action);

			Assert.AreEqual (1, declaration.SecurityAttributes.Count);

			var attribute = declaration.SecurityAttributes [0];

			Assert.AreEqual ("System.Security.Permissions.PermissionSetAttribute", attribute.AttributeType.FullName);

			Assert.AreEqual (1, attribute.Properties.Count);

			var named_argument = attribute.Properties [0];

			Assert.AreEqual ("XML", named_argument.Name);

			var argument = named_argument.Argument;

			Assert.AreEqual ("System.String", argument.Type.FullName);

			const string permission_set = "<PermissionSet class=\"System.Security.PermissionSe"
				+ "t\"\r\nversion=\"1\">\r\n<IPermission class=\"System.Security.Permis"
				+ "sions.SecurityPermission, mscorlib, Version=2.0.0.0, Culture"
				+ "=neutral, PublicKeyToken=b77a5c561934e089\"\r\nversion=\"1\"\r\nFla"
				+ "gs=\"UnmanagedCode\"/>\r\n</PermissionSet>\r\n";

			Assert.AreEqual (permission_set, argument.Value);
		}

		[TestModule ("decsec1-xml.dll")]
		public void XmlNet_1_1SecurityDeclaration (ModuleDefinition module)
		{
			var type = module.GetType ("SubLibrary");

			Assert.IsTrue (type.HasSecurityDeclarations);

			Assert.AreEqual (1, type.SecurityDeclarations.Count);

			var declaration = type.SecurityDeclarations [0];
			Assert.AreEqual (SecurityAction.Deny, declaration.Action);

			Assert.AreEqual (1, declaration.SecurityAttributes.Count);

			var attribute = declaration.SecurityAttributes [0];

			Assert.AreEqual ("System.Security.Permissions.PermissionSetAttribute", attribute.AttributeType.FullName);

			Assert.AreEqual (1, attribute.Properties.Count);

			var named_argument = attribute.Properties [0];

			Assert.AreEqual ("XML", named_argument.Name);

			var argument = named_argument.Argument;

			Assert.AreEqual ("System.String", argument.Type.FullName);

			const string permission_set = "<PermissionSet class=\"System.Security.PermissionSe"
				+ "t\"\r\nversion=\"1\">\r\n<IPermission class=\"System.Security.Permis"
				+ "sions.SecurityPermission, mscorlib, Version=1.0.0.0, Culture"
				+ "=neutral, PublicKeyToken=b77a5c561934e089\"\r\nversion=\"1\"\r\nFla"
				+ "gs=\"UnmanagedCode\"/>\r\n</PermissionSet>\r\n";

			Assert.AreEqual (permission_set, argument.Value);
		}

		[Test]
		public void DefineSecurityDeclarationByBlob ()
		{
			var file = Path.Combine(Path.GetTempPath(), "SecDecBlob.dll");
			var module = ModuleDefinition.CreateModule ("SecDecBlob.dll", new ModuleParameters { Kind = ModuleKind.Dll, Runtime = TargetRuntime.Net_2_0 });

			const string permission_set = "<PermissionSet class=\"System.Security.PermissionSe"
				+ "t\"\r\nversion=\"1\">\r\n<IPermission class=\"System.Security.Permis"
				+ "sions.SecurityPermission, mscorlib, Version=2.0.0.0, Culture"
				+ "=neutral, PublicKeyToken=b77a5c561934e089\"\r\nversion=\"1\"\r\nFla"
				+ "gs=\"UnmanagedCode\"/>\r\n</PermissionSet>\r\n";

			var declaration = new SecurityDeclaration (SecurityAction.Deny, Encoding.Unicode.GetBytes (permission_set));
			module.Assembly.SecurityDeclarations.Add (declaration);

			module.Write (file);
			module = ModuleDefinition.ReadModule (file);

			declaration = module.Assembly.SecurityDeclarations [0];
			Assert.AreEqual (SecurityAction.Deny, declaration.Action);
			Assert.AreEqual (1, declaration.SecurityAttributes.Count);

			var attribute = declaration.SecurityAttributes [0];
			Assert.AreEqual ("System.Security.Permissions.PermissionSetAttribute", attribute.AttributeType.FullName);
			Assert.AreEqual (1, attribute.Properties.Count);

			var named_argument = attribute.Properties [0];
			Assert.AreEqual ("XML", named_argument.Name);
			var argument = named_argument.Argument;
			Assert.AreEqual ("System.String", argument.Type.FullName);
			Assert.AreEqual (permission_set, argument.Value);
		}

		[TestModule ("empty-decsec-att.dll")]
		public void SecurityDeclarationWithoutAttributes (ModuleDefinition module)
		{
			var type = module.GetType ("TestSecurityAction.ModalUITypeEditor");
			var method = type.GetMethod ("GetEditStyle");

			Assert.IsNotNull (method);

			Assert.AreEqual (1, method.SecurityDeclarations.Count);

			var declaration = method.SecurityDeclarations [0];
			Assert.AreEqual (SecurityAction.LinkDemand, declaration.Action);
			Assert.AreEqual (1, declaration.SecurityAttributes.Count);

			var attribute = declaration.SecurityAttributes [0];
			Assert.AreEqual ("System.Security.Permissions.SecurityPermissionAttribute", attribute.AttributeType.FullName);
			Assert.AreEqual (0, attribute.Fields.Count);
			Assert.AreEqual (0, attribute.Properties.Count);
		}

		[TestModule ("decsec-att.dll")]
		public void AttributeSecurityDeclaration (ModuleDefinition module)
		{
			var type = module.GetType ("SubLibrary");

			Assert.IsTrue (type.HasSecurityDeclarations);

			Assert.AreEqual (1, type.SecurityDeclarations.Count);

			var declaration = type.SecurityDeclarations [0];
			Assert.AreEqual (SecurityAction.Deny, declaration.Action);

			Assert.AreEqual (1, declaration.SecurityAttributes.Count);

			var attribute = declaration.SecurityAttributes [0];

			Assert.AreEqual ("System.Security.Permissions.SecurityPermissionAttribute", attribute.AttributeType.FullName);

			Assert.AreEqual (1, attribute.Properties.Count);

			var named_argument = attribute.Properties [0];

			Assert.AreEqual ("UnmanagedCode", named_argument.Name);

			var argument = named_argument.Argument;

			Assert.AreEqual ("System.Boolean", argument.Type.FullName);

			Assert.AreEqual (true, argument.Value);
		}

		static void AssertCustomAttributeArgument (string expected, CustomAttributeNamedArgument named_argument)
		{
			AssertCustomAttributeArgument (expected, named_argument.Argument);
		}

		static void AssertCustomAttributeArgument (string expected, CustomAttributeArgument argument)
		{
			var result = new StringBuilder ();
			PrettyPrint (argument, result);

			Assert.AreEqual (expected, result.ToString ());
		}

		static string PrettyPrint (CustomAttribute attribute)
		{
			var signature = new StringBuilder ();
			signature.Append (".ctor (");

			for (int i = 0; i < attribute.ConstructorArguments.Count; i++) {
				if (i > 0)
					signature.Append (", ");

				PrettyPrint (attribute.ConstructorArguments [i], signature);
			}

			signature.Append (")");
			return signature.ToString ();
		}

		static void PrettyPrint (CustomAttributeArgument argument, StringBuilder signature)
		{
			var value = argument.Value;

			signature.Append ("(");

			PrettyPrint (argument.Type, signature);

			signature.Append (":");

			PrettyPrintValue (argument.Value, signature);

			signature.Append (")");
		}

		static void PrettyPrintValue (object value, StringBuilder signature)
		{
			if (value == null) {
				signature.Append ("null");
				return;
			}

			var arguments = value as CustomAttributeArgument [];
			if (arguments != null) {
				signature.Append ("{");
				for (int i = 0; i < arguments.Length; i++) {
					if (i > 0)
						signature.Append (", ");

					PrettyPrint (arguments [i], signature);
				}
				signature.Append ("}");

				return;
			}

			switch (Type.GetTypeCode (value.GetType ())) {
			case TypeCode.String:
				signature.AppendFormat ("\"{0}\"", value);
				break;
			case TypeCode.Char:
				signature.AppendFormat ("'{0}'", (char) value);
				break;
			default:
				var formattable = value as IFormattable;
				if (formattable != null) {
					signature.Append (formattable.ToString (null, CultureInfo.InvariantCulture));
					return;
				}

				if (value is CustomAttributeArgument) {
					PrettyPrint ((CustomAttributeArgument) value, signature);
					return;
				}
				break;
			}
		}

		static void PrettyPrint (TypeReference type, StringBuilder signature)
		{
			if (type.IsArray) {
				ArrayType array = (ArrayType) type;
				signature.AppendFormat ("{0}[]", array.ElementType.etype.ToString ());
			} else if (type.etype == ElementType.None) {
				signature.Append (type.FullName);
			} else
				signature.Append (type.etype.ToString ());
		}

		static void AssertArgument<T> (T value, CustomAttributeNamedArgument named_argument)
		{
			AssertArgument (value, named_argument.Argument);
		}

		static void AssertArgument<T> (T value, CustomAttributeArgument argument)
		{
			AssertArgument (typeof (T).FullName, (object) value, argument);
		}

		static void AssertArgument (string type, object value, CustomAttributeArgument argument)
		{
			Assert.AreEqual (type, argument.Type.FullName);
			Assert.AreEqual (value, argument.Value);
		}
	}
}
