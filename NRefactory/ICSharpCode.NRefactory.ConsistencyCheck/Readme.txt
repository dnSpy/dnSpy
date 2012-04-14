This is an automatic consistency check for NRefactory.
It loads a solution file and parses all the source code and referenced libraries,
and then performs a set of consistency checks.
These checks assume that the code is valid C# without any compilation errors,
so make sure to only pass in compilable source code.

Checks currently being performed:
 - IDStringConsistencyCheck: Checks that ID strings are unique and refer back to the correct entity
 - RoundtripTest: parses C# code and outputs it again using CSharpOutputVisitor, checking that only whitespace is changing
 - ResolverTest: fully resolves all ASTs and validates that no errors are detected (no false positives in semantic error checking)
 - RandomizedOrderResolverTest: checks that the order of Resolve()/GetResolverState() calls has no effect on the result
 - FindReferencesConsistencyCheck: checks that FindReferences is the inverse of FindReferencedEntities
 
 XML Tests:
 - IncrementalXmlParserTests: tests that incremental parsing produces results identical to a full reparse
 - XmlReaderTests: compares AXmlParser.Parse().CreateReader() with new XmlTextReader()
 
 
 
 Ideas for further tests:
  - Test token positions (see AstVerifier)
  - Compare resolve results with csc compiler output (using Cecil)
  - Randomly mutate a C# file (e.g. remove tokens) and verify that the parser does not crash
