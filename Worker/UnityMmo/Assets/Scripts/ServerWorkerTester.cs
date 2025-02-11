using Mmogf;
using Mmogf.Core;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mmogf
{
    public class ServerWorkerTester : MonoBehaviour
    {
        [SerializeField]
        ServerHandler _serverHandler;

        // Start is called before the first frame update
        void Start()
        {
        
        }

        // Update is called once per frame
        void Update()
        {
            #if UNITY_EDITOR
            //debugging
            if (Input.GetKeyDown(KeyCode.Z))
            {
                //create ship
                _serverHandler.SendCommand< World.CreateEntity,CreateEntityRequest,NothingInternal> (0, 0, new CreateEntityRequest("Ship", new Position() { Y = 0, }.ToFixedVector3(), RandomHeading().ToRotation(), 
                    new Dictionary<short, byte[]>() 
                    {
                        { Cannon.ComponentId, MessagePack.MessagePackSerializer.Serialize(new Cannon()) },
                        { Health.ComponentId, MessagePack.MessagePackSerializer.Serialize(new Health() { Current = 100, Max = 100, }) },
                    }, 
                    new List<Acl>() 
                    {
                        new Acl() { ComponentId = FixedVector3.ComponentId, WorkerType = "Dragon-Worker" },
                        new Acl() { ComponentId = Rotation.ComponentId, WorkerType = "Dragon-Worker" },
                        new Acl() { ComponentId = Acls.ComponentId, WorkerType = "Dragon-Worker" },
                        new Acl() { ComponentId = Cannon.ComponentId, WorkerType = "Dragon-Worker" },
                        new Acl() { ComponentId = Health.ComponentId, WorkerType = "Dragon-Worker" },
                    }), 
                    response => {
                        Debug.Log($"Create Entity! {response.CommandStatus} - {response.Message}");
                    });
            }

            if (Input.GetKeyDown(KeyCode.X))
            {
                //create shark
                _serverHandler.SendCommand<World.CreateEntity,CreateEntityRequest,NothingInternal>(0, 0, new CreateEntityRequest("Shark", new Position() { Y = -5, }.ToFixedVector3(), RandomHeading().ToRotation(),
                    new Dictionary<short, byte[]>()
                    {
                        { Cannon.ComponentId, MessagePack.MessagePackSerializer.Serialize(new Cannon()) },
                    },
                    new List<Acl>()
                    {
                        new Acl() { ComponentId = FixedVector3.ComponentId, WorkerType = "Dragon-Worker" },
                        new Acl() { ComponentId = Rotation.ComponentId, WorkerType = "Dragon-Worker" },
                        new Acl() { ComponentId = Acls.ComponentId, WorkerType = "Dragon-Worker" },
                    }),
                    response => {
                        Debug.Log($"Create Entity! {response.CommandStatus} - {response.Message}");
                    });
            }

            if (Input.GetKeyDown(KeyCode.Delete))
            {
                foreach(var entity in _serverHandler.GameObjectRepresentation.Entities)
                {
                    if(((EntityType)entity.Value.Data[EntityType.ComponentId]).Name == "Ship")
                    {
                        Debug.Log($"Requesting delete: {entity.Key}");
                        _serverHandler.SendCommand<World.DeleteEntity, DeleteEntityRequest, NothingInternal>(0, 0, new DeleteEntityRequest(entity.Key), response => {
                            Debug.Log($"Deleted Entity! {response.CommandStatus} - {response.Message}");
                        });
                        break;
                    }
                }
            }
            #endif
        }

        Quaternion RandomHeading()
        {
            return Quaternion.Euler(0, Random.Range(-180, 180), 0);
        }
    }
}
