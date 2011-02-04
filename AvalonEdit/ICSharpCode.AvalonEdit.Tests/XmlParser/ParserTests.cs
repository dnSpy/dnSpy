// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using System.IO;
using System.Text;

using ICSharpCode.AvalonEdit.Xml;
using ICSharpCode.SharpZipLib.Zip;
using NUnit.Framework;

namespace ICSharpCode.AvalonEdit.Xml
{
	class TestFile 
	{
		public string Name { get; set; }
		public string Content { get; set; }
		public string Canonical { get; set; }
		public string Description { get; set; }
	}
	
	[TestFixture]
	public class ParserTests
	{
		readonly string zipFileName = @"XmlParser\W3C.zip";
		
		List<TestFile> xmlFiles = new List<TestFile>();
		
		[TestFixtureSetUp]
		public void OpenZipFile()
		{
			ZipFile zipFile = new ZipFile(zipFileName);
			
			Dictionary<string, TestFile> xmlFiles = new Dictionary<string, TestFile>();
			
			// Decompress XML files
			foreach(ZipEntry zipEntry in zipFile.Cast<ZipEntry>().Where(zip => zip.IsFile && zip.Name.EndsWith(".xml"))) {
				Stream stream = zipFile.GetInputStream(zipEntry);
				string content = new StreamReader(stream).ReadToEnd();
				xmlFiles.Add(zipEntry.Name, new TestFile { Name = zipEntry.Name, Content = content });
			}
			// Add descriptions
			foreach(TestFile metaData in xmlFiles.Values.Where(f => f.Name.StartsWith("ibm/ibm_oasis"))) {
				var doc = System.Xml.Linq.XDocument.Parse(metaData.Content);
				foreach(var testElem in doc.Descendants("TEST")) {
					string uri = "ibm/" + testElem.Attribute("URI").Value;
					string description = testElem.Value.Replace("\n    ", "\n").TrimStart('\n');
					if (xmlFiles.ContainsKey(uri))
						xmlFiles[uri].Description = description;
				}
			}
			// Copy canonical forms
			foreach(TestFile canonical in xmlFiles.Values.Where(f => f.Name.Contains("/out/"))) {
				string uri = canonical.Name.Replace("/out/", "/");
				if (xmlFiles.ContainsKey(uri))
					xmlFiles[uri].Canonical = canonical.Content;
			}
			// Copy resuts to field
			this.xmlFiles.AddRange(xmlFiles.Values.Where(f => !f.Name.Contains("/out/")));
		}
		
		IEnumerable<TestFile> GetXmlFilesStartingWith(string directory)
		{
			return xmlFiles.Where(f => f.Name.StartsWith(directory));
		}
		
		[Test]
		public void W3C_Valid()
		{
			string[] exclude = {
				// NAME in DTD infoset
				"ibm02v01", "ibm03v01", "ibm85v01", "ibm86v01", "ibm87v01", "ibm88v01", "ibm89v01",
			};
			TestFiles(GetXmlFilesStartingWith("ibm/valid/"), true, exclude);
		}
		
		[Test]
		public void W3C_Invalid()
		{
			string[] exclude = {
				// Default attribute value
				"ibm56i03",
			};
			TestFiles(GetXmlFilesStartingWith("ibm/invalid/"), true, exclude);
		}
		
		[Test]
		public void W3C_NotWellformed()
		{
			string[] exclude = {
				// XML declaration well formed
				"ibm23n", "ibm24n", "ibm26n01", "ibm32n", "ibm80n06", "ibm81n01", "ibm81n02", "ibm81n03", "ibm81n04", "ibm81n05", "ibm81n06", "ibm81n07", "ibm81n08", "ibm81n09",
				// Invalid chars in a comment - do we care?
				"ibm02n",
				// Invalid char ref - do we care?
				"ibm66n12", "ibm66n13", "ibm66n14", "ibm66n15",
				// DTD in wrong location
				"ibm27n01", "ibm43n",
				// Entity refs depending on DTD
				"ibm41n10", "ibm41n11", "ibm41n12", "ibm41n13", "ibm41n14", "ibm68n04", "ibm68n06", "ibm68n07", "ibm68n08", "ibm68n09", "ibm68n10",
				// DTD Related tests
				"ibm09n01", "ibm09n02", "ibm13n01", "ibm13n02", "ibm13n03", "ibm28n01", "ibm28n02", "ibm28n03", "ibm29n01", "ibm29n03", "ibm29n04", "ibm29n07", "ibm30n01", "ibm31n01", "ibm45n01", "ibm45n02", "ibm45n03", "ibm45n04", "ibm45n05", "ibm45n06", "ibm46n01", "ibm46n02", "ibm46n03", "ibm46n04",
				"ibm46n05", "ibm47n01", "ibm47n02", "ibm47n03", "ibm47n04", "ibm47n05", "ibm47n06", "ibm48n01", "ibm48n02", "ibm48n03", "ibm48n04", "ibm48n05", "ibm48n06", "ibm48n07", "ibm49n01", "ibm49n02", "ibm49n03", "ibm49n04", "ibm49n05", "ibm49n06", "ibm50n01", "ibm50n02", "ibm50n03", "ibm50n04",
				"ibm50n05", "ibm50n06", "ibm50n07", "ibm51n01", "ibm51n02", "ibm51n03", "ibm51n04", "ibm51n05", "ibm51n06", "ibm51n07", "ibm52n01", "ibm52n02", "ibm52n03", "ibm53n01", "ibm53n02", "ibm53n03", "ibm53n04", "ibm53n05", "ibm53n06", "ibm53n07", "ibm53n08", "ibm54n01", "ibm54n02", "ibm55n01",
				"ibm55n02", "ibm55n03", "ibm56n01", "ibm56n02", "ibm56n03", "ibm56n04", "ibm56n05", "ibm56n06", "ibm56n07", "ibm57n01", "ibm58n01", "ibm58n02", "ibm58n03", "ibm58n04", "ibm58n05", "ibm58n06", "ibm58n07", "ibm58n08", "ibm59n01", "ibm59n02", "ibm59n03", "ibm59n04", "ibm59n05", "ibm59n06",
				"ibm60n01", "ibm60n02", "ibm60n03", "ibm60n04", "ibm60n05", "ibm60n06", "ibm60n07", "ibm60n08", "ibm61n01", "ibm62n01", "ibm62n02", "ibm62n03", "ibm62n04", "ibm62n05", "ibm62n06", "ibm62n07", "ibm62n08", "ibm63n01", "ibm63n02", "ibm63n03", "ibm63n04", "ibm63n05", "ibm63n06", "ibm63n07",
				"ibm64n01", "ibm64n02", "ibm64n03", "ibm65n01", "ibm65n02", "ibm66n01", "ibm66n03", "ibm66n05", "ibm66n07", "ibm66n09", "ibm66n11", "ibm69n01", "ibm69n02", "ibm69n03", "ibm69n04", "ibm69n05", "ibm69n06", "ibm69n07", "ibm70n01", "ibm71n01", "ibm71n02", "ibm71n03", "ibm71n04", "ibm71n05",
				"ibm72n01", "ibm72n02", "ibm72n03", "ibm72n04", "ibm72n05", "ibm72n06", "ibm72n09", "ibm73n01", "ibm73n03", "ibm74n01", "ibm75n01", "ibm75n02", "ibm75n03", "ibm75n04", "ibm75n05", "ibm75n06", "ibm75n07", "ibm75n08", "ibm75n09", "ibm75n10", "ibm75n11", "ibm75n12", "ibm75n13", "ibm76n01",
				"ibm76n02", "ibm76n03", "ibm76n04", "ibm76n05", "ibm76n06", "ibm76n07", "ibm77n01", "ibm77n02", "ibm77n03", "ibm77n04", "ibm78n01", "ibm78n02", "ibm79n01", "ibm79n02", "ibm82n01", "ibm82n02", "ibm82n03", "ibm82n04", "ibm82n08", "ibm83n01", "ibm83n03", "ibm83n04", "ibm83n05", "ibm83n06",
				// No idea what this is
				"misc/432gewf", "ibm28an01",
			};
			TestFiles(GetXmlFilesStartingWith("ibm/not-wf/"), false, exclude);
		}
		
		StringBuilder errorOutput;
		
		void TestFiles(IEnumerable<TestFile> files, bool areWellFormed, string[] exclude)
		{
			errorOutput = new StringBuilder();
			int testsRun = 0;
			int ignored = 0;
			foreach (TestFile file in files) {
				if (exclude.Any(exc => file.Name.Contains(exc))) {
					ignored++;
				} else {
					testsRun++;
					TestFile(file, areWellFormed);
				}
			}
			if (testsRun == 0) {
				Assert.Fail("Test files not found");
			}
			if (errorOutput.Length > 0) {
				// Can not output ]]> otherwise nuint will crash
				Assert.Fail(errorOutput.Replace("]]>", "]]~NUNIT~>").ToString());
			}
		}
		
		/// <remarks>
		/// If using DTD, canonical representation is not checked
		/// If using DTD, uknown entiry references are not error
		/// </remarks>
		bool TestFile(TestFile testFile, bool isWellFormed)
		{
			bool passed = true;
			
			string content = testFile.Content;
			Debug.WriteLine("Testing " + testFile.Name + "...");
			AXmlParser parser = new AXmlParser();
			
			bool usingDTD = content.Contains("<!DOCTYPE") && (content.Contains("<!ENTITY") || content.Contains(" SYSTEM "));
			if (usingDTD)
				parser.UnknownEntityReferenceIsError = false;
			
			AXmlDocument document;
			
			parser.Lock.EnterWriteLock();
			try {
				document = parser.Parse(content, null);
			} finally {
				parser.Lock.ExitWriteLock();
			}
			
			string printed = PrettyPrintAXmlVisitor.PrettyPrint(document);
			if (content != printed) {
				errorOutput.AppendFormat("Output of pretty printed XML for \"{0}\" does not match the original.\n", testFile.Name);
				errorOutput.AppendFormat("Pretty printed:\n{0}\n", Indent(printed));
				passed = false;
			}
			
			if (isWellFormed && !usingDTD) {
				string canonicalPrint = CanonicalPrintAXmlVisitor.Print(document);
				if (testFile.Canonical != null) {
					if (testFile.Canonical != canonicalPrint) {
						errorOutput.AppendFormat("Canonical XML for \"{0}\" does not match the excpected.\n", testFile.Name);
						errorOutput.AppendFormat("Expected:\n{0}\n", Indent(testFile.Canonical));
						errorOutput.AppendFormat("Seen:\n{0}\n", Indent(canonicalPrint));
						passed = false;
					}
				} else {
					errorOutput.AppendFormat("Can not find canonical output for \"{0}\"", testFile.Name);
					errorOutput.AppendFormat("Suggested canonical output:\n{0}\n", Indent(canonicalPrint));
					passed = false;
				}
			}
			
			bool hasErrors = document.SyntaxErrors.FirstOrDefault() != null;
			if (isWellFormed && hasErrors) {
				errorOutput.AppendFormat("Syntax error(s) in well formed file \"{0}\":\n", testFile.Name);
				foreach (var error in document.SyntaxErrors) {
					string followingText = content.Substring(error.StartOffset, Math.Min(10, content.Length - error.StartOffset));
					errorOutput.AppendFormat("Error ({0}-{1}): {2} (followed by \"{3}\")\n", error.StartOffset, error.EndOffset, error.Message, followingText);
				}
				passed = false;
			}
			
			if (!isWellFormed && !hasErrors) {
				errorOutput.AppendFormat("No syntax errors reported for mallformed file \"{0}\"\n", testFile.Name);
				passed = false;
			}
			
			// Epilog
			if (!passed) {
				if (testFile.Description != null) {
					errorOutput.AppendFormat("Test description:\n{0}\n", Indent(testFile.Description));
				}
				errorOutput.AppendFormat("File content:\n{0}\n", Indent(content));
				errorOutput.AppendLine();
			}
			
			return passed;
		}
		
		string Indent(string text)
		{
			return "  " + text.TrimEnd().Replace("\n", "\n  ");
		}
	}
}
