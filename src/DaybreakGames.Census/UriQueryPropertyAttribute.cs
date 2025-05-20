using System;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace DaybreakGames.Census
{
    [AttributeUsage(AttributeTargets.Property)]
    internal class UriQueryPropertyAttribute : Attribute
    {
        public string Name { get; set; }

        public UriQueryPropertyAttribute([CallerMemberName] string name = null)
        {
            Name = JsonNamingPolicy.CamelCase.ConvertName(name);
        }
    }
}
