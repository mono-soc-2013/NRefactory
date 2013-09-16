// 
// NullValueAnalysis.cs
//  
// Author:
//       Luís Reis <luiscubal@gmail.com>
// 
// Copyright (c) 2013 Luís Reis
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Text;
using ICSharpCode.NRefactory.CSharp.Resolver;
using ICSharpCode.NRefactory.Semantics;
using ICSharpCode.NRefactory.TypeSystem;
using ICSharpCode.NRefactory.CSharp.Refactoring;
using ICSharpCode.NRefactory.PatternMatching;
using ICSharpCode.NRefactory.CSharp;
using ICSharpCode.NRefactory.Utils;

namespace ICSharpCode.NRefactory.CSharp.Analysis
{
	public class NullValueAnalysis
	{
		sealed class NullAnalysisNode : ControlFlowNode
		{
			public readonly VariableStatusInfo VariableState = new VariableStatusInfo();
			public bool Visited { get; private set; }

			public NullAnalysisNode(Statement previousStatement, Statement nextStatement, ControlFlowNodeType type)
				: base(previousStatement, nextStatement, type)
			{
			}

			public bool ReceiveIncoming(VariableStatusInfo incomingState)
			{
				bool changed = VariableState.ReceiveIncoming(incomingState);
				if (!Visited) {
					Visited = true;
					return true;
				}
				return changed;
			}
		}

		sealed class NullAnalysisGraphBuilder : ControlFlowGraphBuilder
		{
			protected override ControlFlowNode CreateNode(Statement previousStatement, Statement nextStatement, ControlFlowNodeType type)
			{
				return new NullAnalysisNode(previousStatement, nextStatement, type);
			}
		}

		class PendingNode : IEquatable<PendingNode> {
			internal readonly NullAnalysisNode nodeToVisit;
			internal ComparableList<VariableStatusInfo> options;
			internal readonly ComparableList<NullAnalysisNode> pendingTryFinallyNodes;
			internal readonly NullAnalysisNode nodeAfterFinally;

			internal PendingNode(NullAnalysisNode nodeToVisit, VariableStatusInfo statusInfo)
				: this(nodeToVisit, new ComparableList<VariableStatusInfo> { statusInfo }, new ComparableList<NullAnalysisNode>(), null)
			{
			}

			internal PendingNode(NullAnalysisNode nodeToVisit, VariableStatusInfo statusInfo, ComparableList<NullAnalysisNode> pendingFinallyNodes, NullAnalysisNode nodeAfterFinally)
			: this(nodeToVisit, new ComparableList<VariableStatusInfo> { statusInfo }, pendingFinallyNodes, nodeAfterFinally)
			{
			}

			public PendingNode(NullAnalysisNode nodeToVisit, ComparableList<VariableStatusInfo> options, ComparableList<NullAnalysisNode> pendingFinallyNodes, NullAnalysisNode nodeAfterFinally)
			{
				this.nodeToVisit = nodeToVisit;
				this.options = options;
				this.pendingTryFinallyNodes = pendingFinallyNodes;
				this.nodeAfterFinally = nodeAfterFinally;
			}

			public override bool Equals(object obj)
			{
				return Equals(obj as PendingNode);
			}

			public bool Equals(PendingNode obj) {
				if (obj == null) return false;

				if (nodeToVisit != obj.nodeToVisit) return false;
				if (options != obj.options) return false;
				if (pendingTryFinallyNodes != obj.pendingTryFinallyNodes) return false;
				if (nodeAfterFinally != obj.nodeAfterFinally) return false;

				return true;
			}

			public override int GetHashCode()
			{
				return nodeToVisit.GetHashCode() ^
					options.GetHashCode() ^
					pendingTryFinallyNodes.GetHashCode() ^
					(nodeAfterFinally == null ? 0 : nodeAfterFinally.GetHashCode());
			}
		}

		readonly BaseRefactoringContext context;
		readonly NullAnalysisVisitor visitor;
		List<NullAnalysisNode> allNodes;
		readonly HashSet<PendingNode> nodesToVisit = new HashSet<PendingNode>();
		Dictionary<Statement, NullAnalysisNode> nodeBeforeStatementDict;
		Dictionary<Statement, NullAnalysisNode> nodeAfterStatementDict;
		readonly Dictionary<Expression, NullValueStatus> expressionResult = new Dictionary<Expression, NullValueStatus>();

		public NullValueAnalysis(BaseRefactoringContext context, MethodDeclaration methodDeclaration, CancellationToken cancellationToken)
			: this(context, methodDeclaration.Body, methodDeclaration.Parameters, cancellationToken)
		{
		}

		readonly IEnumerable<ParameterDeclaration> parameters;
		readonly Statement rootStatement;

		readonly CancellationToken cancellationToken;

		public NullValueAnalysis(BaseRefactoringContext context, Statement rootStatement, IEnumerable<ParameterDeclaration> parameters, CancellationToken cancellationToken)
		{
			if (rootStatement == null)
				throw new ArgumentNullException("rootStatement");
			if (context == null)
				throw new ArgumentNullException("context");

			this.context = context;
			this.rootStatement = rootStatement;
			this.parameters = parameters;
			this.visitor = new NullAnalysisVisitor(this);
			this.cancellationToken = cancellationToken;
		}

		void SetupNode(NullAnalysisNode node)
		{
			foreach (var parameter in parameters) {
				var resolveResult = context.Resolve(parameter.Type);
				node.VariableState[parameter.Name] = GetInitialVariableStatus(resolveResult);
			}

			nodesToVisit.Add(new PendingNode(node, node.VariableState));
		}

		static bool IsTypeNullable(IType type)
		{
			return type.IsReferenceType == true || type.FullName == "System.Nullable";
		}

		public bool IsParametersAreUninitialized {
			get;
			set;
		}

		NullValueStatus GetInitialVariableStatus(ResolveResult resolveResult)
		{
			var typeResolveResult = resolveResult as TypeResolveResult;
			if (typeResolveResult == null) {
				return NullValueStatus.Error;
			}
			var type = typeResolveResult.Type;
			if (type.IsReferenceType == null) {
				return NullValueStatus.Error;
			}
			if (!IsParametersAreUninitialized)
				return NullValueStatus.DefinitelyNotNull;
			return IsTypeNullable(type) ? NullValueStatus.PotentiallyNull : NullValueStatus.DefinitelyNotNull;
		}

		public void Analyze()
		{
			var cfgBuilder = new NullAnalysisGraphBuilder();
			allNodes = cfgBuilder.BuildControlFlowGraph(rootStatement, cancellationToken).Cast<NullAnalysisNode>().ToList();
			nodeBeforeStatementDict = allNodes.Where(node => node.Type == ControlFlowNodeType.StartNode || node.Type == ControlFlowNodeType.BetweenStatements)
				.ToDictionary(node => node.NextStatement);
			nodeAfterStatementDict = allNodes.Where(node => node.Type == ControlFlowNodeType.BetweenStatements || node.Type == ControlFlowNodeType.EndNode)
				.ToDictionary(node => node.PreviousStatement);

			foreach (var node in allNodes) {
				if (node.Type == ControlFlowNodeType.StartNode && node.NextStatement == rootStatement) {
					Debug.Assert(!nodesToVisit.Any());

					SetupNode(node);
				}
			}

			while (nodesToVisit.Any()) {
				var nodeToVisit = nodesToVisit.First();
				nodesToVisit.Remove(nodeToVisit);

				Visit(nodeToVisit);
			}
		}

		int visits = 0;

		public int NodeVisits
		{
			get {
				return visits;
			}
		}

		void Visit(PendingNode nodeInfo)
		{
			cancellationToken.ThrowIfCancellationRequested();

			var node = nodeInfo.nodeToVisit;
			var options = nodeInfo.options;

			visits++;
			if (visits > 100) {
				//Visiting way too often, let's enter fast mode
				//Fast mode is slighly less accurate but visits each node less times
				nodesToVisit.RemoveWhere(candidate => candidate.nodeToVisit == nodeInfo.nodeToVisit &&
				                         candidate.pendingTryFinallyNodes.Equals(nodeInfo.pendingTryFinallyNodes) &&
				                         candidate.nodeAfterFinally == nodeInfo.nodeAfterFinally);
				options = new ComparableList<VariableStatusInfo> { node.VariableState };
			}

			VisitorState result = VisitorState.ForVariables(options);

			var nextStatement = node.NextStatement;
			if (nextStatement != null && (!(nextStatement is DoWhileStatement || nextStatement is ForStatement) || node.Type == ControlFlowNodeType.LoopCondition)) {
				result = nextStatement.AcceptVisitor(visitor, result);
				if (result == null) {
					Console.WriteLine("Failure in {0}", nextStatement);
					throw new InvalidOperationException();
				}
			}

			if (!result.AlwaysThrows && node.Outgoing.Any()) {
				result = result.NoExceptionOptions;

				var tryFinallyStatement = nextStatement as TryCatchStatement;

				foreach (var outgoingEdge in node.Outgoing) {
					var edgeInfo = result.Clone();

					if (node.Type == ControlFlowNodeType.EndNode) {
						var previousBlock = node.PreviousStatement as BlockStatement;
						if (previousBlock != null) {
							//We're leaving a block statement.
							//As such, we'll remove the variables that were declared *in* the loop
							//This helps GetVariableStatusAfter/BeforeStatement be more accurate
							//and prevents some redundant revisiting.
							
							foreach (var variableInitializer in previousBlock.Statements
							.OfType<VariableDeclarationStatement>()
							.SelectMany(declaration => declaration.Variables)) {
								foreach (var option in edgeInfo.Options) {
									option.Variables [variableInitializer.Name] = NullValueStatus.UnreachableOrInexistent;
								}
							}
						}
					}

					if (tryFinallyStatement != null) {
						//With the exception of try statements, this needs special handling:
						//we'll set all changed variables to Unknown or CapturedUnknown
						if (outgoingEdge.To.NextStatement == tryFinallyStatement.FinallyBlock) {
							foreach (var identifierExpression in tryFinallyStatement.TryBlock.Descendants.OfType<IdentifierExpression>()) {
								//TODO: Investigate CaptureUnknown
								SetLocalVariableValue(edgeInfo, identifierExpression, NullValueStatus.Unknown);
							}
						} else {
							var clause = tryFinallyStatement.CatchClauses
							.FirstOrDefault(candidateClause => candidateClause.Body == outgoingEdge.To.NextStatement);
							
							if (clause != null) {
								SetLocalVariableValue(edgeInfo, clause.VariableNameToken, NullValueStatus.DefinitelyNotNull);
								
								foreach (var identifierExpression in tryFinallyStatement.TryBlock.Descendants.OfType<IdentifierExpression>()) {
									//TODO: Investigate CaptureUnknown
									SetLocalVariableValue(edgeInfo, identifierExpression, NullValueStatus.Unknown);
								}
							}
						}
					}

					switch (outgoingEdge.Type) {
						case ControlFlowEdgeType.ConditionTrue:
							edgeInfo = edgeInfo.TruePathVisitorResult;

							break;
						case ControlFlowEdgeType.ConditionFalse:
							edgeInfo = edgeInfo.FalsePathVisitorResult;

							break;
					}

					if (outgoingEdge.IsLeavingTryFinally) {
						var nodeAfterFinally = (NullAnalysisNode)outgoingEdge.To;
						var finallyNodes = outgoingEdge.TryFinallyStatements.Select(tryFinally => nodeBeforeStatementDict [tryFinally.FinallyBlock]).ToList();
						var nextNode = finallyNodes.First();
						var remainingFinallyNodes = new ComparableList<NullAnalysisNode>(finallyNodes.Skip(1));

						//We have to visit the node even if ReceiveIncoming returns false
						//since the finallyNodes/nodeAfterFinally might be different even if the values of variables are the same -- and they need to be visited either way!
						//TODO 1: Is there any point in visiting the finally statement here?
						//TODO 2: Do we need the ReceiveIncoming at all?

						foreach (var option in edgeInfo.Options) {
							nextNode.ReceiveIncoming(option.Variables);
							nodesToVisit.Add(new PendingNode(nextNode, option.Variables, remainingFinallyNodes, nodeAfterFinally));
						}
					} else {
						foreach (var option in edgeInfo.Options) {
							var outgoingNode = (NullAnalysisNode)outgoingEdge.To;
							if (outgoingNode.ReceiveIncoming(option.Variables)) {
								nodesToVisit.Add(new PendingNode(outgoingNode, option.Variables));
							}
						}
					}
				}
			} else {
				//Found termination node
				var finallyBlockStarts = nodeInfo.pendingTryFinallyNodes;
				var nodeAfterFinally = nodeInfo.nodeAfterFinally;

				if (finallyBlockStarts.Any()) {
					var nextNode = finallyBlockStarts.First();

					foreach (var option in result.Options) {
						if (nextNode.ReceiveIncoming(option.Variables))
							nodesToVisit.Add(new PendingNode(nextNode, option.Variables, new ComparableList<NullAnalysisNode>(finallyBlockStarts.Skip(1)), nodeInfo.nodeAfterFinally));
					}

					return;
				}

				foreach (var option in result.Options) {
					if (nodeAfterFinally != null) {
						if (nodeAfterFinally.ReceiveIncoming(option.Variables)) {
							nodesToVisit.Add(new PendingNode(nodeAfterFinally, option.Variables));
							continue;
						}
					}

					var statement = node.PreviousStatement ?? node.NextStatement;
					Debug.Assert(statement != null);

					var parent = statement.GetParent<Statement>();
					var parentTryCatch = parent as TryCatchStatement;
					if (parentTryCatch != null) {
						var nextNode = nodeAfterStatementDict [parentTryCatch];
						if (nextNode.ReceiveIncoming(option.Variables)) {
							nodesToVisit.Add(new PendingNode(nextNode, option.Variables));
						}
					}
				}
			}
		}

		public NullValueStatus GetExpressionResult(Expression expr)
		{
			if (expr == null)
				throw new ArgumentNullException("expr");

			NullValueStatus info;
			if (expressionResult.TryGetValue(expr, out info)) {
				return info;
			}

			return NullValueStatus.UnreachableOrInexistent;
		}

		public NullValueStatus GetVariableStatusBeforeStatement(Statement stmt, string variableName)
		{
			if (stmt == null)
				throw new ArgumentNullException("stmt");
			if (variableName == null)
				throw new ArgumentNullException("variableName");

			NullAnalysisNode node;
			if (nodeBeforeStatementDict.TryGetValue(stmt, out node)) {
				return node.VariableState [variableName];
			}

			return NullValueStatus.UnreachableOrInexistent;
		}

		public NullValueStatus GetVariableStatusAfterStatement(Statement stmt, string variableName)
		{
			if (stmt == null)
				throw new ArgumentNullException("stmt");
			if (variableName == null)
				throw new ArgumentNullException("variableName");

			NullAnalysisNode node;
			if (nodeAfterStatementDict.TryGetValue(stmt, out node)) {
				return node.VariableState [variableName];
			}

			return NullValueStatus.UnreachableOrInexistent;
		}

		/// <summary>
		/// Sets the local variable value.
		/// This method does not change anything if identifier does not refer to a local variable.
		/// Do not use this in variable declarations since resolving the variable won't work yet.
		/// </summary>
		/// <returns><c>true</c>, if local variable value was set, <c>false</c> otherwise.</returns>
		/// <param name="data">The variable status data to change.</param>
		/// <param name="identifierNode">The identifier to set.</param>
		/// <param name="identifierName">The name of the identifier to set.</param>
		/// <param name="value">The value to set the identifier.</param>
		bool SetLocalVariableValue (VariableStatusInfo data, AstNode identifierNode, string identifierName, NullValueStatus value) {
			var resolveResult = context.Resolve(identifierNode);
			if (resolveResult is LocalResolveResult) {
				if (data [identifierName] != NullValueStatus.CapturedUnknown) {
					data [identifierName] = value;
					
					return true;
				}
			}
			return false;
		}
		
		bool SetLocalVariableValue (VariableStatusInfo data, IdentifierExpression identifierExpression, NullValueStatus value) {
			return SetLocalVariableValue(data, identifierExpression, identifierExpression.Identifier, value);
		}
		
		bool SetLocalVariableValue (VariableStatusInfo data, Identifier identifier, NullValueStatus value) {
			return SetLocalVariableValue(data, identifier, identifier.Name, value);
		}
		
		void SetLocalVariableValue (VisitorState variables, Identifier identifier, NullValueStatus value) {
			foreach (var option in variables.Options) {
				SetLocalVariableValue(option.Variables, identifier, value);
			}
		}

		void SetLocalVariableValue (VisitorState variables, IdentifierExpression identifier, NullValueStatus value) {
			foreach (var option in variables.Options) {
				SetLocalVariableValue(option.Variables, identifier, value);
			}
		}

		class ResultPossibility
		{
			/// <summary>
			/// Indicates the return value of the expression.
			/// </summary>
			/// <remarks>
			/// Only applicable for expressions.
			/// </remarks>
			public NullValueStatus NullableReturnResult;
			
			/// <summary>
			/// Indicates the value of each item in an array or linq query.
			/// </summary>
			public NullValueStatus EnumeratedValueResult;

			/// <summary>
			/// The state of the variables after the expression is executed.
			/// </summary>
			public VariableStatusInfo Variables;

			/// <summary>
			/// The expression is known to be invalid and trigger an error
			/// (e.g. a NullReferenceException)
			/// </summary>
			public bool ThrowsException;

			/// <summary>
			/// The known bool result of an expression.
			/// </summary>
			public bool? KnownBoolResult;

			public ResultPossibility Negated {
				get {
					var resultPossibility = Clone();

					resultPossibility.KnownBoolResult = !resultPossibility.KnownBoolResult;

					return resultPossibility;
				}
			}

			public ResultPossibility Clone() {
				var resultPossibility = new ResultPossibility();

				resultPossibility.EnumeratedValueResult = EnumeratedValueResult;
				resultPossibility.KnownBoolResult = KnownBoolResult;
				resultPossibility.NullableReturnResult = NullableReturnResult;
				resultPossibility.ThrowsException = ThrowsException;
				resultPossibility.Variables = Variables.Clone();

				return resultPossibility;
			}

			public ResultPossibility WithResult(bool value)
			{
				var possibility = Clone();
				possibility.NullableReturnResult = NullValueStatus.DefinitelyNotNull;
				possibility.KnownBoolResult = value;
				return possibility;
			}

			public ResultPossibility WithResult(NullValueStatus value)
			{
				var possibility = Clone();
				possibility.NullableReturnResult = value;
				possibility.KnownBoolResult = null;
				possibility.EnumeratedValueResult = NullValueStatus.Unknown;
				return possibility;
			}

			public static ResultPossibility ForReturnValue(VariableStatusInfo variables, NullValueStatus returnValue)
			{
				var possibility = new ResultPossibility();
				possibility.Variables = variables.Clone();
				possibility.NullableReturnResult = returnValue;
				return possibility;
			}

			public static ResultPossibility ForEnumeratedValue(VariableStatusInfo variables, NullValueStatus enumeratedResult)
			{
				var possibility = new ResultPossibility();
				possibility.Variables = variables.Clone();
				possibility.NullableReturnResult = NullValueStatus.DefinitelyNotNull;
				possibility.EnumeratedValueResult = enumeratedResult;
				return possibility;
			}

			public List<ResultPossibility> ContinueEnumerable(VisitorState result)
			{
				List<ResultPossibility> possibilities = new List<ResultPossibility>();
				foreach (var option in result.Options) {
					var newOption = option.Clone();
					newOption.EnumeratedValueResult = VariableStatusInfo.CombineStatus(EnumeratedValueResult, option.NullableReturnResult);
					possibilities.Add(newOption);
				}
				return possibilities;
			}

			public static ResultPossibility ForVariables(VariableStatusInfo variables)
			{
				var option = new ResultPossibility();
				option.Variables = variables.Clone();
				return option;
			}
		}

		class VisitorState
		{
			public List<ResultPossibility> Options = new List<ResultPossibility>();

			public static VisitorState ForVariables(VariableStatusInfo variables)
			{
				return ForSingleOption(ResultPossibility.ForVariables(variables));
			}

			public static VisitorState ForSingleOption(ResultPossibility possitibility) {
				var result = new VisitorState();
				result.Options.Add(possitibility);
				return result;
			}

			public static VisitorState ForVariables(IEnumerable<VariableStatusInfo> options)
			{
				var result = new VisitorState();
				result.Options.AddRange(options.Select(ResultPossibility.ForVariables));
				return result;
			}

			public static VisitorState ForEnumeratedValue(VariableStatusInfo variables, NullValueStatus itemValues)
			{
				var option = ResultPossibility.ForEnumeratedValue(variables, itemValues);
				return VisitorState.ForSingleOption(option);
			}

			public static VisitorState ForValue(VariableStatusInfo variables, NullValueStatus returnValue)
			{
				var option = new ResultPossibility();
				option.NullableReturnResult = returnValue;
				option.Variables = variables.Clone();
				return VisitorState.ForSingleOption(option);
			}

			public static VisitorState ForBoolValue(VariableStatusInfo variables, bool newValue)
			{
				var option = new ResultPossibility();
				option.NullableReturnResult = NullValueStatus.DefinitelyNotNull; //Bool expressions are never null
				option.KnownBoolResult = newValue;
				option.Variables = variables.Clone();
				return VisitorState.ForSingleOption(option);
			}

			public static VisitorState ForException(VariableStatusInfo variables) {
				var option = new ResultPossibility();
				option.NullableReturnResult = NullValueStatus.UnreachableOrInexistent;
				option.ThrowsException = true;
				option.Variables = variables.Clone();
				return VisitorState.ForSingleOption(option);
			}

			public VisitorState Negated {
				get {
					var result = new VisitorState();
					foreach (var option in Options) {
						result.Options.Add(option.Negated);
					}
					return result;
				}
			}

			public VisitorState TruePathVisitorResult {
				get {
					var visitorResult = new VisitorState();
					foreach (var option in Options) {
						if (option.KnownBoolResult != false) {
							var newOption = option.Clone();
							newOption.KnownBoolResult = true;
							visitorResult.Options.Add(newOption);
						}
					}
					return visitorResult;
				}
			}

			public VisitorState FalsePathVisitorResult {
				get {
					var visitorResult = new VisitorState();
					foreach (var option in Options) {
						if (option.KnownBoolResult != true) {
							var newOption = option.Clone();
							newOption.KnownBoolResult = false;
							visitorResult.Options.Add(newOption);
						}
					}
					return visitorResult;
				}
			}

			public static VisitorState AndOperation(VisitorState tentativeLeftResult, VisitorState tentativeRightResult)
			{
				var result = new VisitorState();
				
				result.Options.AddRange(tentativeLeftResult.FalsePathVisitorResult.Options);
				result.Options.AddRange(tentativeRightResult.FalsePathVisitorResult.Options);
				result.Options.AddRange(tentativeRightResult.TruePathVisitorResult.Options);
				
				return result;
			}

			public static VisitorState OrOperation(VisitorState tentativeLeftResult, VisitorState tentativeRightResult)
			{
				var result = new VisitorState();

				result.Options.AddRange(tentativeLeftResult.TruePathVisitorResult.Options);
				result.Options.AddRange(tentativeRightResult.TruePathVisitorResult.Options);
				result.Options.AddRange(tentativeRightResult.FalsePathVisitorResult.Options);

				return result;
			}

			public static VisitorState CombineOptions(params VisitorState[] results) {
				return CombineOptions((IEnumerable<VisitorState>)results);
			}

			public static VisitorState CombineOptions(IEnumerable<VisitorState> results) {
				results = results.Where(result => result != null);

				var newResult = new VisitorState();
				newResult.Options.AddRange(results.SelectMany(result => result.Options));
				return newResult;
			}

			public bool AlwaysThrows {
				get {
					return Options.All(option => option.ThrowsException);
				}
			}

			public VisitorState NoExceptionOptions {
				get {
					var result = new VisitorState();
					result.Options.AddRange(Options.Where(option => !option.ThrowsException));
					return result;
				}
			}

			public VisitorState Clone() {
				var visitorResult = new VisitorState();
				visitorResult.Options.AddRange(Options.Select(option => option.Clone()));
				return visitorResult;
			}

			public VisitorState WithVariablePossibilities(string name, VisitorState result)
			{
				var visitorResult = new VisitorState();

				foreach (var option in Options) {
					foreach (var valueOption in result.Options) {
						var newOption = option.Clone();
						newOption.Variables [name] = valueOption.NullableReturnResult;

						visitorResult.Options.Add(newOption);
					}
				}

				return visitorResult;
			}

			public VisitorState WithVariable(string name, NullValueStatus value, bool overrideCapture)
			{
				var visitorResult = new VisitorState();
				
				foreach (var option in Options) {
					var newOption = option.Clone();
					if (overrideCapture || option.Variables [name] != NullValueStatus.CapturedUnknown) {
						newOption.Variables [name] = value;
					}
					
					visitorResult.Options.Add(newOption);
				}
				
				return visitorResult;
			}

			public VisitorState WithVariableFromResult(string newVariable)
			{
				var visitorResult = Clone();
				foreach (var option in visitorResult.Options) {
					option.Variables [newVariable] = option.NullableReturnResult;
				}
				return visitorResult;
			}

			public VisitorState WithNullResult()
			{
				var visitorResult = new VisitorState();
				
				foreach (var option in Options) {
					if (option.NullableReturnResult == NullValueStatus.DefinitelyNotNull)
						continue;
					
					visitorResult.Options.Add(option.WithResult(NullValueStatus.DefinitelyNull));
				}
				
				return visitorResult;
			}

			public VisitorState WithNotNullResult() {
				var visitorResult = new VisitorState();

				foreach (var option in Options) {
					if (option.NullableReturnResult == NullValueStatus.DefinitelyNull)
						continue;

					visitorResult.Options.Add(option.WithResult(NullValueStatus.DefinitelyNotNull));
				}

				return visitorResult;
			}

			public VisitorState WithVariableNotNull(string name)
			{
				var visitorResult = new VisitorState();
				
				foreach (var option in Options) {
					if (option.NullableReturnResult == NullValueStatus.DefinitelyNull)
						continue;

					var newOption = option.Clone();
					newOption.Variables [name] = newOption.Variables [name] == NullValueStatus.CapturedUnknown ? NullValueStatus.CapturedUnknown : NullValueStatus.DefinitelyNotNull;
					visitorResult.Options.Add(newOption);
				}

				return visitorResult;
			}

			public VisitorState Returning(NullValueStatus value)
			{
				var visitorResult = new VisitorState();

				visitorResult.Options.AddRange(Options.Select(option => option.WithResult(value)));

				return visitorResult;
			}

			public VisitorState Returning(bool value)
			{
				var visitorResult = new VisitorState();

				visitorResult.Options.AddRange(Options.Select(option => option.WithResult(value)));

				return visitorResult;
			}

			public VisitorState ReturningVariableValue(string name)
			{
				var visitorResult = new VisitorState();
				foreach (var option in Options) {
					var newOption = option.Clone();
					
					newOption.NullableReturnResult = newOption.Variables[name];
					if (newOption.NullableReturnResult == NullValueStatus.CapturedUnknown)
						newOption.NullableReturnResult = NullValueStatus.Unknown;
					visitorResult.Options.Add(newOption);
				}

				return visitorResult;
			}

			public VisitorState ReturningEnumerable(NullValueStatus value)
			{
				var visitorResult = new VisitorState();
				foreach (var option in Options) {
					var newOption = option.Clone();
					
					newOption.NullableReturnResult = NullValueStatus.DefinitelyNotNull;
					newOption.EnumeratedValueResult = value;
					visitorResult.Options.Add(newOption);
				}
				
				return visitorResult;
			}

			public VisitorState ToEnumeration()
			{
				var visitorResult = new VisitorState();
				foreach (var option in Options) {
					var newOption = option.Clone();
					newOption.EnumeratedValueResult = newOption.NullableReturnResult;
					newOption.NullableReturnResult = NullValueStatus.DefinitelyNotNull;
					visitorResult.Options.Add(newOption);
				}
				return visitorResult;
			}

			public VisitorState Enumerate()
			{
				var visitorResult = new VisitorState();
				foreach (var option in Options) {
					var newOption = option.Clone();
					newOption.EnumeratedValueResult = NullValueStatus.Unknown;
					newOption.NullableReturnResult = newOption.EnumeratedValueResult;
					visitorResult.Options.Add(newOption);
				}
				return visitorResult;
			}

			public bool MayReturnNull()
			{
				return Options.Any(option => option.NullableReturnResult != NullValueStatus.DefinitelyNotNull);
			}
		}

		class NullAnalysisVisitor : DepthFirstAstVisitor<VisitorState, VisitorState>
		{
			NullValueAnalysis analysis;

			public NullAnalysisVisitor(NullValueAnalysis analysis) {
				this.analysis = analysis;
			}

			VisitorState WithLocalVariableNotNull (VisitorState data, AstNode identifier, string identifierName) {
				var resolveResult = analysis.context.Resolve(identifier);
				if (resolveResult is LocalResolveResult) {
					return data.WithVariableNotNull(identifierName);
				}

				return data.Clone();
			}

			VisitorState WithLocalVariableNotNull (VisitorState data, Identifier identifier) {
				return WithLocalVariableNotNull(data, identifier, identifier.Name);
			}

			VisitorState WithLocalVariableNotNull (VisitorState data, IdentifierExpression identifier) {
				return WithLocalVariableNotNull(data, identifier, identifier.Identifier);
			}

			public override VisitorState VisitEmptyStatement(EmptyStatement emptyStatement, VisitorState data)
			{
				return data;
			}

			public override VisitorState VisitBlockStatement(BlockStatement blockStatement, VisitorState data)
			{
				//We'll visit the child statements later (we'll visit each one directly from the CFG)
				//As such this is mostly a dummy node.
				return data;
			}

			public override VisitorState VisitVariableDeclarationStatement(VariableDeclarationStatement variableDeclarationStatement, VisitorState data)
			{
				foreach (var variable in variableDeclarationStatement.Variables) {
					var result = variable.AcceptVisitor(this, data);
					if (result.AlwaysThrows)
						return result;
					data = result.NoExceptionOptions;
				}

				return data;
			}

			public override VisitorState VisitVariableInitializer(VariableInitializer variableInitializer, VisitorState data)
			{
				if (variableInitializer.Initializer.IsNull) {
					data = data.WithVariable(variableInitializer.Name, NullValueStatus.Unassigned, true);
				} else {
					var result = variableInitializer.Initializer.AcceptVisitor(this, data);
					if (result.AlwaysThrows)
						return result;
					data = result.NoExceptionOptions;
					data = data.WithVariablePossibilities(variableInitializer.Name, result);
				}

				return data;
			}

			public override VisitorState VisitIfElseStatement(IfElseStatement ifElseStatement, VisitorState data)
			{
				//We'll visit the true/false statements later (directly from the CFG)
				return ifElseStatement.Condition.AcceptVisitor(this, data);
			}

			public override VisitorState VisitWhileStatement(WhileStatement whileStatement, VisitorState data)
			{
				return whileStatement.Condition.AcceptVisitor(this, data);
			}

			public override VisitorState VisitDoWhileStatement(DoWhileStatement doWhileStatement, VisitorState data)
			{
				return doWhileStatement.Condition.AcceptVisitor(this, data);
			}

			public override VisitorState VisitForStatement(ForStatement forStatement, VisitorState data)
			{
				//The initializers, the embedded statement and the iterators aren't visited here
				//because they have their own CFG nodes.
				if (forStatement.Condition.IsNull)
					return data;
				return forStatement.Condition.AcceptVisitor(this, data);
			}

			public override VisitorState VisitForeachStatement(ForeachStatement foreachStatement, VisitorState data)
			{
				var newVariable = foreachStatement.VariableNameToken;
				var inExpressionResult = foreachStatement.InExpression.AcceptVisitor(this, data);
				if (inExpressionResult.AlwaysThrows)
					return inExpressionResult;

				var newData = inExpressionResult.NoExceptionOptions;

				var resolveResult = analysis.context.Resolve(foreachStatement.VariableNameToken) as LocalResolveResult;
				if (resolveResult != null) {
					newData = newData.Clone();

					foreach (var option in newData.Options) {
						//C# 5.0 changed the meaning of foreach so that each iteration declares a new variable
						//as such, the variable is "uncaptured" only for C# >= 5.0
						if (analysis.context.Supports(new Version(5, 0)) || option.Variables [newVariable.Name] != NullValueStatus.CapturedUnknown) {
							option.Variables [newVariable.Name] = NullValueAnalysis.IsTypeNullable(resolveResult.Type) ? option.EnumeratedValueResult : NullValueStatus.DefinitelyNotNull;
						}
					}
				}

				return newData;
			}

			public override VisitorState VisitUsingStatement(UsingStatement usingStatement, VisitorState data)
			{
				return usingStatement.ResourceAcquisition.AcceptVisitor(this, data);
			}

			public override VisitorState VisitFixedStatement(FixedStatement fixedStatement, VisitorState data)
			{
				foreach (var variable in fixedStatement.Variables) {
					var result = variable.AcceptVisitor(this, data);
					if (result.AlwaysThrows)
						return result;
					data = result.NoExceptionOptions;
				}

				return data;
			}

			public override VisitorState VisitSwitchStatement(SwitchStatement switchStatement, VisitorState data)
			{
				//We could do better than this, but it would require special handling outside the visitor
				//so for now, for simplicity, we'll just take the easy way

				var tentativeResult = switchStatement.Expression.AcceptVisitor(this, data);
				if (tentativeResult.AlwaysThrows) {
					return tentativeResult;
				}

				data = tentativeResult.NoExceptionOptions;

				foreach (var section in switchStatement.SwitchSections) {
					//No need to check for ThrowsException, since it will always be false (see VisitSwitchSection)
					section.AcceptVisitor(this, data);
				}

				return data;
			}

			public override VisitorState VisitSwitchSection(SwitchSection switchSection, VisitorState data)
			{
				return data;
			}

			public override VisitorState VisitExpressionStatement(ExpressionStatement expressionStatement, VisitorState data)
			{
				return expressionStatement.Expression.AcceptVisitor(this, data);
			}

			public override VisitorState VisitReturnStatement(ReturnStatement returnStatement, VisitorState data)
			{
				if (returnStatement.Expression.IsNull)
					return data;
				return returnStatement.Expression.AcceptVisitor(this, data);
			}

			public override VisitorState VisitTryCatchStatement(TryCatchStatement tryCatchStatement, VisitorState data)
			{
				//The needs special treatment in the analyser itself
				return data;
			}

			public override VisitorState VisitBreakStatement(BreakStatement breakStatement, VisitorState data)
			{
				return data;
			}

			public override VisitorState VisitContinueStatement(ContinueStatement continueStatement, VisitorState data)
			{
				return data;
			}

			public override VisitorState VisitGotoStatement(GotoStatement gotoStatement, VisitorState data)
			{
				return data;
			}

			public override VisitorState VisitGotoCaseStatement(GotoCaseStatement gotoCaseStatement, VisitorState data)
			{
				return data;
			}

			public override VisitorState VisitGotoDefaultStatement(GotoDefaultStatement gotoDefaultStatement, VisitorState data)
			{
				return data;
			}

			public override VisitorState VisitLabelStatement(LabelStatement labelStatement, VisitorState data)
			{
				return data;
			}

			public override VisitorState VisitUnsafeStatement(UnsafeStatement unsafeStatement, VisitorState data)
			{
				return data;
			}

			public override VisitorState VisitLockStatement(LockStatement lockStatement, VisitorState data)
			{
				var expressionResult = lockStatement.Expression.AcceptVisitor(this, data).WithNotNullResult();
				if (expressionResult.AlwaysThrows)
					return expressionResult;

				var identifier = CSharpUtil.GetInnerMostExpression(lockStatement.Expression) as IdentifierExpression;
				if (identifier != null) {
					return WithLocalVariableNotNull(expressionResult, identifier);
				}

				return expressionResult;
			}

			public override VisitorState VisitThrowStatement(ThrowStatement throwStatement, VisitorState data)
			{
				if (throwStatement.Expression.IsNull)
					return data;
				return throwStatement.Expression.AcceptVisitor(this, data);
			}

			public override VisitorState VisitYieldBreakStatement(YieldBreakStatement yieldBreakStatement, VisitorState data)
			{
				return data;
			}

			public override VisitorState VisitYieldReturnStatement(YieldReturnStatement yieldReturnStatement, VisitorState data)
			{
				return yieldReturnStatement.Expression.AcceptVisitor(this, data);
			}

			public override VisitorState VisitCheckedStatement(CheckedStatement checkedStatement, VisitorState data)
			{
				return data;
			}

			public override VisitorState VisitUncheckedStatement(UncheckedStatement uncheckedStatement, VisitorState data)
			{
				return data;
			}

			void RegisterExpressionResult(Expression expression, NullValueStatus expressionResult)
			{
				NullValueStatus oldStatus;
				if (analysis.expressionResult.TryGetValue(expression, out oldStatus)) {
					analysis.expressionResult[expression] = VariableStatusInfo.CombineStatus(analysis.expressionResult[expression], expressionResult);
				}
				else {
					analysis.expressionResult[expression] = expressionResult;
				}
			}

			VisitorState HandleExpressionResult(Expression expression, VariableStatusInfo dataAfterExpression, NullValueStatus expressionResult) {
				RegisterExpressionResult(expression, expressionResult);

				return VisitorState.ForValue(dataAfterExpression, expressionResult);
			}

			VisitorState HandleExpressionResult(Expression expression, VariableStatusInfo dataAfterExpression, bool expressionResult) {
				RegisterExpressionResult(expression, NullValueStatus.DefinitelyNotNull);

				return VisitorState.ForBoolValue(dataAfterExpression, expressionResult);
			}

			VisitorState HandleExpressionResult(Expression expression, VisitorState result) {
				foreach (var option in result.Options) {
					RegisterExpressionResult(expression, option.NullableReturnResult);
				}

				return result;
			}

			public override VisitorState VisitAssignmentExpression(AssignmentExpression assignmentExpression, VisitorState data)
			{
				var tentativeResult = assignmentExpression.Left.AcceptVisitor(this, data);
				if (tentativeResult.AlwaysThrows)
					return HandleExpressionResult(assignmentExpression, tentativeResult);

				tentativeResult = assignmentExpression.Right.AcceptVisitor(this, tentativeResult.NoExceptionOptions);
				if (tentativeResult.AlwaysThrows)
					return HandleExpressionResult(assignmentExpression, tentativeResult);

				tentativeResult = tentativeResult.NoExceptionOptions;

				var leftIdentifier = assignmentExpression.Left as IdentifierExpression;
				if (leftIdentifier != null) {
					var resolveResult = analysis.context.Resolve(leftIdentifier);
					if (resolveResult.IsError) {
						return HandleExpressionResult(assignmentExpression, tentativeResult);
					}

					if (resolveResult is LocalResolveResult) {
						foreach (var option in tentativeResult.Options) {
							var oldValue = option.Variables [leftIdentifier.Identifier];

							if (assignmentExpression.Operator == AssignmentOperatorType.Assign ||
							    oldValue == NullValueStatus.Unassigned ||
							    oldValue == NullValueStatus.DefinitelyNotNull ||
                                option.NullableReturnResult == NullValueStatus.Error ||
                                option.NullableReturnResult == NullValueStatus.Unknown) {

								analysis.SetLocalVariableValue(option.Variables, leftIdentifier, option.NullableReturnResult);
							} else {
								if (oldValue == NullValueStatus.DefinitelyNull) {
									//Do nothing --it'll remain null
								} else {
									analysis.SetLocalVariableValue(option.Variables, leftIdentifier, NullValueStatus.PotentiallyNull);
								}
							}
						}
					}
				}

				return HandleExpressionResult(assignmentExpression, tentativeResult);
			}

			public override VisitorState VisitIdentifierExpression(IdentifierExpression identifierExpression, VisitorState data)
			{
				var resolveResult = analysis.context.Resolve(identifierExpression);
				if (resolveResult.IsError) {
					return HandleExpressionResult(identifierExpression, data.Returning(NullValueStatus.Error));
				}

				var local = resolveResult as LocalResolveResult;
				if (local != null) {
					return HandleExpressionResult(identifierExpression, data.ReturningVariableValue(local.Variable.Name));
				}

				if (resolveResult.IsCompileTimeConstant) {
					object value = resolveResult.ConstantValue;
					if (value == null) {
						return HandleExpressionResult(identifierExpression, data.Returning(NullValueStatus.DefinitelyNull));
					}
					var boolValue = value as bool?;
					if (boolValue != null) {
						return HandleExpressionResult(identifierExpression, data.Returning((bool)boolValue));
					}
					return HandleExpressionResult(identifierExpression, data.Returning(NullValueStatus.DefinitelyNotNull));
				}

				var memberResolveResult = resolveResult as MemberResolveResult;

				var returnData = data.Clone();
				foreach (var option in returnData.Options) {
					var returnValue = GetFieldReturnValue(memberResolveResult, option.Variables);
					option.NullableReturnResult = returnValue;
				}

				return HandleExpressionResult(identifierExpression, returnData);
			}

			public override VisitorState VisitDefaultValueExpression(DefaultValueExpression defaultValueExpression, VisitorState data)
			{
				var resolveResult = analysis.context.Resolve(defaultValueExpression);
				if (resolveResult.IsError) {
					return HandleExpressionResult(defaultValueExpression, data.Returning(NullValueStatus.Unknown));
				}

				Debug.Assert(resolveResult.IsCompileTimeConstant);

				return HandleExpressionResult(defaultValueExpression, data.Returning(resolveResult.ConstantValue == null ? NullValueStatus.DefinitelyNull : NullValueStatus.DefinitelyNotNull));
			}

			public override VisitorState VisitNullReferenceExpression(NullReferenceExpression nullReferenceExpression, VisitorState data)
			{
				return HandleExpressionResult(nullReferenceExpression, data.Returning(NullValueStatus.DefinitelyNull));
			}

			public override VisitorState VisitPrimitiveExpression(PrimitiveExpression primitiveExpression, VisitorState data)
			{
				return HandleExpressionResult(primitiveExpression, data.Returning(NullValueStatus.DefinitelyNotNull));
			}

			public override VisitorState VisitParenthesizedExpression(ParenthesizedExpression parenthesizedExpression, VisitorState data)
			{
				return HandleExpressionResult(parenthesizedExpression, parenthesizedExpression.Expression.AcceptVisitor(this, data));
			}

			public override VisitorState VisitConditionalExpression(ConditionalExpression conditionalExpression, VisitorState data)
			{
				var tentativeBaseResult = conditionalExpression.Condition.AcceptVisitor(this, data);
				if (tentativeBaseResult.AlwaysThrows)
					return HandleExpressionResult(conditionalExpression, tentativeBaseResult);

				tentativeBaseResult = tentativeBaseResult.NoExceptionOptions;

				var conditionResolveResult = analysis.context.Resolve(conditionalExpression.Condition);

				if (true.Equals(conditionResolveResult.ConstantValue)) {
					return HandleExpressionResult(conditionalExpression, conditionalExpression.TrueExpression.AcceptVisitor(this, tentativeBaseResult));
				}
				if (false.Equals(conditionResolveResult.ConstantValue)) {
					return HandleExpressionResult(conditionalExpression, conditionalExpression.FalseExpression.AcceptVisitor(this, tentativeBaseResult));
				}

				var truePath = tentativeBaseResult.TruePathVisitorResult;
				VisitorState trueCaseResult = conditionalExpression.TrueExpression.AcceptVisitor(this, truePath);

				var falsePath = tentativeBaseResult.FalsePathVisitorResult;
				VisitorState falseCaseResult = conditionalExpression.FalseExpression.AcceptVisitor(this, falsePath);

				return HandleExpressionResult(conditionalExpression, VisitorState.CombineOptions(trueCaseResult, falseCaseResult));
			}

			public override VisitorState VisitBinaryOperatorExpression(BinaryOperatorExpression binaryOperatorExpression, VisitorState data)
			{
				//Let's not evaluate the sides just yet because of ??, && and ||

				//We'll register the results here (with HandleExpressionResult)
				//so each Visit*Expression won't have to do it itself
				switch (binaryOperatorExpression.Operator) {
					case BinaryOperatorType.ConditionalAnd:
						return HandleExpressionResult(binaryOperatorExpression, VisitConditionalAndExpression(binaryOperatorExpression, data));
					case BinaryOperatorType.ConditionalOr:
						return HandleExpressionResult(binaryOperatorExpression, VisitConditionalOrExpression(binaryOperatorExpression, data));
					case BinaryOperatorType.NullCoalescing:
						return HandleExpressionResult(binaryOperatorExpression, VisitNullCoalescing(binaryOperatorExpression, data));
					case BinaryOperatorType.Equality:
						return HandleExpressionResult(binaryOperatorExpression, VisitEquality(binaryOperatorExpression, data));
					case BinaryOperatorType.InEquality:
						return HandleExpressionResult(binaryOperatorExpression, VisitEquality(binaryOperatorExpression, data).Negated);
					default:
						return HandleExpressionResult(binaryOperatorExpression, VisitOtherBinaryExpression(binaryOperatorExpression, data));
				}
			}

			VisitorState VisitOtherBinaryExpression(BinaryOperatorExpression binaryOperatorExpression, VisitorState data)
			{
				var leftTentativeResult = binaryOperatorExpression.Left.AcceptVisitor(this, data);
				if (leftTentativeResult.AlwaysThrows)
					return leftTentativeResult;

				leftTentativeResult = leftTentativeResult.NoExceptionOptions;

				var expressionResult = new VisitorState();

				foreach (var leftOption in leftTentativeResult.Options) {

					var leftOptionResult = VisitorState.ForSingleOption(leftOption);

					var rightTentativeResult = binaryOperatorExpression.Right.AcceptVisitor(this, leftOptionResult);
					if (rightTentativeResult.AlwaysThrows)
						continue;

					rightTentativeResult = rightTentativeResult.NoExceptionOptions;

					foreach (var rightOption in rightTentativeResult.Options) {

					//TODO: Assuming operators are not overloaded by users
					// (or, if they are, that they retain similar behavior to the default ones)

						switch (binaryOperatorExpression.Operator) {
							case BinaryOperatorType.LessThan:
							case BinaryOperatorType.GreaterThan:
						//Operations < and > with nulls always return false
						//Those same operations will other values may or may not return false
								if (leftOption.NullableReturnResult == NullValueStatus.DefinitelyNull ||
								    rightOption.NullableReturnResult == NullValueStatus.DefinitelyNull) {
									expressionResult.Options.Add(rightOption.WithResult(false));
								} else {
									//We don't know what the value is, but we know that both true and false are != null.
									expressionResult.Options.Add(rightOption.WithResult(NullValueStatus.DefinitelyNotNull));
								}
								break;
							case BinaryOperatorType.LessThanOrEqual:
							case BinaryOperatorType.GreaterThanOrEqual:
								if (leftOption.NullableReturnResult == NullValueStatus.DefinitelyNull) {
									if (rightOption.NullableReturnResult == NullValueStatus.DefinitelyNull) {
										expressionResult.Options.Add(rightOption.WithResult(true));
									} else if (rightOption.NullableReturnResult == NullValueStatus.DefinitelyNotNull) {
										expressionResult.Options.Add(rightOption.WithResult(false));
									}
								} else if (leftOption.NullableReturnResult == NullValueStatus.DefinitelyNotNull) {
									if (rightOption.NullableReturnResult == NullValueStatus.DefinitelyNull) {
										expressionResult.Options.Add(rightOption.WithResult(false));
									}
								}

								expressionResult.Options.Add(rightOption.WithResult(NullValueStatus.Unknown));
								break;
							default:
						//Anything else: null + anything == anything + null == null.
						//not null + not null = not null
								if (leftOption.NullableReturnResult == NullValueStatus.DefinitelyNull) {
									expressionResult.Options.Add(rightOption.WithResult(NullValueStatus.DefinitelyNull));
								}
								else if (leftOption.NullableReturnResult == NullValueStatus.DefinitelyNotNull) {
									if (rightOption.NullableReturnResult == NullValueStatus.DefinitelyNull)
										expressionResult.Options.Add(rightOption.WithResult(NullValueStatus.DefinitelyNull));
									else if (rightOption.NullableReturnResult == NullValueStatus.DefinitelyNotNull)
										expressionResult.Options.Add(rightOption.WithResult(NullValueStatus.DefinitelyNotNull));
								}

								expressionResult.Options.Add(rightOption.WithResult(NullValueStatus.Unknown));
								break;
						}
					}
				}

				return expressionResult;
			}

			VisitorState WithVariableValue(ResultPossibility option, IdentifierExpression identifier, bool isNull)
			{
				var localVariableResult = analysis.context.Resolve(identifier) as LocalResolveResult;
				if (localVariableResult != null) {
					var truePossibility = option.Clone();
					option.Variables [identifier.Identifier] = isNull ? NullValueStatus.DefinitelyNull : NullValueStatus.DefinitelyNotNull;
					var falsePossibility = option.Clone();
					if (isNull) {
						option.Variables [identifier.Identifier] = NullValueStatus.DefinitelyNotNull;
					}

					var result = new VisitorState();
					result.Options.Add(truePossibility);
					result.Options.Add(falsePossibility);
					return result;
				}
				return VisitorState.ForSingleOption(option);
			}

			VisitorState VisitEquality(BinaryOperatorExpression binaryOperatorExpression, VisitorState data)
			{
				//TODO: Should this check for user operators?

				var tentativeLeftResult = binaryOperatorExpression.Left.AcceptVisitor(this, data);
				if (tentativeLeftResult.AlwaysThrows)
					return tentativeLeftResult;
				tentativeLeftResult = tentativeLeftResult.NoExceptionOptions;

				var result = new VisitorState();

				var leftIdentifier = CSharpUtil.GetInnerMostExpression(binaryOperatorExpression.Left) as IdentifierExpression;
				var rightIdentifier = CSharpUtil.GetInnerMostExpression(binaryOperatorExpression.Right) as IdentifierExpression;

				string leftVariable = leftIdentifier != null && analysis.context.Resolve(leftIdentifier) is LocalResolveResult ? leftIdentifier.Identifier : null;
				string rightVariable = rightIdentifier != null && analysis.context.Resolve(rightIdentifier) is LocalResolveResult ? rightIdentifier.Identifier : null;

				foreach (var option in tentativeLeftResult.Options) {
					var optionResult = VisitorState.ForSingleOption(option);

					var tentativeRightResult = binaryOperatorExpression.Right.AcceptVisitor(this, optionResult);
					if (tentativeRightResult.AlwaysThrows)
						continue;
					tentativeRightResult = tentativeRightResult.NoExceptionOptions;

					string optionLeft = leftVariable == null || option.Variables [leftVariable] == NullValueStatus.CapturedUnknown ?
					                    null : leftVariable;

					foreach (var rightOption in tentativeRightResult.Options) {
						string optionRight = rightVariable == null || option.Variables [rightVariable] == NullValueStatus.CapturedUnknown ?
						                     null : rightVariable;

						if (option.KnownBoolResult != null && rightOption.KnownBoolResult != null) {
							result.Options.Add(rightOption.WithResult(option.KnownBoolResult == rightOption.KnownBoolResult));
							continue;
						}

						if (option.NullableReturnResult.IsDefiniteValue() && rightOption.NullableReturnResult.IsDefiniteValue()) {
							if (option.NullableReturnResult == NullValueStatus.DefinitelyNull) {
								result.Options.Add(rightOption.WithResult(rightOption.NullableReturnResult == NullValueStatus.DefinitelyNull));
								continue;
							}
							if (option.NullableReturnResult == NullValueStatus.DefinitelyNotNull) {
								result.Options.Add(rightOption.WithResult(false));
								continue;
							}
						}

						if (optionLeft != null) {
							if (optionRight != null) {
								//x == y. x and y must have the same value for the result to be true

								if (option.NullableReturnResult != NullValueStatus.DefinitelyNotNull) {
									if (rightOption.NullableReturnResult != NullValueStatus.DefinitelyNotNull) {
										var nullOption = rightOption.WithResult(true);
										nullOption.Variables [optionLeft] = NullValueStatus.DefinitelyNull;
										nullOption.Variables [optionRight] = NullValueStatus.DefinitelyNull;
										result.Options.Add(nullOption);
									}

									if (rightOption.NullableReturnResult != NullValueStatus.DefinitelyNull) {
										var nullOption = rightOption.WithResult(false);
										nullOption.Variables [optionLeft] = NullValueStatus.DefinitelyNull;
										nullOption.Variables [optionRight] = NullValueStatus.DefinitelyNotNull;
										result.Options.Add(nullOption);
									}
								}
								if (option.NullableReturnResult != NullValueStatus.DefinitelyNull) {
									if (rightOption.NullableReturnResult != NullValueStatus.DefinitelyNotNull) {
										var nullOption = rightOption.WithResult(false);
										nullOption.Variables [optionLeft] = NullValueStatus.DefinitelyNotNull;
										nullOption.Variables [optionRight] = NullValueStatus.DefinitelyNull;
										result.Options.Add(nullOption);
									}
									
									if (rightOption.NullableReturnResult != NullValueStatus.DefinitelyNull) {
										//We can't know if <not-null> == <not-null> is true, since 1 == 1 but 1 != 2
										// even though both 1 and 2 are <not-null>.
										var nullOption = rightOption.WithResult(NullValueStatus.DefinitelyNotNull);
										nullOption.Variables [optionLeft] = NullValueStatus.DefinitelyNotNull;
										nullOption.Variables [optionRight] = NullValueStatus.DefinitelyNotNull;
										result.Options.Add(nullOption);
									}
								}

								continue;
							}

							//x == <not a var>
							if (rightOption.NullableReturnResult == NullValueStatus.DefinitelyNull) {
								var trueOption = rightOption.WithResult(true);
								trueOption.Variables [optionLeft] = NullValueStatus.DefinitelyNull;
								result.Options.Add(trueOption);

								var falseOption = rightOption.WithResult(false);
								falseOption.Variables [optionLeft] = NullValueStatus.DefinitelyNotNull;
								result.Options.Add(falseOption);
							} else if (rightOption.NullableReturnResult == NullValueStatus.DefinitelyNotNull) {

								var trueOption = rightOption.WithResult(NullValueStatus.DefinitelyNotNull);
								trueOption.Variables [optionLeft] = NullValueStatus.DefinitelyNotNull;
								result.Options.Add(trueOption);
									
								var falseOption = rightOption.WithResult(false);
								falseOption.Variables [optionLeft] = NullValueStatus.DefinitelyNull;
								result.Options.Add(falseOption);
							}

							continue;
						}

						if (optionRight != null) {
							//<not a var> == x
							if (option.NullableReturnResult == NullValueStatus.DefinitelyNull) {
								var trueOption = rightOption.WithResult(true);
								trueOption.Variables [optionRight] = NullValueStatus.DefinitelyNull;
								result.Options.Add(trueOption);
								
								var falseOption = rightOption.WithResult(false);
								falseOption.Variables [optionRight] = NullValueStatus.DefinitelyNotNull;
								result.Options.Add(falseOption);
							} else if (option.NullableReturnResult == NullValueStatus.DefinitelyNotNull) {
								
								var trueOption = rightOption.WithResult(NullValueStatus.DefinitelyNotNull);
								trueOption.Variables [optionRight] = NullValueStatus.DefinitelyNotNull;
								result.Options.Add(trueOption);
								
								var falseOption = rightOption.WithResult(false);
								falseOption.Variables [optionRight] = NullValueStatus.DefinitelyNull;
								result.Options.Add(falseOption);
							}

							continue;
						}

						result.Options.Add(rightOption.WithResult(NullValueStatus.DefinitelyNotNull));
					}
				}

				return result;
			}

			VisitorState VisitConditionalAndExpression(BinaryOperatorExpression binaryOperatorExpression, VisitorState data)
			{
				var tentativeLeftResult = binaryOperatorExpression.Left.AcceptVisitor(this, data);
				if (tentativeLeftResult.AlwaysThrows)
					return tentativeLeftResult;
				tentativeLeftResult = tentativeLeftResult.NoExceptionOptions;

				var tentativeRightResult = binaryOperatorExpression.Right.AcceptVisitor(this, tentativeLeftResult.TruePathVisitorResult);
				if (tentativeRightResult.AlwaysThrows) {
					//If the true path throws an exception, then the only way for the expression to complete
					//successfully is if the left expression is false
					return tentativeLeftResult.FalsePathVisitorResult;
				}

				tentativeRightResult = tentativeRightResult.NoExceptionOptions;

				return VisitorState.AndOperation(tentativeLeftResult, tentativeRightResult);
			}

			VisitorState VisitConditionalOrExpression(BinaryOperatorExpression binaryOperatorExpression, VisitorState data)
			{
				var tentativeLeftResult = binaryOperatorExpression.Left.AcceptVisitor(this, data);
				if (tentativeLeftResult.AlwaysThrows)
					return tentativeLeftResult;

				tentativeLeftResult = tentativeLeftResult.NoExceptionOptions;

				var falsePath = tentativeLeftResult.FalsePathVisitorResult;
				var tentativeRightResult = binaryOperatorExpression.Right.AcceptVisitor(this, falsePath);
				if (tentativeRightResult.AlwaysThrows) {
					//If the false path throws an exception, then the only way for the expression to complete
					//successfully is if the left expression is true
					return tentativeLeftResult.TruePathVisitorResult;
				}

				tentativeRightResult = tentativeRightResult.NoExceptionOptions;

				return VisitorState.OrOperation(tentativeLeftResult, tentativeRightResult);
			}

			VisitorState VisitNullCoalescing(BinaryOperatorExpression binaryOperatorExpression, VisitorState data)
			{
				var leftTentativeResult = binaryOperatorExpression.Left.AcceptVisitor(this, data);
				if (leftTentativeResult.AlwaysThrows) {
					return leftTentativeResult;
				}

				leftTentativeResult = leftTentativeResult.NoExceptionOptions;

				if (!leftTentativeResult.MayReturnNull()) {
					return leftTentativeResult;
				}

				var nullLeftResult = leftTentativeResult.WithNullResult();
				var notNullLeftResult = leftTentativeResult.WithNotNullResult();

				//If the right side is found, then the left side is known to be null
				var leftIdentifier = CSharpUtil.GetInnerMostExpression(binaryOperatorExpression.Left) as IdentifierExpression;
				if (leftIdentifier != null) {
					analysis.SetLocalVariableValue(nullLeftResult, leftIdentifier, NullValueStatus.DefinitelyNull);
					analysis.SetLocalVariableValue(notNullLeftResult, leftIdentifier, NullValueStatus.DefinitelyNotNull);
				}

				var rightTentativeResult = binaryOperatorExpression.Right.AcceptVisitor(this, nullLeftResult);
				rightTentativeResult = rightTentativeResult.NoExceptionOptions;

				return VisitorState.CombineOptions(notNullLeftResult, rightTentativeResult);
			}

			public override VisitorState VisitUnaryOperatorExpression(UnaryOperatorExpression unaryOperatorExpression, VisitorState data)
			{
				//TODO: Again, what to do when overloaded operators are found?

				var tentativeResult = unaryOperatorExpression.Expression.AcceptVisitor(this, data);
				if (tentativeResult.AlwaysThrows)
					return HandleExpressionResult(unaryOperatorExpression, tentativeResult);

				tentativeResult = tentativeResult.NoExceptionOptions;

				if (unaryOperatorExpression.Operator == UnaryOperatorType.Not) {
					return HandleExpressionResult(unaryOperatorExpression, tentativeResult.Negated);
				}
				return HandleExpressionResult(unaryOperatorExpression, tentativeResult);
			}

			public override VisitorState VisitInvocationExpression(InvocationExpression invocationExpression, VisitorState data)
			{
				var targetResult = invocationExpression.Target.AcceptVisitor(this, data);
				if (targetResult.AlwaysThrows)
					return HandleExpressionResult(invocationExpression, targetResult);

				targetResult = targetResult.NoExceptionOptions;

				var expressionState = new List<Tuple<ResultPossibility, List<ResultPossibility>>>();
				foreach (var option in targetResult.Options) {
					expressionState.Add(Tuple.Create(option, new List<ResultPossibility>()));
				}

				var methodResolveResult = analysis.context.Resolve(invocationExpression) as CSharpInvocationResolveResult;

				foreach (var argumentToHandle in invocationExpression.Arguments.Select((argument, argumentIndex) => new { argument, argumentIndex })) {
					var argument = argumentToHandle.argument;
					var argumentIndex = argumentToHandle.argumentIndex;

					var newExpressionState = new List<Tuple<ResultPossibility, List<ResultPossibility>>>();

					foreach (var option in expressionState) {
						var resultSoFar = option.Item1;
						var parametersSoFar = option.Item2;

						var paramData = argument.AcceptVisitor(this, VisitorState.ForSingleOption(resultSoFar));
						if (paramData.AlwaysThrows)
							continue;
						paramData = paramData.NoExceptionOptions;

						var namedArgument = argument as NamedArgumentExpression;

						var directionExpression = (namedArgument == null ? argument : namedArgument.Expression) as DirectionExpression;
						if (directionExpression != null) {
							var identifier = directionExpression.Expression as IdentifierExpression;
							if (identifier != null) {
								//out and ref parameters do *NOT* capture the variable (since they must stop changing it by the time they return)
								var identifierResolveResult = analysis.context.Resolve(identifier) as LocalResolveResult;
								if (identifierResolveResult != null && IsTypeNullable(identifierResolveResult.Type)) {
									FixParameter(argument, methodResolveResult.Member.Parameters, argumentIndex, identifier, paramData, false);
								}
							}
						}

						foreach (var paramOption in paramData.Options) {
							var parameterList = new List<ResultPossibility>(parametersSoFar);
							parameterList.Add(paramOption);
							newExpressionState.Add(Tuple.Create(paramOption, parameterList));
						}
					}

					expressionState = newExpressionState;
				}

				//TODO: handle identifier target

				VisitorState invocationResult = new VisitorState();
				foreach (var state in expressionState) {
					invocationResult.Options.AddRange(GetMethodVisitorResult(methodResolveResult, state.Item1.Variables, state.Item2).NoExceptionOptions.Options);
				}

				/*var identifierExpression = CSharpUtil.GetInnerMostExpression(invocationExpression.Target) as IdentifierExpression;

				if (identifierExpression != null) {
					foreach (var paramOption in paramData.Options) {
						if (paramOption.NullableReturnResult == NullValueStatus.DefinitelyNull) {
							//Exception
							continue;
						}

						var descendentIdentifiers = invocationExpression.Arguments.SelectMany(argument => argument.DescendantsAndSelf).OfType<IdentifierExpression>();
						if (!descendentIdentifiers.Any(identifier => identifier.Identifier == identifierExpression.Identifier)) {
							//TODO: We can make this check better (see VisitIndexerExpression for more details)
							paramOption = paramOption.Clone();
							analysis.SetLocalVariableValue(paramOption.Variables, identifierExpression, NullValueStatus.DefinitelyNotNull);
						}

						invocationResult.Options.AddRange(GetMethodVisitorResult(methodResolveResult, paramOption.Variables, parameterResults).Options);
					}
				}
*/
				return HandleExpressionResult(invocationExpression, invocationResult);
			}

			static VisitorState GetMethodVisitorResult(CSharpInvocationResolveResult methodResolveResult, VariableStatusInfo data, List<ResultPossibility> parameterResults)
			{
				if (methodResolveResult == null)
					return VisitorState.ForValue(data, NullValueStatus.Unknown);

				if (methodResolveResult.Member.Attributes.Any(attribute => attribute.AttributeType.FullName == "JetBrains.Annotations.TerminatesProgramExecution")) {
					return VisitorState.ForException(data);
				}

				var method = methodResolveResult.Member as IMethod;

				if (method != null) {
					if (method.GetAttribute(new FullTypeName("JetBrains.Annotations.AssertionMethodAttribute")) != null) {
						var assertionParameters = method.Parameters.Select((parameter, index) => new { index, parameter })
							.Select(parameter => new { parameter.index, parameter.parameter, attributes = parameter.parameter.Attributes.Where(attribute => attribute.AttributeType.FullName == "JetBrains.Annotations.AssertionConditionAttribute").ToList() })
							.Where(parameter => parameter.attributes.Count() == 1)
							.Select(parameter => new { parameter.index, parameter.parameter, attribute = parameter.attributes[0] })
							.ToList();

						//Unclear what should be done if there are multiple assertion conditions
						if (assertionParameters.Count() == 1) {
							Debug.Assert(methodResolveResult.Arguments.Count == parameterResults.Count);

							var assertionParameter = assertionParameters [0];
							ResultPossibility assertionParameterResult = null;

							object intendedResult = true;
							var positionalArgument = assertionParameter.attribute.PositionalArguments.FirstOrDefault() as MemberResolveResult;
							if (positionalArgument != null && positionalArgument.Type.FullName == "JetBrains.Annotations.AssertionConditionType") {
								switch (positionalArgument.Member.FullName) {
									case "JetBrains.Annotations.AssertionConditionType.IS_TRUE":
										intendedResult = true;
										break;
									case "JetBrains.Annotations.AssertionConditionType.IS_FALSE":
										intendedResult = false;
										break;
									case "JetBrains.Annotations.AssertionConditionType.IS_NULL":
										intendedResult = null;
										break;
									case "JetBrains.Annotations.AssertionConditionType.IS_NOT_NULL":
										intendedResult = "<not-null>";
										break;
								}
							}

							int parameterIndex = assertionParameter.index;
							if (assertionParameter.index < methodResolveResult.Arguments.Count && !(methodResolveResult.Arguments [assertionParameter.index] is NamedArgumentResolveResult)) {
								//Use index
								assertionParameterResult = parameterResults [assertionParameter.index];
							} else {
								//Use named argument
								int? nameIndex = methodResolveResult.Arguments.Select((argument, index) => new { argument, index})
									.Where(argument => {
										var namedArgument = argument.argument as NamedArgumentResolveResult;
										return namedArgument != null && namedArgument.ParameterName == assertionParameter.parameter.Name;
									}).Select(argument => (int?)argument.index).FirstOrDefault();

								if (nameIndex != null) {
									parameterIndex = nameIndex.Value;
									assertionParameterResult = parameterResults [nameIndex.Value];
								} else if (assertionParameter.parameter.IsOptional) {
									//Try to use default value

									if (intendedResult is string) {
										if (assertionParameter.parameter.ConstantValue == null) {
											return VisitorState.ForException(data);
										}
									} else {
										if (!object.Equals(assertionParameter.parameter.ConstantValue, intendedResult)) {
											return VisitorState.ForException(data);
										}
									}
								} else {
									//The parameter was not specified, yet it is not optional?
									return VisitorState.ForException(data);
								}
							}

							//Now check assertion
							if (assertionParameterResult != null) {
								if (intendedResult is bool) {
									if (assertionParameterResult.KnownBoolResult == !(bool)intendedResult) {
										return VisitorState.ForException(data);
									}
								} else {
									bool shouldBeNull = intendedResult == null;

									if (assertionParameterResult.NullableReturnResult == (shouldBeNull ? NullValueStatus.DefinitelyNotNull : NullValueStatus.DefinitelyNull)) {
										return VisitorState.ForException(data);
									}

									var parameterResolveResult = methodResolveResult.Arguments [parameterIndex];

									LocalResolveResult localVariableResult = null;

									var conversionResolveResult = parameterResolveResult as ConversionResolveResult;
									if (conversionResolveResult != null) {
										if (!IsTypeNullable(conversionResolveResult.Type)) {
											if (intendedResult == null) {
												return VisitorState.ForException(data);
											}
										} else {
											localVariableResult = conversionResolveResult.Input as LocalResolveResult;
										}
									} else {
										localVariableResult = parameterResolveResult as LocalResolveResult;
									}

									if (localVariableResult != null && data[localVariableResult.Variable.Name] != NullValueStatus.CapturedUnknown) {
										data = data.Clone();
										data [localVariableResult.Variable.Name] = shouldBeNull ? NullValueStatus.DefinitelyNull : NullValueStatus.DefinitelyNotNull;
									}
								}
							}
						}
					}
				}

				bool isNullable = IsTypeNullable(methodResolveResult.Type);
				if (!isNullable) {
					return VisitorState.ForValue(data, NullValueStatus.DefinitelyNotNull);
				}

				if (method != null)
					return VisitorState.ForValue(data, GetNullableStatus(method));

				return VisitorState.ForValue(data, GetNullableStatus(methodResolveResult.TargetResult.Type.GetDefinition()));
			}

			static NullValueStatus GetNullableStatus(IEntity entity)
			{
				if (entity.DeclaringType != null && entity.DeclaringType.Kind == TypeKind.Delegate) {
					//Handle Delegate.Invoke method
					return GetNullableStatus(entity.DeclaringTypeDefinition);
				}
				
				return GetNullableStatus(fullTypeName => entity.GetAttribute(new FullTypeName(fullTypeName)));
			}

			static NullValueStatus GetNullableStatus(IParameter parameter)
			{
				return GetNullableStatus(fullTypeName => parameter.Attributes.FirstOrDefault(attribute => attribute.AttributeType.FullName == fullTypeName));
			}

			static NullValueStatus GetNullableStatus(Func<string, IAttribute> attributeGetter)
			{
				if (attributeGetter("JetBrains.Annotations.NotNullAttribute") != null) {
					return NullValueStatus.DefinitelyNotNull;
				}
				if (attributeGetter("JetBrains.Annotations.CanBeNullAttribute") != null) {
					return NullValueStatus.PotentiallyNull;
				}
				return NullValueStatus.Unknown;
			}

			public override VisitorState VisitMemberReferenceExpression(MemberReferenceExpression memberReferenceExpression, VisitorState data)
			{
				var targetResult = memberReferenceExpression.Target.AcceptVisitor(this, data);
				if (targetResult.AlwaysThrows)
					return HandleExpressionResult(memberReferenceExpression, targetResult);

				targetResult = targetResult.NoExceptionOptions;

				var memberReferenceResult = new VisitorState();

				var memberResolveResult = analysis.context.Resolve(memberReferenceExpression) as MemberResolveResult;
				var targetIdentifier = CSharpUtil.GetInnerMostExpression(memberReferenceExpression.Target) as IdentifierExpression;
				if (targetIdentifier != null) {
					if (memberResolveResult == null) {
						var invocation = memberReferenceExpression.Parent as InvocationExpression;
						if (invocation != null) {
							memberResolveResult = analysis.context.Resolve(invocation) as MemberResolveResult;
						}
					}
				}

				foreach (var option in targetResult.Options) {

					var variables = option.Variables;

					if (targetIdentifier != null) {
						if (memberResolveResult != null && memberResolveResult.Member.FullName != "System.Nullable.HasValue") {
							var method = memberResolveResult.Member as IMethod;
							if (method == null || !method.IsExtensionMethod) {
								if (option.NullableReturnResult == NullValueStatus.DefinitelyNull) {
									continue;
								}
								if (variables [targetIdentifier.Identifier] != NullValueStatus.CapturedUnknown) {
									variables = variables.Clone();
									analysis.SetLocalVariableValue(variables, targetIdentifier, NullValueStatus.DefinitelyNotNull);
								}
							}
						}
					}

					var returnValue = GetFieldReturnValue(memberResolveResult, variables);
					memberReferenceResult.Options.Add(ResultPossibility.ForReturnValue(variables, returnValue));
				}
				return HandleExpressionResult(memberReferenceExpression, memberReferenceResult);
			}

			static NullValueStatus GetFieldReturnValue(MemberResolveResult memberResolveResult, VariableStatusInfo data)
			{
				bool isNullable = memberResolveResult == null || IsTypeNullable(memberResolveResult.Type);
				if (!isNullable) {
					return NullValueStatus.DefinitelyNotNull;
				}

				if (memberResolveResult != null) {
					return GetNullableStatus(memberResolveResult.Member);
				}

				return NullValueStatus.Unknown;
			}

			public override VisitorState VisitTypeReferenceExpression(TypeReferenceExpression typeReferenceExpression, VisitorState data)
			{
				return HandleExpressionResult(typeReferenceExpression, data.Returning(NullValueStatus.Unknown));
			}

			void FixParameter(Expression argument, IList<IParameter> parameters, int parameterIndex, IdentifierExpression identifier, ResultPossibility data, bool ignoreCapture)
			{
				if (!ignoreCapture && data.Variables [identifier.Identifier] == NullValueStatus.CapturedUnknown)
					return;

				NullValueStatus newValue = NullValueStatus.Unknown;
				if (argument is NamedArgumentExpression) {
					var namedResolveResult = analysis.context.Resolve(argument) as NamedArgumentResolveResult;
					if (namedResolveResult != null) {
						newValue = GetNullableStatus(namedResolveResult.Parameter);
					}
				}
				else {
					var parameter = parameters[parameterIndex];
					newValue = GetNullableStatus(parameter);
				}
				analysis.SetLocalVariableValue(data.Variables, identifier, newValue);
			}

			void FixParameter(Expression argument, IList<IParameter> parameters, int parameterIndex, IdentifierExpression identifier, VisitorState data, bool ignoreCapture)
			{
				foreach (var option in data.Options) {
					FixParameter(argument, parameters, parameterIndex, identifier, option, ignoreCapture);
				}
			}

			public override VisitorState VisitObjectCreateExpression(ObjectCreateExpression objectCreateExpression, VisitorState data)
			{
				var constructorResolveResult = analysis.context.Resolve(objectCreateExpression) as CSharpInvocationResolveResult;

				foreach (var argumentToHandle in objectCreateExpression.Arguments.Select((argument, parameterIndex) => new { argument, parameterIndex })) {
					var argument = argumentToHandle.argument;
					var parameterIndex = argumentToHandle.parameterIndex;

					var namedArgument = argument as NamedArgumentExpression;

					var directionExpression = (namedArgument == null ? argument : namedArgument.Expression) as DirectionExpression;
					if (directionExpression != null) {
						var identifier = directionExpression.Expression as IdentifierExpression;
						if (identifier != null) {
							//out and ref parameters do *NOT* capture the variable (since they must stop changing it by the time they return)
							data = data.Clone();

							FixParameter(argument, constructorResolveResult.Member.Parameters, parameterIndex, identifier, data, false);
						}
						continue;
					}

					var argumentResult = argument.AcceptVisitor(this, data);
					if (argumentResult.AlwaysThrows)
						return argumentResult;

					data = argumentResult.NoExceptionOptions;
				}

				//Constructors never return null
				return HandleExpressionResult(objectCreateExpression, data.Returning(NullValueStatus.DefinitelyNotNull));
			}

			public override VisitorState VisitArrayCreateExpression(ArrayCreateExpression arrayCreateExpression, VisitorState data)
			{
				foreach (var argument in arrayCreateExpression.Arguments) {
					var result = argument.AcceptVisitor(this, data);
					if (result.AlwaysThrows)
						return result;
					data = result.NoExceptionOptions;
				}

				if (arrayCreateExpression.Initializer.IsNull) {
					return HandleExpressionResult(arrayCreateExpression, data.Returning(NullValueStatus.DefinitelyNotNull));
				}

				return HandleExpressionResult(arrayCreateExpression, arrayCreateExpression.Initializer.AcceptVisitor(this, data));
			}

			public override VisitorState VisitArrayInitializerExpression(ArrayInitializerExpression arrayInitializerExpression, VisitorState data)
			{
				if (arrayInitializerExpression.IsSingleElement) {
					return HandleExpressionResult(arrayInitializerExpression, arrayInitializerExpression.Elements.Single().AcceptVisitor(this, data));
				}
				if (!arrayInitializerExpression.Elements.Any()) {
					//Empty array
					return HandleExpressionResult(arrayInitializerExpression, data.ReturningEnumerable(NullValueStatus.UnreachableOrInexistent));
				}

				VisitorState expressionResult = data.ReturningEnumerable(NullValueStatus.UnreachableOrInexistent);

				foreach (var element in arrayInitializerExpression.Elements) {
					var newResult = new VisitorState();

					foreach (var option in expressionResult.Options) {
						var result = element.AcceptVisitor(this, VisitorState.ForSingleOption(option));
						if (result.AlwaysThrows)
							continue;

						result = result.NoExceptionOptions;

						newResult.Options.AddRange(option.ContinueEnumerable(result));
					}

					expressionResult = newResult;
				}

				return HandleExpressionResult(arrayInitializerExpression, expressionResult);
			}

			public override VisitorState VisitAnonymousTypeCreateExpression(AnonymousTypeCreateExpression anonymousTypeCreateExpression, VisitorState data)
			{
				foreach (var initializer in anonymousTypeCreateExpression.Initializers) {
					var result = initializer.AcceptVisitor(this, data);
					if (result.AlwaysThrows)
						return result;
					data = result.NoExceptionOptions;
				}

				return HandleExpressionResult(anonymousTypeCreateExpression, data.Returning(NullValueStatus.DefinitelyNotNull));
			}

			void HandleVariableCapture(AstNode expressionNode, VisitorState newData)
			{
				var identifiers = expressionNode.Descendants.OfType<IdentifierExpression>();
				foreach (var identifier in identifiers) {
					//Check if it is in a "change-null-state" context
					//For instance, x++ does not change the null state
					//but `x = y` does.
					if (identifier.Parent is AssignmentExpression && identifier.Role == AssignmentExpression.LeftRole) {
						var parent = (AssignmentExpression)identifier.Parent;
						if (parent.Operator != AssignmentOperatorType.Assign) {
							continue;
						}
					}
					else {
						//No other context matters
						//Captured variables are never passed by reference (out/ref)
						continue;
					}
					//At this point, we know there's a good chance the variable has been changed
					var identifierResolveResult = analysis.context.Resolve(identifier) as LocalResolveResult;
					if (identifierResolveResult != null && IsTypeNullable(identifierResolveResult.Type)) {
						foreach (var option in newData.Options) {
							analysis.SetLocalVariableValue(option.Variables, identifier, NullValueStatus.CapturedUnknown);
						}
					}
				}
			}

			public override VisitorState VisitLambdaExpression(LambdaExpression lambdaExpression, VisitorState data)
			{
				var newData = data.Clone();

				HandleVariableCapture(lambdaExpression, newData);

				//The lambda itself is known not to be null
				return HandleExpressionResult(lambdaExpression, newData.Returning(NullValueStatus.DefinitelyNotNull));
			}

			public override VisitorState VisitAnonymousMethodExpression(AnonymousMethodExpression anonymousMethodExpression, VisitorState data)
			{
				var newData = data.Clone();

				HandleVariableCapture(anonymousMethodExpression, newData);

				//The anonymous method itself is known not to be null
				return HandleExpressionResult(anonymousMethodExpression, newData.Returning(NullValueStatus.DefinitelyNotNull));
			}


			public override VisitorState VisitNamedExpression(NamedExpression namedExpression, VisitorState data)
			{
				return HandleExpressionResult(namedExpression, namedExpression.Expression.AcceptVisitor(this, data));
			}

			public override VisitorState VisitAsExpression(AsExpression asExpression, VisitorState data)
			{
				var tentativeResult = asExpression.Expression.AcceptVisitor(this, data);
				if (tentativeResult.AlwaysThrows)
					return tentativeResult;

				tentativeResult = tentativeResult.NoExceptionOptions;

				foreach (var option in tentativeResult.Options) {
					NullValueStatus result;
					if (option.NullableReturnResult == NullValueStatus.DefinitelyNull) {
						result = NullValueStatus.DefinitelyNull;
					} else {
						var asResolveResult = analysis.context.Resolve(asExpression) as CastResolveResult;
						if (asResolveResult == null ||
						   asResolveResult.IsError ||
						   asResolveResult.Input.Type.Kind == TypeKind.Unknown ||
						   asResolveResult.Type.Kind == TypeKind.Unknown) {

							result = NullValueStatus.Unknown;
						} else {
							var conversion = new CSharpConversions(analysis.context.Compilation);
							var foundConversion = conversion.ExplicitConversion(asResolveResult.Input.Type, asResolveResult.Type);

							if (foundConversion == Conversion.None) {
								result = NullValueStatus.DefinitelyNull;
							} else if (foundConversion == Conversion.IdentityConversion) {
								result = option.NullableReturnResult;
							} else {
								result = NullValueStatus.PotentiallyNull;
							}
						}
					}

					option.NullableReturnResult = result;
				}

				return HandleExpressionResult(asExpression, tentativeResult);
			}

			public override VisitorState VisitCastExpression(CastExpression castExpression, VisitorState data)
			{
				var tentativeResult = castExpression.Expression.AcceptVisitor(this, data);
				if (tentativeResult.AlwaysThrows)
					return tentativeResult;

				tentativeResult = tentativeResult.NoExceptionOptions;

				foreach (var option in tentativeResult.Options) {
					VariableStatusInfo variables = option.Variables;

					var resolveResult = analysis.context.Resolve(castExpression) as CastResolveResult;
					if (resolveResult != null && !IsTypeNullable(resolveResult.Type)) {
						if (option.NullableReturnResult == NullValueStatus.DefinitelyNull) {
							continue;
						}

						var identifierExpression = CSharpUtil.GetInnerMostExpression(castExpression.Expression) as IdentifierExpression;
						if (identifierExpression != null) {
							var currentValue = variables [identifierExpression.Identifier];
							if (currentValue != NullValueStatus.CapturedUnknown &&
							   currentValue != NullValueStatus.UnreachableOrInexistent &&
							   currentValue != NullValueStatus.DefinitelyNotNull) {
								//DefinitelyNotNull is included in this list because if that's the status
								// then we don't need to change anything

								variables [identifierExpression.Identifier] = NullValueStatus.DefinitelyNotNull;
							}
						}

						option.NullableReturnResult = NullValueStatus.DefinitelyNotNull;
					}
				}

				return HandleExpressionResult(castExpression, tentativeResult);
			}

			public override VisitorState VisitIsExpression(IsExpression isExpression, VisitorState data)
			{
				var tentativeResult = isExpression.Expression.AcceptVisitor(this, data);
				if (tentativeResult.AlwaysThrows)
					return tentativeResult;

				tentativeResult = tentativeResult.NoExceptionOptions;

				//TODO: Consider, for instance: new X() is X. The result is known to be true, so we can use KnownBoolValue
				return HandleExpressionResult(isExpression, tentativeResult.Returning(NullValueStatus.DefinitelyNotNull));
			}

			public override VisitorState VisitDirectionExpression(DirectionExpression directionExpression, VisitorState data)
			{
				return HandleExpressionResult(directionExpression, directionExpression.Expression.AcceptVisitor(this, data));
			}

			public override VisitorState VisitCheckedExpression(CheckedExpression checkedExpression, VisitorState data)
			{
				return HandleExpressionResult(checkedExpression, checkedExpression.Expression.AcceptVisitor(this, data));
			}

			public override VisitorState VisitUncheckedExpression(UncheckedExpression uncheckedExpression, VisitorState data)
			{
				return HandleExpressionResult(uncheckedExpression, uncheckedExpression.Expression.AcceptVisitor(this, data));
			}

			public override VisitorState VisitThisReferenceExpression(ThisReferenceExpression thisReferenceExpression, VisitorState data)
			{
				return HandleExpressionResult(thisReferenceExpression, data.Returning(NullValueStatus.DefinitelyNotNull));
			}

			public override VisitorState VisitIndexerExpression(IndexerExpression indexerExpression, VisitorState data)
			{
				data = indexerExpression.Target.AcceptVisitor(this, data);
				if (data.AlwaysThrows)
					return data;
				data = data.NoExceptionOptions;

				foreach (var argument in indexerExpression.Arguments) {
					data = argument.AcceptVisitor(this, data);
					if (data.AlwaysThrows)
						return data;
					data = data.NoExceptionOptions;
				}

				IdentifierExpression targetAsIdentifier = CSharpUtil.GetInnerMostExpression(indexerExpression.Target) as IdentifierExpression;

				if (targetAsIdentifier != null) {
					var newOptions = data.Clone();
					foreach (var option in data.Options) {
						if (option.NullableReturnResult == NullValueStatus.DefinitelyNull)
							continue;

						newOptions.Options.Add(option);
						//If this doesn't cause an exception, then the target is not null
						//But we won't set it if it has been changed
						var descendentIdentifiers = indexerExpression.Arguments
							.SelectMany(argument => argument.DescendantsAndSelf).OfType<IdentifierExpression>();
						bool targetWasModified = descendentIdentifiers.Any(identifier => identifier.Identifier == targetAsIdentifier.Identifier);

						if (!targetWasModified) {
							//TODO: this check might be improved to include more legitimate cases
							//A good check will necessarily have to consider captured variables
							analysis.SetLocalVariableValue(option.Variables, targetAsIdentifier, NullValueStatus.DefinitelyNotNull);
						}
					}
					data = newOptions;
				}

				var indexerResolveResult = analysis.context.Resolve(indexerExpression) as CSharpInvocationResolveResult;
				bool isNullable = indexerResolveResult == null || IsTypeNullable(indexerResolveResult.Type);
				
				var returnValue = isNullable ? NullValueStatus.Unknown : NullValueStatus.DefinitelyNotNull;
				return HandleExpressionResult(indexerExpression, data.Returning(returnValue));
			}

			public override VisitorState VisitBaseReferenceExpression(BaseReferenceExpression baseReferenceExpression, VisitorState data)
			{
				return HandleExpressionResult(baseReferenceExpression, data.Returning(NullValueStatus.DefinitelyNotNull));
			}

			public override VisitorState VisitTypeOfExpression(TypeOfExpression typeOfExpression, VisitorState data)
			{
				return HandleExpressionResult(typeOfExpression, data.Returning(NullValueStatus.DefinitelyNotNull));
			}

			public override VisitorState VisitSizeOfExpression(SizeOfExpression sizeOfExpression, VisitorState data)
			{
				return HandleExpressionResult(sizeOfExpression, data.Returning(NullValueStatus.DefinitelyNotNull));
			}

			public override VisitorState VisitPointerReferenceExpression(PointerReferenceExpression pointerReferenceExpression, VisitorState data)
			{
				var targetResult = pointerReferenceExpression.Target.AcceptVisitor(this, data);
				if (targetResult.AlwaysThrows)
					return targetResult;

				targetResult = targetResult.NoExceptionOptions;

				return HandleExpressionResult(pointerReferenceExpression, targetResult.Returning(NullValueStatus.DefinitelyNotNull));
			}

			public override VisitorState VisitStackAllocExpression(StackAllocExpression stackAllocExpression, VisitorState data)
			{
				var countResult = stackAllocExpression.CountExpression.AcceptVisitor(this, data);
				if (countResult.AlwaysThrows)
					return countResult;

				countResult = countResult.NoExceptionOptions;

				return HandleExpressionResult(stackAllocExpression, countResult.Returning(NullValueStatus.DefinitelyNotNull));
			}

			public override VisitorState VisitNamedArgumentExpression(NamedArgumentExpression namedArgumentExpression, VisitorState data)
			{
				return HandleExpressionResult(namedArgumentExpression, namedArgumentExpression.Expression.AcceptVisitor(this, data));
			}

			public override VisitorState VisitUndocumentedExpression(UndocumentedExpression undocumentedExpression, VisitorState data)
			{
				throw new NotImplementedException();
			}

			public override VisitorState VisitQueryExpression(QueryExpression queryExpression, VisitorState data)
			{
				var clauses = queryExpression.Clauses.ToList();
				
				var backtrackingClauses = (from item in clauses.Select((clause, i) => new { clause, i })
					          where item.clause is QueryFromClause || item.clause is QueryJoinClause || item.clause is QueryContinuationClause
					select item.i).ToList();

				var queryResult = new VisitorState();

				//Our analysis here is somewhat limited and it's possible that it could be improved

				foreach (var option in data.Options) {
					VariableStatusInfo optionResult = null;
					NullValueStatus enumeratedResult = NullValueStatus.UnreachableOrInexistent;

					var stateBeforeClause = new Dictionary<int, VariableStatusInfo>();
					var pendingVisits = new Queue<Tuple<int, VariableStatusInfo>>();
					pendingVisits.Enqueue(Tuple.Create(0, option.Variables));

					while (pendingVisits.Any()) {
						var pendingVisit = pendingVisits.Dequeue();

						var clause = clauses[pendingVisit.Item1];
						var result = clause.AcceptVisitor(this, VisitorState.ForVariables(pendingVisit.Item2));

						foreach (var optionToMerge in result.Options) {
							if (optionResult == null)
								optionResult = optionToMerge.Variables.Clone();
							else
								optionResult.ReceiveIncoming(optionToMerge.Variables);

							if (optionToMerge.KnownBoolResult != false) {
								int clauseIndex = pendingVisit.Item1 + 1;
							
								if (clauseIndex == clauses.Count) {
									enumeratedResult = VariableStatusInfo.CombineStatus(enumeratedResult, optionToMerge.EnumeratedValueResult);
								}
								else if (clauseIndex < clauses.Count) {

									bool requiresVisit;
									if (stateBeforeClause.ContainsKey(clauseIndex)) {
										requiresVisit = stateBeforeClause [clauseIndex].ReceiveIncoming(optionToMerge.Variables);
									} else {
										stateBeforeClause [clauseIndex] = optionToMerge.Variables;
										requiresVisit = true;
									}
								
									if (requiresVisit) {
										pendingVisits.Enqueue(Tuple.Create(clauseIndex, optionToMerge.Variables));
									}
								}
							}
						}
					}

					//TODO: EnumeratedValue
					queryResult.Options.Add(ResultPossibility.ForEnumeratedValue(optionResult, enumeratedResult));
				}

				return queryResult;
			}

			public override VisitorState VisitQueryContinuationClause(QueryContinuationClause queryContinuationClause, VisitorState data)
			{
				return IntroduceVariableFromEnumeratedValue(queryContinuationClause.Identifier, queryContinuationClause.PrecedingQuery, data);
			}

			VisitorState IntroduceVariableFromEnumeratedValue(string newVariable, Expression expression, VisitorState data)
			{
				var combinedResult = new VisitorState();

				foreach (var option in data.Options) {
					var result = expression.AcceptVisitor(this, VisitorState.ForSingleOption(option));
					foreach (var resultOption in result.Options) {
						var newOption = resultOption.Clone();
						newOption.Variables [newVariable] = newOption.EnumeratedValueResult;

						combinedResult.Options.Add(newOption);
					}
				}

				return combinedResult;
			}

			public override VisitorState VisitQueryFromClause(QueryFromClause queryFromClause, VisitorState data)
			{
				return IntroduceVariableFromEnumeratedValue(queryFromClause.Identifier, queryFromClause.Expression, data);
			}

			public override VisitorState VisitQueryJoinClause(QueryJoinClause queryJoinClause, VisitorState data)
			{
				//TODO: Check if this really works in weird edge-cases.
				var tentativeResult = IntroduceVariableFromEnumeratedValue(queryJoinClause.JoinIdentifier, queryJoinClause.InExpression, data);
				tentativeResult = queryJoinClause.OnExpression.AcceptVisitor(this, tentativeResult);
				tentativeResult = queryJoinClause.EqualsExpression.AcceptVisitor(this, tentativeResult);

				if (queryJoinClause.IsGroupJoin) {
					analysis.SetLocalVariableValue(tentativeResult, queryJoinClause.IntoIdentifierToken, NullValueStatus.DefinitelyNotNull);
				}

				return tentativeResult.Returning(NullValueStatus.Unknown);
			}

			public override VisitorState VisitQueryLetClause(QueryLetClause queryLetClause, VisitorState data)
			{
				var result = queryLetClause.Expression.AcceptVisitor(this, data).NoExceptionOptions;

				string newVariable = queryLetClause.Identifier;
				return result.WithVariableFromResult(newVariable).Returning(NullValueStatus.Unknown);
			}

			public override VisitorState VisitQuerySelectClause(QuerySelectClause querySelectClause, VisitorState data)
			{
				var result = querySelectClause.Expression.AcceptVisitor(this, data).NoExceptionOptions;

				//The value of the expression in select becomes the "enumerated" value
				return result.ToEnumeration();
			}

			public override VisitorState VisitQueryWhereClause(QueryWhereClause queryWhereClause, VisitorState data)
			{
				var result = queryWhereClause.Condition.AcceptVisitor(this, data);

				return result.NoExceptionOptions;
			}

			public override VisitorState VisitQueryOrderClause(QueryOrderClause queryOrderClause, VisitorState data)
			{
				foreach (var ordering in queryOrderClause.Orderings) {
					data = ordering.AcceptVisitor(this, data).NoExceptionOptions;
				}

				return data.Returning(NullValueStatus.Unknown);
			}

			public override VisitorState VisitQueryOrdering(QueryOrdering queryOrdering, VisitorState data)
			{
				return queryOrdering.Expression.AcceptVisitor(this, data).Returning(NullValueStatus.Unknown);
			}

			public override VisitorState VisitQueryGroupClause(QueryGroupClause queryGroupClause, VisitorState data)
			{
				var projectionResult = queryGroupClause.Projection.AcceptVisitor(this, data);
				if (projectionResult.AlwaysThrows)
					return projectionResult;
				projectionResult = projectionResult.NoExceptionOptions;

				return queryGroupClause.Key.AcceptVisitor(this, projectionResult).ReturningEnumerable(NullValueStatus.DefinitelyNotNull);
			}
		}
	}
}

