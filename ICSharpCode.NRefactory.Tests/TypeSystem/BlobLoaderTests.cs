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
using ICSharpCode.NRefactory.TypeSystem.Implementation;
using Mono.Cecil;
using NUnit.Framework;

namespace ICSharpCode.NRefactory.TypeSystem
{
	/* Commented out because the Mono.Cecil 0.9.5 release does not have the SecurityDeclaration ctor
	[TestFixture]
	public class BlobLoaderTests
	{
		[Test]
		public void GetCompressedStackSecDecl()
		{
			// Compressed Stack from mscorlib 2.0
			// [SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.UnmanagedCode),
			//  StrongNameIdentityPermission(SecurityAction.LinkDemand, PublicKey = "0x00000000000000000400000000000000")]
			byte[] blob = Convert.FromBase64String(@"
LgKAhFN5c3RlbS5TZWN1cml0eS5QZXJtaXNzaW9ucy5TZWN1cml0eVBlcm1pc3Npb25BdHRyaWJ1dGUs
IG1zY29ybGliLCBWZXJzaW9uPTIuMC4wLjAsIEN1bHR1cmU9bmV1dHJhbCwgUHVibGljS2V5VG9rZW49
Yjc3YTVjNTYxOTM0ZTA4OUABVFUyU3lzdGVtLlNlY3VyaXR5LlBlcm1pc3Npb25zLlNlY3VyaXR5UGVy
bWlzc2lvbkZsYWcFRmxhZ3MCAAAAgI5TeXN0ZW0uU2VjdXJpdHkuUGVybWlzc2lvbnMuU3Ryb25nTmFt
ZUlkZW50aXR5UGVybWlzc2lvbkF0dHJpYnV0ZSwgbXNjb3JsaWIsIFZlcnNpb249Mi4wLjAuMCwgQ3Vs
dHVyZT1uZXV0cmFsLCBQdWJsaWNLZXlUb2tlbj1iNzdhNWM1NjE5MzRlMDg5MAFUDglQdWJsaWNLZXki
MHgwMDAwMDAwMDAwMDAwMDAwMDQwMDAwMDAwMDAwMDAwMA==");
			var attributes = new CecilLoader().ReadSecurityDeclaration(new SecurityDeclaration(SecurityAction.LinkDemand, blob));
			Assert.AreEqual(2, attributes.Count);
			var compilation = new SimpleCompilation(CecilLoaderTests.Mscorlib);
			var context = new SimpleTypeResolveContext(compilation.MainAssembly);
			var permissionAttr = attributes[0].CreateResolvedAttribute(context);
			var strongNameAttr = attributes[1].CreateResolvedAttribute(context);
			Assert.AreEqual("System.Security.Permissions.SecurityPermissionAttribute", permissionAttr.AttributeType.FullName);
			Assert.AreEqual("System.Security.Permissions.StrongNameIdentityPermissionAttribute", strongNameAttr.AttributeType.FullName);
		}
	}
	*/
}
