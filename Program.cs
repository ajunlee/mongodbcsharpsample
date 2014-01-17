using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MongoApp
{
    public class ChatUser {
        [BsonId]
        public ObjectId _id { get; set; }
        public string Name { get; set; }
        public List<ChatConnection> connections { get; set; }
    }

    public class ChatConnection{
        public string connectionID{get;set;}
        public string userAgent { get; set; }
        public bool connected { get; set; }
    }

    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                string connStr = System.Configuration.ConfigurationManager.AppSettings["mongoDB"];
                MongoClient mClient = new MongoClient(connStr);
                MongoServer mServer = mClient.GetServer();
                MongoDatabase mDB = mServer.GetDatabase("test");
                MongoCollection mCol = mDB.GetCollection("chatUser");

                #region insert
                for (int i = 0; i < 10; i++)
                {
                    #region insert with object model
                    ChatUser item = new ChatUser();
                    item.Name = "adamlee" + i.ToString("000");
                    item.connections = new List<ChatConnection> { 
                        new ChatConnection {connectionID = Guid.NewGuid().ToString(), userAgent = "this is chrome", connected= true},
                        new ChatConnection {connectionID = Guid.NewGuid().ToString(), userAgent = "this is firefox", connected= false}
                    };
                    item.connections.Add(new ChatConnection
                    {
                        connectionID = Guid.NewGuid().ToString(),
                        userAgent = "this is IE"
                    });
                    mCol.Insert(item);
                    #endregion

                    #region insert with BsonDocument
                    BsonDocument _item = new BsonDocument{ {"Name","adown" + i.ToString("000")} , {"connections", new BsonArray{
                        new BsonDocument{ {"connectionID", Guid.NewGuid().ToString()},{"userAgent","this is firefox"}, {"connected", BsonBoolean.True} },
                        new BsonDocument{ {"connectionID", Guid.NewGuid().ToString()},{"userAgent","this is opera"}, {"connected", BsonBoolean.True} }
                    }}};
                    _item["connections"].AsBsonArray.Add(
                        new BsonDocument { { "connectionID", Guid.NewGuid().ToString() }, { "userAgent", "this is navi" }, { "connected", BsonBoolean.True } }
                    );
                    mCol.Insert(_item);                    
                    #endregion

                }
                #endregion


                #region query with object model
                List<ChatUser> _list1 = mCol.FindAs<ChatUser>(Query.EQ("connections.userAgent", "this is firefox")).ToList();
                Console.WriteLine(_list1.Count);
              
                #endregion

                #region query with BsonDocument
                List<BsonDocument> _list2 = mCol.FindAs<BsonDocument>(Query.EQ("connections.connected", BsonBoolean.False)).ToList();
                for (int i = 0; i < _list2.Count; i++)
                {
                    Console.WriteLine(_list2[i].GetValue("Name", "No Name"));
                    BsonArray _arr = _list2[i]["connections"].AsBsonArray;
                    Console.WriteLine(_arr.Count);
                    for (int j = 0; j < _arr.Count; j++)
                    {
                        BsonDocument _itm = _arr[j].AsBsonDocument;
                        Console.WriteLine(_itm.GetValue("connectionID") + " : " + _itm.GetValue("userAgent", "no user agent"));
                    }
                }
                #endregion

                string connectionId = Guid.NewGuid().ToString();
                #region add new embedded document
                mCol.Update(
                    Query.EQ("Name", "adamlee000"),
                    Update.Push("connections",
                        BsonDocumentWrapper.Create<ChatConnection>(
                        new ChatConnection
                        {
                            connectionID = connectionId,
                            userAgent = "this is opera",
                            connected = false
                        })));

                #endregion
                
                #region update existing item
                mCol.Update(
                    Query.EQ("connections.connectionID", connectionId), 
                    Update.Set("connections.$", 
                        BsonDocumentWrapper.Create<ChatConnection>(new ChatConnection {
                            connectionID = connectionId,
                            userAgent = "this is IE9"
                        })));

                #endregion

                #region remove embedded document
                mCol.Update(
                    Query.EQ("connections.connectionID", connectionId),
                    Update.Pull("connections", Query.EQ("connectionID", connectionId))
                );
                
                #endregion
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }

            Console.WriteLine("Press Enter to continue");
            Console.ReadLine();
        }
    }
}
