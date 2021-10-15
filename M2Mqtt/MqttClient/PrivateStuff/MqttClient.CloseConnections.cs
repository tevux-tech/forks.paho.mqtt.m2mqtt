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
   Simonas Greicius - 2021 rework
*/

namespace Tevux.Protocols.Mqtt {
    public partial class MqttClient {
        private bool _isConnectionClosing;

        private void CloseConnections(bool isClosedManually = false) {
            if (_isConnectionClosing) { return; }

            _log.Info($"Shutting connections down due to {(isClosedManually ? "user request" : "external sources")}. ");

            _isConnectionClosing = true;
            IsConnected = false;
            _channel.Close();

            // This is a dummy packet, so it just fits my the event queue.
            _eventQueue.Enqueue(new EventSource(new DisconnectPacket(), null));

            _isConnectionClosing = false;
            if (_channelConnectionOptions.IsReconnectionEnabled && (isClosedManually == false)) { _isConnectionRequested = true; }
        }
    }
}
