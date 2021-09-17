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
using System.Threading;
using uPLibrary.Networking.M2Mqtt.Exceptions;
using uPLibrary.Networking.M2Mqtt.Internal;
using uPLibrary.Networking.M2Mqtt.Messages;
using uPLibrary.Networking.M2Mqtt.Utility;

namespace uPLibrary.Networking.M2Mqtt {
    public partial class MqttClient {
        private void ProcessInflightThread() {
            MqttMsgContext msgContext = null;
            MqttMsgBase msgReceived = null;
            InternalEvent internalEvent;
            var timeout = Timeout.Infinite;
            int delta;
            var msgReceivedProcessed = false;

            try {
                while (_isRunning) {
                    // wait on message queueud to inflight
                    _inflightWaitHandle.WaitOne(timeout);

                    // it could be unblocked because Close() method is joining
                    if (_isRunning) {
                        lock (_inflightQueue) {
                            // message received and peeked from internal queue is processed
                            // NOTE : it has the corresponding message in inflight queue based on messageId
                            //        (ex. a PUBREC for a PUBLISH)
                            //        if it's orphan we need to remove from internal queue
                            msgReceivedProcessed = false;
                            var acknowledge = false;
                            msgReceived = null;

                            // set timeout tu MaxValue instead of Infinte (-1) to perform
                            // compare with calcultad current msgTimeout
                            timeout = int.MaxValue;

                            // a message inflight could be re-enqueued but we have to
                            // analyze it only just one time for cycle
                            var count = _inflightQueue.Count;
                            // process all inflight queued messages
                            while (count > 0) {
                                count--;
                                acknowledge = false;
                                msgReceived = null;

                                // check to be sure that client isn't closing and all queues are now empty !
                                if (!_isRunning) {
                                    break;
                                }

                                // dequeue message context from queue
                                msgContext = (MqttMsgContext)_inflightQueue.Dequeue();

                                // get inflight message
                                var msgInflight = (MqttMsgBase)msgContext.Message;

                                switch (msgContext.State) {
                                    case MqttMsgState.QueuedQos0:

                                        // QoS 0, PUBLISH message to send to broker, no state change, no acknowledge
                                        if (msgContext.Flow == MqttMsgFlow.ToPublish) {
                                            Send((ISentToBroker)msgInflight);
                                        }
                                        // QoS 0, no need acknowledge
                                        else if (msgContext.Flow == MqttMsgFlow.ToAcknowledge) {
                                            internalEvent = new MsgInternalEvent(msgInflight);
                                            // notify published message from broker (no need acknowledged)
                                            OnInternalEvent(internalEvent);
                                        }


                                        Trace.WriteLine(TraceLevel.Queuing, "processed {0}", msgInflight);

                                        break;

                                    case MqttMsgState.QueuedQos1:

                                        // QoS 1, PUBLISH  to send to broker, state change to wait PUBACK 
                                        if (msgContext.Flow == MqttMsgFlow.ToPublish) {
                                            msgContext.Timestamp = Environment.TickCount;
                                            msgContext.Attempt++;

                                            if (msgInflight.Type == MqttMsgBase.MessageType.Publish) {
                                                // PUBLISH message to send, wait for PUBACK
                                                msgContext.State = MqttMsgState.WaitForPuback;
                                                // retry ? set dup flag [v3.1.1] only for PUBLISH message
                                                if (msgContext.Attempt > 1) {
                                                    msgInflight.DupFlag = true;
                                                }
                                            }

                                            Send((ISentToBroker)msgInflight);

                                            // update timeout : minimum between delay (based on current message sent) or current timeout
                                            timeout = (_settings.DelayOnRetry < timeout) ? _settings.DelayOnRetry : timeout;

                                            // re-enqueue message (I have to re-analyze for receiving PUBACK
                                            _inflightQueue.Enqueue(msgContext);
                                        }
                                        // QoS 1, PUBLISH message received from broker to acknowledge, send PUBACK
                                        else if (msgContext.Flow == MqttMsgFlow.ToAcknowledge) {
                                            var puback = new MqttMsgPuback {
                                                MessageId = msgInflight.MessageId
                                            };

                                            Send(puback);

                                            internalEvent = new MsgInternalEvent(msgInflight);
                                            // notify published message from broker and acknowledged
                                            OnInternalEvent(internalEvent);

                                            Trace.WriteLine(TraceLevel.Queuing, "processed {0}", msgInflight);
                                        }
                                        break;

                                    case MqttMsgState.QueuedQos2:

                                        // QoS 2, PUBLISH message to send to broker, state change to wait PUBREC
                                        if (msgContext.Flow == MqttMsgFlow.ToPublish) {
                                            msgContext.Timestamp = Environment.TickCount;
                                            msgContext.Attempt++;
                                            msgContext.State = MqttMsgState.WaitForPubrec;
                                            // retry ? set dup flag
                                            if (msgContext.Attempt > 1) {
                                                msgInflight.DupFlag = true;
                                            }

                                            Send((ISentToBroker)msgInflight);

                                            // update timeout : minimum between delay (based on current message sent) or current timeout
                                            timeout = (_settings.DelayOnRetry < timeout) ? _settings.DelayOnRetry : timeout;

                                            // re-enqueue message (I have to re-analyze for receiving PUBREC)
                                            _inflightQueue.Enqueue(msgContext);
                                        }
                                        // QoS 2, PUBLISH message received from broker to acknowledge, send PUBREC, state change to wait PUBREL
                                        else if (msgContext.Flow == MqttMsgFlow.ToAcknowledge) {
                                            var pubrec = new MqttMsgPubrec {
                                                MessageId = msgInflight.MessageId
                                            };

                                            msgContext.State = MqttMsgState.WaitForPubrel;

                                            Send(pubrec);

                                            // re-enqueue message (I have to re-analyze for receiving PUBREL)
                                            _inflightQueue.Enqueue(msgContext);
                                        }
                                        break;

                                    case MqttMsgState.WaitForPuback:

                                        // QoS 1, waiting for PUBACK of a PUBLISH message sent
                                        if (msgContext.Flow == MqttMsgFlow.ToPublish) {
                                            acknowledge = false;
                                            lock (_internalQueue) {
                                                if (_internalQueue.Count > 0) {
                                                    msgReceived = (MqttMsgBase)_internalQueue.Peek();
                                                }
                                            }

                                            // it is a PUBACK message
                                            if (msgReceived != null) {
                                                // PUBACK message for the current message
                                                if ((msgReceived.Type == MqttMsgBase.MessageType.PubAck) && (msgInflight.Type == MqttMsgBase.MessageType.Publish) && (msgReceived.MessageId == msgInflight.MessageId)) {
                                                    lock (_internalQueue) {
                                                        // received message processed
                                                        _internalQueue.Dequeue();
                                                        acknowledge = true;
                                                        msgReceivedProcessed = true;

                                                        Trace.WriteLine(TraceLevel.Queuing, "dequeued {0}", msgReceived);
                                                    }

                                                    // if PUBACK received, confirm published with flag
                                                    if (msgReceived.Type == MqttMsgBase.MessageType.PubAck) {
                                                        internalEvent = new MsgPublishedInternalEvent(msgReceived, true);
                                                    }
                                                    else {
                                                        internalEvent = new MsgInternalEvent(msgReceived);
                                                    }

                                                    // notify received acknowledge from broker of a published message or subscribe/unsubscribe message
                                                    OnInternalEvent(internalEvent);

                                                    // PUBACK received for PUBLISH message with QoS Level 1, remove from session state
                                                    if ((msgInflight.Type == MqttMsgBase.MessageType.Publish) && (_session != null) && _session.InflightMessages.ContainsKey(msgContext.Key)) {
                                                        _session.InflightMessages.Remove(msgContext.Key);
                                                    }

                                                    Trace.WriteLine(TraceLevel.Queuing, "processed {0}", msgInflight);
                                                }
                                            }

                                            // current message not acknowledged, no PUBACK or SUBACK/UNSUBACK or not equal messageid 
                                            if (!acknowledge) {
                                                delta = Environment.TickCount - msgContext.Timestamp;
                                                // check timeout for receiving PUBACK since PUBLISH was sent 

                                                if (delta >= _settings.DelayOnRetry) {
                                                    // max retry not reached, resend
                                                    if (msgContext.Attempt < _settings.AttemptsOnRetry) {
                                                        msgContext.State = MqttMsgState.QueuedQos1;

                                                        // re-enqueue message
                                                        _inflightQueue.Enqueue(msgContext);

                                                        // update timeout (0 -> reanalyze queue immediately)
                                                        timeout = 0;
                                                    }
                                                    else {
                                                        // if PUBACK for a PUBLISH message not received after retries, raise event for not published
                                                        if (msgInflight.Type == MqttMsgBase.MessageType.Publish) {
                                                            // PUBACK not received in time, PUBLISH retries failed, need to remove from session inflight messages too
                                                            if ((_session != null) && _session.InflightMessages.ContainsKey(msgContext.Key)) {
                                                                _session.InflightMessages.Remove(msgContext.Key);
                                                            }

                                                            internalEvent = new MsgPublishedInternalEvent(msgInflight, false);

                                                            // notify not received acknowledge from broker and message not published
                                                            OnInternalEvent(internalEvent);
                                                        }
                                                        // NOTE : not raise events for SUBACK or UNSUBACK not received
                                                        //        for the user no event raised means subscribe/unsubscribe failed
                                                    }
                                                }
                                                else {
                                                    // re-enqueue message (I have to re-analyze for receiving PUBACK, SUBACK or UNSUBACK)
                                                    _inflightQueue.Enqueue(msgContext);

                                                    // update timeout
                                                    var msgTimeout = (_settings.DelayOnRetry - delta);
                                                    timeout = (msgTimeout < timeout) ? msgTimeout : timeout;
                                                }
                                            }
                                        }
                                        break;

                                    case MqttMsgState.WaitForPubrec:

                                        // QoS 2, waiting for PUBREC of a PUBLISH message sent
                                        if (msgContext.Flow == MqttMsgFlow.ToPublish) {
                                            acknowledge = false;
                                            lock (_internalQueue) {
                                                if (_internalQueue.Count > 0) {
                                                    msgReceived = (MqttMsgBase)_internalQueue.Peek();
                                                }
                                            }

                                            // it is a PUBREC message
                                            if ((msgReceived != null) && (msgReceived.Type == MqttMsgBase.MessageType.PubRec)) {
                                                // PUBREC message for the current PUBLISH message, send PUBREL, wait for PUBCOMP
                                                if (msgReceived.MessageId == msgInflight.MessageId) {
                                                    lock (_internalQueue) {
                                                        // received message processed
                                                        _internalQueue.Dequeue();
                                                        acknowledge = true;
                                                        msgReceivedProcessed = true;

                                                        Trace.WriteLine(TraceLevel.Queuing, "dequeued {0}", msgReceived);
                                                    }

                                                    var pubrel = new MqttMsgPubrel {
                                                        MessageId = msgInflight.MessageId
                                                    };

                                                    msgContext.State = MqttMsgState.WaitForPubcomp;
                                                    msgContext.Timestamp = Environment.TickCount;
                                                    msgContext.Attempt = 1;

                                                    Send(pubrel);

                                                    // update timeout : minimum between delay (based on current message sent) or current timeout
                                                    timeout = (_settings.DelayOnRetry < timeout) ? _settings.DelayOnRetry : timeout;

                                                    // re-enqueue message
                                                    _inflightQueue.Enqueue(msgContext);
                                                }
                                            }

                                            // current message not acknowledged
                                            if (!acknowledge) {
                                                delta = Environment.TickCount - msgContext.Timestamp;
                                                // check timeout for receiving PUBREC since PUBLISH was sent
                                                if (delta >= _settings.DelayOnRetry) {
                                                    // max retry not reached, resend
                                                    if (msgContext.Attempt < _settings.AttemptsOnRetry) {
                                                        msgContext.State = MqttMsgState.QueuedQos2;

                                                        // re-enqueue message
                                                        _inflightQueue.Enqueue(msgContext);

                                                        // update timeout (0 -> reanalyze queue immediately)
                                                        timeout = 0;
                                                    }
                                                    else {
                                                        // PUBREC not received in time, PUBLISH retries failed, need to remove from session inflight messages too
                                                        if ((_session != null) && _session.InflightMessages.ContainsKey(msgContext.Key)) {
                                                            _session.InflightMessages.Remove(msgContext.Key);
                                                        }

                                                        // if PUBREC for a PUBLISH message not received after retries, raise event for not published
                                                        internalEvent = new MsgPublishedInternalEvent(msgInflight, false);
                                                        // notify not received acknowledge from broker and message not published
                                                        OnInternalEvent(internalEvent);
                                                    }
                                                }
                                                else {
                                                    // re-enqueue message
                                                    _inflightQueue.Enqueue(msgContext);

                                                    // update timeout
                                                    var msgTimeout = (_settings.DelayOnRetry - delta);
                                                    timeout = (msgTimeout < timeout) ? msgTimeout : timeout;
                                                }
                                            }
                                        }
                                        break;

                                    case MqttMsgState.WaitForPubrel:

                                        // QoS 2, waiting for PUBREL of a PUBREC message sent
                                        if (msgContext.Flow == MqttMsgFlow.ToAcknowledge) {
                                            lock (_internalQueue) {
                                                if (_internalQueue.Count > 0) {
                                                    msgReceived = (MqttMsgBase)_internalQueue.Peek();
                                                }
                                            }

                                            // it is a PUBREL message
                                            if ((msgReceived != null) && (msgReceived.Type == MqttMsgBase.MessageType.PubRel)) {
                                                // PUBREL message for the current message, send PUBCOMP
                                                if (msgReceived.MessageId == msgInflight.MessageId) {
                                                    lock (_internalQueue) {
                                                        // received message processed
                                                        _internalQueue.Dequeue();
                                                        msgReceivedProcessed = true;

                                                        Trace.WriteLine(TraceLevel.Queuing, "dequeued {0}", msgReceived);
                                                    }

                                                    var pubcomp = new MqttMsgPubcomp {
                                                        MessageId = msgInflight.MessageId
                                                    };

                                                    Send(pubcomp);

                                                    internalEvent = new MsgInternalEvent(msgInflight);
                                                    // notify published message from broker and acknowledged
                                                    OnInternalEvent(internalEvent);

                                                    // PUBREL received (and PUBCOMP sent) for PUBLISH message with QoS Level 2, remove from session state
                                                    if ((msgInflight.Type == MqttMsgBase.MessageType.Publish) &&
                                                        (_session != null) &&
                                                        _session.InflightMessages.ContainsKey(msgContext.Key)) {
                                                        _session.InflightMessages.Remove(msgContext.Key);
                                                    }

                                                    Trace.WriteLine(TraceLevel.Queuing, "processed {0}", msgInflight);
                                                }
                                                else {
                                                    // re-enqueue message
                                                    _inflightQueue.Enqueue(msgContext);
                                                }
                                            }
                                            else {
                                                // re-enqueue message
                                                _inflightQueue.Enqueue(msgContext);
                                            }
                                        }
                                        break;

                                    case MqttMsgState.WaitForPubcomp:

                                        // QoS 2, waiting for PUBCOMP of a PUBREL message sent
                                        if (msgContext.Flow == MqttMsgFlow.ToPublish) {
                                            acknowledge = false;
                                            lock (_internalQueue) {
                                                if (_internalQueue.Count > 0) {
                                                    msgReceived = (MqttMsgBase)_internalQueue.Peek();
                                                }
                                            }

                                            // it is a PUBCOMP message
                                            if ((msgReceived != null) && (msgReceived.Type == MqttMsgBase.MessageType.PubComp)) {
                                                // PUBCOMP message for the current message
                                                if (msgReceived.MessageId == msgInflight.MessageId) {
                                                    lock (_internalQueue) {
                                                        // received message processed
                                                        _internalQueue.Dequeue();
                                                        acknowledge = true;
                                                        msgReceivedProcessed = true;

                                                        Trace.WriteLine(TraceLevel.Queuing, "dequeued {0}", msgReceived);
                                                    }

                                                    internalEvent = new MsgPublishedInternalEvent(msgReceived, true);
                                                    // notify received acknowledge from broker of a published message
                                                    OnInternalEvent(internalEvent);

                                                    // PUBCOMP received for PUBLISH message with QoS Level 2, remove from session state
                                                    if ((msgInflight.Type == MqttMsgBase.MessageType.Publish) && (_session != null) && _session.InflightMessages.ContainsKey(msgContext.Key)) {
                                                        _session.InflightMessages.Remove(msgContext.Key);
                                                    }

                                                    Trace.WriteLine(TraceLevel.Queuing, "processed {0}", msgInflight);
                                                }
                                            }
                                            // it is a PUBREC message
                                            else if ((msgReceived != null) && (msgReceived.Type == MqttMsgBase.MessageType.PubRec)) {
                                                // another PUBREC message for the current message due to a retransmitted PUBLISH
                                                // I'm in waiting for PUBCOMP, so I can discard this PUBREC
                                                if (msgReceived.MessageId == msgInflight.MessageId) {
                                                    lock (_internalQueue) {
                                                        // received message processed
                                                        _internalQueue.Dequeue();
                                                        acknowledge = true;
                                                        msgReceivedProcessed = true;
                                                        Trace.WriteLine(TraceLevel.Queuing, "dequeued {0}", msgReceived);

                                                        // re-enqueue message
                                                        _inflightQueue.Enqueue(msgContext);
                                                    }
                                                }
                                            }

                                            // current message not acknowledged
                                            if (!acknowledge) {
                                                delta = Environment.TickCount - msgContext.Timestamp;
                                                // check timeout for receiving PUBCOMP since PUBREL was sent
                                                if (delta >= _settings.DelayOnRetry) {
                                                    // max retry not reached, resend
                                                    if (msgContext.Attempt < _settings.AttemptsOnRetry) {
                                                        msgContext.State = MqttMsgState.SendPubrel;

                                                        // re-enqueue message
                                                        _inflightQueue.Enqueue(msgContext);

                                                        // update timeout (0 -> reanalyze queue immediately)
                                                        timeout = 0;
                                                    }
                                                    else {
                                                        // PUBCOMP not received, PUBREL retries failed, need to remove from session inflight messages too
                                                        if ((_session != null) && _session.InflightMessages.ContainsKey(msgContext.Key)) {
                                                            _session.InflightMessages.Remove(msgContext.Key);
                                                        }

                                                        // if PUBCOMP for a PUBLISH message not received after retries, raise event for not published
                                                        internalEvent = new MsgPublishedInternalEvent(msgInflight, false);
                                                        // notify not received acknowledge from broker and message not published
                                                        OnInternalEvent(internalEvent);
                                                    }
                                                }
                                                else {
                                                    // re-enqueue message
                                                    _inflightQueue.Enqueue(msgContext);

                                                    // update timeout
                                                    var msgTimeout = (_settings.DelayOnRetry - delta);
                                                    timeout = (msgTimeout < timeout) ? msgTimeout : timeout;
                                                }
                                            }
                                        }
                                        break;

                                    case MqttMsgState.SendPubrec:

                                        // TODO : impossible ? --> QueuedQos2 ToAcknowledge
                                        break;

                                    case MqttMsgState.SendPubrel:

                                        // QoS 2, PUBREL message to send to broker, state change to wait PUBCOMP
                                        if (msgContext.Flow == MqttMsgFlow.ToPublish) {
                                            var pubrel = new MqttMsgPubrel {
                                                MessageId = msgInflight.MessageId
                                            };

                                            msgContext.State = MqttMsgState.WaitForPubcomp;
                                            msgContext.Timestamp = Environment.TickCount;
                                            msgContext.Attempt++;

                                            Send(pubrel);

                                            // update timeout : minimum between delay (based on current message sent) or current timeout
                                            timeout = (_settings.DelayOnRetry < timeout) ? _settings.DelayOnRetry : timeout;

                                            // re-enqueue message
                                            _inflightQueue.Enqueue(msgContext);
                                        }
                                        break;

                                    case MqttMsgState.SendPubcomp:
                                        // TODO : impossible ?
                                        break;
                                    case MqttMsgState.SendPuback:
                                        // TODO : impossible ? --> QueuedQos1 ToAcknowledge
                                        break;
                                    default:
                                        break;
                                }
                            }

                            // if calculated timeout is MaxValue, it means that must be Infinite (-1)
                            if (timeout == int.MaxValue) {
                                timeout = Timeout.Infinite;
                            }

                            // if message received is orphan, no corresponding message in inflight queue
                            // based on messageId, we need to remove from the queue
                            if ((msgReceived != null) && !msgReceivedProcessed) {
                                _ = _internalQueue.Dequeue();

                                Trace.WriteLine(TraceLevel.Queuing, "dequeued {0} orphan", msgReceived);
                            }
                        }
                    }
                }
            }
            catch (MqttCommunicationException e) {
                // possible exception on Send, I need to re-enqueue not sent message
                if (msgContext != null) {
                    // re-enqueue message
                    _inflightQueue.Enqueue(msgContext);
                }

                Trace.WriteLine(TraceLevel.Error, "Exception occurred: {0}", e.ToString());

                // raise disconnection client event
                OnConnectionClosing();
            }
        }
    }


}
