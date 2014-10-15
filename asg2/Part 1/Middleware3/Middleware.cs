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
public class Middleware
{
	public static int Main(String[] args)
	{
		Application.Run(new WinFormExample());
		return 0;
	}

	public class WinFormExample : Form
	{
		const int myPort = 1084;
		private Button button;
		ListBox sent = new ListBox();
		List<String> sentMessages = new List<String>();
		ListBox rec = new ListBox();
		List<RecListItem> recMessages = new List<RecListItem>();
		ListBox ready = new ListBox();
		List<String> readyMessages = new List<String>();
		int num = myPort - 1081;
		int[] timestamps = { 0, 0, 0, 0, 0 };

		private MessageInfo GetMessageInfo(string message)
		{
			string pattern = @"Message # (\d+) from MiddleWare (\d+)\s*:([\d+,\s]+).*";
			Regex rgx = new Regex(pattern, RegexOptions.IgnoreCase);
			MatchCollection matches = rgx.Matches(message);
			Console.WriteLine("got here");
			Console.WriteLine(message);
			Console.WriteLine(matches.Count);
			if (matches.Count > 0)
			{
				foreach (Match m in matches)
				{
					GroupCollection groups = m.Groups;
					Console.WriteLine("match found Message" + groups[1].Value + " from " + groups[2].Value + " hi " + groups[3].Value + "lol");
					string[] nums = groups[3].Value.Split(' ');
					int[] val = new int[5];
					int middlewareNum = Convert.ToInt32(groups[2].Value);
					int messageNum = Convert.ToInt32(groups[1].Value);

					for (int i = 0; i < nums.Length; i ++)
					{
						val[i] = Convert.ToInt32(nums[i]);
						Console.Write(val[i]);
					}

					Console.WriteLine("");

					MessageInfo mInfo = new MessageInfo(val, messageNum, middlewareNum);
					return mInfo;
				}
			}

			return null;
		}

		public WinFormExample()
		{
			SetupMessages();
			DisplayGUI();
			ReceiveMulticast();
			//this.DoWork();
		}

		private void SetupMessages()
		{
			//sentMessages.Add("What up");
			//recMessages.Add("What up DAWG");
			//readyMessages.Add("What up CAT");
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

		private void MyButtonClick(object source, EventArgs e)
		{
			DoWork();
		}

		private void AddSentMessage(string m)
		{
			this.sentMessages.Add(m);
			this.sent.DataSource = null;
			this.sent.DataSource = sentMessages;
		}

		private void AddRecMessage(string m)
		{
			RecListItem item = new RecListItem(m, false);
			this.recMessages.Add(item);
			this.rec.DataSource = null;
			this.rec.DataSource = recMessages;
		}

		private RecListItem GetRecMessage(string m)
		{
			foreach (RecListItem item in this.recMessages)
			{
				if (item.Data == m)
				{
					return item;
				}
			}

			return null;
		}

		private void EditMessageValidity(string m, bool isValid)
		{
			RecListItem item = GetRecMessage(m);
			item.IsValidated = isValid;
			if (isValid)
				AddReadyMessage(m);
		}

		private void AddReadyMessage(string m)
		{
			this.readyMessages.Add(m);
			this.ready.DataSource = null;
			this.ready.DataSource = readyMessages;
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
					AddRecMessage(data);

					EditMessageValidity(data, CheckValidity(data));
					ReCheckValidity();
				}
			}
			catch (Exception ee)
			{
				Console.WriteLine(ee.ToString());
			}
		}

		private void ReCheckValidity()
		{
			int numOfValidMes = 0;
			foreach(RecListItem message in recMessages)
			{
				if(message.IsValidated)
					numOfValidMes++;
			}

			int prevNum = 0;
			while (prevNum != numOfValidMes)
			{
				prevNum = numOfValidMes;
				foreach (RecListItem message in recMessages)
				{
					if(!message.IsValidated)
					{
						bool isValid = CheckValidity(message.Data);
						EditMessageValidity(message.Data, isValid);
						if(isValid)
							numOfValidMes++;
					}
				}
			}
			
		}
		// This method first sets up a task for receiving messages from the Network.
		// Then, it sends a multicast message to the Netwrok.
        public void DoWork()
		{
			// Send a multicast message to the Network
            try
			{
				// Find the IP address of localhost
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

				IPEndPoint remoteEP = new IPEndPoint(ipAddress, 1081);
				Socket sendSocket;
				try
				{
					// Create a TCP/IP  socket.
                    sendSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

					// Connect to the Network 
                    sendSocket.Connect(remoteEP);
					int numOfMessages = sentMessages.Count + 1;
					this.timestamps[num - 1 ] ++;
					String message = "Message # " +numOfMessages+" from Middleware "+ num + ":"+GetTimeStampStr()+"<EOM>\n";
					// Generate and encode the multicast message into a byte array.
                    byte[] msg = Encoding.ASCII.GetBytes(message);

					// Send the data to the network.
                    int bytesSent = sendSocket.Send(msg);

					sendSocket.Shutdown(SocketShutdown.Both);
					sendSocket.Close();
					AddSentMessage(message);

					//Console.WriteLine("Press ENTER to terminate ...");
					//Console.ReadLine();
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

		private void IncTimeStampVal(int val)
		{
			timestamps[val-1] ++;
		}

		private int GetTimeStampVal(int val)
		{
			return timestamps[val-1];
		}

		private string GetTimeStampStr()
		{
			string tmp = "";
			for(int i = 0; i < timestamps.Length; i++)
			{
				tmp += timestamps[i];
				if(i != timestamps.Length -1)
				{
					tmp += " ";
				}
			}

			return tmp;
		}

		public bool CheckValidity(String data)
		{
			MessageInfo mInfo = GetMessageInfo(data);
			
			if (mInfo.MiddlewareNum == this.num)
			{
				//AddReadyMessage(data);
				return true;
			}else if ((mInfo.Timestamp[mInfo.MiddlewareNum - 1] == this.timestamps[mInfo.MiddlewareNum - 1] + 1)
			&&(mInfo.Compare(this.timestamps)))
			{
				//AddReadyMessage(data);
				this.timestamps[mInfo.MiddlewareNum-1] ++;
				return true;
			}else
			{
				return false;
			}
		}
	}

	public class MessageInfo
	{
		private int[] timestamp;
		private int messageNum;
		private int middlewareNum;

		public int[] Timestamp
		{
			get { return timestamp; }
		}

		public int MessageNum
		{
			get { return messageNum; }
		}

		public int MiddlewareNum
		{
			get { return middlewareNum; }
		}

		public bool Compare(int[] tmpTimeStamp)
		{
			for(int i = 0; i < this.timestamp.Length; i ++)
			{
				if ((i != this.middlewareNum - 1)&(this.timestamp[i] > tmpTimeStamp[i]))
				{
					return false;
				}
			}

			return true;
		}

		public MessageInfo(int[] time, int mes, int mid)
		{
			this.timestamp = time;
			this.messageNum = mes;
			this.middlewareNum = mid;
		}
	}

	public class RecListItem
	{
		private string data;
		private bool isValidated;

		public string Data
		{
			get { return data; }
		}

		public bool IsValidated
		{
			get { return isValidated; }
			set { isValidated = value; }
		}

		public RecListItem(string data, bool isValid)
		{
			this.data = data;
			this.isValidated = isValid;
		}

		public override string ToString()
		{
			return data ;
		}
	}
}