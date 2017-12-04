using System;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

// ReSharper disable once CheckNamespace
namespace Hangfire.MaximumConcurrentExecutionsInfrastructure
{
    /// <summary>
    /// These are from hangfire but as they are internal we cannot reuse them do we depulicate
    /// See https://github.com/HangfireIO/Hangfire/blob/master/src/Hangfire.Core/Common/TypeExtensions.cs
    /// Copyright © 2013-2016 Sergey Odinokov.
    /// Hangfire is an Open Source project licensed under the terms of the LGPLv3 license. Please see http://www.gnu.org/licenses/lgpl-3.0.html for license text or COPYING.LESSER file distributed with the source code.
    /// </summary>
    internal static class TypeExtensions
    {
        public static string ToGenericTypeString(this Type type)
        {
            if (!type.GetTypeInfo().IsGenericType)
                return type.GetFullNameWithoutNamespace()
                    .ReplacePlusWithDotInNestedTypeName();

            return type.GetGenericTypeDefinition()
                .GetFullNameWithoutNamespace()
                .ReplacePlusWithDotInNestedTypeName()
                .ReplaceGenericParametersInGenericTypeName(type);
        }

        private static string GetFullNameWithoutNamespace(this Type type)
        {
            if (type.IsGenericParameter)
                return type.Name;

            const int dotLength = 1;

            // ReSharper disable once PossibleNullReferenceException
            return !string.IsNullOrEmpty(type.Namespace)
                ? type.FullName.Substring(type.Namespace.Length + dotLength)
                : type.FullName;
        }

        private static string ReplacePlusWithDotInNestedTypeName(this string typeName)
        {
            return typeName.Replace('+', '.');
        }

        private static string ReplaceGenericParametersInGenericTypeName(this string typeName, Type type)
        {
            var genericArguments = type.GetTypeInfo().GetAllGenericArguments();

            const string regexForGenericArguments = @"`[1-9]\d*";

            var rgx = new Regex(regexForGenericArguments);

            typeName = rgx.Replace(typeName, match =>
            {
                var currentGenericArgumentNumbers = int.Parse(match.Value.Substring(1));
                var currentArguments = string.Join(",",
                    genericArguments.Take(currentGenericArgumentNumbers).Select(ToGenericTypeString));
                genericArguments = genericArguments.Skip(currentGenericArgumentNumbers).ToArray();
                return string.Concat("<", currentArguments, ">");
            });

            return typeName;
        }

        private static Type[] GetAllGenericArguments(this TypeInfo type)
        {
            return type.GenericTypeArguments.Length > 0 ? type.GenericTypeArguments : type.GenericTypeParameters;
        }
    }
}