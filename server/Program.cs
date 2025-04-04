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
    static bool skipAck = false;
    static bool endConnection = false;


    // TODO: [Read the JSON file and return the list of DNSRecords]
    static List<DNSRecord>? DNSRecords = JsonSerializer.Deserialize<List<DNSRecord>>(File.ReadAllText("./DNSrecords.json"));

    public static void start() 
    {
        // TODO: [Create a socket and endpoints and bind it to the server IP address and port number]
        try{
            serverEndPoint = new IPEndPoint(IPAddress.Parse(setting.ServerIPAddress), setting.ServerPortNumber);
            serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            clientEndPoint = new IPEndPoint(IPAddress.Parse(setting.ClientIPAddress), setting.ClientPortNumber);
            serverSocket.Bind(serverEndPoint);
        }
        catch (FormatException ex)
        {
            Console.WriteLine($"Invalid IP address format: {ex.Message}");
            return;
        }
        catch (SocketException ex)
        {
            Console.WriteLine($"Socket error: {ex.Message}");
            return;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Unexpected error: {ex.Message}");
            return;
        }
        if(serverEndPoint is null || serverSocket is null || clientEndPoint is null){
            Console.WriteLine("for some unexpected reason, a server/client endpoint or the server socket was null after iniliaization");
        }

        //serverSocket.SendTimeout = 10000; // so it doesn't unexpectedly block
        //serverSocket.ReceiveTimeout = 10000; // so it doesn't unexpectedly block


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
        while(true)
        {
            // TODO:[Receive and print Hello]
            if (!ReceiveHello())
            {
                Console.WriteLine("ReceiveHello step failed. Ending protocol");
                break;
            }
            // TODO:[Send Welcome to the client]
            if (!SendWelcome())
            {
                Console.WriteLine("SendWelcome step failed. Ending protocol");
                break;
            }
            int ResponseCount = 0;
            while (true)
            {
                // TODO:[Receive and print DNSLookup]
                (bool success, Message message) = ReceiveAndPrintDNS();
                if (!success)
                {
                    Console.WriteLine("ReceiveAndPrintDNS step failed. trying next DNSLookup.");
                    endConnection = true;
                    ResponseCount++;
                    continue;
                }
                // TODO:[Query the DNSRecord in Json file]
                // TODO:[If found Send DNSLookupReply containing the DNSRecord]
                // TODO:[If not found Send Error]
                DNSMsgId = message.MsgId; //assigning message.MsgId to DNSMsgId to ensure that the same number is used.
                if (!ProcessDNSLookup(message))
                {
                    Console.WriteLine("ProcessDNSLookup step failed. trying next DNSLookup.");
                    endConnection = true;
                    ResponseCount++;
                    continue;
                }
                // TODO:[Receive Ack about correct DNSLookupReply from the client]
                if (!skipAck)
                { 
                    if(!ReceiveAck())
                    {
                        Console.WriteLine("ReceiveAck step failed. trying next DNSLookup.");
                        continue;
                    }
                }
                else if(skipAck) endConnection = true;
                ResponseCount++;
                if (ResponseCount == 4)
                {
                    if(endConnection){
                        endConnection = false;
                        Console.WriteLine($"There was an invalid DNSLookup. Ending DNSQuery.");
                        break;
                    }
                    else
                    {
                        Console.WriteLine($"Received {ResponseCount} Acks. Sending End message to client.");
                    SendEnd();
                    Console.WriteLine("End of communication, waiting for next client");
                    break;
                    }
                    
                }
                
            }
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
        Console.WriteLine($"ReceiveHello(): Server {setting.ServerIPAddress}:{setting.ServerPortNumber} received from client {setting.ClientIPAddress}:{setting.ClientPortNumber} a message:{receivedMessage.Content} ");
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
                Console.WriteLine($"ReceiveAndPrintDNS(): Server {setting.ServerIPAddress}:{setting.ServerPortNumber} received from Client {setting.ClientIPAddress}:{setting.ClientPortNumber} a message:{receivedMessage.Content} ");
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
                Console.WriteLine("ProcessDNSLookup(): DNSRecord found: " + record.ToString() + "!");
                DNSMsgId = message.MsgId;
                Message dnsLookupReply = new Message
                {
                    MsgId = message.MsgId, // Keep original MsgId
                    MsgType = MessageType.DNSLookupReply,
                    Content = record
                };
                SendMessage(dnsLookupReply);
                Console.WriteLine($"ProcessDNSLookup(): Sent DNSLookupReply: {dnsLookupReply.Content} ");
                return true;
            }
            else
            {
                Console.WriteLine("ProcessDNSLookup(): No matching DNSRecord found!");
                Message errorMessage = new Message
                {
                    MsgId = message.MsgId, // Keep original MsgId
                    MsgType = MessageType.Error,
                    Content = $"ProcessDNSLookup(): No DNS record found for {queryName} with type {queryType}"
                };
                SendMessage(errorMessage);
                Console.WriteLine($"ProcessDNSLookup(): ErrorMessage sent: {errorMessage.Content}");
                skipAck = true;
                return false;
            }
        }
        else {
            Console.WriteLine("ProcessDNSLookup(): message.Content was not of the correct type");
            return false;
        }
        
    }

    public static bool ReceiveAck()
    {
        byte[] buffer = new byte[1024];
        int receivedBytes = serverSocket.ReceiveFrom(buffer, ref clientEndPoint);
        string receivedString = Encoding.UTF8.GetString(buffer, 0, receivedBytes);
        Message? receivedMessage = JsonSerializer.Deserialize<Message>(receivedString);
        if(receivedMessage.MsgType == MessageType.Error){
            Console.WriteLine($"ReceiveAck(): The received message was an ErrorMessage: {receivedMessage.Content}. Ending protocol");
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
        Console.WriteLine("SendEnd(): sending End Message");
        return sent;
    }
}