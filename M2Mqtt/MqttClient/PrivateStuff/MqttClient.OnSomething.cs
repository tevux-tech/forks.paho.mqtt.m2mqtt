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
using uPLibrary.Networking.M2Mqtt.Internal;
using uPLibrary.Networking.M2Mqtt.Messages;

namespace uPLibrary.Networking.M2Mqtt {
    public partial class MqttClient {
        /// <summary>
        /// Wrapper method for raising events
        /// </summary>
        /// <param name="internalEvent">Internal event</param>
        private void OnInternalEvent(InternalEvent internalEvent) {
            _eventQueue.Enqueue(internalEvent);

            _receiveEventWaitHandle.Set();
        }

        /// <summary>
        /// Wrapper method for raising closing connection event
        /// </summary>
        private void OnConnectionClosing() {
            if (!_isConnectionClosing) {
                _isConnectionClosing = true;
                _receiveEventWaitHandle.Set();
            }
        }

        /// <summary>
        /// Wrapper method for raising PUBLISH message received event
        /// </summary>
        /// <param name="publish">PUBLISH message received</param>
        private void OnMqttMsgPublishReceived(MqttMsgPublish publish) {
            MqttMsgPublishReceived?.Invoke(this, new MqttMsgPublishEventArgs(publish.Topic, publish.Message, publish.DupFlag, publish.QosLevel, publish.Retain));
        }

        /// <summary>
        /// Wrapper method for raising published message event
        /// </summary>
        /// <param name="messageId">Message identifier for published message</param>
        /// <param name="isPublished">Publish flag</param>
        private void OnMqttMsgPublished(ushort messageId, bool isPublished) {
            MqttMsgPublished?.Invoke(this, new MqttMsgPublishedEventArgs(messageId, isPublished));
        }

        /// <summary>
        /// Wrapper method for raising subscribed topic event
        /// </summary>
        /// <param name="suback">SUBACK message received</param>
        private void OnMqttMsgSubscribed(MqttMsgSuback suback) {
            MqttMsgSubscribed?.Invoke(this, new MqttMsgSubscribedEventArgs(suback.MessageId, suback.GrantedQoSLevels));
        }

        /// <summary>
        /// Wrapper method for raising unsubscribed topic event
        /// </summary>
        /// <param name="messageId">Message identifier for unsubscribed topic</param>
        private void OnMqttMsgUnsubscribed(ushort messageId) {
            MqttMsgUnsubscribed?.Invoke(this, new MqttMsgUnsubscribedEventArgs(messageId));
        }

        /// <summary>
        /// Wrapper method for peer/client disconnection
        /// </summary>
        private void OnConnectionClosed() {
            ConnectionClosed?.Invoke(this, EventArgs.Empty);
        }
    }
}
