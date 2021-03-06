﻿using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.IO;
using System.Xml.Serialization;
using System.Collections.Generic;

namespace PosServer
{
    public class Message
    {
        public string From { get; set; }
        public string To { get; set; }
        public string Msg { get; set; }
        public string Stamp { get; set; }

        public override string ToString()
        {
            return $"From: {From}\nTo: {To}\n{Msg}\nStamp: {Stamp}";
        }
    }

    public class Server
    {
        public static int PORT = 14300;
        public static int TAM = 1024;

        public static Dictionary<string, List<Message>> repo = new Dictionary<string, List<Message>>();

        public static IPAddress GetLocalIpAddress()
        {
            List<IPAddress> ipAddressList = new List<IPAddress>();
            IPHostEntry ipHostInfo = Dns.GetHostEntry(Dns.GetHostName());
            IPAddress ipAddress = ipHostInfo.AddressList[0];
            int t = ipHostInfo.AddressList.Length;
            string ip;
            for (int i = 0; i < t; i++)
            {
                ip = ipHostInfo.AddressList[i].ToString();
                if (ip.Contains(".") && !ip.Equals("127.0.0.1")) ipAddressList.Add(ipHostInfo.AddressList[i]);
            }
            if (ipAddressList.Count == 1)
            {
                return ipAddressList[0];
            }
            else
            {
                int i = 0;
                foreach (IPAddress ipa in ipAddressList)
                {
                    Console.WriteLine($"[{i++}]: {ipa}");
                }
                System.Console.Write($"Opción [0-{t - ipAddressList.Count}]: ");
                string s = Console.ReadLine();
                if (Int32.TryParse(s, out int j))
                {
                    if ((j >= 0) && (j <= t))
                    {
                        return ipAddressList[j];
                    }
                }
                return null;
            }
        }

        public static void StartListening()
        {
            byte[] bytes = new Byte[TAM];

            IPAddress ipAddress = GetLocalIpAddress();
            if (ipAddress == null) return;
            IPEndPoint localEndPoint = new IPEndPoint(ipAddress, PORT);

            Socket listener = new Socket(ipAddress.AddressFamily,
                SocketType.Stream, ProtocolType.Tcp);

            try
            {
                listener.Bind(localEndPoint);
                listener.Listen(10);

                while (true)
                {
                    Console.WriteLine("Waiting for a connection at {0}:{1} ...", ipAddress, PORT);
                    Socket handler = listener.Accept();

                    Message request = Receive(handler);

                    Console.WriteLine(request);//Print it

                    Message response = Process(request);

                    Send(handler, response);

                    handler.Shutdown(SocketShutdown.Both);
                    handler.Close();
                }

            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }

            Console.WriteLine("\nPress ENTER to continue...");
            Console.Read();

        }

        public static void Send(Socket socket, Message message)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(Message));
            Stream stream = new MemoryStream();
            serializer.Serialize(stream, message);
            byte[] byteData = ((MemoryStream)stream).ToArray();
            // string xml = Encoding.ASCII.GetString(byteData, 0, byteData.Length);
            // Console.WriteLine(xml);//Imprime el texto enviado
            int bytesSent = socket.Send(byteData);
        }

        public static Message Receive(Socket socket)
        {
            byte[] bytes = new byte[TAM];
            int bytesRec = socket.Receive(bytes);
            string xml = Encoding.ASCII.GetString(bytes, 0, bytesRec);
            // Console.WriteLine(xml);//Imprime el texto recibido
            byte[] byteArray = Encoding.ASCII.GetBytes(xml);
            MemoryStream stream = new MemoryStream(byteArray);
            Message response = (Message)new XmlSerializer(typeof(Message)).Deserialize(stream);
            return response;
        }

        public static void AddMessage(Message message)
        {
            //Message m1 = new Message { From = "22", To = "11", Msg = "Adeu!", Stamp = "A.E." }
            //TODO: Add Message
            List<Message> mensajes = new List<Message>();
            if(!repo.ContainsKey(message.To)){
                mensajes.Add(message);
                repo.Add(message.To, mensajes);
            }
            else{
                repo[message.To].Add(message);
            }

        }

        public static Message ListMessages(string toClient)
        {
            StringBuilder sb = new StringBuilder();

            //TODO: List Messages
            var i = 0;
            if(repo.ContainsKey(toClient)){
                List<Message> mensajes = repo[toClient];
                foreach(Message msg in mensajes){
                    sb.Append($"[{i}] From: {msg.From}\n");
                    i++;
                }
            }
            return new Message { From = "0", To = toClient, Msg = sb.ToString(), Stamp = "Server\n" };
            
        }

        public static Message RetrMessage(string toClient, int index)
        {
            Message msg = new Message { From = "0", To = toClient, Msg = "NOT FOUND", Stamp = "Server\n" };

            //TODO: Retr Message
            try{
                if(repo.ContainsKey(toClient)){
                    List<Message> mensajes = repo[toClient];
                    if(index >= 0){
                        msg = mensajes[index];
                        repo.Remove(toClient);
                    }
                }
            }
            catch{
                return msg;
            }

            return msg;
        }

        public static Message Process(Message request)
        {
            Message response = new Message { From = "0", To = request.From, Msg = "ERROR", Stamp = "Server\n" };

            //TODO: Process
            try
            {
                if(request.To != "0"){
                AddMessage(request);
                response.Msg = "OK";
                }
                else{
                    String[] arrayAux = request.Msg.Split(" ");
                    switch(arrayAux[0]){

                        case "LIST":
                            response = ListMessages(request.From);
                            break;

                        case "RETR":
                            response = RetrMessage(request.From, Int32.Parse(arrayAux[1]));
                            break;
                    }
                }
                return response;
            }
            catch
            {
                return response;
            }
        }

        public static int Main(String[] args)
        {
            StartListening();
            return 0;
        }
    }
}