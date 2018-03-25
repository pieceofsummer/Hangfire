using Newtonsoft.Json;
using System;
using System.Reflection;
using System.Runtime.Serialization.Formatters;

namespace Hangfire.Common
{
    internal static class JsonSerializerSettingsExtensions
    {
        private static readonly PropertyInfo TypeNameAssemblyFormat = typeof(JsonSerializerSettings).GetRuntimeProperty(nameof(TypeNameAssemblyFormat));
        private static readonly PropertyInfo TypeNameAssemblyFormatHandling = typeof(JsonSerializerSettings).GetRuntimeProperty(nameof(TypeNameAssemblyFormatHandling));

        public static JsonSerializerSettings WithSimpleTypeNameAssemblyFormat(this JsonSerializerSettings settings)
        {
            if (settings == null)
                throw new ArgumentNullException(nameof(settings));
            
#if NETFULL
            settings.TypeNameAssemblyFormat = FormatterAssemblyStyle.Simple;
#else
            var property = TypeNameAssemblyFormatHandling ?? TypeNameAssemblyFormat;
            property.SetValue(settings, Enum.Parse(property.PropertyType, "Simple"));
#endif

            return settings;
        }
    }
}
