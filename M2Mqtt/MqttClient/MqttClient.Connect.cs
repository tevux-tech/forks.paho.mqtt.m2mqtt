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
using System.Threading;
using uPLibrary.Networking.M2Mqtt.Exceptions;
using uPLibrary.Networking.M2Mqtt.Messages;
using static uPLibrary.Networking.M2Mqtt.Messages.MqttMsgConnack;

namespace uPLibrary.Networking.M2Mqtt {
    public partial class MqttClient {
        /// <summary>
        /// Connect to broker
        /// </summary>
        /// <returns>Return code of CONNACK message from broker</returns>
        public ReturnCodes Connect() {
            return Connect(new ConnectionOptions());
        }


        /// <summary>
        /// Connect to broker
        /// </summary>
        /// <returns>Return code of CONNACK message from broker</returns>
        public ReturnCodes Connect(ConnectionOptions connectionOptions) {
            var connectMessage = new MqttMsgConnect(connectionOptions);

            try {
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

            _connectStateMachine.Connect(connectMessage);
            while (_connectStateMachine.IsConnectionCompleted == false) {
                Thread.Sleep(1000);
            }

            if (_connectStateMachine.IsServerLost) {
                _isRunning = false;
            }
            else {
                // if connection accepted, start keep alive timer and 
                if (_connectStateMachine.ConnectionResult == ReturnCodes.Accepted) {
                    // set all client properties

                    ConnectionOptions = connectionOptions;

                    _pingStateMachine.Reset();

                    // restore previous session
                    RestoreSession();

                    // keep alive period equals zero means turning off keep alive mechanism
                    if (connectionOptions.KeepAlivePeriod != 0) {
                        // start thread for sending keep alive message to the broker
                        Fx.StartThread(KeepAliveThread);
                    }

                    IsConnected = true;
                }
            }

            return _connectStateMachine.ConnectionResult;
        }
    }
}
