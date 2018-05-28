using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json.Serialization;

namespace RemoteInspector.Server
{
    public class IgnorePropertiesContractResolver : DefaultContractResolver
    {
        protected override List<MemberInfo> GetSerializableMembers( Type objectType )
        {
            return objectType.GetFields( BindingFlags.Instance | BindingFlags.Public ).Cast<MemberInfo>().ToList();
        }
    }
}