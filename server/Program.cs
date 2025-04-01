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
        if(!ServerUDP.start()){
            Console.WriteLine("Server initialization failed. Ending protocol");
            return;
        }
        if(!ServerUDP.ReceiveAny()){
            Console.WriteLine("ReceiveAny step failed. Ending protocol");
            return;
        }
        if(!ServerUDP.ReceiveHello()){
            Console.WriteLine("ReceiveHello step failed. Ending protocol");
            return;
        }
        if(!ServerUDP.SendWelcome()){
            Console.WriteLine("SendWelcome step failed. Ending protocol");
            return;
        }
        if(!ServerUDP.ReceiveAndPrintDNSLookup()){
            Console.WriteLine("ReceiveAndPrintDNSLookup step failed. Ending protocol");
            return;
        }

        if(!ServerUDP.QueryDNSRecord()){
            Console.WriteLine("DNSRecord not found, sending DNSNotFoundError");
            if(!ServerUDP.SendDNSNotFoundError()){
                Console.WriteLine("SendDNSNotFoundError step failed. Ending protocol");
                return;
            }
            Console.WriteLine("SendDNSNotFoundError step succesful. Ending protocol");
            return;
        }
        else{
            Console.WriteLine("DNSRecord found, sending DNSReply");
            if(!ServerUDP.SendDNSReply())
            {
                Console.WriteLine("SendDNSReply step failed. Ending protocol");
                return;
            }
        }
        if(!ServerUDP.ReceiveAck()){
            Console.WriteLine("ReceiveHello step failed. Ending protocol");
            return;
        }
        if(!ServerUDP.SendEnd()){
            Console.WriteLine("ReceiveHello step failed. Ending protocol");
            return;
        }
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

    static IPEndPoint serverEndPoint;
    static Socket serverSocket;
    static EndPoint clientEndPoint;


    // TODO: [Read the JSON file and return the list of DNSRecords]

    public static bool start() 
    {
        
        // TODO: [Create a socket and endpoints and bind it to the server IP address and port number]
        serverEndPoint = new IPEndPoint(IPAddress.Parse(setting.ServerIPAddress), setting.ServerPortNumber);
        serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        serverSocket.Bind(serverEndPoint);

        serverSocket.SendTimeout = 10000; // so it doesn't unexpectedly block
        serverSocket.ReceiveTimeout = 10000; // so it doesn't unexpectedly block

        clientEndPoint = new IPEndPoint(IPAddress.Parse(setting.ClientIPAddress), setting.ClientPortNumber);

        return true;

    }

    public static bool ReceiveAny()
    {
        // TODO:[Receive and print a received Message from the client]
        byte[] buffer = new byte[1024];
        int receivedBytesCount = serverSocket.ReceiveFrom(buffer, ref clientEndPoint);
        string receivedString = Encoding.UTF8.GetString(buffer, 0, receivedBytesCount);
        Message? receivedMessage = JsonSerializer.Deserialize<Message>(receivedString);
        if(receivedMessage is null){
            Console.WriteLine("A message object was expected but not received.");
            return false;
        }
        Console.WriteLine($"Server {setting.ServerIPAddress}:{setting.ServerPortNumber} received from client {setting.ClientIPAddress}:{setting.ClientPortNumber} a message:{receivedMessage} ");
        return true;
    }
    public static bool ReceiveHello()
    {
        // TODO:[Receive and print Hello]
        byte[] buffer = new byte[1024];
        int receivedBytesCount = serverSocket.ReceiveFrom(buffer, ref clientEndPoint);
        string receivedString = Encoding.UTF8.GetString(buffer, 0, receivedBytesCount);
        Message? receivedMessage = JsonSerializer.Deserialize<Message>(receivedString);
        if(receivedMessage is null){
            Console.WriteLine("A message object was expected but not received.");
            return false;
        }
        if(receivedMessage.MsgType != MessageType.Hello){
            Console.WriteLine("The received message was not of type MessageType.Hello.");
            return false;
        }
        Console.WriteLine($"Server {setting.ServerIPAddress}:{setting.ServerPortNumber} received from client {setting.ClientIPAddress}:{setting.ClientPortNumber} a message:{receivedMessage} ");
        return true;

    }

    // TODO:[Send Welcome to the client]
    public static bool SendWelcome()
    {
//         { “MsgId”: “4” , “MsgType”: "Welcome", “Content": “Welcome
// from server”}
        Message welcomeMessage = new Message();
        welcomeMessage.MsgId = 1;
        welcomeMessage.MsgType = MessageType.Hello;
        welcomeMessage.Content = "Hello fromclient";
        var welcomeJson = JsonSerializer.Serialize(welcomeMessage);

        
        serverSocket.SendTo(Encoding.UTF8.GetBytes(welcomeJson), clientEndPoint);
        return true;
    }

    // TODO:[Receive and print DNSLookup]
    public static bool ReceiveAndPrintDNSLookup()
    {
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