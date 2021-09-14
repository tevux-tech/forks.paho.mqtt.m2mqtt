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

namespace uPLibrary.Networking.M2Mqtt.Messages {
    /// <summary>
    /// Event Args class for subscribe request on topics
    /// </summary>
    public class MqttMsgSubscribeEventArgs : EventArgs {
        public ushort MessageId { get; internal set; }

        public string[] TopicsToSubscribe { get; internal set; }

        public byte[] QoSLevels { get; internal set; }

        public MqttMsgSubscribeEventArgs(ushort messageId, string[] topics, byte[] qosLevels) {
            MessageId = messageId;
            TopicsToSubscribe = topics;
            QoSLevels = qosLevels;
        }
    }
}
