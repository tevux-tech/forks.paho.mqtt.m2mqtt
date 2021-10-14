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

using System.Threading;
using Tevux.Protocols.Mqtt.Utility;

namespace Tevux.Protocols.Mqtt {
    internal class SubscriptionStateMachine {
        private MqttClient _client;
        private readonly ResendingStateMachine _sendQueue = new ResendingStateMachine();

        public void Initialize(MqttClient client) {
            _client = client;
            _sendQueue.Initialize(client);
        }

        public void Tick() {
            _sendQueue.Tick();
        }

        public bool Subscribe(SubscribePacket packet, bool waitForCompletion = false) {
            return InternalSubscribeUnsubscribe(packet, waitForCompletion);
        }

        public bool Unsubscribe(UnsubscribePacket packet, bool waitForCompletion = false) {
            return InternalSubscribeUnsubscribe(packet, waitForCompletion);
        }

        private bool InternalSubscribeUnsubscribe(ControlPacketBase packet, bool waitForCompletion = false) {
            var currentTime = Helpers.GetCurrentTime();

            var transmissionContext = new TransmissionContext(packet, currentTime);

            _sendQueue.EnqueueAndSend(transmissionContext);

            if (waitForCompletion) {
                var timeToBreak = false;
                while (timeToBreak == false) {
                    Thread.Sleep(10);
                    if (transmissionContext.IsFinished) { timeToBreak = true; }
                    if (_client.IsConnected == false) { timeToBreak = true; }
                }
            }

            var returnResult = waitForCompletion ? transmissionContext.IsSucceeded : true;

            return returnResult;
        }

        public void ProcessPacket(SubackPacket packet) {
            InternalProcessPacket(packet);
        }
        public void ProcessPacket(UnsubackPacket packet) {
            InternalProcessPacket(packet);
        }

        private void InternalProcessPacket(ControlPacketBase packet) {
            Trace.LogIncomingPacket(packet);

            if (_sendQueue.TryFinalize(packet.PacketId, out var finalizedContext)) {
                _client.OnPacketAcknowledged(finalizedContext.PacketToSend, packet);
            }
            else {
                HandleRoguePacketReceived(packet);
            }
        }

        private void HandleRoguePacketReceived(ControlPacketBase packet) {
            Trace.LogIncomingPacket(packet, true);
        }
    }
}
