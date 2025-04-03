using System;
using System.Data;
using System.Data.SqlTypes;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

using LibData;

// ReceiveFrom();
class Program
{
    static void Main(string[] args)
    {
        ServerUDP.start();
    }
}

public class Setting
{
    public int ServerPortNumber { get; set; }
    public string? ServerIPAddress { get; set; }
    public int ClientPortNumber { get; set; }
    public string? ClientIPAddress { get; set; }
}


class ServerUDP
{
    static string configFile = @"../Setting.json";
    static string configContent = File.ReadAllText(configFile);
    static Setting? setting = JsonSerializer.Deserialize<Setting>(configContent);

    static IPEndPoint? serverEndPoint;    
    static Socket? serverSocket;
    
    static EndPoint? clientEndPoint;


    // TODO: [Read the JSON file and return the list of DNSRecords]
    static List<DNSRecord>? DNSRecords = JsonSerializer.Deserialize<List<DNSRecord>>(File.ReadAllText("./DNSrecords.json"));

    public static void start() 
    {
        // TODO: [Create a socket and endpoints and bind it to the server IP address and port number]
        serverEndPoint = new IPEndPoint(IPAddress.Parse(setting.ServerIPAddress), setting.ServerPortNumber);
        serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        clientEndPoint = new IPEndPoint(IPAddress.Parse(setting.ClientIPAddress), setting.ClientPortNumber);
        serverSocket.Bind(serverEndPoint);
        serverSocket.SendTimeout = 10000; // so it doesn't unexpectedly block
        serverSocket.ReceiveTimeout = 10000; // so it doesn't unexpectedly block

        byte[] buffer = new byte[1024];

        while (true)
        {
            try
            {
                clientEndPoint = new IPEndPoint(IPAddress.Any, 0);
                EndPoint remoteEndPoint = (EndPoint)clientEndPoint;

                // Receive message from client
                int receivedBytes = serverSocket.ReceiveFrom(buffer, ref remoteEndPoint);
                string receivedString = Encoding.UTF8.GetString(buffer, 0, receivedBytes);
                Message? receivedMessage = JsonSerializer.Deserialize<Message>(receivedString);

                if (receivedMessage == null)
                {
                    Console.WriteLine("Error: Received invalid message.");
                    continue;
                }

                Console.WriteLine($"Received from {remoteEndPoint}: {receivedMessage.MsgType}");

                switch (receivedMessage.MsgType)
                {
                    case MessageType.Hello:
                        SendWelcome(receivedMessage);
                        break;

                    case MessageType.DNSLookup:
                        if (receivedMessage.Content is not string domain)
                        {
                            SendMessage(remoteEndPoint, new Message { MsgId = receivedMessage.MsgId, MsgType = MessageType.Error, Content = "Invalid DNSLookup request" });
                            break;
                        }

                        DNSRecord? record = DNSRecords.Find(d => d.Name.Equals(domain, StringComparison.OrdinalIgnoreCase));
                        if (record != null)
                        {
                            SendMessage(remoteEndPoint, new Message { MsgId = receivedMessage.MsgId, MsgType = MessageType.DNSLookupReply, Content = record });
                        }
                        else
                        {
                            SendMessage(remoteEndPoint, new Message { MsgId = receivedMessage.MsgId, MsgType = MessageType.Error, Content = "Domain not found" });
                        }
                        break;

                    case MessageType.Ack:
                        Console.WriteLine("Client acknowledged the response.");
                        break;

                    case MessageType.End:
                        Console.WriteLine("Client has ended communication.");
                        serverSocket.Close();
                        return;

                    default:
                        Console.WriteLine("Received unknown message type.");
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }
    }

    public static bool ReceiveHello()
    {

        Console.WriteLine($"ReceiveHello(): Server {setting.ServerIPAddress}:{setting.ServerPortNumber} received from client {setting.ClientIPAddress}:{setting.ClientPortNumber} a message:{receivedMessage} ");
        return true;

    }

    // TODO:[Send Welcome to the client]
    public static bool SendWelcome(Message receivedmessage)
    {
//         { “MsgId”: “4” , “MsgType”: "Welcome", “Content": “Welcome
// from server”}
        Message welcomeMessage = new Message();
        welcomeMessage.MsgId = receivedmessage.MsgId;
        welcomeMessage.MsgType = MessageType.Welcome;
        welcomeMessage.Content = "Welcome from server";
        var welcomeJson = JsonSerializer.Serialize(welcomeMessage);

        
        serverSocket.SendTo(Encoding.UTF8.GetBytes(welcomeJson), clientEndPoint);
        return true;
    }

    // TODO:[Receive and print DNSLookup]
    public static bool ReceiveAndPrintDNSLookup()
    {
        byte[] buffer = new byte[1024];
        int receivedBytesCount = serverSocket.ReceiveFrom(buffer, ref clientEndPoint);
        string receivedString = Encoding.UTF8.GetString(buffer, 0, receivedBytesCount);
        Message? receivedMessage = JsonSerializer.Deserialize<Message>(receivedString);
        if(receivedMessage is null){
            Console.WriteLine("ReceiveAndPrintDNSLookup(): A message object was expected but not received.");
            return false;
        }
        if(receivedMessage.MsgType != MessageType.DNSLookup){
            Console.WriteLine("ReceiveAndPrintDNSLookup(): The received message was not of type MessageType.DNSLookup.");
            return false;
        }
        Console.WriteLine($"ReceiveAndPrintDNSLookup(): Server {setting.ServerIPAddress}:{setting.ServerPortNumber} received from Client {setting.ClientIPAddress}:{setting.ClientPortNumber} a message:{receivedMessage} ");
        return true;
    }

    // TODO:[Query the DNSRecord in Json file]
    public static bool QueryDNSRecord()
    {
        return true;
    }

    // TODO:[If found Send DNSLookupReply containing the DNSRecord]
    public static bool SendDNSReply()
    {
        return true;
    }

    // TODO:[If not found Send Error]
    public static bool SendDNSNotFoundError()
    {
        return true;
    }

    // TODO:[Receive Ack about correct DNSLookupReply from the client]
    public static bool ReceiveAck()
    {
        return true;
    }

    // TODO:[If no further requests receieved send End to the client]
    public static bool SendEnd()
    {
        
        return true;
    }
}