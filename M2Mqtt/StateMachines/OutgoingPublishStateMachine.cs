using Tevux.Protocols.Mqtt.Utility;

namespace Tevux.Protocols.Mqtt {
    internal class OutgoingPublishStateMachine {
        private MqttClient _client;

        private ResendingStateMachine _qos1PublishQueue = new ResendingStateMachine();
        private ResendingStateMachine _qos2PublishQueue = new ResendingStateMachine();
        private ResendingStateMachine _qos2PubrelQueue = new ResendingStateMachine();

        public void Initialize(MqttClient client) {
            _client = client;
            _qos1PublishQueue.Initialize(client);
            _qos2PublishQueue.Initialize(client);
            _qos2PubrelQueue.Initialize(client);
        }

        public void Tick() {
            _qos1PublishQueue.Tick();
            _qos2PublishQueue.Tick();
            _qos2PubrelQueue.Tick();
        }

        public void Publish(PublishPacket packet) {
            var currentTime = Helpers.GetCurrentTime();

            var context = new PublishTransmissionContext(packet, packet, currentTime);

            if (packet.QosLevel == QosLevel.AtMostOnce) {
                // Those are the best packets - just sending and waiting for no responses!
                _client.Send(context.PacketToSend);
                Trace.LogOutgoingPacket(packet);
            }
            else if (packet.QosLevel == QosLevel.AtLeastOnce) {
                _qos1PublishQueue.Enqueue(context);
            }
            else if (packet.QosLevel == QosLevel.ExactlyOnce) {
                _qos2PublishQueue.Enqueue(context);
            }

#warning errrr?..
            packet.DupFlag = true;
        }

        public void ProcessPacket(PubackPacket packet) {
            if (_qos1PublishQueue.TryFinalize(packet.PacketId, out var finalizedContext)) {
                Trace.LogIncomingPacket(packet);
                NotifyPublishSucceeded(((PublishTransmissionContext)finalizedContext).OriginalPublishPacket);
            }
            else {
                NotifyRoguePacketReceived(packet);
            }
        }

        public void ProcessPacket(PubrecPacket packet) {
            var currentTime = Helpers.GetCurrentTime();
            var isOk = true;

            if (_qos2PublishQueue.TryFinalize(packet.PacketId, out var finalizedContext)) {
                Trace.LogIncomingPacket(packet);
                NotifyPublishSucceeded(((PublishTransmissionContext)finalizedContext).OriginalPublishPacket);
            }
            else {
                isOk = false;
                NotifyRoguePacketReceived(packet);
            }

            if (isOk) {
                var pubrelPacket = new PubrelPacket(packet.PacketId);
                finalizedContext.PacketToSend = pubrelPacket;
                finalizedContext.AttemptNumber = 1;
                finalizedContext.Timestamp = currentTime;
                finalizedContext.IsFinished = false;
                finalizedContext.IsSucceeded = false;
                _qos2PubrelQueue.Enqueue(finalizedContext);
            }
        }

        public void ProcessPacket(PubcompPacket packet) {
            Trace.LogIncomingPacket(packet);

            if (_qos2PubrelQueue.TryFinalize(packet.PacketId, out var finalizedContext)) {
                // do nothing?..
            }
            else {
                NotifyRoguePacketReceived(packet);
            }
        }

#warning of course, that's not the place to raise events.
        private void NotifyPublishSucceeded(ControlPacketBase packet) {
            _client.OnMqttMsgPublished(packet.PacketId, true);
        }
        private void NotifyPublishFailed(ushort packetId) {
            _client.OnMqttMsgPublished(packetId, false);
        }
        private void NotifyRoguePacketReceived(ControlPacketBase packet) {
            Trace.LogIncomingPacket(packet, true);
        }

    }
}
