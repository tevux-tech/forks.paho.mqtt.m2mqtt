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

using Tevux.Protocols.Mqtt.Utility;

namespace Tevux.Protocols.Mqtt {
    internal class PingStateMachine {
        private bool _isWaitingForPingResponse;
        private double _requestTimestamp;
        private MqttClient _client;

        public bool IsBrokerAlive { get; private set; } = true;

        public void Initialize(MqttClient client) {
            _client = client;
            Reset();
        }

        public void Tick(double lastCommunicationTime) {
            var currentTime = Helpers.GetCurrentTime();

            if (_isWaitingForPingResponse) {
                if (currentTime - _requestTimestamp > _client.ConnectionOptions.KeepAlivePeriod) {
                    // Problem. Server does not respond.
                    _isWaitingForPingResponse = false;
                    IsBrokerAlive = false;
                }
            }
            else {
                // Keep alive period equals zero means turning off keep alive mechanism.
                if ((currentTime - lastCommunicationTime > _client.ConnectionOptions.KeepAlivePeriod) && (_client.ConnectionOptions.KeepAlivePeriod > 0)) {
                    var pingreq = new PingreqPacket();
                    _client.Send(pingreq);
                    PacketTracer.LogOutgoingPacket(pingreq);
                    _isWaitingForPingResponse = true;
                    _requestTimestamp = currentTime;
                }
            }
        }

        public void ProcessPacket(PingrespPacket packet) {
            PacketTracer.LogIncomingPacket(packet);
            IsBrokerAlive = true;
            _isWaitingForPingResponse = false;
        }

        public void Reset() {
            IsBrokerAlive = true;
            _isWaitingForPingResponse = false;
        }
    }
}
