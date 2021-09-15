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

using uPLibrary.Networking.M2Mqtt.Exceptions;
using uPLibrary.Networking.M2Mqtt.Internal;
using uPLibrary.Networking.M2Mqtt.Messages;

namespace uPLibrary.Networking.M2Mqtt {
    public partial class MqttClient {
        /// <summary>
        /// Thread for raising event
        /// </summary>
        private void DispatchEventThread() {
            while (_isRunning) {
                if ((_eventQueue.Count == 0) && !_isConnectionClosing) {
                    // wait on receiving message from client
                    _receiveEventWaitHandle.WaitOne();
                }

                // check if it is running or we are closing client
                if (_isRunning) {
                    // get event from queue

                    if (_eventQueue.TryDequeue(out var internalEvent)) {

                        // TODO: remove this green code once proven that ConcurrentQueue works.
                        //InternalEvent internalEvent = null;
                        //lock (_eventQueue) {
                        //    if (_eventQueue.Count > 0) {
                        //        internalEvent = (InternalEvent)_eventQueue.Dequeue();
                        //    }
                        //}

                        //// it's an event with a message inside
                        //if (internalEvent != null) {

                        var msg = ((MsgInternalEvent)internalEvent).Message;

                        if (msg != null) {
                            switch (msg.Type) {
                                case MqttMsgBase.MessageType.Connect:
                                    throw new MqttClientException(MqttClientErrorCode.WrongBrokerMessage);

                                // SUBSCRIBE message received
                                case MqttMsgBase.MessageType.Subscribe:
                                    throw new MqttClientException(MqttClientErrorCode.WrongBrokerMessage);

                                // SUBACK message received
                                case MqttMsgBase.MessageType.SubAck:

                                    // raise subscribed topic event (SUBACK message received)
                                    OnMqttMsgSubscribed((MqttMsgSuback)msg);
                                    break;

                                // PUBLISH message received
                                case MqttMsgBase.MessageType.Publish:

                                    // PUBLISH message received in a published internal event, no publish succeeded
                                    if (internalEvent.GetType() == typeof(MsgPublishedInternalEvent)) {
                                        OnMqttMsgPublished(msg.MessageId, false);
                                    }
                                    else {
                                        // raise PUBLISH message received event 
                                        OnMqttMsgPublishReceived((MqttMsgPublish)msg);
                                    }

                                    break;

                                // PUBACK message received
                                case MqttMsgBase.MessageType.PubAck:

                                    // raise published message event
                                    // (PUBACK received for QoS Level 1)
                                    OnMqttMsgPublished(msg.MessageId, true);
                                    break;

                                // PUBREL message received
                                case MqttMsgBase.MessageType.PubRel:

                                    // raise message received event 
                                    // (PUBREL received for QoS Level 2)
                                    OnMqttMsgPublishReceived((MqttMsgPublish)msg);
                                    break;

                                // PUBCOMP message received
                                case MqttMsgBase.MessageType.PubComp:

                                    // raise published message event
                                    // (PUBCOMP received for QoS Level 2)
                                    OnMqttMsgPublished(msg.MessageId, true);
                                    break;

                                // UNSUBSCRIBE message received from client
                                case MqttMsgBase.MessageType.Unsubscribe:
                                    throw new MqttClientException(MqttClientErrorCode.WrongBrokerMessage);

                                // UNSUBACK message received
                                case MqttMsgBase.MessageType.UnsubAck:

                                    // raise unsubscribed topic event
                                    OnMqttMsgUnsubscribed(msg.MessageId);
                                    break;

                                // DISCONNECT message received from client
                                case MqttMsgBase.MessageType.Disconnect:
                                    throw new MqttClientException(MqttClientErrorCode.WrongBrokerMessage);
                            }
                        }
                    }

                    // all events for received messages dispatched, check if there is closing connection
                    if ((_eventQueue.Count == 0) && _isConnectionClosing) {
                        // client must close connection
                        Close();

                        // client raw disconnection
                        OnConnectionClosed();
                    }
                }
            }
        }
    }


}
