using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
using CuteAnt.Reflection;
using Microsoft.Extensions.Logging;

namespace EventStore.Core.Messaging
{
    public abstract class Message
    {
        protected static int NextMsgId = -1;
        private static readonly int TypeId = Interlocked.Increment(ref NextMsgId);
        public virtual int MsgTypeId { get { return TypeId; } }
    }

    public static class MessageHierarchy
    {
        private static readonly ILogger Log = TraceLogger.GetLogger(typeof(MessageHierarchy));

        public static readonly Dictionary<Type, List<Type>> Descendants;
        public static readonly int[][] ParentsByTypeId;
        public static readonly int[][] DescendantsByTypeId;
        public static readonly Dictionary<Type, int[]> DescendantsByType;
        public static readonly Dictionary<Type, int> MsgTypeIdByType;
        public static readonly int MaxMsgTypeId;

        static MessageHierarchy()
        {
            var sw = Stopwatch.StartNew();

            MsgTypeIdByType = new Dictionary<Type, int>();
            var descendants = new Dictionary<int, List<int>>();
            var parents = new Dictionary<int, List<int>>();
            var rootMsgType = typeof(Message);

            Descendants = new Dictionary<Type, List<Type>>();

            int msgTypeCount = 0;
            foreach (var msgType in
                (from assembly in AppDomain.CurrentDomain.GetAssemblies()
                 from type in TypeUtils.GetTypes(assembly, t => true) //LoadAvailableTypes(assembly)
                 where rootMsgType.IsAssignableFrom(type)
                 select type))
            {
                msgTypeCount += 1;

                var msgTypeId = GetMsgTypeId(msgType);
                MsgTypeIdByType.Add(msgType, msgTypeId);
                parents.Add(msgTypeId, new List<int>());

                MaxMsgTypeId = Math.Max(msgTypeId, MaxMsgTypeId);
                //Log.WriteLine("Found {0} with MsgTypeId {1}", msgType.Name, msgTypeId);

                var type = msgType;
                while (true)
                {
                    var typeId = GetMsgTypeId(type);
                    parents[msgTypeId].Add(typeId);
                    if (!descendants.TryGetValue(typeId, out List<int> list))
                    {
                        list = new List<int>();
                        descendants.Add(typeId, list);
                    }
                    list.Add(msgTypeId);

                    if (!Descendants.TryGetValue(type, out List<Type> typeList))
                    {
                        typeList = new List<Type>();
                        Descendants.Add(type, typeList);
                    }
                    typeList.Add(msgType);

                    if (type == rootMsgType) { break; }
                    type = type.BaseType;
                };
            }

            if (msgTypeCount - 1 != MaxMsgTypeId)
            {
                var wrongTypes = from typeId in MsgTypeIdByType
                                 group typeId.Key by typeId.Value into g
                                 where g.Count() > 1
                                 select new
                                 {
                                     TypeId = g.Key,
                                     MsgTypes = g.ToArray()
                                 };

                foreach (var wrongType in wrongTypes)
                {
                    Log.LogCritical("MsgTypeId {0} is assigned to type: {1}",
                        wrongType.TypeId, string.Join(", ", wrongType.MsgTypes.Select(x => x.Name)));
                }

                ThrowHelper.ThrowException(ExceptionResource.Incorrect_Message_Type_IDs_setup);
            }

            DescendantsByTypeId = new int[MaxMsgTypeId + 1][];
            ParentsByTypeId = new int[MaxMsgTypeId + 1][];
            for (int i = 0; i <= MaxMsgTypeId; ++i)
            {
                var list = descendants[i];
                DescendantsByTypeId[i] = new int[list.Count];
                for (int j = 0; j < list.Count; ++j)
                {
                    DescendantsByTypeId[i][j] = list[j];
                }

                list = parents[i];
                ParentsByTypeId[i] = new int[list.Count];
                for (int j = 0; j < list.Count; ++j)
                {
                    ParentsByTypeId[i][j] = list[j];
                }
            }
            DescendantsByType = new Dictionary<Type, int[]>();
            foreach (var typeIdMap in MsgTypeIdByType)
            {
                DescendantsByType.Add(typeIdMap.Key, DescendantsByTypeId[typeIdMap.Value]);
            }

            if (Log.IsTraceLevelEnabled()) { Log.MessageHierarchyInitializationTook(sw.Elapsed); }
        }

        static Type[] LoadAvailableTypes(Assembly assembly)
        {
            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                if (Log.IsInformationLevelEnabled())
                {
                    if ((uint)ex.LoaderExceptions.Length > 0u)
                    {
                        Log.TheExceptionsOccuredWhenScanningForMessageTypes(ex);
                    }
                    else
                    {
                        Log.Exception_while_scanning_for_message_types(ex);
                    }
                }
                return ex.Types;
            }
        }

        private static int GetMsgTypeId(Type msgType)
        {
            if (MsgTypeIdByType.TryGetValue(msgType, out int typeId)) { return typeId; }

            var msgTypeField = msgType.GetFields(BindingFlags.Static | BindingFlags.NonPublic).FirstOrDefault(x => x.Name == "TypeId");
            if (msgTypeField == null)
            {
                if (Log.IsInformationLevelEnabled()) Log.Message_doesnot_have_TypeId_field(msgType);
                ThrowHelper.ThrowException_MessageDoesntHaveTypeIdField(msgType);
            }
            var msgTypeId = (int)msgTypeField.GetValue(null);
            return msgTypeId;
        }
    }

}
