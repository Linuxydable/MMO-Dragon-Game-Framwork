// <auto-generated>
// THIS (.cs) FILE IS GENERATED BY MPC(MessagePack-CSharp). DO NOT CHANGE IT.
// </auto-generated>

#pragma warning disable 618
#pragma warning disable 612
#pragma warning disable 414
#pragma warning disable 168

#pragma warning disable SA1200 // Using directives should be placed correctly
#pragma warning disable SA1312 // Variable names should begin with lower-case letter
#pragma warning disable SA1649 // File name should match first type name

namespace MessagePack.Resolvers
{
    using System;

    public class GeneratedResolver : global::MessagePack.IFormatterResolver
    {
        public static readonly global::MessagePack.IFormatterResolver Instance = new GeneratedResolver();

        private GeneratedResolver()
        {
        }

        public global::MessagePack.Formatters.IMessagePackFormatter<T> GetFormatter<T>()
        {
            return FormatterCache<T>.Formatter;
        }

        private static class FormatterCache<T>
        {
            internal static readonly global::MessagePack.Formatters.IMessagePackFormatter<T> Formatter;

            static FormatterCache()
            {
                var f = GeneratedResolverGetFormatterHelper.GetFormatter(typeof(T));
                if (f != null)
                {
                    Formatter = (global::MessagePack.Formatters.IMessagePackFormatter<T>)f;
                }
            }
        }
    }

    internal static class GeneratedResolverGetFormatterHelper
    {
        private static readonly global::System.Collections.Generic.Dictionary<Type, int> lookup;

        static GeneratedResolverGetFormatterHelper()
        {
            lookup = new global::System.Collections.Generic.Dictionary<Type, int>(19)
            {
                { typeof(global::System.Collections.Generic.Dictionary<int, byte[]>), 0 },
                { typeof(global::Mmogf.Core.CommandStatus), 1 },
                { typeof(global::Dragongf.Assets.MmogfMessages.GameLogic.Cannon), 2 },
                { typeof(global::Dragongf.Assets.MmogfMessages.GameLogic.Cannon.FireCommand), 3 },
                { typeof(global::Dragongf.Assets.MmogfMessages.GameLogic.Cannon.FireEvent), 4 },
                { typeof(global::Mmogf.Core.ChangeInterestArea), 5 },
                { typeof(global::Mmogf.Core.ClientConnect), 6 },
                { typeof(global::Mmogf.Core.CommandRequest), 7 },
                { typeof(global::Mmogf.Core.CommandResponse), 8 },
                { typeof(global::Mmogf.Core.EntityInfo), 9 },
                { typeof(global::Mmogf.Core.EntityType), 10 },
                { typeof(global::Mmogf.Core.EntityUpdate), 11 },
                { typeof(global::Mmogf.Core.EventData), 12 },
                { typeof(global::Mmogf.Core.GameData), 13 },
                { typeof(global::Mmogf.Core.Position), 14 },
                { typeof(global::Mmogf.Core.Rotation), 15 },
                { typeof(global::Mmogf.Core.ServerConnect), 16 },
                { typeof(global::Mmogf.Core.SimpleMessage), 17 },
                { typeof(global::Mmogf.Core.World.CreateEntity), 18 },
            };
        }

        internal static object GetFormatter(Type t)
        {
            int key;
            if (!lookup.TryGetValue(t, out key))
            {
                return null;
            }

            switch (key)
            {
                case 0: return new global::MessagePack.Formatters.DictionaryFormatter<int, byte[]>();
                case 1: return new MessagePack.Formatters.Mmogf.Core.CommandStatusFormatter();
                case 2: return new MessagePack.Formatters.Dragongf.Assets.MmogfMessages.GameLogic.CannonFormatter();
                case 3: return new MessagePack.Formatters.Dragongf.Assets.MmogfMessages.GameLogic.Cannon_FireCommandFormatter();
                case 4: return new MessagePack.Formatters.Dragongf.Assets.MmogfMessages.GameLogic.Cannon_FireEventFormatter();
                case 5: return new MessagePack.Formatters.Mmogf.Core.ChangeInterestAreaFormatter();
                case 6: return new MessagePack.Formatters.Mmogf.Core.ClientConnectFormatter();
                case 7: return new MessagePack.Formatters.Mmogf.Core.CommandRequestFormatter();
                case 8: return new MessagePack.Formatters.Mmogf.Core.CommandResponseFormatter();
                case 9: return new MessagePack.Formatters.Mmogf.Core.EntityInfoFormatter();
                case 10: return new MessagePack.Formatters.Mmogf.Core.EntityTypeFormatter();
                case 11: return new MessagePack.Formatters.Mmogf.Core.EntityUpdateFormatter();
                case 12: return new MessagePack.Formatters.Mmogf.Core.EventDataFormatter();
                case 13: return new MessagePack.Formatters.Mmogf.Core.GameDataFormatter();
                case 14: return new MessagePack.Formatters.Mmogf.Core.PositionFormatter();
                case 15: return new MessagePack.Formatters.Mmogf.Core.RotationFormatter();
                case 16: return new MessagePack.Formatters.Mmogf.Core.ServerConnectFormatter();
                case 17: return new MessagePack.Formatters.Mmogf.Core.SimpleMessageFormatter();
                case 18: return new MessagePack.Formatters.Mmogf.Core.World_CreateEntityFormatter();
                default: return null;
            }
        }
    }
}

#pragma warning restore 168
#pragma warning restore 414
#pragma warning restore 618
#pragma warning restore 612

#pragma warning restore SA1312 // Variable names should begin with lower-case letter
#pragma warning restore SA1200 // Using directives should be placed correctly
#pragma warning restore SA1649 // File name should match first type name
