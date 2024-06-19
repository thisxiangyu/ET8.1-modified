using System;
using System.ComponentModel;
using System.Threading.Tasks;
using MemoryPack;

namespace ET
{
    public static partial class MemoryPackHelper
    {
        public static void SerializeAsync(object obj, MemoryBuffer stream)
        {
            if (obj is ISupportInitialize supportInitialize)
            {
                supportInitialize.BeginInit();
            }
            ValueTask vt = MemoryPackSerializer.SerializeAsync(obj.GetType(), stream, obj);
            //MongoHelper.Serialize(obj, stream);
        }
    }
}