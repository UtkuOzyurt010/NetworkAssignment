using System.Collections.Immutable;
using System.ComponentModel;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using LibData;

// SendTo();
class Program
{
    static void Main(string[] args)
    {
        if(!ClientUDP.start()){
            Console.WriteLine("start step (client initialization) failed. Ending protocol");
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

class ClientUDP
{

    //TODO: [Deserialize Setting.json]
    static string configFile = @"../Setting.json";
    static string configContent = File.ReadAllText(configFile);
    static Setting? setting = JsonSerializer.Deserialize<Setting>(configContent);

    static EndPoint clientEndPoint;
    static Socket clientSocket;
    static EndPoint serverEndPoint;

    static int sentDNSMsgId = -1;
    static int receivedDNSMsgId = -2;

    public static bool start()
    {
        //TODO: [Create endpoints and socket]
        clientEndPoint = new IPEndPoint(IPAddress.Parse(setting.ClientIPAddress), setting.ClientPortNumber);
        clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        clientSocket.Bind(clientEndPoint);

        serverEndPoint = new IPEndPoint(IPAddress.Parse(setting.ServerIPAddress), setting.ServerPortNumber);

        string dnsRecordsContent = File.ReadAllText("./SearchDNSRecords.json");
        DNSRecord[] dNSRecords = JsonSerializer.Deserialize<DNSRecord[]>(dnsRecordsContent);

        //TODO: [Create and send HELLO]
        if(!ClientUDP.SendHello()){
            Console.WriteLine("SendHello step failed. Ending protocol");
            return false;
        }
        //TODO: [Receive and print Welcome from server]
        if(!ClientUDP.ReceiveWelcome()){
            Console.WriteLine("ReceiveWelcome step failed. Ending protocol");
            return false;
        }

        for(int i = 0; i < 4; i++)
        {
            //TODO: [Create and send DNSLookup Message]
            if(!ClientUDP.SendDNSLookUp(dNSRecords[i])){
                Console.WriteLine("SendDNSLookUp step failed. sending next DNSLookup");
                continue;
            }
            //TODO: [Receive and print DNSLookupReply from server]
            if(!ClientUDP.ReceiveDNSLookupReply()){
                Console.WriteLine("ReceiveDNSLookupReply step failed. sending next DNSLookup");
                continue;
            }
            if(!ClientUDP.SendAck()){
                Console.WriteLine("SendAck step failed. Ending protocol");
                return false; //if this gets called it SHOULD work, so we cant skip to next DNS.
            }
        }
        

        if(!ClientUDP.ReceiveEnd()){
            Console.WriteLine("ReceiveEnd step failed. Ending protocol");
            return false;
        }

        return true;

    }

    //TODO: [Create and send HELLO]
    public static bool SendHello()
    {
        Message helloMessage = new Message();
        helloMessage.MsgId = 1;
        helloMessage.MsgType = MessageType.Hello;
        helloMessage.Content = "Hello fromclient";
        var helloJson = JsonSerializer.Serialize(helloMessage);
        int SentByesCount = clientSocket.SendTo(Encoding.UTF8.GetBytes(helloJson), serverEndPoint);
        // if(SentByesCount != expectedByesCount){ // to check if sending actually worked as expected
        //     return false;
        // }
        return true;
    }
    //TODO: [Receive and print Welcome from server]
    public static bool ReceiveWelcome()
    {
        byte[] buffer = new byte[1024];
        int receivedBytesCount = clientSocket.ReceiveFrom(buffer, ref serverEndPoint);
        string receivedString = Encoding.UTF8.GetString(buffer, 0, receivedBytesCount);
        Message? receivedMessage = JsonSerializer.Deserialize<Message>(receivedString);
        if(receivedMessage is null){
            Console.WriteLine("ReceiveWelcome(): A message object was expected but not received.");
            return false;
        }
        if(receivedMessage.MsgType != MessageType.Welcome){
            Console.WriteLine("ReceiveWelcome(): The received message was not of type MessageType.Welcome.");
            return false;
        }
        Console.WriteLine($"ReceiveWelcome(): Client {setting.ClientIPAddress}:{setting.ClientPortNumber} received from server {setting.ServerIPAddress}:{setting.ServerPortNumber} a message:{receivedMessage} ");
        return true;
    }

    //TODO: [Create and send DNSLookup Message]
    public static bool SendDNSLookUp(DNSRecord dnsRecord)
    {
        Message DNSLookupMessage = new Message();
        DNSLookupMessage.MsgId = 33;
        sentDNSMsgId = DNSLookupMessage.MsgId;
        DNSLookupMessage.MsgType = MessageType.DNSLookup;
        DNSLookupMessage.Content = dnsRecord;
        var DNSLookupMessageJson = JsonSerializer.Serialize(DNSLookupMessage);
        int SentByesCount = clientSocket.SendTo(Encoding.UTF8.GetBytes(DNSLookupMessageJson), serverEndPoint);
        // if(SentByesCount != expectedByesCount){ // to check if sending actually worked as expected
        //     return false;
        // }
        Console.WriteLine($"sent DNSLookupMessage : {DNSLookupMessage.Content}");
        return true;
    }
    //TODO: [Receive and print DNSLookupReply from server]
    public static bool ReceiveDNSLookupReply()
    {
        byte[] buffer = new byte[1024];
        int receivedBytesCount = clientSocket.ReceiveFrom(buffer, ref serverEndPoint);
        string receivedString = Encoding.UTF8.GetString(buffer, 0, receivedBytesCount);
        Message? receivedMessage = JsonSerializer.Deserialize<Message>(receivedString);
        if(receivedMessage is null){
            Console.WriteLine("ReceiveDNSLookupReply(): A message object was expected but not received.");
            return false;
        }
        if (receivedMessage.MsgType == MessageType.End)
        {
            Console.WriteLine("ReceiveDNSLookupReply(): The received message was of type MessageType.End. Ending protocol.");
            return false;
        }
        if(receivedMessage.MsgType == MessageType.DNSLookupReply){
            Console.WriteLine($"ReceiveDNSLookupReply(): Client {setting.ClientIPAddress}:{setting.ClientPortNumber} received from server {setting.ServerIPAddress}:{setting.ServerPortNumber} a DNSLookupReply:{receivedMessage.Content} ");
            receivedDNSMsgId = receivedMessage.MsgId;
            return true;
        }
        if(receivedMessage.MsgType == MessageType.Error){
            Console.WriteLine($"ReceiveDNSLookupReply(): Client {setting.ClientIPAddress}:{setting.ClientPortNumber} received from server {setting.ServerIPAddress}:{setting.ServerPortNumber} an Error:{receivedMessage.Content} ");
            return false;
        }
        else{
            Console.WriteLine($"ReceiveDNSLookupReply(): Client {setting.ClientIPAddress}:{setting.ClientPortNumber} received from server {setting.ServerIPAddress}:{setting.ServerPortNumber} a {receivedMessage.MsgType}: {receivedMessage}, which is unexpected");
            return false;
        }
        
    }
     //TODO: [Send Acknowledgment to Server]
    public static bool SendAck()
    {
        if(receivedDNSMsgId == sentDNSMsgId){
            Console.WriteLine($"the MsgId of received DNSLookupReply({receivedDNSMsgId}) matches the MsgId of the sent DNSLookUp({sentDNSMsgId}). sending acknowledgement");
            Message ackMessage = new Message();
            ackMessage.MsgId = 40;
            ackMessage.MsgType = MessageType.Ack;
            ackMessage.Content = receivedDNSMsgId;
            var ackMessageJson = JsonSerializer.Serialize(ackMessage);
            int SentByesCount = clientSocket.SendTo(Encoding.UTF8.GetBytes(ackMessageJson), serverEndPoint);
            // if(SentByesCount != expectedByesCount){ // to check if sending actually worked as expected
            //     return false;
            // }
            Console.WriteLine($"sent Acknowledgement : {ackMessage.Content}");

            sentDNSMsgId = -1;
            receivedDNSMsgId = -2;
            return true;

        }
        else{
            Console.WriteLine($"the MsgId of received DNSLookupReply({receivedDNSMsgId}) did not match the MsgId of the sent DNSLookUp({sentDNSMsgId}). NOT sending acknowledgement");
            return false;
        }
    }
    //TODO: [Send next DNSLookup to server]
    // repeat the process until all DNSLoopkups (correct and incorrect onces) are sent to server and the replies with DNSLookupReply


    //TODO: [Receive and print End from server]

    public static bool ReceiveEnd()
    {
        byte[] buffer = new byte[1024];
        int receivedBytesCount = clientSocket.ReceiveFrom(buffer, ref serverEndPoint);
        string receivedString = Encoding.UTF8.GetString(buffer, 0, receivedBytesCount);
        Message? endMessage = JsonSerializer.Deserialize<Message>(receivedString);
        if(endMessage == null){
            Console.WriteLine("ReceiveEnd(): received message was null");
            return false;
        }
        if (endMessage.MsgType != MessageType.End){
            Console.WriteLine($"ReceiveEnd(): received message did not have MessageType.End, but MessageType {endMessage.MsgType}");
            return false;

        }
        if (endMessage.MsgType == MessageType.End){
            Console.WriteLine("ReceiveEnd(): Succesfully received message with MessageType.End. Ending protocol.");
            return true;

        }
        return false;
    }



}