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

    static int DNSMsgId;


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
        while (true) //keep the server running
        {
            // TODO:[Receive and print a received Message from the client]
        /*  byte[] buffer = new byte[1024];
            int receivedBytes = serverSocket.ReceiveFrom(buffer, ref clientEndPoint);
            string receivedString = Encoding.UTF8.GetString(buffer, 0, receivedBytes);
            Message? receivedMessage = JsonSerializer.Deserialize<Message>(receivedString);

            if (receivedMessage == null)
            {
                Console.WriteLine("Error: Received invalid message.");
            }

            Console.WriteLine($"Received from {clientEndPoint}: {receivedMessage.MsgType}"); */

            // TODO:[Receive and print Hello]
            if (!ReceiveHello())
            {
                Console.WriteLine("ReceiveHello step failed. Ending protocol");
                return;
            }
            // TODO:[Send Welcome to the client]
            if (!SendWelcome())
            {
                Console.WriteLine("SendWelcome step failed. Ending protocol");
                return;
            }
            int AckCount = 0;
            while (true)
            {
                // TODO:[Receive and print DNSLookup]
                (bool success, Message message) = ReceiveAndPrintDNS();
                if (!success)
                {
                    Console.WriteLine("ReceiveAndPrintDNS step failed. Ending protocol");
                    return;
                }
                // TODO:[Query the DNSRecord in Json file]
                // TODO:[If found Send DNSLookupReply containing the DNSRecord]
                // TODO:[If not found Send Error]
                DNSMsgId = message.MsgId; //assigning message.MsgId to DNSMsgId to ensure that the same number is used.
                if (!ProcessDNSLookup(message))
                {
                    Console.WriteLine("ProcessDNSLookup step failed. Ending protocol");
                    return;
                }
                // TODO:[Receive Ack about correct DNSLookupReply from the client]
                if (!ReceiveAck())
                {
                    Console.WriteLine("ReceiveAck step failed. Ending protocol");
                    return;
                }
                AckCount++;
                if (AckCount == 4)
                {
                    Console.WriteLine($"Received {AckCount} Acks. Sending End message to client.");
                    break;
                }

            }
            SendEnd();
            Console.WriteLine("End of communication. Awaiting next client.");
        }
    }

    public static bool SendMessage(Message message)
    {
        try
        {
            string jsonMessage = JsonSerializer.Serialize(message);
            serverSocket?.SendTo(Encoding.UTF8.GetBytes(jsonMessage), clientEndPoint);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error sending message: {ex.Message}");
            return false;
        }
        return true;
    }

    public static bool ReceiveHello()
    {
        byte[] buffer = new byte[1024];
        int receivedBytes = serverSocket.ReceiveFrom(buffer, ref clientEndPoint);
        string receivedString = Encoding.UTF8.GetString(buffer, 0, receivedBytes);
        Message? receivedMessage = JsonSerializer.Deserialize<Message>(receivedString);
        if(receivedMessage is null){
            Console.WriteLine("ReceiveHello(): A message object was expected but not received.");
            return false;
        }
        if (receivedMessage.MsgType != MessageType.Hello){
            Console.WriteLine("ReceiveHello(): The received message was not of type MessageType.Hello.");
            return false;
        }
        Console.WriteLine($"ReceiveHello(): Server {setting.ServerIPAddress}:{setting.ServerPortNumber} received from client {setting.ClientIPAddress}:{setting.ClientPortNumber} a message:{receivedString} ");
        return true;
    }

    public static bool SendWelcome()
    {
        SendMessage(new Message
        {
            MsgId = 999,
            MsgType = MessageType.Welcome,
            Content = "Welcome to the server!"
        });
        return true;
    }

    
    public static (bool, Message) ReceiveAndPrintDNS()
    {
        byte[] buffer = new byte[1024];
        int receivedBytesCount = serverSocket.ReceiveFrom(buffer, ref clientEndPoint);
        string receivedString = Encoding.UTF8.GetString(buffer, 0, receivedBytesCount);
        Message? receivedMessage = JsonSerializer.Deserialize<Message>(receivedString);
        if(receivedMessage is null){
            Console.WriteLine("ReceiveAndPrintDNS(): A message object was expected but not received.");
            SendMessage(new Message
            {
                MsgId = receivedMessage.MsgId,
                MsgType = MessageType.Error,
                Content = "A message object was expected but not received"
            });
            return (false, null);
        }
        if(receivedMessage.MsgType != MessageType.DNSLookup){
            Console.WriteLine("ReceiveAndPrintDNS(): The received message was not of type MessageType.DNSLookup.");
            SendMessage(new Message
            {
                MsgId = receivedMessage.MsgId,
                MsgType = MessageType.Error,
                Content = "The received message was not of type MessageType.DNSLookup"
            });
            return (false, null);
        }
        if (receivedMessage.Content is JsonElement jsonElement)
        {
            string? queryType = jsonElement.GetProperty("Type").GetString();
            string? queryName = jsonElement.GetProperty("Name").GetString();

            if (string.IsNullOrEmpty(queryType) || string.IsNullOrEmpty(queryName))
            {
                Console.WriteLine("ReceiveAndPrintDNS(): Missing Type or Name in DNSLookup request.");
                SendMessage(new Message
                {
                    MsgId = receivedMessage.MsgId,
                    MsgType = MessageType.Error,
                    Content = "Missing Type or Name in DNSLookup request"
                });
                return (false, null);
            }
            else{
                Console.WriteLine($"ReceiveAndPrintDNS(): Server {setting.ServerIPAddress}:{setting.ServerPortNumber} received from Client {setting.ClientIPAddress}:{setting.ClientPortNumber} a message:{receivedMessage} ");
                return (true, receivedMessage);
            }
        }
        else{
            Console.WriteLine("ReceiveAndPrintDNS(): Invalid DNSLookup request format.");
            SendMessage(new Message
            {
                MsgId = receivedMessage.MsgId,
                MsgType = MessageType.Error,
                Content = "Invalid DNSLookup request format"
            });
            return (false, null);
        }
    }


    public static bool ProcessDNSLookup(Message message)
    {
        if (message.Content is JsonElement jsonElement)
        {
            string? queryType = jsonElement.GetProperty("Type").GetString();
            string? queryName = jsonElement.GetProperty("Name").GetString();
            DNSRecord? record = DNSRecords.Find(d => d.Type.Equals(queryType, StringComparison.OrdinalIgnoreCase) &&
                                        d.Name.Equals(queryName, StringComparison.OrdinalIgnoreCase));
            if (record != null)
            {
                Console.WriteLine("DNSRecord found: " + record.ToString() + "!");
                DNSMsgId = message.MsgId;
                SendMessage(new Message
                {
                    MsgId = message.MsgId, // Keep original MsgId
                    MsgType = MessageType.DNSLookupReply,
                    Content = record
                });
            }
            else
            {
                Console.WriteLine("No matching DNSRecord found!");
                SendMessage(new Message
                {
                    MsgId = message.MsgId, // Keep original MsgId
                    MsgType = MessageType.Error,
                    Content = $"No DNS record found for {queryName} with type {queryType}"
                });
            }
        }
        return true;
    }

    public static bool ReceiveAck()
    {
        byte[] buffer = new byte[1024];
        int receivedBytes = serverSocket.ReceiveFrom(buffer, ref clientEndPoint);
        string receivedString = Encoding.UTF8.GetString(buffer, 0, receivedBytes);
        Message? receivedMessage = JsonSerializer.Deserialize<Message>(receivedString);
        if(receivedMessage.MsgType != MessageType.Ack){
            Console.WriteLine("ReceiveAck(): The received message was not of type MessageType.Ack.");
            return false;
        }
        if(receivedMessage.Content.ToString() != DNSMsgId.ToString()){
            Console.WriteLine("ReceiveAck(): The received MsgId did not match the MsgId of the DNSLookupReply that was sent.");
            return false;
        }
        else{
            Console.WriteLine($"ReceiveAck(): The received MsgId({receivedMessage.Content}) matched the MsgId of the DNSLookupReply({DNSMsgId}) that was sent.");
            return true;
        }

    }

    // TODO:[If no further requests receieved send End to the client]
    public static bool SendEnd()
    {
        bool sent = SendMessage(new Message
        {
            MsgId = 0,
            MsgType = MessageType.End,
            Content = "End of communication"
        });
        return sent;
    }
}