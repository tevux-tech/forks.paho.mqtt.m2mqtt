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

using uPLibrary.Networking.M2Mqtt.Internal;
using uPLibrary.Networking.M2Mqtt.Messages;

namespace uPLibrary.Networking.M2Mqtt {
    public partial class MqttClient {
        private void DispatchEventThread() {
            while (_isRunning) {
                if ((_eventQueue.Count == 0) && !_isConnectionClosing) {
                    _receiveEventWaitHandle.WaitOne();
                }

                // check if it is running or we are closing client
                if (_isRunning) {
                    if (_eventQueue.TryDequeue(out var internalEvent)) {

                        var msg = ((MsgInternalEvent)internalEvent).Message;

                        if (msg != null) {
                            switch (msg.Type) {
                                case MqttMsgBase.MessageType.Publish:
                                    // PUBLISH message received in a published internal event, no publish succeeded
                                    if (internalEvent.GetType() == typeof(MsgPublishedInternalEvent)) {
                                        OnMqttMsgPublished(msg.MessageId, false);
                                    }
                                    else {
                                        OnMqttMsgPublishReceived((MqttMsgPublish)msg);
                                    }
                                    break;

                                //case MqttMsgBase.MessageType.PubAck:
                                //    // (PUBACK received for QoS Level 1)
                                //    OnMqttMsgPublished(msg.MessageId, true);
                                //    break;

                                case MqttMsgBase.MessageType.PubRel:
                                    // (PUBREL received for QoS Level 2)
                                    OnMqttMsgPublishReceived((MqttMsgPublish)msg);
                                    break;

                                    //case MqttMsgBase.MessageType.PubComp:
                                    //    // (PUBCOMP received for QoS Level 2)
                                    //    OnMqttMsgPublished(msg.MessageId, true);
                                    //    break;
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
