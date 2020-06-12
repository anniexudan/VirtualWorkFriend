using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace VirtualWorkFriendBot.Helpers
{
    public static class EnumHelpers
    {
        public static T GetValueFromDescription<T>(this string description) where T : Enum
        {
            var type = typeof(T);
            if (!type.IsEnum) throw new InvalidOperationException();
            foreach (var field in type.GetFields())
            {
                var attribute = Attribute.GetCustomAttribute(field,
                    typeof(DescriptionAttribute)) as DescriptionAttribute;
                if (attribute != null)
                {
                    if (String.Compare(attribute.Description, description, true) == 0)
                        return (T)field.GetValue(null);
                }
                else
                {
                    if (String.Compare(field.Name, description, true) == 0)
                        return (T)field.GetValue(null);
                }
            }
            throw new ArgumentException("Not found.", nameof(description));
            // or return default(T);
        }
        public static string GetDescription<T>(this T value) where T : Enum
        {
            FieldInfo field = value.GetType().GetField(value.ToString());

            DescriptionAttribute attribute
                    = Attribute.GetCustomAttribute(field, typeof(DescriptionAttribute))
                        as DescriptionAttribute;

            return attribute == null ? value.ToString() : attribute.Description;
        }

        public static IEnumerable<string> GetDescriptions<T>() where T : Enum
        {
            var result = Enum.GetValues(typeof(T))
                .Cast<T>().Select(v => GetDescription<T>(v));
            return result;
        }
        public static bool TryGetValueFromDescription<T>(this string description, out T value) where T : Enum
        {
            var success = false;
            value = default;
            try
            {
                value = GetValueFromDescription<T>(description);
                success = true;
            }
            catch (ArgumentException) { }
            return success;
        }
    }
}
