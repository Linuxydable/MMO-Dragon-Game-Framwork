using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Google.Protobuf.WellKnownTypes;
using Lidgren.Network;
using MessagePack;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Mmogf.Core;
using Mmogf.Servers.Services;
//using Prometheus;

namespace MmoGameFramework
{
    public class MmoServer
    {
        public bool Active => s_server.Status == NetPeerStatus.Running;
        public string WorkerType => _config.AppIdentifier;

        //private readonly Gauge ConnectedWorkersGauge;

        private NetServer s_server;
        OrchestrationService _orchestrationService;
        private EntityStore _entities;
        NetPeerConfiguration _config;
        ILogger _logger;
        IConfiguration _configuration;

        bool _clientWorker = false;
        Stopwatch _stopwatch;
        int _tickRate;

        public Dictionary<long, WorkerConnection> _connections = new Dictionary<long, WorkerConnection>();

        public MmoServer(OrchestrationService orchestrationService, EntityStore entities, NetPeerConfiguration config, bool clientWorker, ILogger<MmoServer> logger, IConfiguration configuration)
        {

            _orchestrationService = orchestrationService;
            _entities = entities;
            _clientWorker = clientWorker;
            _configuration = configuration;
            _tickRate = _configuration.GetValue<int?>(key: "TickRate") ?? 60;
            //var description = "Number of connected workers.";
            //if(_clientWorker)
            //{
            //    description = "Number of connected clients.";
            //}
            //ConnectedWorkersGauge = Metrics.CreateGauge($"dragongf_{config.AppIdentifier.Replace('-', '_')}", description);
            _stopwatch = new Stopwatch();

            // set up network
            _config = config;
            s_server = new NetServer(_config);
            _logger = logger;

            _entities.OnUpdateEntity += OnEntityUpdate;
            _entities.OnEntityDelete += OnEntityDelete;
            _entities.OnEntityEvent += OnEntityEvent;
            _entities.OnEntityCommand += OnEntityCommand;
            _entities.OnEntityCommandResponse += OnEntityCommandResponse;
            _entities.OnUpdateEntityPartial += OnEntityUpdatePartial;

        }

        public void Start()
        {
            s_server.Start();

            var thread1 = new Thread(async () => await Loop());
            thread1.Priority = ThreadPriority.AboveNormal;
            thread1.Start();

        }

        public void Stop()
        {
            //send stop to peers
            s_server.Shutdown("End");
        }

        async Task Loop()
        {
            while (s_server.Status != NetPeerStatus.NotRunning)
            {
                _stopwatch.Restart();
                try
                {

                    NetIncomingMessage im;
                    while ((im = s_server.ReadMessage()) != null)
                    {
                        // handle incoming message
                        switch (im.MessageType)
                        {
                            case NetIncomingMessageType.DebugMessage:
                                string text = im.ReadString();
                                _logger.LogDebug(text);
                                break;
                            case NetIncomingMessageType.ErrorMessage:
                                string text2 = im.ReadString();
                                _logger.LogError(text2);
                                break;
                            case NetIncomingMessageType.WarningMessage:
                                string text3 = im.ReadString();
                                _logger.LogWarning(text3);
                                break;
                            case NetIncomingMessageType.VerboseDebugMessage:
                                string text4 = im.ReadString();
                                _logger.LogDebug(text4);
                                break;
                            case NetIncomingMessageType.StatusChanged:
                                NetConnectionStatus status = (NetConnectionStatus)im.ReadByte();

                                string reason = im.ReadString();
                                if(_logger.IsEnabled(LogLevel.Debug))
                                    _logger.LogDebug(im.SenderConnection.RemoteUniqueIdentifier + " " + status + ": " + reason);

                                if (status == NetConnectionStatus.Connected)
                                {
                                    //todo: do some sort of worker type validation from a config
                                    _connections.Add(im.SenderConnection.RemoteUniqueIdentifier, new WorkerConnection(im.SenderConnection.RemoteHailMessage.ReadString(), im.SenderConnection, new Position()));
                                        _logger.LogInformation("Remote hail: " + im.SenderConnection.RemoteHailMessage.ReadString());
                                    var message = new MmoMessage()
                                    {
                                        MessageId = ServerCodes.ClientConnect,
                                        Info = MessagePackSerializer.Serialize(new ClientConnect()
                                        {
                                            ClientId = im.SenderConnection.RemoteUniqueIdentifier,
                                        }),
                                    };
                                    NetOutgoingMessage om = s_server.CreateMessage();
                                    om.Write(MessagePackSerializer.Serialize(message));
                                    s_server.SendMessage(om, im.SenderConnection, NetDeliveryMethod.ReliableUnordered);
                                }
                                else if(status == NetConnectionStatus.Disconnected)
                                {
                                    _logger.LogInformation($"{_config.AppIdentifier} {im.SenderConnection.RemoteUniqueIdentifier} Disconnected");
                                    _connections.Remove(im.SenderConnection.RemoteUniqueIdentifier);
                                }

                                break;
                            case NetIncomingMessageType.Data:
                                //if(_logger.IsEnabled(LogLevel.Debug))
                                //    _logger.LogDebug(im.SenderConnection.RemoteUniqueIdentifier +" - '" + BitConverter.ToString(im.Data) + "'");
                                var simpleData = MessagePackSerializer.Deserialize<MmoMessage>(im.Data);
                                //if (_logger.IsEnabled(LogLevel.Debug))
                                //    _logger.LogDebug($"Serve Codes {simpleData.MessageId}");

                                switch (simpleData.MessageId)
                                {
                                    case ServerCodes.ChangeInterestArea:
                                        var interestArea = MessagePackSerializer.Deserialize<ChangeInterestArea>(simpleData.Info);
                                        WorkerConnection worker;
                                        if (_connections.TryGetValue(im.SenderConnection.RemoteUniqueIdentifier, out worker))
                                        {
                                            worker.InterestPosition = interestArea.Position.ToPosition();

                                            var entities = _entities.GetInArea(worker.InterestPosition,
                                                worker.InterestRange);

                                            var newEntityIds = new List<int>();

                                            foreach (var entityInfo in entities)
                                            {
                                                newEntityIds.Add(entityInfo.EntityId);
                                                worker.EntitiesInRange.Add(entityInfo.EntityId);

                                                //send of entities checkout change

                                                Send(im.SenderConnection, new MmoMessage()
                                                {
                                                    MessageId = ServerCodes.EntityInfo,
                                                    Info = MessagePackSerializer.Serialize(entityInfo.ToEntityInfo()),
                                                }, NetDeliveryMethod.ReliableUnordered);
                                            }

                                            Send(im.SenderConnection, new MmoMessage()
                                            {
                                                MessageId = ServerCodes.EntityCheckout,
                                                Info = MessagePackSerializer.Serialize(new EntityCheckout()
                                                {
                                                    Checkouts = newEntityIds,
                                                    Remove = false,
                                                }),
                                            }, NetDeliveryMethod.ReliableOrdered, 11);
                                        }

                                        break;
                                    case ServerCodes.EntityUpdate:
                                        HandleEntityUpdate(im, simpleData);
                                        break;
                                    case ServerCodes.EntityEvent:
                                        HandleEntityEvent(im, simpleData);
                                        break;
                                    case ServerCodes.EntityCommandRequest:
                                        HandleEntityCommand(im, simpleData);
                                        break;
                                    case ServerCodes.EntityCommandResponse:
                                        HandleEntityCommandResponse(im, simpleData);
                                        break;
                                    case ServerCodes.Ping:
                                        Send(im.SenderConnection, new MmoMessage()
                                        {
                                            MessageId = ServerCodes.Ping,
                                            Info = new byte[0],
                                        });
                                        break;
                                    default:
                                        // incoming chat message from a client
                                        //string chat = im.ReadString();

                                        break;
                                }
                                
                                break;
                            default:
                                _logger.LogError("Unhandled type: " + im.MessageType + " " + im.LengthBytes + " bytes " + im.DeliveryMethod + "|" + im.SequenceChannel);
                                break;
                        }
                        s_server.Recycle(im);
                    }

                    //ConnectedWorkersGauge.Set(s_server.ConnectionsCount);

                    if(s_server.ConnectionsCount > 0)
                    {
                        await _orchestrationService.ReadyAsync();
                    }

                    var time = _stopwatch.ElapsedMilliseconds;

                    var tickMilliseconds = 1000.0 / _tickRate;

                    if(time < tickMilliseconds)
                    {
                        int delay = (int)(tickMilliseconds - time);
                        if(delay > 0)
                        {
                            await Task.Delay(delay);
                        }
                    }
                }
                catch (Exception e)
                {
                    _logger.LogError(e, e.Message);
                }
            }
        }

        private void HandleEntityCommand(NetIncomingMessage im, MmoMessage simpleData)
        {
            //get command info
            var commandRequest = MessagePackSerializer.Deserialize<CommandRequest>(simpleData.Info);

            if(commandRequest.EntityId == 0 && commandRequest.ComponentId == 0)
            {
                //world command
                HandleWorldCommand(im, simpleData, commandRequest);
                return;
            }

            var entityInfo = _entities.GetEntity(commandRequest.EntityId);

            if (entityInfo == null)
                return;
            WorkerConnection worker;
            if (!_connections.TryGetValue(im.SenderConnection.RemoteUniqueIdentifier, out worker))
            {
                //send failure
                Send(im.SenderConnection, new MmoMessage() {
                    MessageId = ServerCodes.EntityCommandResponse,
                    Info = MessagePackSerializer.Serialize(CommandResponse.Create(commandRequest, CommandStatus.InvalidRequest, "No Worker Identified", null)),
                });
            }

            commandRequest.RequestorWorkerType = worker.ConnectionType;
            commandRequest.RequesterId = worker.Connection.RemoteUniqueIdentifier;

            //pass to authority aka worker

            _entities.SendCommand(commandRequest);

        }

        private void HandleWorldCommand(NetIncomingMessage im, MmoMessage message, CommandRequest commandRequest)
        {

            WorkerConnection worker;
            if (!_connections.TryGetValue(im.SenderConnection.RemoteUniqueIdentifier, out worker))
            {
                //send failure
                Send(im.SenderConnection, new MmoMessage()
                {
                    MessageId = ServerCodes.EntityCommandResponse,
                    Info = MessagePackSerializer.Serialize(CommandResponse.Create(commandRequest, CommandStatus.InvalidRequest, "No Worker Identified", null)),
                });
                return;
            }

            //todo: verify sender has permissions from config settings

            if (worker.ConnectionType != "Dragon-Worker")
            {
                Send(im.SenderConnection, new MmoMessage()
                {
                    MessageId = ServerCodes.EntityCommandResponse,
                    Info = MessagePackSerializer.Serialize(CommandResponse.Create(commandRequest, CommandStatus.InvalidRequest, "No permission to create entities.", null)),
                });
                return;
            }

            switch(commandRequest.CommandId)
            {
                case World.CreateEntity.CommandId:
                    var createEntity = MessagePackSerializer.Deserialize<World.CreateEntity>(commandRequest.Payload);

                    var requestPayload = createEntity.Request.Value;
                    var entityInfo = _entities.Create(requestPayload.EntityType, requestPayload.Position.ToPosition(), requestPayload.Rotation, requestPayload.Acls, null, requestPayload.Components);
                    _entities.UpdateEntity(entityInfo);
                    createEntity.Response = new NothingInternal();
                    Send(im.SenderConnection, new MmoMessage()
                    {
                        MessageId = ServerCodes.EntityCommandResponse,
                        Info = MessagePackSerializer.Serialize(CommandResponse.Create(commandRequest, CommandStatus.Success, "", MessagePackSerializer.Serialize(createEntity))),
                    }, NetDeliveryMethod.ReliableUnordered);
                    break;
                case World.DeleteEntity.CommandId:
                    var deleteEntity = MessagePackSerializer.Deserialize<World.DeleteEntity>(commandRequest.Payload);
                    var deleteRequestPayload = deleteEntity.Request.Value;
                    _entities.Delete(deleteRequestPayload.EntityId);
                    deleteEntity.Response = new NothingInternal();
                    Send(im.SenderConnection, new MmoMessage()
                    {
                        MessageId = ServerCodes.EntityCommandResponse,
                        Info = MessagePackSerializer.Serialize(CommandResponse.Create(commandRequest, CommandStatus.Success, "", MessagePackSerializer.Serialize(deleteEntity))),
                    }, NetDeliveryMethod.ReliableUnordered);
                    break;
            }
        }

        private void HandleEntityCommandResponse(NetIncomingMessage im, MmoMessage simpleData)
        {
            //get command info
            var commandResponse = MessagePackSerializer.Deserialize<CommandResponse>(simpleData.Info);
            var entityInfo = _entities.GetEntity(commandResponse.EntityId);

            if (entityInfo == null)
                return;

            //pass to authority aka worker

            _entities.SendCommandResponse(commandResponse);

        }

        void HandleEntityUpdate(NetIncomingMessage im, MmoMessage simpleData)
        {
            var entityUpdate = MessagePackSerializer.Deserialize<EntityUpdate>(simpleData.Info);
            var entity = _entities.GetEntity(entityUpdate.EntityId);

            if (entity == null)
                return;

            WorkerConnection worker;
            if (!_connections.TryGetValue(im.SenderConnection.RemoteUniqueIdentifier, out worker))
            {
                //disconnected??
                return;
            }
            var acls = entity.Value.Acls;
            if(!acls.CanWrite(entityUpdate.ComponentId, worker.ConnectionType))
            {
                //log
                return;
            }

            entity.Value.UpdateComponent(entityUpdate.ComponentId, entityUpdate.Info);
            //if (_logger.IsEnabled(LogLevel.Debug) && entityUpdate.ComponentId == Position.ComponentId)
            //{
            //    var position = MessagePackSerializer.Deserialize<Position>(entityUpdate.Info);
            //    _logger.LogDebug($"Entity: {entityInfo.Value.EntityId} position to {position.ToString()}");
            //}

            _entities.UpdateEntityPartial(entity.Value, entityUpdate, worker.Connection.RemoteUniqueIdentifier);

        }


        void HandleEntityEvent(NetIncomingMessage im, MmoMessage simpleData)
        {
            var eventRequest = MessagePackSerializer.Deserialize<EventRequest>(simpleData.Info);
            var entityInfo = _entities.GetEntity(eventRequest.EntityId);

            if (entityInfo == null)
                return;

            WorkerConnection worker;
            if (!_connections.TryGetValue(im.SenderConnection.RemoteUniqueIdentifier, out worker))
            {
                //disconnected??
                return;
            }
            var acls = entityInfo.Value.Acls;
            if (!acls.CanWrite(eventRequest.ComponentId, worker.ConnectionType))
            {
                //log
                return;
            }

            _entities.SendEvent(eventRequest);

        }

        public void Send(NetConnection connection, MmoMessage message, NetDeliveryMethod deliveryMethod = NetDeliveryMethod.Unreliable, int sequenceChannel = 0)
        {
            var bytes = MessagePackSerializer.Serialize(message);
            NetOutgoingMessage om = s_server.CreateMessage(bytes.Length);
            om.Write(bytes);
            s_server.SendMessage(om, connection, deliveryMethod, sequenceChannel);
        }

        public void SendCheckedout(int entityId, MmoMessage message, NetDeliveryMethod deliveryMethod = NetDeliveryMethod.Unreliable, int sequenceChannel = 0)
        {
            var connections = new List<NetConnection>();
            foreach (var workerConnection in _connections)
            {
                if(!workerConnection.Value.EntitiesInRange.Contains(entityId))
                    continue;

                connections.Add(workerConnection.Value.Connection);
            }

            if (connections.Count < 1)
                return;

            NetOutgoingMessage om = s_server.CreateMessage();
            om.Write(MessagePackSerializer.Serialize(message));
            s_server.SendMessage(om, connections, deliveryMethod, sequenceChannel);
        }

        public void SendArea(Position position, MmoMessage message, long workerExcludeId, NetDeliveryMethod deliveryMethod = NetDeliveryMethod.Unreliable, int sequenceChannel = 0)
        {
            var connections = new List<NetConnection>(100);
            foreach (var workerConnection in _connections)
            {
                if(workerConnection.Key == workerExcludeId)
                    continue;

                if (Position.WithinArea(position, workerConnection.Value.InterestPosition,
                    workerConnection.Value.InterestRange))
                {
                    connections.Add(workerConnection.Value.Connection);

                }
            }

            if(connections.Count < 1)
                return;

            NetOutgoingMessage om = s_server.CreateMessage();
            om.Write(MessagePackSerializer.Serialize(message));
            s_server.SendMessage(om, connections, deliveryMethod, sequenceChannel);
        }

        public void SendToAuthority(MmoMessage message, long workerId)
        {
            //send to all for now

            NetOutgoingMessage om = s_server.CreateMessage();
            om.Write(MessagePackSerializer.Serialize(message));
            if(workerId > 0 && _connections.TryGetValue(workerId, out var connection))
            {
                s_server.SendMessage(om, connection.Connection, NetDeliveryMethod.ReliableUnordered);
            }
            else
            {
                s_server.SendToAll(om, NetDeliveryMethod.ReliableUnordered);
            }
        }

        private void OnEntityUpdate(Entity entityInfo)
        {
            var message = new MmoMessage()
            {
                MessageId = ServerCodes.EntityInfo,

                Info = MessagePackSerializer.Serialize(entityInfo.ToEntityInfo()),
            };
            if(_logger.IsEnabled(LogLevel.Debug))
                _logger.LogDebug($"Sending Entity Info {entityInfo.EntityId}" );
            //SendCheckedout(entityInfo.EntityId, message, NetDeliveryMethod.ReliableOrdered);
            SendArea(entityInfo.Position, message, 0, NetDeliveryMethod.ReliableUnordered);
        }

        private void OnEntityDelete(Entity entityInfo)
        {
            //find who has checked out
            var message = new MmoMessage()
            {
                MessageId = ServerCodes.EntityDelete,

                Info = MessagePackSerializer.Serialize(entityInfo),
            };

            _logger.LogInformation($"Deleting Entity {entityInfo.EntityId}");
            //SendCheckedout(entityInfo.EntityId, message, NetDeliveryMethod.ReliableOrdered);
            SendArea(entityInfo.Position, message, 0, NetDeliveryMethod.ReliableUnordered);
        }

        private void OnEntityUpdatePartial(EntityUpdate entityUpdate, long workerId)
        {

            var entity = _entities.GetEntity(entityUpdate.EntityId);
            if(entity == null)
                return;

            var message = new MmoMessage()
            {
                MessageId = ServerCodes.EntityUpdate,

                Info = MessagePackSerializer.Serialize(entityUpdate),
            };
            //if (_logger.IsEnabled(LogLevel.Debug))
            //    _logger.LogDebug("Sending Entity Update");
            SendArea(entity.Value.Position, message, workerId, NetDeliveryMethod.Unreliable);
        }

        private void OnEntityEvent(EventRequest eventRequest)
        {
            var entity = _entities.GetEntity(eventRequest.EntityId);
            if (entity == null)
                return;

            var message = new MmoMessage()
            {
                MessageId = ServerCodes.EntityEvent,

                Info = MessagePackSerializer.Serialize(eventRequest),
            };

            if (_logger.IsEnabled(LogLevel.Debug))
                _logger.LogDebug($"Sending Entity Event {eventRequest.ComponentId}-{eventRequest.EventId}");
            SendArea(entity.Value.Position, message, 0, NetDeliveryMethod.ReliableUnordered);
        }

        private void OnEntityCommand(CommandRequest commandRequest)
        {

            var entity = _entities.GetEntity(commandRequest.EntityId);
            if (entity == null)
                return;

            //todo: get ACL and find who has authority over the command
            var acls = entity.Value.Acls;
            Acl? entityAcl = null;
            long workerId = 0;
            foreach(var acl in acls.AclList)
            {
                if(acl.ComponentId != commandRequest.ComponentId)
                    continue;
                entityAcl = acl;
            }
            if (entityAcl == null)
                return;

            if(entityAcl.Value.WorkerType != WorkerType)
                return;

            workerId = entityAcl.Value.WorkerId;

            var message = new MmoMessage()
            {
                MessageId = ServerCodes.EntityCommandRequest,

                Info = MessagePackSerializer.Serialize(commandRequest),
            };

            if (_logger.IsEnabled(LogLevel.Debug))
                _logger.LogDebug($"Sending Command Request {commandRequest.RequestId} {commandRequest.ComponentId}-{commandRequest.CommandId}");
            SendToAuthority(message, workerId);
        }

        private void OnEntityCommandResponse(CommandResponse commandResponse)
        {

            var entity = _entities.GetEntity(commandResponse.EntityId);
            if (entity == null)
                return;

            //todo: we need an internal request table

            WorkerConnection worker;
            if (!_connections.TryGetValue(commandResponse.RequesterId, out worker))
            {
                //disconnected??
                return;
            }

            //fix / validate data
            commandResponse.RequesterId = worker.Connection.RemoteUniqueIdentifier;

            var message = new MmoMessage()
            {
                MessageId = ServerCodes.EntityCommandResponse,

                Info = MessagePackSerializer.Serialize(commandResponse),
            };
            if (_logger.IsEnabled(LogLevel.Debug))
                _logger.LogDebug($"Sending Command Response {commandResponse.RequestId} {commandResponse.ComponentId}-{commandResponse.CommandId}");
            Send(worker.Connection, message, NetDeliveryMethod.ReliableUnordered);
        }

    }
}
