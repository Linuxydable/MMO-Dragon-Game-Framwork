// <auto-generated>
// THIS (.cs) FILE IS GENERATED BY MPC(MessagePack-CSharp). DO NOT CHANGE IT.
// </auto-generated>

#pragma warning disable 618
#pragma warning disable 612
#pragma warning disable 414
#pragma warning disable 168

#pragma warning disable SA1200 // Using directives should be placed correctly
#pragma warning disable SA1403 // File may only contain a single namespace
#pragma warning disable SA1649 // File name should match first type name

namespace MessagePack.Formatters.Mmogf.Core
{
    using System;
    using System.Buffers;
    using MessagePack;

    public sealed class CommandStatusFormatter : global::MessagePack.Formatters.IMessagePackFormatter<global::Mmogf.Core.CommandStatus>
    {
        public void Serialize(ref MessagePackWriter writer, global::Mmogf.Core.CommandStatus value, global::MessagePack.MessagePackSerializerOptions options)
        {
            writer.Write((Int32)value);
        }

        public global::Mmogf.Core.CommandStatus Deserialize(ref MessagePackReader reader, global::MessagePack.MessagePackSerializerOptions options)
        {
            return (global::Mmogf.Core.CommandStatus)reader.ReadInt32();
        }
    }
}

#pragma warning restore 168
#pragma warning restore 414
#pragma warning restore 618
#pragma warning restore 612

#pragma warning restore SA1200 // Using directives should be placed correctly
#pragma warning restore SA1403 // File may only contain a single namespace
#pragma warning restore SA1649 // File name should match first type name
