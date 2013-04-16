using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace ExpressionTrees
{
    public static class ObjectExtensions
    {
        public static TResult TryFetch<T, TResult>(this T target, Expression<Func<T, TResult>> exp, TResult fallback)
            where TResult : class
        {
            return (TResult) GetValueOfExpression(target, exp.Body) ?? fallback;
        }

        public static TResult TryFetch<T, TResult>(this T target, Expression<Func<T, TResult>> exp)
        {
            return (TResult) GetValueOfExpression(target, exp.Body);
        }

        private static object GetValueOfExpression<T>(T target, Expression exp)
        {
            if (exp.NodeType == ExpressionType.Constant)
            {
                var constantExpression = (ConstantExpression) exp;
                return constantExpression.Value;
            }
            if (exp.NodeType == ExpressionType.Parameter)
                return target;

            if (exp.NodeType == ExpressionType.MemberAccess)
                return GetValueOfMemberExpression(target, (MemberExpression) exp);

            if (exp.NodeType == ExpressionType.Call)
                return GetValueOfMethodCallExpression(target, (MethodCallExpression) exp);

            throw new ArgumentException(@"The expression must contain only member access calls.", "exp");
        }

        private static object GetValueOfMethodCallExpression<T>(T target, MethodCallExpression callExpression)
        {
            var memberInfo = callExpression.Method;

            // Extension method 
            if (callExpression.Object == null)
                return GetValueOfExtensionMethod(target, callExpression, memberInfo);

            object parentValue = GetValueOfExpression(target, callExpression.Object);
            if (parentValue == null)
                return HandleParentValueIsNull(callExpression);

            var parameters = Evaluate(parentValue, callExpression.Arguments).ToArray();
            object result = memberInfo.Invoke(parentValue, parameters);

            return result;
        }

        private static object GetValueOfMemberExpression<T>(T target, MemberExpression memberExpression)
        {
            var parentValue = GetValueOfExpression(target, memberExpression.Expression);

            if (parentValue == null)
                return HandleParentValueIsNull(memberExpression);

            if (memberExpression.Member is PropertyInfo)
                return ((PropertyInfo) memberExpression.Member).GetValue(parentValue, null);

            return ((FieldInfo) memberExpression.Member).GetValue(parentValue);
        }

        private static object GetValueOfExtensionMethod<T>(T target, MethodCallExpression callExpression,
                                                           MethodInfo memberInfo)
        {
            var arg0 = callExpression.Arguments[0];

            object parentValue = GetValueOfExpression(target, arg0);

            if (parentValue == null)
                return HandleParentValueIsNull(callExpression);

            // extension method without arguments (eg. ToList())
            if (callExpression.Arguments.Count == 1)
                return memberInfo.Invoke(parentValue, new[] {parentValue});

            // assumes extension method contains Lambda (eg. Single(t => t.Id == 1), will fail otherwise...
            var lambda = callExpression.Arguments[1] as LambdaExpression;

            if (lambda == null)
                throw new ArgumentException(
                    @"The expression can only contain extension methods with lambda expressoins, eg. .Single(x => x.Id == 1).",
                    "callExpression");

            return memberInfo.Invoke(parentValue, new[] {parentValue, lambda.Compile()});
        }

        private static object HandleParentValueIsNull(Expression expression)
        {
            var type = expression.Type;
            if (type == typeof (string))
            {
                return string.Empty;
            }
            if (type.IsValueType)
            {
                return Activator.CreateInstance(type);
            }
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof (ISet<>))
            {
                var genericType = type.GetGenericArguments();
                var setType = typeof (HashSet<>).MakeGenericType(genericType);
                return Activator.CreateInstance(setType);
            }
            return null;
        }

        private static IEnumerable<object> Evaluate(object target, IEnumerable<Expression> arguments)
        {
            foreach (var expression in arguments)
            {
                yield return GetValueOfExpression(target, expression);
            }
        }
    }
}