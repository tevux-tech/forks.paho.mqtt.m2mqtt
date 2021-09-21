using System.Collections;
using Tevux.Protocols.Mqtt.Utility;

namespace Tevux.Protocols.Mqtt {
    internal class ResendingStateMachine {
        private readonly string _indent = "            ";
        private readonly string _blank = ControlPacketBase.PacketTypes.GetShortBlank();
        private MqttClient _client;
        private readonly Hashtable _contexts = new Hashtable();
        private readonly ConcurrentQueue _itemsToRemove = new ConcurrentQueue();

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
                Trace.WriteLine(TraceLevel.Queuing, $"{_indent}{_blank}{context.PacketToSend.PacketId:X4} FAILED");
                lock (_contexts.SyncRoot) {
                    _contexts.Remove(context.PacketId);
                }
            }
        }

        public void Send(TransmissionContext context) {
            Trace.LogOutgoingPacket(context.PacketToSend, context.AttemptNumber);

            _client.Send(context.PacketToSend);
        }

        public void Enqueue(TransmissionContext context) {
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
