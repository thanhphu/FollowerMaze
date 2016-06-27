using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace FollowerMazeServer
{
    class Client
    {
        const int BufferSize = 1024;

        public int ID { get; internal set; }
        BackgroundWorker Receiver;
        TcpClient Connection;

        // Triggered when the client sends its ID
        public event EventHandler OnIDAvailable;

        // Triggered when the client disconnects
        public event EventHandler OnDisconnect;

        // Triggered when a new command is received
        public event EventHandler OnCommandAvailable;

        public Client(TcpClient _Connection)
        {
            this.Connection = _Connection;            
            this.Receiver = new BackgroundWorker();
            this.Receiver.DoWork += Receiver_DoWork;
            this.Receiver.RunWorkerAsync();

            OnCommandAvailable += Client_OnCommandAvailable;
        }

        private void Client_OnCommandAvailable(object sender, EventArgs e)
        {
            throw new NotImplementedException();
        }

        private void Receiver_DoWork(object sender, DoWorkEventArgs e)
        {
            NetworkStream networkStream = Connection.GetStream();
            byte[] Buffer = new Byte[0];

            while (this.Connection.Connected)
            {
                // Read new data
                byte[] Incoming = new byte[BufferSize];
                int ReadBytes = networkStream.Read(Incoming, 0, BufferSize);

                // Append the previous data to the new data
                {
                    int newLength = ReadBytes + Buffer.Length;
                    byte[] NewBuffer = new byte[newLength];
                    Array.Copy(Buffer, NewBuffer, Buffer.Length);
                    Array.Copy(Incoming, 0, NewBuffer, Buffer.Length, ReadBytes);
                    Buffer = NewBuffer;
                }

                // While there's a message to process
                // Finds the new line in appended data
                for (int? Position = FindNewLine(Buffer); Position.HasValue; Position = FindNewLine(Buffer))
                {
                    // Extracts the command
                    string Command = System.Text.Encoding.UTF8.GetString(Buffer, 0, Position.Value);
                    OnCommandAvailable(this, new EventArgs());

                    // Trim the processed command from the buffer
                    byte[] NewBuffer = new byte[Position.Value];
                    Array.Copy(Buffer, 0, NewBuffer, 0, Buffer.Length - Position.Value);
                    Buffer = NewBuffer;
                }
            }
            // Process the remaining buffer before quitting
            this.Receiver.CancelAsync();
            OnDisconnect(this, null);
        }

        private static int? FindNewLine(byte[] buffer)
        {
            for (int i = 0; i < buffer.Length - 1; i++)
            {
                // The new line
                if (buffer[i] == '\r' && buffer[i + 1] == '\n')
                    return i;
            }
            return null;
        }
    }
}
