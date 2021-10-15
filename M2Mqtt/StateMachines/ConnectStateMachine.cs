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
    /// <summary>
    /// This state machine handles the exchange of CONNECT-CONNACK packets.
    /// </summary>
    internal class ConnectStateMachine {
        private readonly NLog.Logger _log = NLog.LogManager.GetCurrentClassLogger();
        private MqttClient _client;
        private bool _isWaitingForConnack;
        private double _requestTimestamp;
        public ConnackPacket.ReturnCodes ConnectionResult { get; private set; }
        public bool IsConnectionCompleted { get; private set; }
        public bool IsConnectionSuccessful { get; private set; }

        public void Connect(ConnectPacket packet) {
            Reset();

            var currentTime = Helpers.GetCurrentTime();
            _client.Send(packet);
            _isWaitingForConnack = true;
            _requestTimestamp = currentTime;
            PacketTracer.LogOutgoingPacket(packet);
        }

        public void Initialize(MqttClient client) {
            _client = client;
            Reset();
        }

        public void ProcessPacket(ConnackPacket packet) {
            PacketTracer.LogIncomingPacket(packet);

            _isWaitingForConnack = false;
            IsConnectionCompleted = true;
            IsConnectionSuccessful = true;
            ConnectionResult = packet.ReturnCode;
        }

        public void Reset() {
            _isWaitingForConnack = false;
            IsConnectionCompleted = false;
        }

        public void Tick() {
            var currentTime = Helpers.GetCurrentTime();

            if (_isWaitingForConnack) {
                if (currentTime - _requestTimestamp > _client.ConnectionOptions.KeepAlivePeriod) {
                    // Problem. Server does not respond.
                    _isWaitingForConnack = false;
                    IsConnectionCompleted = true;
                    IsConnectionSuccessful = false;
                    _log.Error($"PINRESP has not been received in {_client.ConnectionOptions.KeepAlivePeriod} s.");
                }
            }
            else {

            }
        }
    }
}
