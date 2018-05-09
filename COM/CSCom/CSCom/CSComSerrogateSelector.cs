using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace CSCom
{
    public class CSComSerrogateSelector : SerializationBinder
    {
        public Dictionary<string, Type> Mapped { get; private set; } = new Dictionary<string, Type>();

        public override Type BindToType(string assemblyName, string typeName)
        {
            if (!Mapped.ContainsKey(typeName))
                Mapped[typeName] = Assembly.GetExecutingAssembly().GetType(typeName);
            return Mapped[typeName];
            //return typeof(object);
        }
    }
}
