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

namespace uPLibrary.Networking.M2Mqtt.Messages
{
    /// <summary>
    /// Event Args class for subscribed topics
    /// </summary>
    public class MqttMsgSubscribedEventArgs : EventArgs
    {
        public ushort MessageId { get; internal set; }

        public byte[] GrantedQoSLevels { get; internal set; }

        public MqttMsgSubscribedEventArgs(ushort messageId, byte[] grantedQosLevels)
        {
            MessageId = messageId;
            GrantedQoSLevels = grantedQosLevels;
        }
    }
}
