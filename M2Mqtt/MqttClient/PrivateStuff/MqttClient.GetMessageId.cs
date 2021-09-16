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

namespace uPLibrary.Networking.M2Mqtt {

    public partial class MqttClient {
        /// <summary>
        /// Generate the next message identifier
        /// </summary>
        /// <returns>Message identifier</returns>
        public static ushort GetNewMessageId() {
            // if 0 or max UInt16, it becomes 1 (first valid messageId)
            _messageIdCounter = ((_messageIdCounter % ushort.MaxValue) != 0) ? (ushort)(_messageIdCounter + 1) : (ushort)1;
            return _messageIdCounter;
        }
    }
}
