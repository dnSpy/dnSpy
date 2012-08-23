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
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using ICSharpCode.NRefactory.CSharp;
using ICSharpCode.NRefactory.CSharp.Resolver;
using ICSharpCode.NRefactory.Semantics;
using ICSharpCode.NRefactory.TypeSystem;

namespace ICSharpCode.NRefactory.ConsistencyCheck
{
	public class FindReferencesConsistencyCheck
	{
		readonly Solution solution;
		Dictionary<IEntity, HashSet<AstNode>> referenceDict = new Dictionary<IEntity, HashSet<AstNode>>();
		
		public FindReferencesConsistencyCheck(Solution solution)
		{
			this.solution = solution;
		}
		
		public void Run()
		{
			using (new Timer("Finding referenced entities... ")) {
				foreach (var file in solution.AllFiles) {
					var navigator = new FindReferencedEntities(
						delegate (AstNode node, IEntity entity) {
							if (node == null)
								throw new ArgumentNullException("node");
							if (entity == null)
								throw new ArgumentNullException("entity");
							
							if (!IgnoreEntity(entity)) {
								HashSet<AstNode> list;
								if (!referenceDict.TryGetValue(entity, out list)) {
									list = new HashSet<AstNode>();
									referenceDict.Add(entity, list);
								}
								list.Add(node);
							}
						}
					);
					file.CreateResolver().ApplyNavigator(navigator);
				}
			}
			Console.WriteLine("For each entity, find all references...");
			Stopwatch w = Stopwatch.StartNew();
			foreach (var project in solution.Projects) {
				foreach (var type in project.Compilation.MainAssembly.GetAllTypeDefinitions()) {
					TestFindReferences(type);
					foreach (IMember m in type.Members) {
						TestFindReferences(m);
					}
					Console.Write('.');
				}
			}
			w.Stop();
			Console.WriteLine("FindReferencesConsistencyCheck is done ({0}).", w.Elapsed);
			PrintTimingsPerEntityType();
		}
		
		bool IgnoreEntity(IEntity entity)
		{
			return false;
			//return entity.FullName != "ICSharpCode.NRefactory.TypeSystem.Implementation.DefaultResolvedTypeDefinition.parts";
		}
		
		Dictionary<EntityType, TimeSpan> timings = new Dictionary<EntityType, TimeSpan>();
		Dictionary<EntityType, int> entityCount = new Dictionary<EntityType, int>();
		
		void TestFindReferences(IEntity entity)
		{
			if (IgnoreEntity(entity))
				return;
			FindReferences fr = new FindReferences();
			fr.FindTypeReferencesEvenIfAliased = true;
			
			Stopwatch w = new Stopwatch();
			var searchScopes = fr.GetSearchScopes(entity);
			foreach (var project in solution.Projects) {
				w.Restart();
				HashSet<AstNode> foundReferences = new HashSet<AstNode>();
				var interestingFiles = new HashSet<CSharpFile>();
				foreach (var searchScope in searchScopes) {
					foreach (var unresolvedFile in fr.GetInterestingFiles(searchScope, project.Compilation)) {
						var file = project.Files.Single(f => f.FileName == unresolvedFile.FileName);
						Debug.Assert(file.UnresolvedTypeSystemForFile == unresolvedFile);
						
						// Skip file if it doesn't contain the search term
						if (searchScope.SearchTerm != null && file.OriginalText.IndexOf(searchScope.SearchTerm, StringComparison.Ordinal) < 0)
							continue;
						
						interestingFiles.Add(file);
					}
				}
				foreach (var file in interestingFiles) {
					fr.FindReferencesInFile(searchScopes, file.UnresolvedTypeSystemForFile, file.SyntaxTree, project.Compilation,
					                        delegate(AstNode node, ResolveResult result) {
					                        	foundReferences.Add(node);
					                        }, CancellationToken.None);
				}
				w.Stop();
				if (timings.ContainsKey(entity.EntityType)) {
					timings[entity.EntityType] += w.Elapsed;
				} else {
					timings[entity.EntityType] = w.Elapsed;
				}
				
				
				IEntity importedEntity = project.Compilation.Import(entity);
				
				HashSet<AstNode> expectedReferences;
				if (importedEntity == null || !referenceDict.TryGetValue(importedEntity, out expectedReferences)) {
					if (foundReferences.Any()) {
						// There aren't any expected references stored, but we found some references anyways:
						Console.WriteLine();
						Console.WriteLine("Entity not in reference dictionary: " + entity);
					}
					return;
				}
				if (foundReferences.Except(expectedReferences).Any()) {
					Console.WriteLine();
					Console.WriteLine("Reference mismatch for " + entity + ":");
					var n = foundReferences.Except(expectedReferences).First();
					Console.WriteLine("Found unexpected reference " + n + " (" + n.GetRegion() + ")");
				}
				if (expectedReferences.Except(foundReferences).Any()) {
					Console.WriteLine();
					Console.WriteLine("Reference mismatch for " + entity + ":");
					var n = expectedReferences.Except(foundReferences).First();
					Console.WriteLine("Did not find expected reference " + n + " (" + n.GetRegion() + ")");
				}
			}
			
			if (entityCount.ContainsKey(entity.EntityType)) {
				entityCount[entity.EntityType]++;
			} else {
				entityCount[entity.EntityType] = 1;
			}
		}
		
		void PrintTimingsPerEntityType()
		{
			foreach (var pair in entityCount) {
				Console.WriteLine("{0} - avg. {1} per entity", pair.Key, TimeSpan.FromSeconds(timings[pair.Key].TotalSeconds / pair.Value));
			}
		}
	}
}
