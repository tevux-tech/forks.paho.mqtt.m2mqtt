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
    /// Class for CONNACK message from broker to client. See section 3.2.
    /// </summary>
    internal class MqttMsgConnack : MqttMsgBase {

        public enum ReturnCodes : byte {
            Accepted = 0x00,
            RefusedUnacceptableProtocolVersion = 0x01,
            RefusedIdentifierRejected = 0x02,
            RefusedServerUnavailable = 0x03,
            RefusedBadUsernameOrPassword = 0x04,
            RefusedNotAuthorized = 0x05
        }

        public bool SessionPresent { get; private set; }
        public ReturnCodes ReturnCode { get; private set; }

        public MqttMsgConnack() {
            Type = MessageType.ConAck;
        }

        public static bool TryParse(byte[] variableHeader, out MqttMsgConnack parsedMessage) {
            var isOk = true;

            parsedMessage = new MqttMsgConnack();

            // Byte 1: ConAck flags.
            if (variableHeader[0] == 0) {
                parsedMessage.SessionPresent = false;
            }
            else if (variableHeader[0] == 1) {
                parsedMessage.SessionPresent = true;
            }
            else {
                // That's a protocol violation. 
                isOk = false;
            }

            // Byte 2: Return code.
            if (variableHeader[1] < 6) {
                parsedMessage.ReturnCode = (ReturnCodes)variableHeader[1];
            }
            else {
                // That's a protocol violation. 
                isOk = false;
            }

            return isOk;
        }

        public override string ToString() {
            return Helpers.GetTraceString("CONNACK", new object[] { "returnCode" }, new object[] { (ReturnCodes)ReturnCode });
        }
    }
}
