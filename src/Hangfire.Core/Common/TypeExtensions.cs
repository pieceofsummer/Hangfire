using Hangfire.Server;
using System;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Hangfire.Common
{
    internal static class TypeExtensions
    {
        public static string ToGenericTypeString(this Type type)
        {
            if (!type.GetTypeInfo().IsGenericType)
            {
                return type.GetFullNameWithoutNamespace()
                        .ReplacePlusWithDotInNestedTypeName();
            }

            return type.GetGenericTypeDefinition()
                    .GetFullNameWithoutNamespace()
                    .ReplacePlusWithDotInNestedTypeName()
                    .ReplaceGenericParametersInGenericTypeName(type);
        }

        private static string GetFullNameWithoutNamespace(this Type type)
        {
            if (type.IsGenericParameter)
            {
                return type.Name;
            }

            const int dotLength = 1;
            // ReSharper disable once PossibleNullReferenceException
            return !String.IsNullOrEmpty(type.Namespace)
                ? type.FullName.Substring(type.Namespace.Length + dotLength)
                : type.FullName;
        }

        private static string ReplacePlusWithDotInNestedTypeName(this string typeName)
        {
            return typeName.Replace('+', '.');
        }

        private static string ReplaceGenericParametersInGenericTypeName(this string typeName, Type type)
        {
            var genericArguments = type .GetTypeInfo().GetAllGenericArguments();

            const string regexForGenericArguments = @"`[1-9]\d*";

            var rgx = new Regex(regexForGenericArguments);

            typeName = rgx.Replace(typeName, match =>
            {
                var currentGenericArgumentNumbers = int.Parse(match.Value.Substring(1));
                var currentArguments = string.Join(",", genericArguments.Take(currentGenericArgumentNumbers).Select(ToGenericTypeString));
                genericArguments = genericArguments.Skip(currentGenericArgumentNumbers).ToArray();
                return string.Concat("<", currentArguments, ">");
            });

            return typeName;
        }

        public static Type[] GetAllGenericArguments(this TypeInfo type)
        {
            return type.GenericTypeArguments.Length > 0 ? type.GenericTypeArguments : type.GenericTypeParameters;
        }
        
        public static Type GetTaskResultType(this Type type)
        {
            if (!typeof(Task).IsAssignableFrom(type))
            {
                // not a task type
                return null;
            }

            if (!type.IsConstructedGenericType)
            {
                // not a genetic type, hence result type is void
                return typeof(void);
            }

            if (type.GetGenericTypeDefinition() != typeof(Task<>))
            {
                // Subclass of Task, but not a Task<T> ?! Very, very unlikely, but still...
                throw new InvalidOperationException($"Don't know how to handle Task type {type}");
            }

            return type.GetGenericArguments()[0];
        }

        #region GetTaskResult implementation

        private static T GetTaskResultImpl<T>(Task task) => ((Task<T>)task).Result;
        
        private static readonly MethodInfo GetTaskResultMethod = typeof(TypeExtensions).GetMethod(
                     nameof(TypeExtensions.GetTaskResultImpl), BindingFlags.Static | BindingFlags.NonPublic);

        private static object GetTaskResult(Task task, Type resultType)
        {
            if (resultType == null || resultType == typeof(void))
            {
                // treat this like a typeless task
                return null;
            }

            var accessor = GetTaskResultMethod.MakeGenericMethod(resultType);
            return accessor.Invoke(null, new object[1] { task });
        }

        #endregion

        /// <summary>
        /// Converts an arbitra <paramref name="task"/> into <see cref="Task{Object}"/> upon completion.
        /// Although task is defined as <see cref="Task"/>, it can be any <see cref="Task{TResult}"/> 
        /// which type we know only at runtime.
        /// </summary>
        /// <param name="task">Task to convert</param>
        /// <param name="resultType">Type argument for <see cref="Task{TResult}"/>, or null/void for <see cref="Task"/>.</param>
        /// <returns></returns>
        public static Task<object> ContinueWithCast(this Task task, Type resultType)
        { 
            var tcs = new TaskCompletionSource<object>();

            task.ContinueWith(t =>
            {
                if (t.IsFaulted)
                {
                    var ex = t.Exception.InnerException ?? t.Exception;
                    tcs.TrySetException(new JobPerformanceException(ex.Message, ex));
                }
                else if (t.IsCanceled)
                {
                    tcs.SetCanceled();
                }
                else
                {
                    tcs.TrySetResult(GetTaskResult(t, resultType));
                }
            }, TaskContinuationOptions.ExecuteSynchronously);

            return tcs.Task;
        }
    }
}
