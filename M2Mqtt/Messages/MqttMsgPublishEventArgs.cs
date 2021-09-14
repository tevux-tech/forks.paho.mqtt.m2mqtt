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
    /// Event Args class for PUBLISH message received from broker
    /// </summary>
    public class MqttMsgPublishEventArgs : EventArgs {

        public string Topic { get; internal set; }
        public byte[] Message { get; internal set; }
        public bool DupFlag { get; set; }
        public byte QosLevel { get; internal set; }
        public bool Retain { get; internal set; }

        public MqttMsgPublishEventArgs(string topic, byte[] message, bool dupFlag, byte qosLevel, bool retain) {
            Topic = topic;
            Message = message;
            DupFlag = dupFlag;
            QosLevel = qosLevel;
            Retain = retain;
        }
    }
}
