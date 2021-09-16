/*
Copyright (c) 2013, 2014 Paolo Patierno

All rights reserved. This program and the accompanying materials
are made available under the terms of the Eclipse Public License v1.0
and Eclipse Distribution License v1.0 which accompany this distribution. 

The Eclipse Public License is available at 
   http://www.eclipse.org/legal/epl-v10.html
and the Eclipse Distribution License is available at 
   http://www.eclipse.org/org/documents/edl-v10.php.

Contributors:
   Paolo Patierno - initial API and implementation and/or initial documentation
*/

using System;
using uPLibrary.Networking.M2Mqtt.Exceptions;
using uPLibrary.Networking.M2Mqtt.Messages;
using static uPLibrary.Networking.M2Mqtt.Messages.MqttMsgConnack;

namespace uPLibrary.Networking.M2Mqtt {
    public partial class MqttClient {
        /// <summary>
        /// Connect to broker
        /// </summary>
        /// <returns>Return code of CONNACK message from broker</returns>
        public ReturnCodes Connect(string clientId) {
            return Connect(clientId, null, null, false, QosLevel.AtMostOnce, false, null, null, true, MqttMsgConnect.KeepAliveDefaultValue);
        }

        /// <summary>
        /// Connect to broker
        /// </summary>
        /// <returns>Return code of CONNACK message from broker</returns>
        public ReturnCodes Connect(string clientId, string username, string password) {
            return Connect(clientId, username, password, false, QosLevel.AtMostOnce, false, null, null, true, MqttMsgConnect.KeepAliveDefaultValue);
        }

        /// <summary>
        /// Connect to broker
        /// </summary>
        /// <returns>Return code of CONNACK message from broker</returns>
        public ReturnCodes Connect(string clientId, string username, string password, bool cleanSession, ushort keepAlivePeriod) {
            return Connect(clientId, username, password, false, QosLevel.AtMostOnce, false, null, null, cleanSession, keepAlivePeriod);
        }

        /// <summary>
        /// Connect to broker
        /// </summary>
        /// <returns>Return code of CONNACK message from broker</returns>
        public ReturnCodes Connect(string clientId, string username, string password, bool willRetain, QosLevel willQosLevel, bool willFlag, string willTopic, string willMessage, bool cleanSession, ushort keepAlivePeriod) {
            // create CONNECT message
            var connect = new MqttMsgConnect(clientId,
                username,
                password,
                willRetain,
                willQosLevel,
                willFlag,
                willTopic,
                willMessage,
                cleanSession,
                keepAlivePeriod);

            try {
                // connect to the broker
                _channel.Connect();
            }
            catch (Exception ex) {
                throw new MqttConnectionException("Exception connecting to the broker", ex);
            }

            LastCommTime = 0;
            _isRunning = true;
            _isConnectionClosing = false;
            // start thread for receiving messages from broker
            Fx.StartThread(ReceiveThread);

            MqttMsgConnack connack = null;
            try {
                connack = (MqttMsgConnack)SendReceive(connect);
            }
            catch (MqttCommunicationException) {
                _isRunning = false;
                throw;
            }

            // if connection accepted, start keep alive timer and 
            if (connack.ReturnCode == ReturnCodes.Accepted) {
                // set all client properties
                ClientId = clientId;
                CleanSession = cleanSession;
                WillFlag = willFlag;
                WillTopic = willTopic;
                WillMessage = willMessage;
                WillQosLevel = willQosLevel;

                _keepAlivePeriod = keepAlivePeriod * 1000; // convert in ms

                _pingStateMachine.Reset();

                // restore previous session
                RestoreSession();

                // keep alive period equals zero means turning off keep alive mechanism
                if (_keepAlivePeriod != 0) {
                    // start thread for sending keep alive message to the broker
                    Fx.StartThread(KeepAliveThread);
                }

                // start thread for raising received message event from broker
                Fx.StartThread(DispatchEventThread);

                // start thread for handling inflight messages queue to broker asynchronously (publish and acknowledge)
                Fx.StartThread(ProcessInflightThread);

                IsConnected = true;
            }
            return connack.ReturnCode;
        }
    }
}
