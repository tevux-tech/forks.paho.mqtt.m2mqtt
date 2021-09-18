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

namespace uPLibrary.Networking.M2Mqtt.Messages {
    /// <summary>
    /// Class for PINGREQ message from client to broker. See section 3.12.
    /// </summary>
    public class MqttMsgPingReq : MqttMsgBase, ISentToBroker {
        public MqttMsgPingReq() {
            Type = MessageType.PingReq;
        }

        public byte[] GetBytes() {
            // Message content is fixed, no variables here.
            var buffer = new byte[2];
            buffer[0] = MessageType.PingReq << 4;
            buffer[1] = 0;

            return buffer;
        }

        public override string ToString() {
            return Helpers.GetTraceString("PINGREQ", null, null);
        }
    }
}
