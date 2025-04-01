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
        if(!ClientUDP.SendHello()){
            Console.WriteLine("SendHello step failed. Ending protocol");
            return;
        }
        if(!ClientUDP.ReceiveWelcome()){
            Console.WriteLine("ReceiveWelcome step failed. Ending protocol");
            return;
        }

        // for(int i = Dns's to lookup)
        // {
            if(!ClientUDP.SendDNSLookUp()){
                Console.WriteLine("SendDNSLookUp step failed. Ending protocol");
                return;
            }
            if(!ClientUDP.ReceiveDNSLookupReply()){
                Console.WriteLine("ReceiveDNSLookupReply step failed. Ending protocol");
                return;
            }
        //}
        if(!ClientUDP.ReceiveEnd()){
            Console.WriteLine("ReceiveEnd step failed. Ending protocol");
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


    public static bool start()
    {
        //TODO: [Create endpoints and socket]
        clientEndPoint = new IPEndPoint(IPAddress.Parse(setting.ClientIPAddress), setting.ClientPortNumber);
        clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        clientSocket.Bind(clientEndPoint);

        serverEndPoint = new IPEndPoint(IPAddress.Parse(setting.ServerIPAddress), setting.ServerPortNumber);

        //server for some reason had 2 TODO receive steps. Im not sure what thats supposed to look like, so I just split them in two ReceiveFrom() steps.
        //though the actual UDP protocol probably only receives HELLO. So I commented these out
        
        //{ “MsgId”: “1” , ”MsgType": "Hello", "Content": “Hello fromclient” }
        // Message anyMessage = new Message();
        // anyMessage.MsgId = 1;
        // anyMessage.MsgType = MessageType.Hello;
        // anyMessage.Content = "Hello fromclient";
        // var anyJson = JsonSerializer.Serialize(anyMessage);
        // clientSocket.SendTo(Encoding.UTF8.GetBytes(anyJson), serverEndPoint);

        //TODO: [Create and send HELLO]
        

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
    public static bool SendDNSLookUp()
    {
        return true;
    }
    //TODO: [Receive and print DNSLookupReply from server]
    public static bool ReceiveDNSLookupReply()
    {
        return true;
    }
     //TODO: [Send Acknowledgment to Server]
    public static bool SendAck()
    {
        return true;
    }
    //TODO: [Send next DNSLookup to server]
    // repeat the process until all DNSLoopkups (correct and incorrect onces) are sent to server and the replies with DNSLookupReply

    //TODO: [Receive and print End from server]

    public static bool ReceiveEnd()
    {
        return true;
    }



}