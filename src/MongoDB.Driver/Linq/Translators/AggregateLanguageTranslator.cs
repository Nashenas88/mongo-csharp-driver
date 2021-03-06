﻿/* Copyright 2015 MongoDB Inc.
*
* Licensed under the Apache License, Version 2.0 (the "License");
* you may not use this file except in compliance with the License.
* You may obtain a copy of the License at
*
* http://www.apache.org/licenses/LICENSE-2.0
*
* Unless required by applicable law or agreed to in writing, software
* distributed under the License is distributed on an "AS IS" BASIS,
* WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
* See the License for the specific language governing permissions and
* limitations under the License.
* 
*/

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using MongoDB.Bson;
using MongoDB.Driver.Linq.Expressions;
using MongoDB.Driver.Linq.Expressions.ResultOperators;
using MongoDB.Driver.Linq.Processors;
using MongoDB.Driver.Support;

namespace MongoDB.Driver.Linq.Translators
{
    internal sealed class AggregateLanguageTranslator
    {
        public static BsonValue Translate(Expression node)
        {
            var builder = new AggregateLanguageTranslator();
            return builder.TranslateValue(node);
        }

        private AggregateLanguageTranslator()
        {
        }

        private BsonValue TranslateValue(Expression node)
        {
            switch (node.NodeType)
            {
                case ExpressionType.Add:
                case ExpressionType.AddChecked:
                    return TranslateAdd((BinaryExpression)node);
                case ExpressionType.And:
                case ExpressionType.AndAlso:
                    return TranslateOperation((BinaryExpression)node, "$and", true);
                case ExpressionType.ArrayLength:
                    return TranslateArrayLength(node);
                case ExpressionType.Call:
                    return TranslateMethodCall((MethodCallExpression)node);
                case ExpressionType.Coalesce:
                    return TranslateOperation((BinaryExpression)node, "$ifNull", false);
                case ExpressionType.Conditional:
                    return TranslateConditional((ConditionalExpression)node);
                case ExpressionType.Constant:
                    return TranslateConstant(node);
                case ExpressionType.Convert:
                case ExpressionType.ConvertChecked:
                    return TranslateValue(((UnaryExpression)node).Operand);
                case ExpressionType.Divide:
                    return TranslateOperation((BinaryExpression)node, "$divide", false);
                case ExpressionType.Equal:
                    return TranslateOperation((BinaryExpression)node, "$eq", false);
                case ExpressionType.GreaterThan:
                    return TranslateOperation((BinaryExpression)node, "$gt", false);
                case ExpressionType.GreaterThanOrEqual:
                    return TranslateOperation((BinaryExpression)node, "$gte", false);
                case ExpressionType.LessThan:
                    return TranslateOperation((BinaryExpression)node, "$lt", false);
                case ExpressionType.LessThanOrEqual:
                    return TranslateOperation((BinaryExpression)node, "$lte", false);
                case ExpressionType.MemberAccess:
                    return TranslateMemberAccess((MemberExpression)node);
                case ExpressionType.MemberInit:
                    return TranslateMemberInit((MemberInitExpression)node);
                case ExpressionType.Modulo:
                    return TranslateOperation((BinaryExpression)node, "$mod", false);
                case ExpressionType.Multiply:
                case ExpressionType.MultiplyChecked:
                    return TranslateOperation((BinaryExpression)node, "$multiply", true);
                case ExpressionType.New:
                    return TranslateNew((NewExpression)node);
                case ExpressionType.Not:
                    return TranslateNot((UnaryExpression)node);
                case ExpressionType.NotEqual:
                    return TranslateOperation((BinaryExpression)node, "$ne", false);
                case ExpressionType.Or:
                case ExpressionType.OrElse:
                    return TranslateOperation((BinaryExpression)node, "$or", true);
                case ExpressionType.Subtract:
                case ExpressionType.SubtractChecked:
                    return TranslateOperation((BinaryExpression)node, "$subtract", false);
                case ExpressionType.Extension:
                    var extensionExpression = node as ExtensionExpression;
                    if (extensionExpression != null)
                    {
                        switch (extensionExpression.ExtensionType)
                        {
                            case ExtensionExpressionType.Accumulator:
                                return TranslateAccumulator((AccumulatorExpression)node);
                            case ExtensionExpressionType.Except:
                                return TranslateExcept((ExceptExpression)node);
                            case ExtensionExpressionType.FieldAsDocument:
                                return TranslateDocumentWrappedField((FieldAsDocumentExpression)node);
                            case ExtensionExpressionType.Field:
                                return TranslateField((FieldExpression)node);
                            case ExtensionExpressionType.GroupingKey:
                                return TranslateGroupingKey((GroupingKeyExpression)node);
                            case ExtensionExpressionType.Intersect:
                                return TranslateIntersect((IntersectExpression)node);
                            case ExtensionExpressionType.Pipeline:
                                return TranslatePipeline((PipelineExpression)node);
                            case ExtensionExpressionType.Select:
                                return TranslateSelect((SelectExpression)node);
                            case ExtensionExpressionType.Union:
                                return TranslateUnion((UnionExpression)node);
                            case ExtensionExpressionType.Where:
                                return TranslateWhere((WhereExpression)node);
                        }
                    }
                    break;
            }

            var message = string.Format("$project or $group does not support {0}.",
                node.ToString());
            throw new NotSupportedException(message);
        }

        private BsonValue TranslateAdd(BinaryExpression node)
        {
            var op = "$add";
            if (node.Left.Type == typeof(string))
            {
                op = "$concat";
            }

            return TranslateOperation(node, op, true);
        }

        private BsonValue TranslateAccumulator(AccumulatorExpression node)
        {
            switch (node.AccumulatorType)
            {
                case AccumulatorType.AddToSet:
                    return new BsonDocument("$addToSet", TranslateValue(node.Argument));
                case AccumulatorType.Average:
                    return new BsonDocument("$avg", TranslateValue(node.Argument));
                case AccumulatorType.First:
                    return new BsonDocument("$first", TranslateValue(node.Argument));
                case AccumulatorType.Last:
                    return new BsonDocument("$last", TranslateValue(node.Argument));
                case AccumulatorType.Max:
                    return new BsonDocument("$max", TranslateValue(node.Argument));
                case AccumulatorType.Min:
                    return new BsonDocument("$min", TranslateValue(node.Argument));
                case AccumulatorType.Push:
                    return new BsonDocument("$push", TranslateValue(node.Argument));
                case AccumulatorType.Sum:
                    return new BsonDocument("$sum", TranslateValue(node.Argument));
            }

            // we should never ever get here.
            var message = string.Format("Unrecognized aggregation type in the expression tree {0}.",
                node.ToString());
            throw new MongoInternalException(message);
        }

        private BsonValue TranslateArrayLength(Expression node)
        {
            return new BsonDocument("$size", TranslateValue(((UnaryExpression)node).Operand));
        }

        private BsonValue TranslateConditional(ConditionalExpression node)
        {
            var condition = TranslateValue(node.Test);
            var truePart = TranslateValue(node.IfTrue);
            var falsePart = TranslateValue(node.IfFalse);

            return new BsonDocument("$cond", new BsonArray(new[] { condition, truePart, falsePart }));
        }

        private static BsonValue TranslateConstant(Expression node)
        {
            var value = BsonValue.Create(((ConstantExpression)node).Value);
            var stringValue = value as BsonString;
            if (stringValue != null && stringValue.Value.StartsWith("$"))
            {
                value = new BsonDocument("$literal", value);
            }
            // NOTE: there may be other instances where we should use a literal...
            // but I can't think of any yet.
            return value;
        }

        private BsonValue TranslateDocumentWrappedField(FieldAsDocumentExpression expression)
        {
            return new BsonDocument(expression.FieldName, TranslateValue(expression.Expression));
        }

        private BsonValue TranslateExcept(ExceptExpression node)
        {
            return new BsonDocument("$setDifference", new BsonArray(new[]
            {
                TranslateValue(node.Source),
                TranslateValue(node.Other)
            }));
        }

        private BsonValue TranslateField(FieldExpression expression)
        {
            return "$" + expression.FieldName;
        }

        private BsonValue TranslateGroupingKey(GroupingKeyExpression node)
        {
            return TranslateValue(node.Expression);
        }

        private BsonValue TranslateIntersect(IntersectExpression node)
        {
            return new BsonDocument("$setIntersection", new BsonArray(new[]
            {
                TranslateValue(node.Source),
                TranslateValue(node.Other)
            }));
        }

        private BsonValue TranslateMemberAccess(MemberExpression node)
        {
            BsonValue result;
            if (node.Expression.Type == typeof(DateTime)
                && TryTranslateDateTimeMemberAccess(node, out result))
            {
                return result;
            }

            if (node.Expression != null
                && (node.Expression.Type.ImplementsInterface(typeof(ICollection<>))
                    || node.Expression.Type.ImplementsInterface(typeof(ICollection)))
                && node.Member.Name == "Count")
            {
                return new BsonDocument("$size", TranslateValue(node.Expression));
            }

            var message = string.Format("Member {0} of type {1} in the expression tree {2} cannot be translated.",
                node.Member.Name,
                node.Member.DeclaringType,
                node.ToString());
            throw new NotSupportedException(message);
        }

        private BsonValue TranslateMethodCall(MethodCallExpression node)
        {
            BsonValue result;
            if (node.Object == null
                && node.Method.DeclaringType == typeof(string)
                && TryTranslateStaticStringMethodCall(node, out result))
            {
                return result;
            }

            if (node.Object != null
                && node.Object.Type == typeof(string)
                && TryTranslateStringMethodCall(node, out result))
            {
                return result;
            }

            if (node.Object != null
                && node.Object.Type.IsGenericType
                && node.Object.Type.GetGenericTypeDefinition() == typeof(HashSet<>)
                && TryTranslateHashSetMethodCall(node, out result))
            {
                return result;
            }

            if (node.Object != null
                && node.Method.Name == "CompareTo"
                && (node.Object.Type.ImplementsInterface(typeof(IComparable<>))
                    || node.Object.Type.ImplementsInterface(typeof(IComparable))))
            {
                return new BsonDocument("$cmp", new BsonArray(new[] { TranslateValue(node.Object), TranslateValue(node.Arguments[0]) }));
            }

            if (node.Object != null
                && node.Method.Name == "Equals"
                && node.Arguments.Count == 1)
            {
                return new BsonDocument("$eq", new BsonArray(new[] { TranslateValue(node.Object), TranslateValue(node.Arguments[0]) }));
            }

            var message = string.Format("{0} of type {1} is not supported in the expression tree {2}.",
                node.Method.Name,
                node.Method.DeclaringType,
                node.ToString());
            throw new NotSupportedException(message);
        }

        private BsonValue TranslateMemberInit(MemberInitExpression node)
        {
            var mapping = ProjectionMapper.Map(node);
            return TranslateMapping(mapping);
        }

        private BsonValue TranslateNew(NewExpression node)
        {
            var mapping = ProjectionMapper.Map(node);
            return TranslateMapping(mapping);
        }

        private BsonValue TranslateMapping(ProjectionMapping mapping)
        {
            BsonDocument doc = new BsonDocument();
            bool hasId = false;
            foreach (var memberMapping in mapping.Members)
            {
                var value = TranslateValue(memberMapping.Expression);
                string name = memberMapping.Member.Name;
                if (!hasId && memberMapping.Expression is GroupingKeyExpression)
                {
                    name = "_id";
                    hasId = true;
                    doc.InsertAt(0, new BsonElement(name, value));
                }
                else
                {
                    doc.Add(name, value);
                }
            }

            return doc;
        }

        private BsonValue TranslateNot(UnaryExpression node)
        {
            var operand = TranslateValue(node.Operand);
            if (!operand.IsBsonArray)
            {
                operand = new BsonArray().Add(operand);
            }
            return new BsonDocument("$not", operand);
        }

        private BsonValue TranslateOperation(BinaryExpression node, string op, bool canBeFlattened)
        {
            var left = TranslateValue(node.Left);
            var right = TranslateValue(node.Right);

            // some operations take an array as the argument.
            // we want to flatten binary values into the top-level 
            // array if they are flattenable :).
            if (canBeFlattened && left.IsBsonDocument && left.AsBsonDocument.Contains(op) && left[op].IsBsonArray)
            {
                left[op].AsBsonArray.Add(right);
                return left;
            }

            return new BsonDocument(op, new BsonArray(new[] { left, right }));
        }

        private BsonValue TranslatePipeline(PipelineExpression node)
        {
            if (node.ResultOperator == null)
            {
                return TranslateValue(node.Source);
            }

            BsonValue result;
            if (TryTranslateAllResultOperator(node, out result) ||
                TryTranslateAnyResultOperator(node, out result) ||
                TryTranslateContainsResultOperator(node, out result) ||
                TryTranslateCountResultOperator(node, out result))
            {
                return result;
            }

            var message = string.Format("The result operation {0} is not supported.", node.ResultOperator.GetType());
            throw new NotSupportedException(message);
        }

        private BsonValue TranslateSelect(SelectExpression node)
        {
            if (node.Source is IFieldExpression && node.Selector is IFieldExpression)
            {
                var prefixName = ((IFieldExpression)node.Source).FieldName;
                return TranslateValue(FieldNamePrefixer.Prefix(node.Selector, prefixName));
            }

            var inputValue = TranslateValue(node.Source);
            var inValue = TranslateValue(FieldNamePrefixer.Prefix(node.Selector, "$" + node.ItemName));

            return new BsonDocument("$map", new BsonDocument
            {
                { "input", inputValue },
                { "as", node.ItemName },
                { "in", inValue}
            });
        }

        private BsonValue TranslateUnion(UnionExpression node)
        {
            return new BsonDocument("$setUnion", new BsonArray(new[]
            {
                TranslateValue(node.Source),
                TranslateValue(node.Other)
            }));
        }

        private BsonValue TranslateWhere(WhereExpression node)
        {
            var inputValue = TranslateValue(node.Source);
            var condValue = TranslateValue(FieldNamePrefixer.Prefix(node.Predicate, "$" + node.ItemName));

            return new BsonDocument("$filter", new BsonDocument
            {
                { "input", inputValue },
                { "as", node.ItemName },
                { "cond", condValue }
            });
        }

        private bool TryTranslateAllResultOperator(PipelineExpression node, out BsonValue result)
        {
            var resultOperator = node.ResultOperator as AllResultOperator;
            if (resultOperator != null)
            {
                var whereExpression = node.Source as WhereExpression;

                if (whereExpression != null)
                {
                    var inValue = TranslateValue(FieldNamePrefixer.Prefix(whereExpression.Predicate, "$" + whereExpression.ItemName));

                    result = new BsonDocument("$map", new BsonDocument
                    {
                        { "input", TranslateValue(whereExpression.Source) },
                        { "as", whereExpression.ItemName },
                        { "in", inValue}
                    });

                    result = new BsonDocument("$allElementsTrue", result);
                    return true;
                }
            }

            result = null;
            return false;
        }

        private bool TryTranslateAnyResultOperator(PipelineExpression node, out BsonValue result)
        {
            var resultOperator = node.ResultOperator as AnyResultOperator;
            if (resultOperator != null)
            {
                var whereExpression = node.Source as WhereExpression;

                if (whereExpression == null)
                {
                    result = new BsonDocument("$gt", new BsonArray(new BsonValue[]
                {
                    new BsonDocument("$size", TranslateValue(node.Source)),
                    0
                }));
                    return true;
                }
                else
                {
                    var inValue = TranslateValue(FieldNamePrefixer.Prefix(whereExpression.Predicate, "$" + whereExpression.ItemName));

                    result = new BsonDocument("$map", new BsonDocument
                    {
                        { "input", TranslateValue(whereExpression.Source) },
                        { "as", whereExpression.ItemName },
                        { "in", inValue}
                    });

                    result = new BsonDocument("$anyElementTrue", result);
                    return true;
                }
            }

            result = null;
            return false;
        }

        private bool TryTranslateContainsResultOperator(PipelineExpression node, out BsonValue result)
        {
            var resultOperator = node.ResultOperator as ContainsResultOperator;
            if (resultOperator != null)
            {
                var source = TranslateValue(node.Source);
                var value = TranslateValue(resultOperator.Value);

                result = new BsonDocument("$anyElementTrue", new BsonDocument("$map", new BsonDocument
                {
                    { "input", source },
                    { "as", "x" },
                    { "in", new BsonDocument("$eq", new BsonArray(new [] { "$$x", value})) }
                }));
                return true;
            }

            result = null;
            return false;
        }

        private bool TryTranslateCountResultOperator(PipelineExpression node, out BsonValue result)
        {
            var resultOperator = node.ResultOperator as CountResultOperator;
            if (resultOperator != null)
            {
                result = new BsonDocument("$size", TranslateValue(node.Source));
                return true;
            }

            result = null;
            return false;
        }

        private bool TryTranslateDateTimeMemberAccess(MemberExpression node, out BsonValue result)
        {
            result = null;
            var field = TranslateValue(node.Expression);
            switch (node.Member.Name)
            {
                case "Day":
                    result = new BsonDocument("$dayOfMonth", field);
                    return true;
                case "DayOfWeek":
                    // The server's day of week values are 1 greater than
                    // .NET's DayOfWeek enum values
                    result = new BsonDocument("$subtract", new BsonArray
                        {
                            new BsonDocument("$dayOfWeek", field),
                            (BsonInt32)1
                        });
                    return true;
                case "DayOfYear":
                    result = new BsonDocument("$dayOfYear", field);
                    return true;
                case "Hour":
                    result = new BsonDocument("$hour", field);
                    return true;
                case "Millisecond":
                    result = new BsonDocument("$millisecond", field);
                    return true;
                case "Minute":
                    result = new BsonDocument("$minute", field);
                    return true;
                case "Month":
                    result = new BsonDocument("$month", field);
                    return true;
                case "Second":
                    result = new BsonDocument("$second", field);
                    return true;
                case "Year":
                    result = new BsonDocument("$year", field);
                    return true;
            }

            return false;
        }

        private bool TryTranslateHashSetMethodCall(MethodCallExpression node, out BsonValue result)
        {
            result = null;
            switch (node.Method.Name)
            {
                case "IsSubsetOf":
                    result = new BsonDocument("$setIsSubset", new BsonArray(new[]
                        {
                            TranslateValue(node.Object),
                            TranslateValue(node.Arguments[0])
                        }));
                    return true;
                case "SetEquals":
                    result = new BsonDocument("$setEquals", new BsonArray(new[]
                        {
                            TranslateValue(node.Object),
                            TranslateValue(node.Arguments[0])
                        }));
                    return true;
            }

            return false;
        }

        private bool TryTranslateStaticStringMethodCall(MethodCallExpression node, out BsonValue result)
        {
            result = null;
            switch (node.Method.Name)
            {
                case "IsNullOrEmpty":
                    var field = TranslateValue(node.Arguments[0]);
                    result = new BsonDocument("$or",
                        new BsonArray
                        {
                            new BsonDocument("$eq", new BsonArray { field, BsonNull.Value }),
                            new BsonDocument("$eq", new BsonArray { field, BsonString.Empty })
                        });
                    return true;
            }

            return false;
        }

        private bool TryTranslateStringMethodCall(MethodCallExpression node, out BsonValue result)
        {
            result = null;
            var field = TranslateValue(node.Object);
            switch (node.Method.Name)
            {
                case "Equals":
                    if (node.Arguments.Count == 2 && node.Arguments[1].NodeType == ExpressionType.Constant)
                    {
                        var comparisonType = (StringComparison)((ConstantExpression)node.Arguments[1]).Value;
                        switch (comparisonType)
                        {
                            case StringComparison.OrdinalIgnoreCase:
                                result = new BsonDocument("$eq",
                                    new BsonArray(new BsonValue[]
                                        {
                                            new BsonDocument("$strcasecmp", new BsonArray(new[] { field, TranslateValue(node.Arguments[0]) })),
                                            0
                                        }));
                                return true;
                            case StringComparison.Ordinal:
                                result = new BsonDocument("$eq", new BsonArray(new[] { field, TranslateValue(node.Arguments[0]) }));
                                return true;
                            default:
                                throw new NotSupportedException("Only Ordinal and OrdinalIgnoreCase are supported for string comparisons.");
                        }
                    }
                    break;
                case "Substring":
                    if (node.Arguments.Count == 2)
                    {
                        result = new BsonDocument("$substr", new BsonArray(new[]
                            {
                                field,
                                TranslateValue(node.Arguments[0]),
                                TranslateValue(node.Arguments[1])
                            }));
                        return true;
                    }
                    break;
                case "ToLower":
                case "ToLowerInvariant":
                    if (node.Arguments.Count == 0)
                    {
                        result = new BsonDocument("$toLower", field);
                        return true;
                    }
                    break;
                case "ToUpper":
                case "ToUpperInvariant":
                    if (node.Arguments.Count == 0)
                    {
                        result = new BsonDocument("$toUpper", field);
                        return true;
                    }
                    break;
            }

            return false;
        }

        private static bool TryFindSerializationExpression(MethodCallExpression node, out ISerializationExpression serializationExpression)
        {
            var current = node.Arguments[0];
            serializationExpression = current as ISerializationExpression;
            if (serializationExpression == null &&
                current.NodeType == ExpressionType.Call &&
                ExpressionHelper.IsLinqMethod((MethodCallExpression)current))
            {
                current = ((MethodCallExpression)current).Arguments[0];
                serializationExpression = current as ISerializationExpression;
            }

            return serializationExpression != null;
        }
    }
}
