using MessagePack;
using Mmogf.Core;
using System.Collections.Generic;
namespace Mmogf.Core
{
    [MessagePackObject]
    public struct ServerConnect : IMessage
    {
        [Key(0)]
        public long ServerId { get; set; }
    }
}