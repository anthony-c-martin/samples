#!/usr/bin/env dotnet

#:package Azure.Deployments.Expression@1.527.0

using Azure.Deployments.Expression.Expressions;
using Azure.Deployments.Expression.Functions;
using Azure.Deployments.Expression.Intermediate;
using Azure.Deployments.Expression.Intermediate.Extensions;
using Microsoft.WindowsAzure.ResourceStack.Common.Collections;
using Microsoft.WindowsAzure.ResourceStack.Common.Extensions;
using FunctionEvaluator = Azure.Deployments.Expression.Intermediate.FunctionEvaluator;
using ExpressionEvaluationContext = Azure.Deployments.Expression.Intermediate.ExpressionEvaluationContext;

var expression = "[reverse(concat('Hello', ' ', 'World!'))]";

var evaluator = new ExpressionEvaluationContext([
    ExpressionBuiltInFunctions.Functions,
    new CustomEvaluator()
]);

var evaluated = evaluator.EvaluateExpression(ExpressionParser.ParseLanguageExpression(expression));
var stringValue = JTokenConverter.SerializeExpressionForErrorMessage(evaluated);

Console.WriteLine($"Result: {stringValue}");

public class ReverseFunction : UnaryExpressionFunction<StringExpression>
{
    public override string Name => "reverse";

    protected override ITemplateLanguageExpression Evaluate(
        string functionName,
        StringExpression arg,
        IPositionalMetadataHolder positionalMetadata)
    {
        var charArray = arg.Value.ToCharArray();
        Array.Reverse(charArray);
        return new string(charArray).AsExpression();
    }
}

public class CustomEvaluator : IExpressionEvaluationScope
{
    private readonly static OrdinalInsensitiveDictionary<ExpressionFunction> AdditionalFunctions = new ExpressionFunction[] {
        new ReverseFunction(),
    }.ToOrdinalInsensitiveDictionary(x => x.Name);

    public FunctionEvaluator? TryGetFunctionEvaluator(string functionName)
        => AdditionalFunctions.TryGetValue(functionName, out var expressionFunction) ? expressionFunction.Evaluate : null;
}