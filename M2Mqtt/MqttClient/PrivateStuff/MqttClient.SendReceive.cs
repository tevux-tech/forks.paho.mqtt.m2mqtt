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
using System.Net.Sockets;
using uPLibrary.Networking.M2Mqtt.Exceptions;
using uPLibrary.Networking.M2Mqtt.Messages;
using uPLibrary.Networking.M2Mqtt.Utility;

namespace uPLibrary.Networking.M2Mqtt {
    public partial class MqttClient {

        /// <summary>
        /// Send a message to the broker and wait answer
        /// </summary>
        /// <returns>MQTT message response</returns>
        private MqttMsgBase SendReceive(byte[] msgBytes, int timeout) {
            // reset handle before sending
            _syncEndReceiving.Reset();
            try {
                // send message
                _channel.Send(msgBytes);

                // update last message sent ticks
                LastCommTime = Environment.TickCount;
            }
            catch (Exception e) {
                if (typeof(SocketException) == e.GetType()) {
                    // connection reset by broker
                    if (((SocketException)e).SocketErrorCode == SocketError.ConnectionReset) {
                        IsConnected = false;
                    }
                }
                Trace.WriteLine(TraceLevel.Error, "Exception occurred: {0}", e.ToString());

                throw new MqttCommunicationException(e);
            }

            // wait for answer from broker
            if (_syncEndReceiving.WaitOne(timeout)) {
                // message received without exception
                if (_exReceiving == null) {
                    return _msgReceived;
                }
                // receiving thread catched exception
                else {
                    throw _exReceiving;
                }
            }
            else {
                // throw timeout exception
                throw new MqttCommunicationException();
            }
        }

        /// <summary>
        /// Send a message to the broker and wait answer
        /// </summary>
        /// <returns>MQTT message response</returns>
        private MqttMsgBase SendReceive(ISentToBroker msg) {
            return SendReceive(msg, MqttSettings.DefaultTimeout);
        }

        /// <summary>
        /// Send a message to the broker and wait answer
        /// </summary>
        /// <returns>MQTT message response</returns>
        private MqttMsgBase SendReceive(ISentToBroker msg, int timeout) {
            Trace.WriteLine(TraceLevel.Frame, "SEND {0}", msg);
            return SendReceive(msg.GetBytes(), timeout);
        }
    }
}
