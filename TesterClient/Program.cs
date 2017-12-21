using System;
using System.Collections.Generic;
using System.Threading;

namespace TesterClient
{
    internal partial class Program
    {
        static int threads = 1;
        static int RunId = 1;

        static void TestGenerator()
        {
            foreach(int t in new int[]{1, 10, 100})
            for(int data=20; data<110000; data=data*10)
            for(int minsend=1024; minsend<110000; minsend=minsend*10)
            foreach(int minsleep in new int[]{0,  100, 1000}){
                int maxsend = minsend * 10;
                int maxsleep = minsleep * 10;
                Console.WriteLine("{0};{1};{2};{3};{4};{5};{6}",
                    t, data,(data*46+45)*t, minsend,maxsend, minsleep, maxsleep);
            }
            for(int data=20; data<110000; data=data*10)
                Console.WriteLine("{0}", data);
        }
        static void Main(string[] argv)
        {
            TestGenerator();
            Console.ReadKey();
            Dictionary<string, string> config= new Dictionary<string, string>();

            foreach (string arg in argv)
            {
                if (arg[0] == '/')
                {
                    string[] option = arg.Split(':');
                    if (option[0].ToUpper() == "/THREADS")
                        int.TryParse(arg.Split(':')[1], out threads);
                    if (option[0].ToUpper() == "/RUNID")
                        int.TryParse(arg.Split(':')[1], out RunId);
                    if(option.Length>=2)
                        config.Add(option[0], option[1]); 
                    else
                        config.Add(option[0], "TRUE");
                }
            }
            for (int i = 0; i < threads; i++)
            {
                TestLoad testLoad = new TestLoad(RunId, i);
                testLoad.Configure(config);
                Thread t = new Thread(()=>
                {
                    testLoad.Callback();
                    Console.WriteLine("Threadend: RuinId={0} TestId={1}", RunId, testLoad.TestId);
                    
                });
                t.Start();
                Console.WriteLine("Threadstart: RuinId={0} TestId={1}", RunId, i);
            }
            Console.WriteLine("ProgramEnd: RunId={0}", RunId);
            //Console.ReadKey();
        }

        static void ExceptionHandler(Exception e)
        {
            Console.WriteLine("Exception: Type={0} Message={1} ", e.GetType(), e.Message);
        }
/* Old Version
 * HTTP/1.1 200 OK
Connection: keep-alive
Keep-Alive: 60
Content-Type: application/json
Content-Length: 67

{"success":true,"answer":{"message":"success attempt"},"errors":[]} 
 
        static void Low()
        {
            // Data buffer for incoming data.
            byte[] bytes = new byte[102400];

            // Connect to a remote device.
            try
            {
                // Establish the remote endpoint for the socket.
                // This example uses port 11000 on the local computer.
                //IPHostEntry ipHostInfo = Dns.GetHostEntry("54.243.175.62");
                IPHostEntry ipHostInfo = Dns.GetHostEntry("iamnotaprogrammer.ru");
                IPAddress ipAddress = ipHostInfo.AddressList[0];
                IPEndPoint remoteEp = new IPEndPoint(ipAddress, 8880);

                // Create a TCP/IP  socket.
                Socket sender = new Socket(AddressFamily.InterNetwork,
                    SocketType.Stream, ProtocolType.Tcp);
                sender.ReceiveTimeout = 3000;
                // Connect the socket to the remote endpoint. Catch any errors.
                try
                {
                    string result = "";
                    sender.Connect(remoteEp);
                    string data =
                        "1E\r\n{'profile':1,'controler':'acc'\r\n1E\r\n,'data':[['2017-12-13 01:15:55\r\n1E\r\n.228',1.3],['2017-12-13 01:15:\r\n1E\r\n55.228',2],['2017-12-13 01:15:\r\nC\r\n55.228',4]]}\r\n0\r\n\r\n";
                    data = data.Replace("'", "\"");
                    string[] requestString =
                    {
                        "POST /api/v1/load HTTP/1.1\r\n",
                        "Host: iamnotaprogrammer.ru:8880\r\n" +
                        "Transfer-Encoding: chunked\r\n",
                        "Keep-Alive: 60\r\n",
                        "Content-Type: application/json\r\n",
                        //string.Format("Content-Length: {0}\r\n", data.Length.ToString())+
                        "User-Agent: Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:57.0) Gecko/20100101 Firefox/57.0\r\n",
                        "MEPHI_SUPER_SECURE: SUPER_SECRET_PASS\r\n\r\n"
                    };
                    foreach (string s in requestString)
                    {
                        byte[] requestData = Encoding.ASCII.GetBytes(s);
                        result += s;
                        int bytesSent = sender.Send(requestData);
                        System.Threading.Thread.Sleep(3000);
                    }
                    result += data;
                    sender.Send(Encoding.ASCII.GetBytes(data));
                    Console.WriteLine("Socket connected to {0}",
                        sender.RemoteEndPoint.ToString());

                    // Encode the data string into a byte array.
                    // Send the data through the socket.


                    // Receive the response from the remote device.
                    int bytesRec = sender.Receive(bytes);
                    do
                    {
                        Console.WriteLine(Encoding.ASCII.GetString(bytes, 0, bytesRec));
                        if (bytesRec == 1024)
                            bytesRec = sender.Receive(bytes);
                        else break;
                    } while (bytesRec != 0);

                    sender.Shutdown(SocketShutdown.Both);
                    sender.Close();


                    // Release the socket.
                }
                catch (ArgumentNullException ane)
                {
                    Console.WriteLine("ArgumentNullException : {0}", ane.ToString());
                }
                catch (SocketException se)
                {
                    Console.WriteLine("SocketException : {0}", se.ToString());
                }
                catch (Exception e)
                {
                    Console.WriteLine("Unexpected exception : {0}", e.ToString());
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

/*        static HttpStatusCode Common()
        {
            HttpWebRequest request =
                (HttpWebRequest) WebRequest.Create(new Uri("http://iamnotaprogrammer.ru:8880/api/ping"));
            request.SendChunked = true;

            request.Headers.Add("MEPHI_SUPER_SECURE", "SUPER_SECRET_PASS");
            HttpWebResponse response = null;
            try
            {
                response = (HttpWebResponse) request.GetResponse();
            }
            catch (WebException e)
            {
                response = (HttpWebResponse) e.Response;
            }
            Console.WriteLine("Status: {0} ({1})", (int) response.StatusCode, response.StatusDescription);
            using (Stream stream = response.GetResponseStream())
            {
                if (stream == null)
                {
                    return response.StatusCode;
                }
                using (StreamReader reader = new StreamReader(stream))
                {
                    string line = "";
                    while ((line = reader.ReadLine()) != null)
                    {
                        Console.WriteLine(line);
                    }
                }
            }
            return response.StatusCode;
        }*/
    }
}