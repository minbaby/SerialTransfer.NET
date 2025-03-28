using System;
using System.IO.Ports;
using System.Text;
using System.Threading;
using SerialTransfer;

namespace SerialTransferDemo
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("SerialTransfer C# Sample Application");
            Console.WriteLine("-----------------------------------");

            // List available ports
            string[] ports = SerialPort.GetPortNames();
            Console.WriteLine("Available ports:");
            foreach (string port in ports)
            {
                Console.WriteLine($"  - {port}");
            }

            if (ports.Length == 0)
            {
                Console.WriteLine("No COM ports found. Please connect a device and try again.");
                Console.ReadKey();
                return;
            }

            // Ask user to select a port
            Console.Write("\nEnter COM port to use (e.g., COM3): ");
            string selectedPort = Console.ReadLine();

            try
            {
                // Create and configure the SerialPort
                SerialPort port = SerialTransfer.SerialTransfer.ConfigurePort(
                    portName: selectedPort,
                    baudRate: 115200,
                    parity: Parity.None,
                    dataBits: 8,
                    stopBits: StopBits.One
                );

                // Create and initialize the SerialTransfer instance
                SerialTransfer.SerialTransfer transfer = new SerialTransfer.SerialTransfer();
                transfer.Begin(port, true);

                Console.WriteLine($"\nConnected to {selectedPort} at 115200 baud");
                Console.WriteLine("\nCommands:");
                Console.WriteLine("  's' - Send a test packet");
                Console.WriteLine("  'r' - Receive data (checks for incoming packets)");
                Console.WriteLine("  'b' - Send binary data (struct)");
                Console.WriteLine("  'q' - Quit the application");

                bool running = true;
                while (running)
                {
                    Console.Write("\n> ");
                    string command = Console.ReadLine().ToLower();

                    switch (command)
                    {
                        case "s":
                            SendTestPacket(transfer);
                            break;

                        case "r":
                            ReceiveData(transfer);
                            break;

                        case "b":
                            SendBinaryData(transfer);
                            break;

                        case "q":
                            running = false;
                            break;

                        default:
                            Console.WriteLine("Unknown command. Try again.");
                            break;
                    }
                }

                // Clean up
                transfer.Close();
                Console.WriteLine("Port closed. Application terminated.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                Console.WriteLine("Press any key to exit...");
                Console.ReadKey();
            }
        }

        static void SendTestPacket(SerialTransfer.SerialTransfer transfer)
        {
            try
            {
                // Prepare a test message
                string message = "Hello from C# SerialTransfer!";
                byte[] messageBytes = Encoding.ASCII.GetBytes(message);

                // Copy message to the transmit buffer
                for (int i = 0; i < messageBytes.Length; i++)
                {
                    transfer.Packet.TxBuff[i] = messageBytes[i];
                }

                // Send the packet with ID 1
                byte bytesSent = transfer.SendData((ushort)messageBytes.Length, 1);
                Console.WriteLine($"Sent {bytesSent} bytes with packet ID 1");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Send error: {ex.Message}");
            }
        }

        static void ReceiveData(SerialTransfer.SerialTransfer transfer)
        {
            try
            {
                Console.WriteLine("Waiting for data (press any key to cancel)...");

                // Poll for incoming data for up to 10 seconds
                DateTime startTime = DateTime.Now;
                bool dataReceived = false;

                while ((DateTime.Now - startTime).TotalSeconds < 10)
                {
                    if (Console.KeyAvailable)
                    {
                        Console.ReadKey(true);
                        break;
                    }

                    // Check for available data
                    byte bytesReceived = transfer.Available();

                    if (bytesReceived > 0)
                    {
                        dataReceived = true;

                        // Get the packet ID
                        byte packetId = transfer.CurrentPacketId();

                        Console.WriteLine($"Received {bytesReceived} bytes with packet ID {packetId}");

                        // Convert the received bytes to a string if it's text
                        if (packetId == 1) // Assuming packet ID 1 is for text messages
                        {
                            string message = Encoding.ASCII.GetString(transfer.Packet.RxBuff, 0, bytesReceived);
                            Console.WriteLine($"Message: {message}");
                        }
                        else
                        {
                            // Display as hex for non-text data
                            Console.Write("Data (hex): ");
                            for (int i = 0; i < bytesReceived; i++)
                            {
                                Console.Write($"{transfer.Packet.RxBuff[i]:X2} ");
                            }
                            Console.WriteLine();
                        }

                        break;
                    }

                    Thread.Sleep(100); // Small delay to prevent CPU thrashing
                }

                if (!dataReceived)
                {
                    Console.WriteLine("No data received within timeout period.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Receive error: {ex.Message}");
            }
        }

        static void SendBinaryData(SerialTransfer.SerialTransfer transfer)
        {
            try
            {
                // Define a sample struct to send
                TestStruct testData = new TestStruct
                {
                    Id = 42,
                    Temperature = 23.5f,
                    Humidity = 65.0f,
                    Status = 1
                };

                // Send the struct (packet ID 2)
                byte bytesSent = transfer.SendDatum(testData);
                Console.WriteLine($"Sent binary struct ({bytesSent} bytes) with packet ID 2");
                Console.WriteLine($"Struct contents: ID={testData.Id}, Temp={testData.Temperature}°C, Humidity={testData.Humidity}%, Status={testData.Status}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Send error: {ex.Message}");
            }
        }

        // Sample struct for binary data transfer
        [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential, Pack = 1)]
        struct TestStruct
        {
            public int Id;
            public float Temperature;
            public float Humidity;
            public byte Status;
        }
    }
}