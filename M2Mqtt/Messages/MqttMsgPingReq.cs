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
    /// Class for PINGREQ message from client to broker
    /// </summary>
    public class MqttMsgPingReq : MqttMsgBase, ISentToBroker {
        public MqttMsgPingReq() {
            type = MessageType.PingReq;
        }

        public byte[] GetBytes() {
            var buffer = new byte[2];
            var index = 0;

            // first fixed header byte

            buffer[index++] = (MessageType.PingReq << FixedHeader.TypeOffset) | MessageFlags.PingReq; // [v.3.1.1]
            buffer[index++] = 0x00;

            return buffer;
        }

        public override string ToString() {
#if TRACE
            return GetTraceString("PINGREQ", null, null);
#else
            return base.ToString();
#endif
        }
    }
}
