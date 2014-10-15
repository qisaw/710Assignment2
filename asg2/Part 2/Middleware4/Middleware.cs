using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Windows.Forms;
using System.Drawing;
using System.Collections.Generic;
using System.Text.RegularExpressions;
//using System.Collections.Generic.IEnumerable;
using System.Linq;
public class Middleware
{
	public static int Main(String[] args)
	{
		Application.Run(new WinFormExample());
		return 0;
	}

	public class WinFormExample : Form
	{
		const int myPort = 1085;

		private Button button;
		ListBox sent = new ListBox();
		List<SentMessage> sentMessages = new List<SentMessage>();
		ListBox rec = new ListBox();
		List<String> recMessages = new List<String>();
		List<RecListItem> holdingQueue = new List<RecListItem>();
		ListBox ready = new ListBox();
		List<RecListItem> readyMessages = new List<RecListItem>();
		int num = myPort - 1081;
		int[] timestamps = { 0, 0, 0, 0, 0 };

		public WinFormExample()
		{
			DisplayGUI();
			ReceiveMulticast();
		}

		private void DisplayGUI()
		{
			this.Name = "MiddleWare " + num.ToString();
			this.Text = "MiddleWare " + num.ToString();
			this.Size = new Size(825, 400);
			this.StartPosition = FormStartPosition.CenterScreen;

			button = new Button();
			button.Name = "button";
			button.Text = "Send";
			button.Location = new Point(15, 15);
			button.Click += new System.EventHandler(this.MyButtonClick);

			Label sentLabel = new Label();
			sentLabel.Text = "Sent";
			sentLabel.Location = new Point(0, 80);
			Label recLabel = new Label();
			recLabel.Text = "Recieved";
			recLabel.Location = new Point(280, 80);
			Label readyLabel = new Label();
			readyLabel.Text = "Ready";
			readyLabel.Location = new Point(560, 80);

			sent.Size = new Size(250, 200);
			sent.DataSource = sentMessages;
			sent.Location = new Point(0 , 100);
			rec.Size = new Size(250, 200);
			rec.DataSource = recMessages;
			rec.Location = new Point(280, 100);
			ready.Size = new Size(250, 200);
			ready.DataSource = readyMessages;
			ready.Location = new Point(560, 100);

			this.Controls.Add(button);
			this.Controls.Add(sent);
			this.Controls.Add(ready);
			this.Controls.Add(rec);
			this.Controls.Add(sentLabel);
			this.Controls.Add(recLabel);
			this.Controls.Add(readyLabel);
		}

		private void sendMulticastContolMessage(String message)
		{
			for (int i = 1; i < 6; i ++)
				sendControlMessage(message, i);
		}

		private void sendControlMessage(string message, int middleware)
		{
			sendMessage(message, 1081+middleware);
		}

		private void sendMessage(string message, int port)
		{
			IPHostEntry ipHostInfo = Dns.GetHostEntry(Dns.GetHostName());
			IPAddress ipAddress = null;
			foreach (IPAddress ip in ipHostInfo.AddressList)
			{
				if (ip.AddressFamily == AddressFamily.InterNetwork)
				{
					ipAddress = ip;
					break;
				}
			}

			IPEndPoint remoteEP = new IPEndPoint(ipAddress, port);
			Socket sendSocket;
			try
			{
				// Create a TCP/IP  socket.
                sendSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

				// Connect to the Network 
                sendSocket.Connect(remoteEP);
				//String mess = "Message # " +numOfMessages+" from Middleware "+ num + " <EOM>\n";
				// Generate and encode the multicast message into a byte array.
                byte[] msg = Encoding.ASCII.GetBytes(message);
				// Send the data to the network.
                int bytesSent = sendSocket.Send(msg);

				sendSocket.Shutdown(SocketShutdown.Both);
				sendSocket.Close();
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

		private void MyButtonClick(object source, EventArgs e)
		{
			DoWork();
		}

		private void AddSentMessage(string m)
		{
			this.sentMessages.Add(new SentMessage(m));
			this.sent.DataSource = null;
			this.sent.DataSource = sentMessages;
		}

		private void AddRecMessage(string m)
		{
			this.recMessages.Add(m);
			this.rec.DataSource = null;
			this.rec.DataSource = recMessages;
		}

		private void AddReadyMessage(string m, Timestamp t)
		{
			this.readyMessages.Add(new RecListItem(m, t));
			this.ready.DataSource = null;
			this.ready.DataSource = readyMessages;
		}

		private void AddToHoldingQueue(string m)
		{
			Timestamp time = GenerateTimeStamp(m);
			RecListItem item = new RecListItem(m, time);
			Console.WriteLine(time.ToString());
			this.holdingQueue.Add(item);
			sendControlMessage("Control " +time.ToString()+ " <EOM>", time.Sender);
		}

		// This method sets up a socket for receiving messages from the Network
        private async void ReceiveMulticast()
		{
			// Data buffer for incoming data.
            byte[] bytes = new Byte[1024];

			// Determine the IP address of localhost
            IPHostEntry ipHostInfo = Dns.GetHostEntry(Dns.GetHostName());
			IPAddress ipAddress = null;
			foreach (IPAddress ip in ipHostInfo.AddressList)
			{
				if (ip.AddressFamily == AddressFamily.InterNetwork)
				{
					ipAddress = ip;
					break;
				}
			}

			IPEndPoint localEndPoint = new IPEndPoint(ipAddress, myPort);

			// Create a TCP/IP socket for receiving message from the Network.
            TcpListener listener = new TcpListener(localEndPoint);
			listener.Start(10);

			try
			{
				string data = null;

				// Start listening for connections.
                while (true)
				{
					Console.WriteLine("Waiting for a connection...");

					// Program is suspended while waiting for an incoming connection.
                    TcpClient tcpClient = await listener.AcceptTcpClientAsync();

					Console.WriteLine("connectted");
					data = null;

					// Receive one message from the network
                    while (true)
					{
						bytes = new byte[1024];
						NetworkStream readStream = tcpClient.GetStream();
						int bytesRec = await readStream.ReadAsync(bytes, 0, 1024);
						data += Encoding.ASCII.GetString(bytes, 0, bytesRec);

						// All messages ends with "<EOM>"
						// Check whether a complete message has been received
                        if (data.IndexOf("<EOM>") > -1)
						{
							break;
						}
					}

					Console.WriteLine("msg received:    {0}", data);

					if(data.StartsWith("Message"))
					{
						AddToHoldingQueue(data);
						AddRecMessage(data);
					}else if (data.StartsWith("Control"))
					{
						ManageControlMessage(data);
					}else if (data.StartsWith("Finalize"))
					{
						ManageFinalizeMethod(data);
					}
				}
			}
			catch (Exception ee)
			{
				Console.WriteLine(ee.ToString());
			}
		}

		public void ManageFinalizeMethod(string data)
		{
			string control = data.Split(' ')[1];
			string[] nums = control.Split(',');

			int timestamp = Convert.ToInt32(nums[0]);
			int sender = Convert.ToInt32(nums[1]);
			int messageNum = Convert.ToInt32(nums[2]);

			foreach(RecListItem message in this.holdingQueue)
			{
				if (message.TStamp.MessageNum == messageNum && message.TStamp.Sender == sender)
				{
					message.TStamp.Value = timestamp;
					message.TStamp.IsFinal = true;
				}
			}

			sortHoldingQueue();
			pullOutReadyItems();
		}

		public void sortHoldingQueue()
		{
			this.holdingQueue = this.holdingQueue.OrderBy(x => x.TStamp.Value).ThenBy(x =>x.TStamp.Sender).ToList();
			foreach (RecListItem item in holdingQueue)
			{
				Console.WriteLine("get " + item.Data + " " + item.TStamp);
			}
		}

		public void pullOutReadyItems()
		{
			while(holdingQueue.Count > 0&& holdingQueue[0].TStamp.IsFinal)
			{
				string data = holdingQueue[0].Data;
				Timestamp t = holdingQueue[0].TStamp;
				holdingQueue.RemoveAt(0);
				AddReadyMessage(data, t);
			}
		}

		public void ManageControlMessage(string data)
		{
			string control = data.Split(' ')[1];
			string[] nums = control.Split(',');

			int value = Convert.ToInt32(nums[0]);
			int sender = Convert.ToInt32(nums[1]);
			int messageNum = Convert.ToInt32(nums[2]);

			sentMessages[messageNum-1].recieved(value);
			Console.WriteLine("Control is "+ value +sender+messageNum);
			if(sentMessages[messageNum-1].allMessagesRecieved())
			{
				finalize(sentMessages[messageNum-1].getLargestTimeStamp(), sender, messageNum);
				Console.WriteLine("done " + sentMessages[messageNum-1].getLargestTimeStamp());
			}
		}

		public void finalize(int tmpstamp, int sender, int messageNum)
		{
			string message = "Finalize "+tmpstamp + "," + sender + "," + messageNum+ " <EOM>";
			Console.WriteLine(message);
			sendMulticastContolMessage(message);
		}

		public Timestamp GenerateTimeStamp(String message)
		{
			string pattern = @"Message # (\d+) from MiddleWare (\d+).*";
			Regex rgx = new Regex(pattern, RegexOptions.IgnoreCase);
			MatchCollection matches = rgx.Matches(message);
			if (matches.Count > 0)
			{
				foreach (Match m in matches)
				{
					GroupCollection groups = m.Groups;
					int middlewareNum = Convert.ToInt32(groups[2].Value);
					int messageNum = Convert.ToInt32(groups[1].Value);
					Timestamp ts = GetLargestTimestamp();
					return new Timestamp(ts.Value + 1, middlewareNum, messageNum);
				}
			}

			return null;
		}

		public Timestamp GetLargestTimestamp()
		{
			Timestamp largest = null;
			if(holdingQueue.Count == 0)
			{
				if(this.readyMessages.Count == 0)
				{
					return new Timestamp(0, 0, 0);
				}else {
					return this.readyMessages[readyMessages.Count-1].TStamp;
				}
			}

			foreach(RecListItem stamp in this.holdingQueue)
			{
				if (largest == null)
				{
					largest = stamp.TStamp;
				}
				else if(stamp.TStamp.Value > largest.Value)
				{
					largest = stamp.TStamp;
				}
				else if (stamp.TStamp.Value == largest.Value &&
					stamp.TStamp.Sender > largest.Sender)
				{
					largest = stamp.TStamp;
				}
			}

			return largest;
		}

		// This method first sets up a task for receiving messages from the Network.
		// Then, it sends a multicast message to the Netwrok.
        public void DoWork()
		{
			// Send a multicast message to the Network
			int numOfMessages = sentMessages.Count + 1;
			String message = "Message # " +numOfMessages+" from Middleware "+ num + " <EOM>\n";
			sendMessage(message, 1081);
			AddSentMessage(message);
		}
	}

	public class Timestamp
	{
		private int val;
		private int sender;
		private bool isFinal;
		private int messageNum;

		public int Value
		{
			get { return val; }
			set { val = value; }
		}

		public int MessageNum
		{
			get { return messageNum; }
		}

		public int Sender
		{
			get { return sender; }
		}

		public bool IsFinal
		{
			get { return isFinal; }
			set { isFinal = value; }
		}

		public Timestamp(int val, int sender, int num)
		{
			this.val = val;
			this.sender = sender;
			this.isFinal = false;
			this.messageNum = num;
		}

		public override string ToString()
		{
			return this.val + "," + this.sender+","+this.messageNum;
		}
	}

	public class RecListItem
	{
		private string data;
		private Timestamp tStamp;

		public string Data
		{
			get { return data; }
		}

		public Timestamp TStamp
		{
			get { return tStamp; }
		}

		public RecListItem(string data, Timestamp tStamp)
		{
			this.data = data;
			this.tStamp = tStamp;
		}

		public override string ToString()
		{
			return data;
		}
	}

	public class SentMessage
	{
		private string data;
		private int recievedNum;
		private readonly int SENT_NUM = 5;
		private int timeStampValue = -1;

		public SentMessage(string data)
		{
			this.data = data;
			this.recievedNum = 0;
		}

		public string Data
		{
			get { return data; }
		}

		public void recieved(int value)
		{
			this.recievedNum++;
			if(value > timeStampValue)
				timeStampValue = value;
			
		}

		public bool allMessagesRecieved()
		{
			if(this.recievedNum >= SENT_NUM)
			{
				return true;
			}

			return false;
		}

		public int getLargestTimeStamp()
		{
			if (!allMessagesRecieved())
			{
				throw new Exception("Not all messages have been received");
			}
			return timeStampValue;
			
		}
		public override string ToString()
		{
			return data;
		}
	}
}