using Tevux.Protocols.Mqtt.Utility;

namespace Tevux.Protocols.Mqtt {
    internal class IncomingPublishStateMachine {
        private MqttClient _client;
        private readonly ResendingStateMachine _qos2PubrecQueue = new ResendingStateMachine();

        public void Initialize(MqttClient client) {
            _client = client;
            _qos2PubrecQueue.Initialize(client);
        }

        public void Tick() {
            _qos2PubrecQueue.Tick();
        }

        public void ProcessPacket(PublishPacket packet) {
            Trace.LogIncomingPacket(packet);

            var currentTime = Helpers.GetCurrentTime();

            if (packet.QosLevel == QosLevel.AtMostOnce) {
                // Those are the best packets - need no responses!
                NotifyPublishReceived(packet);
            }
            else if (packet.QosLevel == QosLevel.AtLeastOnce) {
                // Puback has no retransmission. So just sending it, and if broker will not receive it, it will retransmit Publish packet.
                var pubAckPacket = new PubackPacket(packet.PacketId);
                _client.Send(pubAckPacket);
                Trace.LogOutgoingPacket(pubAckPacket);
            }
            else if (packet.QosLevel == QosLevel.ExactlyOnce) {
                // Before adding a Pubrec packet to the send queue, checking if it is not there already by trying to finalize it.
                // If finalization succeeds, that means broker did not get a Pubrec of the previous packet and is now resending
                // original Publish packet. So we simply discard the original Transmission context and start a new transmission.
                var weHaveAProblem = _qos2PubrecQueue.TryFinalize(packet.PacketId, out var finalizedContext);
                if (weHaveAProblem) { Trace.LogIncomingPacket(((PublishTransmissionContext)finalizedContext).OriginalPublishPacket, true); }

                // Normal workflow starts here.
                var pubRecPacket = new PubrecPacket(packet.PacketId);
                var context = new PublishTransmissionContext(packet, pubRecPacket, currentTime);
                _qos2PubrecQueue.Enqueue(context);
            }
        }

        public void ProcessPacket(PubrelPacket packet) {
            if (_qos2PubrecQueue.TryFinalize(packet.PacketId, out var finalizedContext)) {
                Trace.LogIncomingPacket(packet);
                NotifyPublishReceived(((PublishTransmissionContext)finalizedContext).OriginalPublishPacket);
            }
            else {
                // Broker did not receive Pubcomp in time, so is now sending a Pubrel again, but the original Publish packet is now not in the queue anymore.
                // Almost certainly it has been processed already. Not a problem, we'll just send the Pubcomp packet anyway.
                NotifyRoguePacketReceived(packet);
            }

            var pubcompPacket = new PubcompPacket(packet.PacketId);
            _client.Send(pubcompPacket);
            Trace.LogOutgoingPacket(pubcompPacket);
        }

        private void NotifyPublishReceived(PublishPacket packet) {
            _client.OnPacketAcknowledged(null, packet);
        }
        private void NotifyRoguePacketReceived(ControlPacketBase packet) {
            Trace.LogIncomingPacket(packet, true);
        }
    }
}
