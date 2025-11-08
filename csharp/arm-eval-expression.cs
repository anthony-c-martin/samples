#!/usr/bin/env dotnet

#:package Azure.Deployments.Expression@1.527.0

using Azure.Deployments.Expression.Expressions;
using Azure.Deployments.Expression.Intermediate;
using Azure.Deployments.Expression.Intermediate.Extensions;
using ExpressionEvaluationContext = Azure.Deployments.Expression.Intermediate.ExpressionEvaluationContext;

static string Evaluate(string expression)
{
    var evaluator = new ExpressionEvaluationContext([ExpressionBuiltInFunctions.Functions]);
    var evaluated = evaluator.EvaluateExpression(ExpressionParser.ParseLanguageExpression(expression));
    return JTokenConverter.SerializeExpressionForErrorMessage(evaluated);
}

var expression = "[guid('foo', 'bar', 'baz')]";
Console.WriteLine($"Result: {Evaluate(expression)}");