/*
Copyright (c) 2021 Simonas Greicius

All rights reserved. This program and the accompanying materials
are made available under the terms of the Eclipse Public License v1.0
and Eclipse Distribution License v1.0 which accompany this distribution. 

The Eclipse Public License is available at 
   http://www.eclipse.org/legal/epl-v10.html
and the Eclipse Distribution License is available at 
   http://www.eclipse.org/org/documents/edl-v10.php.

Contributors:
   Simonas Greicius - creation of state machine classes
*/

using System.Collections;
using Tevux.Protocols.Mqtt.Utility;

namespace Tevux.Protocols.Mqtt {
    internal class ResendingStateMachine {

        private MqttClient _client;
        private readonly Hashtable _contexts = new Hashtable();
        private readonly ConcurrentQueue _itemsToRemove = new ConcurrentQueue();
        private readonly NLog.Logger _log = NLog.LogManager.GetCurrentClassLogger();

        public void Initialize(MqttClient client) {
            _client = client;
        }

        public void Tick() {
            var currentTime = Helpers.GetCurrentTime();

            lock (_contexts.SyncRoot) {
                foreach (DictionaryEntry item in _contexts) {
                    var context = (TransmissionContext)item.Value;

                    if (context.AttemptNumber >= _client.ConnectionOptions.MaxRetryCount) {
                        context.IsFinished = true;
                        context.IsSucceeded = false;
                        _itemsToRemove.Enqueue(context);
                    }
                    else if (currentTime - context.Timestamp > _client.ConnectionOptions.RetryDelay) {
                        context.AttemptNumber++;
                        context.Timestamp = currentTime;
                        Send(context);
                    }
                }
            }

            while (_itemsToRemove.TryDequeue(out var item)) {
                var context = (TransmissionContext)item;
                _log.Error($"{ControlPacketBase.PacketTypes.GetShortName(context.PacketToSend.Type)} {context.PacketToSend.PacketId:X4} failed to send.");

                lock (_contexts.SyncRoot) {
                    _contexts.Remove(context.PacketId);
                }
            }
        }

        public void Send(TransmissionContext context) {
            PacketTracer.LogOutgoingPacket(context.PacketToSend, context.AttemptNumber);

            _client.Send(context.PacketToSend);
        }

        public void EnqueueAndSend(TransmissionContext context) {
            Send(context);
            lock (_contexts.SyncRoot) {
                _contexts.Add(context.PacketId, context);
            }
        }

        public bool TryFinalize(ushort packetId, out TransmissionContext finalizedContext) {
            var isFinalized = false;

            lock (_contexts.SyncRoot) {

                if (_contexts.Contains(packetId)) {
                    var contextToRemove = (TransmissionContext)_contexts[packetId];
                    contextToRemove.IsFinished = true;
                    contextToRemove.IsSucceeded = true;
                    _contexts.Remove(packetId);
                    finalizedContext = contextToRemove;
                    isFinalized = true;
                }
                else {
                    finalizedContext = null;
                }
            }

            return isFinalized;
        }
    }
}
