/*
Copyright (c) 2021 Simonas Greicius

All rights reserved. This program and the accompanying materials
are made available under the terms of the Eclipse Public License v1.0
and Eclipse Distribution License v1.0 which accompany this distribution. 

The Eclipse Public License is available at 
   http://www.eclipse.org/legal/epl-v10.html
and the Eclipse Distribution License is available at 
   http://www.eclipse.org/org/documents/edl-v10.php.

Contributors:
   Simonas Greicius - connection rework 2021
*/

using System;

namespace Tevux.Protocols.Mqtt {
    public class MqttConnectionOptions {
        public string ClientId { get; private set; } = "";
        public string Username { get; private set; }
        public string Password { get; private set; }
        public bool IsCleanSession { get; private set; } = true;
        public ushort KeepAlivePeriod { get; private set; } = 15;
        public bool IsWillUsed { get; private set; } = false;
        public string WillTopic { get; private set; } = "";
        public byte[] WillMessage { get; private set; } = new byte[0];
        public QosLevel WillQosLevel { get; private set; } = QosLevel.AtMostOnce;
        public bool IsWillRetained { get; private set; }
        public int MaxRetryCount { get; private set; } = 3;
        public int RetryDelay { get; private set; } = 10;

        public MqttConnectionOptions() {
            ClientId = Guid.NewGuid().ToString();
        }

        public void SetClientId(string clientId) {
            if (string.IsNullOrEmpty(clientId)) { throw new ArgumentException($"Argument '{nameof(clientId)}' has to be a valid non-empty string", nameof(clientId)); }

            ClientId = clientId;
        }

        public void SetCredencials(string username, string password) {
            if (string.IsNullOrEmpty(username)) { throw new ArgumentException($"Argument '{nameof(username)}' has to be a valid non-empty string", nameof(username)); }
            if (password == null) { throw new ArgumentNullException(nameof(password)); }

            Username = username;
            Password = password;
        }

        public void SetCleanSession(bool cleanSession) {
            IsCleanSession = cleanSession;
        }

        public void SetKeepAlivePeriod(ushort keepalivePeriod) {
            KeepAlivePeriod = keepalivePeriod;
        }

        public void SetWill(string topic, byte[] message, QosLevel qosLevel, bool retain) {
            if (string.IsNullOrEmpty(topic)) { throw new ArgumentException($"Argument '{nameof(topic)}' has to be a valid non-empty string", nameof(topic)); }
            if (message == null) { throw new ArgumentNullException(nameof(message)); }

            IsWillUsed = true;

            WillTopic = topic;
            WillMessage = message;
            WillQosLevel = qosLevel;
            IsWillRetained = retain;
        }

        public void SetRetransmissionParameters(int maxRetryCount, int retryDelay) {
            MaxRetryCount = maxRetryCount;
            RetryDelay = retryDelay;
        }
    }
}
