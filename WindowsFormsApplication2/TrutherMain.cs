using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Collections;
//Needed
using System.IO;
using System.Net;

//regex
using System.Text.RegularExpressions;

//db
using System.Data.OleDb;

//thread
using System.Threading;

namespace EffiaTruther
{
    public partial class Form1 : Form
    {
        OleDbConnection m_cnADONETConnection = new OleDbConnection();
        OleDbDataAdapter m_daDataAdapter = new OleDbDataAdapter();
        DataTable m_dtPatents = new DataTable();
        //DataSet ds = new DataSet("Patents");
        
        int m_rowPosition = 0;
        int mouse_x = 0;
        int mouse_y = 0;
        Point LocalMousePosition = Point.Empty;
        int dgvCurrentRowIndex = 0;
        int dgvPrevRowIndex = 0;

        ArrayList Transistors = new ArrayList();
        ArrayList Resistors = new ArrayList();
        ArrayList Capacitors = new ArrayList();
        ArrayList Inductors = new ArrayList();
        ArrayList Diodes = new ArrayList();
        ArrayList Nodes = new ArrayList();
        ArrayList CS = new ArrayList();
        ArrayList VS = new ArrayList();
        ArrayList Amplifier = new ArrayList();
        ArrayList Attenuator = new ArrayList();
        ArrayList Bias = new ArrayList();
        ArrayList PDet = new ArrayList();
        ArrayList Switch = new ArrayList();
        ArrayList Other_c = new ArrayList();
        ArrayList ANDgate = new ArrayList();
        ArrayList Tgate = new ArrayList();
        ArrayList ORgate = new ArrayList();
        ArrayList NORgate = new ArrayList();
        ArrayList Other_bb = new ArrayList();
        ArrayList Buffer = new ArrayList();
        ArrayList Supply = new ArrayList();
        ArrayList Ground = new ArrayList();
        ArrayList newC = new ArrayList();

        /// <summary>
        /// drag box variables
        /// </summary>
        ArrayList pointArrayForMouseDreag = new ArrayList();
        Boolean DRAG = false;
        int drag_len, drag_wid;

        Hashtable dgvCoordinates = new Hashtable(); //stores all coordinates for dgv memebers 
                                                    //key is component name (first coloumn member)
        ArrayList allDGVElements = new ArrayList(); //All the elements in DGV

        Image rootImage; //store the actual image that has not been marked upon (clean image)

        Stack<ArrayList> actionState = new Stack<ArrayList>(); //this is to store the current actions for undo
        //Stack<int> actionIndices = new Stack<int>(); //this is to store the current row related drawing info
        
        Boolean ON_TRUTHING = false; //currently truthing
        Boolean ON_NETLIST = false; //currently netlisting
        Boolean PICK_SCHEMATIC = false; //click on pick schematic
        Boolean HIGHLIGHT_ALL_CLOSE = false; //highlight closest components during netlisting
        Boolean TOGGLE_AUTO_CELL_MOVE = false; //automatically move to next cell

        int DGV_COLS = 4;
        

        public Form1()
        {
            InitializeComponent();
            
            //temporary
            //loadTestImage();
        }

       

        private byte[] downloadedData;

        //Connects to a URL and attempts to download the file
        /// <summary>
        /// Function obtained from C# example
        /// Downloads data from URL into an array called downloaded data
        /// </summary>
        /// <param name="url"></param>
        private void downloadData(string url)
        {
            downloadedData = new byte[0];
            try
            {
                //Optional
                writeLog("Connecting...\n");
                Application.DoEvents();

                //Get a data stream from the url
                WebRequest req = WebRequest.Create(url);
                WebResponse response = req.GetResponse();
                Stream stream = response.GetResponseStream();

                //Download in chuncks
                byte[] buffer = new byte[1024];

                //Get Total Size
                int dataLength = (int)response.ContentLength;



                writeLog("Downloading...\n");
                Application.DoEvents();

                //Download to memory
                //Note: adjust the streams here to download directly to the hard drive
                MemoryStream memStream = new MemoryStream();
                while (true)
                {
                    //Try to read the data
                    int bytesRead = stream.Read(buffer, 0, buffer.Length);

                    if (bytesRead == 0)
                    {
                        Application.DoEvents();
                        //Finished downloading
                        break;
                    }
                    else
                    {
                        //Write the downloaded data
                        memStream.Write(buffer, 0, bytesRead);
                        Application.DoEvents();
                      
                    }
                }

                //Convert the downloaded stream to a byte array
                downloadedData = memStream.ToArray();

                //Clean up
                stream.Close();
                memStream.Close();
            }
            catch (Exception)
            {
                //May not be connected to the internet
                //Or the URL might not exist
                writeLog("There was an error accessing the URL.\n");
            }

            
            writeLog("Download Data through HTTP\n");
        }

        //Start downloading process
        /// <summary>
        /// Function to download the image file from the weburl above
        /// saves the image to jpeg file
        /// the name of the image is derived from path, unique ID
        /// </summary>
        private void downloadFile()
        {
            downloadData(txtUrl.Text);

            //Get the last part of the url, ie the file name
            if (downloadedData != null && downloadedData.Length != 0)
            {
                string urlName = txtUrl.Text;
                string path = txtData.Text;
                if (urlName.EndsWith("/"))
                    urlName = urlName.Substring(0, urlName.Length - 1); //Chop off the last '/'

                urlName = urlName.Substring(urlName.LastIndexOf('/') + 1);

                //save
                richTextBox1.AppendText("Saving Data...");
                Application.DoEvents();

                //Write the bytes to a file
                string fileName = findPatentID(urlName);
               // MessageBox.Show(fileName);
                ///Save data into a jpeg file
                FileStream newFile = new FileStream(String.Concat(path,fileName,".jpg"), FileMode.Create);
                newFile.Write(downloadedData, 0, downloadedData.Length);
                newFile.Close();

                writeLog("Download Data");
                ///Load that image into picture box
                //pictureBox1.Image = System.Drawing.Image.FromFile(String.Concat(txtData.Text, fileName, ".jpg"));
                rootImage = System.Drawing.Image.FromFile(String.Concat(txtData.Text, fileName, ".jpg"));
                setPictureBoxImage(rootImage);
                //MessageBox.Show("Saved Successfully");
                txtPno.Text = findPatentNumber(txtUrl.Text);
                updateDatabase(fileName, txtPno.Text, String.Concat(txtData.Text, fileName, ".jpg"));
                txtUno.Text = fileName;
            }
        }
        /// <summary>
        /// Simple routine sets picutrebox image = i
        /// </summary>
        /// <param name="i"></param>
        private void setPictureBoxImage(Image i)
        {
            pictureBox1.Image = i;
        }

        /// <summary>
        /// Simple function to print string to the log window (richtextbox1)
        /// </summary>
        /// <param name="message"></param>
        private void writeLog(string message)
        {
            richTextBox1.AppendText(message);
            richTextBox1.ScrollToCaret();

        }
        /// <summary>
        /// Find the patent number from the downloaded data
        /// From the URL of the image, substring is derived which goes to the patent homepage
        /// the patent or app number is right before the string "Filing Date" 
        /// </summary>
        /// <param name="urlname"></param>
        /// <returns></returns>
        private string findPatentNumber(string urlname)
        {

            //Patent number will be obtained from the document
            //the image url is just for the link
            //From the image url, we can get the patent home page when we
            //chop the link to until the &pg= field

            string patentUrl = urlname.Substring(0, urlname.IndexOf("&pg="));
            const string HTML_TAG_PATTERN = "<.*?>"; //for regex
            int foo, foob;

            //clear the byte array
            //we want to fetch the HTML now and not the image
            downloadedData.Initialize();
            //MessageBox.Show(patentUrl);
            //the patent URL points to teh patent homepage, meaning data will be html
            downloadData(patentUrl);
            if (downloadedData != null && downloadedData.Length != 0)
            {
                string s = Encoding.ASCII.GetString(downloadedData);
                //Strip the HTML tags <, *, ? etc
                s = Regex.Replace(s, HTML_TAG_PATTERN, " ");
                writeLog(s);
                
                //Extract the patent number which is between Patent number field and 
                //Filing date field
                foo = s.IndexOf("Filing date");
                string s3 = s.Substring(0, foo);
                foob = s3.LastIndexOf(':');
                string s2 = s.Substring(foob+1, foo-foob-1);
                //foo = s.IndexOf("Patent number : ")+15;
                //foob = s.IndexOf("Filing date");
                //return (String.Concat("Patent number: ",s.Substring(foo,foob-foo)));
                
                return s2;
                
                //MessageBox.Show(s);
            }
            return null;
        }
        /// <summary>
        /// This is the ID that google generates/associates with each patent image
        /// </summary>
        /// <param name="urlname"></param>
        /// <returns></returns>
        private string findPatentID(string urlname)
        {
            //patent ID is given by &ci=()&
            //& begin and & end
            //This patent ID is the unique ID that google generates
            //This is not the patent number
            //this is used for the file name
            int startIndex = urlname.IndexOf("&ci=");
            int endIndex = urlname.IndexOf("&edge=");
            return (urlname.Substring(startIndex + 4, endIndex - (startIndex + 4))).Replace("%2C","");
        }

        /// <summary>
        /// When the load URL is clicked, open the URL file and download
        /// all the files to harddrive in form of jpeg images
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void loadURLFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //Open the text file that contains a bunch of image urls
            //this function goes through that text file and
            //opens every image pointed by the url and saves into a
            //directory indicated by path textbox
            
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                System.IO.StreamReader sr = new System.IO.StreamReader(openFileDialog1.FileName);
                while (sr.Peek() >= 0)
                {
                    string str = sr.ReadLine();
                    txtUrl.Text = str;
                    downloadFile();
                }

            }
        }

        /// <summary>
        /// Open the ADO.net database connection
        /// Define dataAdpater and COmmand builder
        /// </summary>
        private void openConnection()
        {
            m_dtPatents.Clear();
            m_cnADONETConnection.ConnectionString = String.Concat(@"Provider=Microsoft.Jet.OLEDB.4.0;Data Source=",textBox1.Text);
            String cmd = String.Concat("Select * From ", textBox2.Text); ;
            m_daDataAdapter = new OleDbDataAdapter(cmd, m_cnADONETConnection);

            OleDbCommandBuilder m_cbCommandBuilder = new OleDbCommandBuilder(m_daDataAdapter);
            m_daDataAdapter.Fill(m_dtPatents);
        }
        /// <summary>
        /// When the form is loaded, open the Database connection by default
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Form1_Load(object sender, EventArgs e)
        {
           
            //We are using ADO.net for database access
            //Access 2002-2007 database is needed
            //open ADONET connection and data adapters
            //The table name is Patents and the database file is patents.mdb
            openConnection();
            //m_cnADONETConnection.Close();
        }

        /// <summary>
        /// Close the database connection
        /// </summary>
        private void closeConnection()
        {
            m_cnADONETConnection.Close();
        }

        /// <summary>
        /// Action when form is closed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            //Force the closing of database when you exit
            //We dont want to rely on MS to do its stuff
           // m_cnADONETConnection.Close();
        }

        /// <summary>
        /// FUnction draws on picturebox
        /// it marks a circle on the coordinates indicated by x and y
        /// color is indicated by val
        /// </summary>
        /// <param name="val"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        private void drawOnPictureBox(Color val, int x, int y)
        {
            //when mouse is clicked on a point, we want to draw ellipse
            //using graphics method
            //The current image has to in non-idexed format for us to draw on
            //it or modify it.

            //takes coordinates in the form of x an dy

            if (ON_TRUTHING || ON_NETLIST)
            {

               
                Graphics G;
               
                Image original = pictureBox1.Image;
                //check if the image is indexed or non indexed
                //if indexed, we need to create a bitmap version of the image before
                //starting to modify it.

                switch (original.PixelFormat)
                {
                    case System.Drawing.Imaging.PixelFormat.Undefined:
                    case System.Drawing.Imaging.PixelFormat.Format1bppIndexed:
                    case System.Drawing.Imaging.PixelFormat.Format4bppIndexed:
                    case System.Drawing.Imaging.PixelFormat.Format8bppIndexed:
                    case System.Drawing.Imaging.PixelFormat.Format16bppGrayScale:
                    case System.Drawing.Imaging.PixelFormat.Format16bppArgb1555:

                        // Create a new BitMap object using original Image instance
                        original = new Bitmap(original);
                        break;
                }

                //now form the graphics element from the non-indexed version of the
                //image
                G = Graphics.FromImage(original);

                //draw the ellipse, the coordinates are the origin from where the 
                //ellipse will be drawn.
                //adjust it as needed
                
                G.DrawEllipse(new Pen(val,2), x - 4,y - 3, 10, 10);
                //G.FillEllipse(new SolidBrush(Color.Black), mouse_x, mouse_y, 10, 10);

                //important step -> to display the drawings, reload the modified imag
                //into the picture box

                pictureBox1.Image = original;

                G.Dispose();
            }
        }
        /// <summary>
        /// Same as previous function
        /// plots a circle of color indicated by val on current mouse coordinates
        /// 
        /// current mouse coordinates are indicated by mouse_x and mouse_y
        /// </summary>
        /// <param name="val"></param>
        private void drawOnPictureBox(Color val)
        {
            //when mouse is clicked on a point, we want to draw ellipse
            //using graphics method
            //The current image has to in non-idexed format for us to draw on
            //it or modify it.

            if (ON_TRUTHING || ON_NETLIST)
            {

               // mouse_x = e.X;
               // mouse_y = e.Y;
                Graphics G;
                Image original = pictureBox1.Image;
                //check if the image is indexed or non indexed
                //if indexed, we need to create a bitmap version of the image before
                //starting to modify it.

                switch (original.PixelFormat)
                {
                    case System.Drawing.Imaging.PixelFormat.Undefined:
                    case System.Drawing.Imaging.PixelFormat.Format1bppIndexed:
                    case System.Drawing.Imaging.PixelFormat.Format4bppIndexed:
                    case System.Drawing.Imaging.PixelFormat.Format8bppIndexed:
                    case System.Drawing.Imaging.PixelFormat.Format16bppGrayScale:
                    case System.Drawing.Imaging.PixelFormat.Format16bppArgb1555:

                        // Create a new BitMap object using original Image instance
                        original = new Bitmap(original);
                        break;
                }

                //now form the graphics element from the non-indexed version of the
                //image
                G = Graphics.FromImage(original);

                //draw the ellipse, the coordinates are the origin from where the 
                //ellipse will be drawn.
                //adjust it as needed

                G.DrawEllipse(new Pen(val,2), mouse_x-4, mouse_y-3, 10, 10);
                
                
                //G.FillEllipse(new SolidBrush(Color.Black), mouse_x, mouse_y, 10, 10);

                //important step -> to display the drawings, reload the modified imag
                //into the picture box

                pictureBox1.Image = original;

                G.Dispose();
            }
        }

        private void dgvReset()
        {
            dgvCoordinates.Clear();
            dgvCurrentRowIndex = 0;
            dgvPrevRowIndex = 0;
            allDGVElements.Clear();
        }

        /// <summary>
        /// Start truthing - ON_TRUTHING is enabled or disabled
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void startTruthingToolStripMenuItem_Click(object sender, EventArgs e)
        {
            

            //Truthing option has to be on to start truthing.
            //the state gets toggled by this button.
            //this function just toggles the truthing state
            if (!ON_TRUTHING)
            {
                //First step is to go the database and cycle all images that dont have a netlist
                cycleImagesFromDatabase();
                pictureBox1.Focus();
                //Clear the array and marking if any
                clearAllArrays();
                resetAllArrays();
                dgvReset();
                ON_TRUTHING = true;
                startTruthingToolStripMenuItem.Text = "Done Truthing";
                writeLog("Truthing begin...\n");
                writeLog("Click on the image to identify components - Select Actions > Done Truthing when done");
            }
            else
            {
               
                clearAllArrays(); //has to call before turning off ON_TRUTHING
                setPictureBoxImage(rootImage);
                ON_TRUTHING = false;
                startTruthingToolStripMenuItem.Text = "Start Truthing";
                writeLog("Truthing done...\n");
                //MessageBox.Show("Truthing done! Select Actions > Create Netlist now");
            }
        }
        /// <summary>
        /// Add new missing component during remark 
        /// </summary>
        private void AddNewC(string n, int size, int compCount)
        {
            string name;
            name = String.Concat(n, (size+compCount).ToString());
            name = String.Concat(name, "_", mouse_x.ToString(), "_", mouse_y.ToString());
            newC.Add(name);

            //add the stack element
            actionState.Push(newC);

            //draw
            drawOnPictureBox(Color.Red);
        }

        /// <summary>
        /// Return how many components of certain type are in newC array
        /// </summary>
        /// <param name="n"></param>
        /// <returns></returns>
        private int newCTotalNumberElements(string n)
        {
            char [] delims = {'_'};
            int count = newC.Count;
            int total = 0;
            string[] foo;
            for (int i = 0; i < count; i++)
            {
                foo = newC[i].ToString().Split(delims, StringSplitOptions.RemoveEmptyEntries);
                if (foo[0].Contains(n))
                    total++;
            }
            return total;

        }

        /// <summary>
        /// Function adds components to the relevant arraylist
        /// </summary>
        private void AddTransistor()
        {
            //FUnction adds the transistors to array list
            //Names of the transistors are autogenerated
            //T0..T1...So forth
            //Autonames are based on the fact that arraylist size gets updated with ever
            //transistor that gets added to it.
            //the array list elements are of the form T0_100_200 where 
            //the first part is transistor name, second part is x coord, third is y coord
            //MessageBox.Show("This is transistor");
            string name = "T";
            int size = Transistors.Count;
            name = String.Concat(name, size.ToString());
            name = String.Concat(name, "_", mouse_x.ToString(), "_", mouse_y.ToString());
            Transistors.Add(name);

            //add the stack element
            actionState.Push(Transistors);

            //draw
            drawOnPictureBox(Color.Red);
        }

        private void transistorToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (ON_TRUTHING && ON_NETLIST) AddNewC("T", Transistors.Count,newCTotalNumberElements("T"));
            else
            if(ON_TRUTHING) AddTransistor();
           
        }
        /// <summary>
        /// Store mouseclick coordinates
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void pictureBox1_MouseUp(object sender, MouseEventArgs e)
        {
            //when mouse pressed and the button is right click
            //draw on it.
            if (e.Button == MouseButtons.Right)
            {
                //drawOnPictureBox(e);
                Point p = new Point(e.X, e.Y);
                Point c = TranslateZoomMousePosition(p, pictureBox1.Image,
                                    pictureBox1.Width, pictureBox1.Height);
                mouse_x = c.X;
                mouse_y = c.Y;

            }
            if (DRAG) DRAG = false;
        }

        /// <summary>
        /// Function adds components to the relevant arraylist
        /// </summary>
        private void AddResistor()
        {
            //FUnction adds the res to array list
            //Names of the res are autogenerated
            //R0..R1...So forth
            //Autonames are based on the fact that arraylist size gets updated with ever
            //transistor that gets added to it.
            //the array list elements are of the form R0_100_200 where 
            //the first part is res name, second part is x coord, third is y coord
            //MessageBox.Show("This is Resistor");
            string name = "R";
            int size = Resistors.Count;
            name = String.Concat(name, size.ToString());
            name = String.Concat(name, "_", mouse_x.ToString(), "_", mouse_y.ToString());
            Resistors.Add(name);

            //add the stack element
            actionState.Push(Resistors);

            //draw
            drawOnPictureBox(Color.Red);
        }

        private void resistorToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (ON_TRUTHING && ON_NETLIST) AddNewC("R", Resistors.Count, newCTotalNumberElements("R"));
            else
            if (ON_TRUTHING) AddResistor();
        }

        /// <summary>
        /// Function adds components to the relevant arraylist
        /// </summary>
        private void AddCapacitor()
        {
            //similar to above

            string name = "C";
            int size = Capacitors.Count;
            name = String.Concat(name, size.ToString());
            name = String.Concat(name, "_", mouse_x.ToString(), "_", mouse_y.ToString());
            Capacitors.Add(name);

            //add the stack element
            actionState.Push(Capacitors);

            //Draw
            drawOnPictureBox(Color.Red);
        }
        private void capacitorToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (ON_TRUTHING && ON_NETLIST) AddNewC("C", Capacitors.Count, newCTotalNumberElements("C"));
            else
            if (ON_TRUTHING) AddCapacitor();
        }

        /// <summary>
        /// Function adds components to the relevant arraylist
        /// </summary>
        private void AddDiode()
        {
            //similar to above - diodes

            string name = "D";
            int size = Diodes.Count;
            name = String.Concat(name, size.ToString());
            name = String.Concat(name, "_", mouse_x.ToString(), "_", mouse_y.ToString());
            Diodes.Add(name);

            //add the stack element
            actionState.Push(Diodes);

            //Draw
            drawOnPictureBox(Color.Red);
        }

        /// <summary>
        /// Function adds components to the relevant arraylist
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void diodeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (ON_TRUTHING && ON_NETLIST) AddNewC("D", Diodes.Count, newCTotalNumberElements("D"));
            else
            if (ON_TRUTHING) AddDiode();
        }

        /// <summary>
        /// Function adds components to the relevant arraylist
        /// </summary>
        private void AddNode()
        {
            //similar to above - nodes

            string name = "No";
            int size = Nodes.Count;
            name = String.Concat(name, size.ToString());
            name = String.Concat(name, "_", mouse_x.ToString(), "_", mouse_y.ToString());
            Nodes.Add(name);

            //add the stack element
            actionState.Push(Nodes);

            //Draw
            drawOnPictureBox(Color.Chocolate);
        }

        private void nodeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (ON_TRUTHING && ON_NETLIST) AddNewC("No", Nodes.Count, newCTotalNumberElements("No"));
            else
            if (ON_TRUTHING) AddNode();
        }

        /// <summary>
        /// Function adds components to the relevant arraylist
        /// </summary>
        private void AddCS()
        {
            //similar to above - currentsource

            string name = "CS";
            int size = CS.Count;
            name = String.Concat(name, size.ToString());
            name = String.Concat(name, "_", mouse_x.ToString(), "_", mouse_y.ToString());
            CS.Add(name);

            //add the stack element
            actionState.Push(CS);

            //Draw
            drawOnPictureBox(Color.Red);
        }

        private void currentToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (ON_TRUTHING && ON_NETLIST) AddNewC("CS", CS.Count, newCTotalNumberElements("CS"));
            else
            if (ON_TRUTHING) AddCS();
        }

        /// <summary>
        /// Function adds components to the relevant arraylist
        /// </summary>
        private void AddVS()
        {
            //similar to above - voltagesource

            string name = "VS";
            int size = VS.Count;
            name = String.Concat(name, size.ToString());
            name = String.Concat(name, "_", mouse_x.ToString(), "_", mouse_y.ToString());
            VS.Add(name);

            //add the stack element
            actionState.Push(VS);

            //Draw
            drawOnPictureBox(Color.Red);
        }

        private void voltageToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (ON_TRUTHING && ON_NETLIST) AddNewC("VS", VS.Count, newCTotalNumberElements("VS"));
            else
            if (ON_TRUTHING) AddVS();
        }

        /// <summary>
        /// Function adds components to the relevant arraylist
        /// </summary>
        private void AddInductor()
        {
            //similar to above - inductor

            string name = "L";
            int size = Inductors.Count;
            name = String.Concat(name, size.ToString());
            name = String.Concat(name, "_", mouse_x.ToString(), "_", mouse_y.ToString());
            Inductors.Add(name);

            //add the stack element
            actionState.Push(Inductors);

            //Draw
            drawOnPictureBox(Color.Red);
        }

        private void inductorToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (ON_TRUTHING && ON_NETLIST) AddNewC("L", Inductors.Count, newCTotalNumberElements("L"));
            else
            if (ON_TRUTHING) AddInductor();
        }

        /// <summary>
        /// Function adds components to the relevant arraylist
        /// </summary>
        private void AddAmplifier()
        {
            //similar to above - Amplifier

            string name = "OP";
            int size = Amplifier.Count;
            name = String.Concat(name, size.ToString());
            name = String.Concat(name, "_", mouse_x.ToString(), "_", mouse_y.ToString());
            Amplifier.Add(name);

            //add the stack element
            actionState.Push(Amplifier);

            //Draw
            drawOnPictureBox(Color.Red);
        }

        private void amplifierToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (ON_TRUTHING && ON_NETLIST) AddNewC("OP", Amplifier.Count, newCTotalNumberElements("OP"));
            else
            if (ON_TRUTHING) AddAmplifier();
        }

        /// <summary>
        /// Function adds components to the relevant arraylist
        /// </summary>
        private void AddAttenuator()
        {
            //similar to above - Attenuator

            string name = "AT";
            int size = Attenuator.Count;
            name = String.Concat(name, size.ToString());
            name = String.Concat(name, "_", mouse_x.ToString(), "_", mouse_y.ToString());
            Attenuator.Add(name);

            //add the stack element
            actionState.Push(Attenuator);

            //Draw
            drawOnPictureBox(Color.Red);
        }

        private void attenuatorToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (ON_TRUTHING && ON_NETLIST) AddNewC("AT", Attenuator.Count, newCTotalNumberElements("AT"));
            else
            if (ON_TRUTHING) AddAttenuator();
        }
        /// <summary>
        /// Function adds components to the relevant arraylist
        /// </summary>
        private void AddBias()
        {
            //similar to above - Bias

            string name = "BS";
            int size = Bias.Count;
            name = String.Concat(name, size.ToString());
            name = String.Concat(name, "_", mouse_x.ToString(), "_", mouse_y.ToString());
            Bias.Add(name);

            //add the stack element
            actionState.Push(Bias);

            //Draw
            drawOnPictureBox(Color.Red);
        }

        /// <summary>
        /// Function adds components to the relevant arraylist
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void biasToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (ON_TRUTHING && ON_NETLIST) AddNewC("BS", Bias.Count, newCTotalNumberElements("BS"));
            else
            if (ON_TRUTHING) AddBias();
        }

        /// <summary>
        /// Function adds components to the relevant arraylist
        /// </summary>
        private void AddPDET()
        {
            //similar to above - PDet

            string name = "PD";
            int size = PDet.Count;
            name = String.Concat(name, size.ToString());
            name = String.Concat(name, "_", mouse_x.ToString(), "_", mouse_y.ToString());
            PDet.Add(name);

            //add the stack element
            actionState.Push(PDet);

            //Draw
            drawOnPictureBox(Color.Red);
        }

        private void powerDetectorToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (ON_TRUTHING && ON_NETLIST) AddNewC("PD", PDet.Count, newCTotalNumberElements("PD"));
            else
            if (ON_TRUTHING) AddPDET();
        }

        /// <summary>
        /// Function adds components to the relevant arraylist
        /// </summary>
        private void AddSW()
        {
            //similar to above - Other_bb

            string name = "SW";
            int size = Switch.Count;
            name = String.Concat(name, size.ToString());
            name = String.Concat(name, "_", mouse_x.ToString(), "_", mouse_y.ToString());
            Switch.Add(name);

            //add the stack element
            actionState.Push(Switch);

            //Draw
            drawOnPictureBox(Color.Red);
        }

        private void otherToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            if (ON_TRUTHING && ON_NETLIST) AddNewC("SW", Switch.Count, newCTotalNumberElements("SW"));
            else
            if (ON_TRUTHING) AddSW();
        }

        /// <summary>
        /// Function adds components to the relevant arraylist
        /// </summary>
        private void AddAND()
        {
            //similar to above - ANDgate

            string name = "AND";
            int size = ANDgate.Count;
            name = String.Concat(name, size.ToString());
            name = String.Concat(name, "_", mouse_x.ToString(), "_", mouse_y.ToString());
            ANDgate.Add(name);

            //add the stack element
            actionState.Push(ANDgate);

            //Draw
            drawOnPictureBox(Color.Red);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void aNDToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (ON_TRUTHING && ON_NETLIST) AddNewC("AND", ANDgate.Count, newCTotalNumberElements("AND"));
            else
            if (ON_TRUTHING) AddAND();
        }

        /// <summary>
        /// Function adds components to the relevant arraylist
        /// </summary>
        private void AddOR()
        {
            //similar to above - ORgate

            string name = "OR";
            int size = ORgate.Count;
            name = String.Concat(name, size.ToString());
            name = String.Concat(name, "_", mouse_x.ToString(), "_", mouse_y.ToString());
            ORgate.Add(name);

            //add the stack element
            actionState.Push(ORgate);

            //Draw
            drawOnPictureBox(Color.Red);
        }

        private void oRToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (ON_TRUTHING && ON_NETLIST) AddNewC("OR", ORgate.Count, newCTotalNumberElements("OR"));
            else
            if (ON_TRUTHING) AddOR();
        }

        /// <summary>
        /// Function adds components to the relevant arraylist
        /// </summary>
        private void AddNOR()
        {
            //similar to above - NORgate

            string name = "NOR";
            int size = NORgate.Count;
            name = String.Concat(name, size.ToString());
            name = String.Concat(name, "_", mouse_x.ToString(), "_", mouse_y.ToString());
            NORgate.Add(name);

            //add the stack element
            actionState.Push(NORgate);

            //Draw
            drawOnPictureBox(Color.Red);
        }

        private void nORToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (ON_TRUTHING && ON_NETLIST) AddNewC("NOR", NORgate.Count, newCTotalNumberElements("NOR"));
            else
            if (ON_TRUTHING) AddNOR();
        }

        /// <summary>
        /// Function adds components to the relevant arraylist
        /// </summary>
        private void AddTG()
        {
            //similar to above - Tgate

            string name = "TG";
            int size = Tgate.Count;
            name = String.Concat(name, size.ToString());
            name = String.Concat(name, "_", mouse_x.ToString(), "_", mouse_y.ToString());
            Tgate.Add(name);

            //add the stack element
            actionState.Push(Tgate);

            //Draw
            drawOnPictureBox(Color.Red);
        }


        private void tGATEToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (ON_TRUTHING && ON_NETLIST) AddNewC("TG", Tgate.Count, newCTotalNumberElements("TG"));
            else
            if (ON_TRUTHING) AddTG();
        }

        /// <summary>
        /// Function adds components to the relevant arraylist
        /// </summary>
        private void AddOC()
        {
            //similar to above - Other_c

            string name = "OC";
            int size = Other_c.Count;
            name = String.Concat(name, size.ToString());
            name = String.Concat(name, "_", mouse_x.ToString(), "_", mouse_y.ToString());
            Other_c.Add(name);

            //add the stack element
            actionState.Push(Other_c);

            //Draw
            drawOnPictureBox(Color.Red);
        }

        private void otherToolStripMenuItem2_Click(object sender, EventArgs e)
        {
            if (ON_TRUTHING && ON_NETLIST) AddNewC("OC", Other_c.Count, newCTotalNumberElements("OC"));
            else
            if (ON_TRUTHING) AddOC();
        }

        /// <summary>
        /// Function adds components to the relevant arraylist
        /// </summary>
        private void AddOBB()
        {
            //similar to above - Other_c

            string name = "OBB";
            int size = Other_bb.Count;
            name = String.Concat(name, size.ToString());
            name = String.Concat(name, "_", mouse_x.ToString(), "_", mouse_y.ToString());
            Other_bb.Add(name);

            //add the stack element
            actionState.Push(Other_bb);

            //Draw
            drawOnPictureBox(Color.Red);
        }

        private void otherToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (ON_TRUTHING && ON_NETLIST) AddNewC("OBB", Other_bb.Count, newCTotalNumberElements("OBB"));
            else
            if (ON_TRUTHING) AddOBB();
        }

        /// <summary>
        /// Function adds components to the relevant arraylist
        /// </summary>
        private void AddBuf()
        {
            //similar to above - Other_c

            string name = "Buf";
            int size = Buffer.Count;
            name = String.Concat(name, size.ToString());
            name = String.Concat(name, "_", mouse_x.ToString(), "_", mouse_y.ToString());
            Buffer.Add(name);

            //add the stack element
            actionState.Push(Buffer);

            //Draw
            drawOnPictureBox(Color.Red);
        }
        private void bufferToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (ON_TRUTHING && ON_NETLIST) AddNewC("Buf", Buffer.Count, newCTotalNumberElements("Buf"));
            else
            if (ON_TRUTHING) AddBuf();
        }
        /// <summary>
        /// Adds supply component
        /// </summary>
        private void AddSupply()
        {
            //similar to above - Supply

            string name = "VDD";
            int size = Supply.Count;
            name = String.Concat(name, size.ToString());
            name = String.Concat(name, "_", mouse_x.ToString(), "_", mouse_y.ToString());
            Supply.Add(name);

            //add the stack element
            actionState.Push(Supply);

            //Draw
            drawOnPictureBox(Color.Red);
        }
        /// <summary>
        /// Add ground component
        /// </summary>
        private void AddGround()
        {
            //similar to above - Supply

            string name = "GND";
            int size = Ground.Count;
            name = String.Concat(name, size.ToString());
            name = String.Concat(name, "_", mouse_x.ToString(), "_", mouse_y.ToString());
            Ground.Add(name);

            //add the stack element
            actionState.Push(Ground);

            //Draw
            drawOnPictureBox(Color.Red);
        }
        private void gNDToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (ON_TRUTHING && ON_NETLIST) AddNewC("GND", Ground.Count, newCTotalNumberElements("GND"));
            else
            AddGround();
        }

        private void supplyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (ON_TRUTHING && ON_NETLIST) AddNewC("VDD", Supply.Count, newCTotalNumberElements("VDD"));
            else
            AddSupply();
        }


        /// <summary>
        /// Undo a previously marked drawing 
        /// Takes a boolean argument that detemines if the element is to be deleted 
        /// from concerned arrayList
        /// </summary>
        /// <param name="deleteItem"></param>
        /// <returns></returns>
        private Boolean undoDrawing(Boolean deleteItem)
        {
            //Undo option
            //There is a stack called actionState that stores all the actions
            //actions here are elements that are identified - capacitors, etc
            //     and the circle is drawn around them
            //When an option is undo-ed the stack pops and the element is examined
            //That element has info on coordinates
            //erase graphics method is called

            ArrayList foo = new ArrayList();
            if (actionState.Count > 0)
            {
                foo = actionState.Pop();
                string sfoo = null;
                char[] delims = { '_' };
                string[] foob = null;
                if (foo.Equals(Transistors))
                {
                    sfoo = Transistors[Transistors.Count - 1].ToString();
                    foob = sfoo.Split(delims, StringSplitOptions.RemoveEmptyEntries);
                    eraseGraphics(int.Parse(foob[1]), int.Parse(foob[2]));
                    if(deleteItem) Transistors.RemoveAt(Transistors.Count - 1);
                    //MessageBox.Show("It was transistors");

                }
                else if (foo.Equals(Resistors))
                {
                    sfoo = Resistors[Resistors.Count - 1].ToString();
                    foob = sfoo.Split(delims, StringSplitOptions.RemoveEmptyEntries);
                    eraseGraphics(int.Parse(foob[1]), int.Parse(foob[2]));
                    if (deleteItem) Resistors.RemoveAt(Resistors.Count - 1);
                }
                else if (foo.Equals(Capacitors))
                {
                    sfoo = Capacitors[Capacitors.Count - 1].ToString();
                    foob = sfoo.Split(delims, StringSplitOptions.RemoveEmptyEntries);
                    eraseGraphics(int.Parse(foob[1]), int.Parse(foob[2]));
                    if (deleteItem) Capacitors.RemoveAt(Capacitors.Count - 1);
                }
                else if (foo.Equals(Inductors))
                {
                    sfoo = Inductors[Inductors.Count - 1].ToString();
                    foob = sfoo.Split(delims, StringSplitOptions.RemoveEmptyEntries);
                    eraseGraphics(int.Parse(foob[1]), int.Parse(foob[2]));
                    if (deleteItem) Inductors.RemoveAt(Inductors.Count - 1);
                }
                else if (foo.Equals(CS))
                {
                    sfoo = CS[CS.Count - 1].ToString();
                    foob = sfoo.Split(delims, StringSplitOptions.RemoveEmptyEntries);
                    eraseGraphics(int.Parse(foob[1]), int.Parse(foob[2]));
                    if (deleteItem) CS.RemoveAt(CS.Count - 1);
                }
                else if (foo.Equals(VS))
                {
                    sfoo = VS[VS.Count - 1].ToString();
                    foob = sfoo.Split(delims, StringSplitOptions.RemoveEmptyEntries);
                    eraseGraphics(int.Parse(foob[1]), int.Parse(foob[2]));
                    if (deleteItem) VS.RemoveAt(VS.Count - 1);
                }
                else if (foo.Equals(Diodes))
                {
                    sfoo = Diodes[Diodes.Count - 1].ToString();
                    foob = sfoo.Split(delims, StringSplitOptions.RemoveEmptyEntries);
                    eraseGraphics(int.Parse(foob[1]), int.Parse(foob[2]));
                    if (deleteItem) Diodes.RemoveAt(Diodes.Count - 1);
                }
                else if (foo.Equals(Nodes))
                {
                    sfoo = Nodes[Nodes.Count - 1].ToString();
                    foob = sfoo.Split(delims, StringSplitOptions.RemoveEmptyEntries);
                    eraseGraphics(int.Parse(foob[1]), int.Parse(foob[2]));
                    if (deleteItem) Nodes.RemoveAt(Nodes.Count - 1);
                }
               
                 else if (foo.Equals(Amplifier))
                {
                    sfoo = Amplifier[Amplifier.Count - 1].ToString();
                    foob = sfoo.Split(delims, StringSplitOptions.RemoveEmptyEntries);
                    eraseGraphics(int.Parse(foob[1]), int.Parse(foob[2]));
                    if (deleteItem) Amplifier.RemoveAt(Amplifier.Count - 1);
                }
                else if (foo.Equals(Attenuator))
                {
                    sfoo = Attenuator[Attenuator.Count - 1].ToString();
                    foob = sfoo.Split(delims, StringSplitOptions.RemoveEmptyEntries);
                    eraseGraphics(int.Parse(foob[1]), int.Parse(foob[2]));
                    if (deleteItem) Attenuator.RemoveAt(Attenuator.Count - 1);
                }
                else if (foo.Equals(Bias))
                {
                    sfoo = Bias[Bias.Count - 1].ToString();
                    foob = sfoo.Split(delims, StringSplitOptions.RemoveEmptyEntries);
                    eraseGraphics(int.Parse(foob[1]), int.Parse(foob[2]));
                    if (deleteItem) Bias.RemoveAt(Bias.Count - 1);
                }
                else if (foo.Equals(PDet))
                {
                    sfoo = PDet[PDet.Count - 1].ToString();
                    foob = sfoo.Split(delims, StringSplitOptions.RemoveEmptyEntries);
                    eraseGraphics(int.Parse(foob[1]), int.Parse(foob[2]));
                    if (deleteItem) PDet.RemoveAt(PDet.Count - 1);
                }
                else if (foo.Equals(Switch))
                {
                    sfoo = Switch[Switch.Count - 1].ToString();
                    foob = sfoo.Split(delims, StringSplitOptions.RemoveEmptyEntries);
                    eraseGraphics(int.Parse(foob[1]), int.Parse(foob[2]));
                    if (deleteItem) Switch.RemoveAt(Switch.Count - 1);
                }
                else if (foo.Equals(Other_c))
                {
                    sfoo = Other_c[Other_c.Count - 1].ToString();
                    foob = sfoo.Split(delims, StringSplitOptions.RemoveEmptyEntries);
                    eraseGraphics(int.Parse(foob[1]), int.Parse(foob[2]));
                    if (deleteItem) Other_c.RemoveAt(Other_c.Count - 1);
                }
                else if (foo.Equals(ANDgate))
                {
                    sfoo = ANDgate[ANDgate.Count - 1].ToString();
                    foob = sfoo.Split(delims, StringSplitOptions.RemoveEmptyEntries);
                    eraseGraphics(int.Parse(foob[1]), int.Parse(foob[2]));
                    if (deleteItem) ANDgate.RemoveAt(ANDgate.Count - 1);
                }
                else if (foo.Equals(Tgate))
                {
                    sfoo = Tgate[Tgate.Count - 1].ToString();
                    foob = sfoo.Split(delims, StringSplitOptions.RemoveEmptyEntries);
                    eraseGraphics(int.Parse(foob[1]), int.Parse(foob[2]));
                    if (deleteItem) Tgate.RemoveAt(Tgate.Count - 1);
                }
                else if (foo.Equals(ORgate))
                {
                    sfoo = ORgate[ORgate.Count - 1].ToString();
                    foob = sfoo.Split(delims, StringSplitOptions.RemoveEmptyEntries);
                    eraseGraphics(int.Parse(foob[1]), int.Parse(foob[2]));
                    if (deleteItem) ORgate.RemoveAt(ORgate.Count - 1);
                }
                else if (foo.Equals(NORgate))
                {
                    sfoo = NORgate[NORgate.Count - 1].ToString();
                    foob = sfoo.Split(delims, StringSplitOptions.RemoveEmptyEntries);
                    eraseGraphics(int.Parse(foob[1]), int.Parse(foob[2]));
                    if (deleteItem) NORgate.RemoveAt(NORgate.Count - 1);
                }
                else if (foo.Equals(Other_bb))
                {
                    sfoo = Other_bb[Other_bb.Count - 1].ToString();
                    foob = sfoo.Split(delims, StringSplitOptions.RemoveEmptyEntries);
                    eraseGraphics(int.Parse(foob[1]), int.Parse(foob[2]));
                    if (deleteItem) Other_bb.RemoveAt(Other_bb.Count - 1);
                }
                else if (foo.Equals(Buffer))
                {
                    sfoo = Buffer[Buffer.Count - 1].ToString();
                    foob = sfoo.Split(delims, StringSplitOptions.RemoveEmptyEntries);
                    eraseGraphics(int.Parse(foob[1]), int.Parse(foob[2]));
                    if (deleteItem) Buffer.RemoveAt(Buffer.Count - 1);
                }
                else if (foo.Equals(Supply))
                {
                    sfoo = Supply[Supply.Count - 1].ToString();
                    foob = sfoo.Split(delims, StringSplitOptions.RemoveEmptyEntries);
                    eraseGraphics(int.Parse(foob[1]), int.Parse(foob[2]));
                    if (deleteItem) Supply.RemoveAt(Supply.Count - 1);
                }
                else if (foo.Equals(Ground))
                {
                    sfoo = Ground[Ground.Count - 1].ToString();
                    foob = sfoo.Split(delims, StringSplitOptions.RemoveEmptyEntries);
                    eraseGraphics(int.Parse(foob[1]), int.Parse(foob[2]));
                    if (deleteItem) Ground.RemoveAt(Ground.Count - 1);
                }
                else if (foo.Equals(newC))
                {
                    sfoo = newC[newC.Count - 1].ToString();
                    foob = sfoo.Split(delims, StringSplitOptions.RemoveEmptyEntries);
                    eraseGraphics(int.Parse(foob[1]), int.Parse(foob[2]));
                    if (deleteItem) newC.RemoveAt(newC.Count - 1);
                }

            }
            else
            {
                //writeLog("Nothing to UNDO");
                return false;
            }
            return true;
        }

        /// <summary>
        /// Recursive function that clears the entire array without deleting storage elements
        /// which are arraylists
        /// </summary>
        private void clearMarkings()
        {
            while (undoDrawing(false)) undoDrawing(false);
        }
        /// <summary>
        /// Function to clear markings for a certain type of compoenent
        /// all the markings of the component represented by arrayList al is cleared
        /// the arrayList itself is untouched
        /// </summary>
        /// <param name="al"></param>
        private void clearMarkings(ArrayList al)
        {
            string sfoo = null;
            char[] delims = { '_' };
            string[] foob = null;
            for (int i = 0; i < al.Count; i++)
            {
                sfoo = al[i].ToString();
                foob = sfoo.Split(delims, StringSplitOptions.RemoveEmptyEntries);
                eraseGraphics(int.Parse(foob[1]), int.Parse(foob[2]));
            }

        }
        /// <summary>
        /// Clear the markings - use a boolean to indicate whether the elements 
        /// should be cleared as well
        /// </summary>
        /// <param name="b"></param>
        private void clearMarkings(Boolean b)
        {
            while (undoDrawing(b)) undoDrawing(b);
        }
        /// <summary>
        /// Undo click
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void undoLastToolStripMenuItem_Click(object sender, EventArgs e)
        {
            undoDrawing(true);
        }

        /// <summary>
        /// Cancel click - Does nothing
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void cancelToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //Do nothing cancel
        }

      /*  /// <summary>
        /// Clear images represented by stack
        /// Stack = Contains arraylist indices of the form [COMP]_[X]_[Y]
        /// Stack = stores the entire datagridview elements in above format
        /// If an element exists in stack, that element's graphics are erased
        /// </summary>
        /// <param name="a"></param>
        /// <param name="al"></param>
        private void clearImagesinStackDGV(Stack<int> a, ArrayList al)
        {
            //Clears images in a stack consisting of an arraylist indices
            //the arraylist elements are of the form standardly used in this tool
            //
            // = T0_x_y
            string[] foo = null;
            char[] delims = { '_' };
            for (int i = 0; i < al.Count; i++)
            {
                if (a.Contains(i))
                {
                    foo = al[i].ToString().Split(delims, StringSplitOptions.RemoveEmptyEntries);
                    eraseGraphics(int.Parse(foo[1]), int.Parse(foo[2]));
                }
                
            }
            a.Clear();

        }
        */
        /// <summary>
        /// Erase the graphics (circle) drawn at x,y
        /// </summary>
        /// <param name="xcood"></param>
        /// <param name="ycood"></param>
        private void eraseGraphics(int xcood, int ycood)
        {
            //To delete the marking, there needs to be some sort of hack
            //Because, we cannot use the graphics.clear method
            //What I am doing here is redrawing the same circle on the picture box with 
            //white color. The red marking is overwritten by white circle
            //the only problem here might be that sometimes the white circle might
            //visible as a break in the black lines/elements on schematics
            //for the time being, no roundabout way exists.

            mouse_x = xcood;
            mouse_y = ycood;
            drawOnPictureBox(Color.White);
        }

        /// <summary>
        /// Update the database entry 
        /// If UID already exists in the database, do nothing and return
        /// If UID does not exist, add new entry 
        /// </summary>
        /// <param name="UID"></param>
        /// <param name="PID"></param>
        /// <param name="FileName"></param>
        private void updateDatabase(string UID, string PID, string FileName)
        {
            //for test purposes
            //return;
            openConnection();
            //Function updates database entry
            //if there is duplicate entry, it removes it.

            if (m_dtPatents.Rows.Count != 0)
                m_rowPosition = m_dtPatents.Rows.Count - 1;
            else m_rowPosition = 0;
            DataRow m_rwPatent = m_dtPatents.NewRow();
           
            for (int i = 0; i < m_dtPatents.Rows.Count; i++)
            {
                m_rwPatent = m_dtPatents.Rows[i];
                if ((m_rwPatent["UID"].ToString()).Equals(UID))
                {

                    //m_dtPatents.Rows[i].Delete();
                    //writeLog(String.Concat("Duplicate row deleted with UID = ", UID, "\n"));
                    //m_daDataAdapter.Update(m_dtPatents);
                    writeLog(String.Concat("Duplicate row identified with UID = ", UID, "\nNot overwriting...\n"));
                    closeConnection();
                    m_dtPatents.Reset();
                    return;
                }

            }


            m_rwPatent = m_dtPatents.NewRow();
            m_rwPatent["UID"] = UID;
            m_rwPatent["PID"] = PID;
            m_rwPatent["FileLocation"] = FileName;
            m_dtPatents.Rows.Add(m_rwPatent);
            m_daDataAdapter.Update(m_dtPatents);
            writeLog(String.Concat("Updated database entry\n","Added Entry = ",UID,"(UID)\t",PID,"(PID)\n"));
            ///If you don't reset the datatable, there will be concurrency
            ///exceptions
            m_dtPatents.Reset();
            closeConnection();
        }
        /// <summary>
        /// Variation of standard updateDatabase Function
        /// For an UID in the database, add netlist
        /// </summary>
        /// <param name="UID"></param>
        /// <param name="Netlist"></param>
        private void updateDatabase(string UID,  string Netlist)
        {
            openConnection();
            //Function updates database entry
            DataRow m_rwPatent = m_dtPatents.NewRow();

            for (int i = 0; i < m_dtPatents.Rows.Count; i++)
            {
                m_rwPatent = m_dtPatents.Rows[i];
                if ((m_rwPatent["UID"].ToString()).Equals(UID))
                {

                    m_dtPatents.Rows[i]["NID"] = Netlist;
                    m_daDataAdapter.Update(m_dtPatents);

                    break;
                }

            }

            closeConnection();
            ///If you don't reset the datatable, there will be concurrency
            ///exceptions
            m_dtPatents.Reset();


        }

        private void moveImageDatabaseEnd()
        {
            openConnection();
            DataRow m_rwPatent = m_dtPatents.NewRow();
            m_rwPatent["UID"] = m_dtPatents.Rows[m_rowPosition]["UID"];
            m_rwPatent["PID"] = m_dtPatents.Rows[m_rowPosition]["PID"];
            m_rwPatent["NID"] = m_dtPatents.Rows[m_rowPosition]["NID"];
            m_rwPatent["FileLocation"] = m_dtPatents.Rows[m_rowPosition]["FileLocation"];
            m_dtPatents.Rows[m_rowPosition].Delete();
            m_dtPatents.Rows.Add(m_rwPatent);
            m_daDataAdapter.Update(m_dtPatents);
            closeConnection();
            m_dtPatents.Reset();
            cycleImagesFromDatabase();
        }
        /// <summary>
        /// Goes through the database and loads each image into the picturebox
        /// for which there is no netlist defined
        /// </summary>
        private void cycleImagesFromDatabase()
        {
            //Function goes through the database and 
            //whereever there is no netlist, loads that
            //image
            openConnection();
            //Function updates database entry
            DataRow m_rwPatent = m_dtPatents.NewRow();

            for (int i = 0; i < m_dtPatents.Rows.Count; i++)
            {
                if (i == 33) writeLog("test");
                m_rwPatent = m_dtPatents.Rows[i];
                writeLog("\n");
                writeLog(String.Concat(i.ToString(),":",m_rwPatent["UID"].ToString()));
                writeLog("\n");
                string[] foo = null;
                char[] delims = { '_',',','(' };
                foo = m_rwPatent["NID"].ToString().Split(delims, StringSplitOptions.RemoveEmptyEntries);
                ///Assumption that the netlist shoud have greater than
                ///5 words
                if (foo.Length < 5)
                {
                    rootImage = System.Drawing.Image.FromFile(m_rwPatent["FileLocation"].ToString());
                    setPictureBoxImage(rootImage);
                    //pictureBox1.Image = System.Drawing.Image.FromFile(m_rwPatent["FileLocation"].ToString());
                    //MessageBox.Show("Saved Successfully");
                    txtPno.Text = m_rwPatent["PID"].ToString(); 
                    txtUno.Text = m_rwPatent["UID"].ToString();
                    txtUrl.Text = "Not Available - Truthing Phase";
                    m_rowPosition = i;
                    break;
                }

            }
            writeLog(String.Concat("row pos = ", m_rowPosition.ToString(), "\n"));
            closeConnection();
            ///If you don't reset the datatable, there will be concurrency
            ///exceptions
            m_dtPatents.Reset();

        }

        /// <summary>
        /// Function skips an image in the database and loads the next one.
        /// </summary>
        private void skipImageInDatabase()
        {
            //Truthing reset
            clearAllArrays(); //has to call before turning off ON_TRUTHING
            resetAllArrays();
            ON_TRUTHING = false;
            startTruthingToolStripMenuItem.Text = "Start Truthing";

            //done reset
            int i = m_rowPosition + 1;
            m_rowPosition = i;
            openConnection();
            DataRow m_rwPatent = m_dtPatents.NewRow();
            m_rwPatent = m_dtPatents.Rows[i];
            writeLog(String.Concat("\nGoing to next image...", "\n"));
            string[] foo = null;
            char[] delims = { '_' };
            foo = m_rwPatent["NID"].ToString().Split(delims, StringSplitOptions.RemoveEmptyEntries);
            if (foo.Length < 5)
            {

                pictureBox1.Image = System.Drawing.Image.FromFile(m_rwPatent["FileLocation"].ToString());
                //MessageBox.Show("Saved Successfully");
                txtPno.Text = m_rwPatent["PID"].ToString();
                txtUno.Text = m_rwPatent["UID"].ToString();
                txtUrl.Text = "Not Available - Truthing Phase";

            }

            closeConnection();
            m_dtPatents.Reset();

        }

        /// <summary>
        /// Function goes back to the previous image in the database
        /// The image loaded is the previous one to the current image
        /// This happens only if netlist for prev image is not available.
        /// If not, does nothing
        /// </summary>
        private void prevImageInDatabase()
        {
            if (m_rowPosition == 0) return;
            int i = m_rowPosition - 1;
            m_rowPosition = i;
            openConnection();
            DataRow m_rwPatent = m_dtPatents.NewRow();
            m_rwPatent = m_dtPatents.Rows[i];
            writeLog(String.Concat("\nGoing to prev image...", "\n"));
            string[] foo = null;
            char[] delims = { '_' };
            foo = m_rwPatent["NID"].ToString().Split(delims, StringSplitOptions.RemoveEmptyEntries);
            if (foo.Length < 5)
            {

                pictureBox1.Image = System.Drawing.Image.FromFile(m_rwPatent["FileLocation"].ToString());
                //MessageBox.Show("Saved Successfully");
                txtPno.Text = m_rwPatent["PID"].ToString();
                txtUno.Text = m_rwPatent["UID"].ToString();
                txtUrl.Text = "Not Available - Truthing Phase";

            }
            else
            {
                writeLog(String.Concat("Cannot skip....", "\n"));
                m_rowPosition = m_rowPosition + 1;
            }

            closeConnection();
            m_dtPatents.Reset();
        }
        /// <summary>
        /// Function creates netlist
        /// Manipulate visibility of datagridview, log window, action buttons
        /// and ON_NETLIST flag
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void createNetlistToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (ON_TRUTHING || allCountZero() )
            {
                writeLog("Finish truthing first\n");
                if (allCountZero()) writeLog("No components have been identified\n");
                return;
            }
            if (!ON_NETLIST)
            {
                ON_NETLIST = true;
                //show dgv
                richTextBox1.Visible = false;
                dataGridView1.Visible = true;
                GenNL.Visible = true;
                viewNL.Visible = true;
                remarkC.Visible = true;
                logLabel.Text = "Netlist";
                createNetlistToolStripMenuItem.Text = "Finish Nelist";
               
                //populate dgv and store coordinates
                populateDGVElements();
                //clearMarkings();

            }
            else
            {
                /*richTextBox1.Visible = true;
                dataGridView1.Visible = false;
                GenNL.Visible = false;
                viewNL.Visible = false;
                remarkC.Visible = false;
                logLabel.Text = "LOG messages";
                ON_NETLIST = false;*/
                GenerateNLandAddDB();
                skipImageInDatabase();
                createNetlistToolStripMenuItem.Text = "Create Netlist";
            }
        }

        /// <summary>
        /// Returns if any elements have been identified
        /// </summary>
        /// <returns></returns>
        private Boolean allCountZero()
        {
            if (Transistors.Count == 0 && Resistors.Count == 0 && Capacitors.Count == 0 &&
                Inductors.Count == 0 && Nodes.Count == 0 && Diodes.Count == 0 && CS.Count == 0 &&
                VS.Count == 0 && Switch.Count == 0 && Other_c.Count == 0 && Buffer.Count == 0 &&
                Amplifier.Count == 0 && Attenuator.Count == 0 && Bias.Count == 0 && PDet.Count == 0 &&
                ANDgate.Count == 0 && ORgate.Count == 0 && NORgate.Count == 0 && Tgate.Count == 0 &&
                Other_bb.Count == 0 && Supply.Count == 0 && Ground.Count == 0 && newC.Count == 0)
                return true;
            else return false;
        }
        /// <summary>
        /// Returns the total number of elements identified
        /// </summary>
        /// <returns></returns>
        private int totalElementsNodes()
        {
            //returns total elements + nodes
            return (Transistors.Count + Resistors.Count + Capacitors.Count + Inductors.Count
                + Nodes.Count + Diodes.Count + CS.Count + VS.Count + Switch.Count + Other_c.Count +
                Buffer.Count + Amplifier.Count + Attenuator.Count + Bias.Count + PDet.Count + 
                ANDgate.Count + ORgate.Count + NORgate.Count + Tgate.Count + Other_bb.Count +
                Supply.Count + Ground.Count + newC.Count);
        }

        /// <summary>
        /// Populate the datagridview1 with all elements
        /// </summary>
        private void populateDGVElements()
        {
            //function populates DGV elements
            //goes over each array of elements and populates the dgv
            //stores the coordinates of the elements in hashtable
            dataGridView1.RowCount = totalElementsNodes();
            string st = null;
            string[] foo = null;
            char[] delims = { '_' };
            int count = 0;
            int i = 0;
            int j = 0;
            for (i = 0; i < Transistors.Count; i++)
            {
                st = Transistors[j].ToString();
                foo = st.Split(delims, StringSplitOptions.RemoveEmptyEntries);
                dataGridView1.Rows[i].Cells[0].Value = foo[0];
                if (!dgvCoordinates.ContainsKey(foo[0]))
                {
                    dgvCoordinates.Add(foo[0],String.Concat(foo[0],"_",foo[1],"_",foo[2]));
                    allDGVElements.Add(String.Concat(foo[0], "_", foo[1], "_", foo[2]));
                }
                j++;
                
            }
            count = i;
            j = 0;
            for (i = count; i < Resistors.Count + count; i++)
            {
                st = Resistors[j].ToString();
                foo = st.Split(delims, StringSplitOptions.RemoveEmptyEntries);
                dataGridView1.Rows[i].Cells[0].Value = foo[0];
                if (!dgvCoordinates.ContainsKey(foo[0]))
                {
                    dgvCoordinates.Add(foo[0],String.Concat(foo[0],"_",foo[1],"_",foo[2]));
                    allDGVElements.Add(String.Concat(foo[0], "_", foo[1], "_", foo[2]));
                }
                j++;
            }
            count = i ;
            j = 0;
            for (i = count; i < Capacitors.Count + count; i++)
            {
                st = Capacitors[j].ToString();
                foo = st.Split(delims, StringSplitOptions.RemoveEmptyEntries);
                dataGridView1.Rows[i].Cells[0].Value = foo[0];
                if (!dgvCoordinates.ContainsKey(foo[0]))
                {
                    dgvCoordinates.Add(foo[0], String.Concat(foo[0],"_", foo[1], "_", foo[2]));
                    allDGVElements.Add(String.Concat(foo[0], "_", foo[1], "_", foo[2]));
                }
                j++;
            }
            count = i;
            j = 0;
            for (i = count; i < Inductors.Count + count; i++)
            {
                st = Inductors[j].ToString();
                foo = st.Split(delims, StringSplitOptions.RemoveEmptyEntries);
                dataGridView1.Rows[i].Cells[0].Value = foo[0];
                if (!dgvCoordinates.ContainsKey(foo[0]))
                {
                    dgvCoordinates.Add(foo[0], String.Concat(foo[0],"_", foo[1], "_", foo[2]));
                    allDGVElements.Add(String.Concat(foo[0], "_", foo[1], "_", foo[2]));
                }
                j++;

            }
            
            count = i;
            j = 0;
            for (i = count; i < Diodes.Count + count; i++)
            {
                st = Diodes[j].ToString();
                foo = st.Split(delims, StringSplitOptions.RemoveEmptyEntries);
                dataGridView1.Rows[i].Cells[0].Value = foo[0];
                if (!dgvCoordinates.ContainsKey(foo[0]))
                {
                    dgvCoordinates.Add(foo[0], String.Concat(foo[0], "_",foo[1], "_", foo[2]));
                    allDGVElements.Add(String.Concat(foo[0], "_", foo[1], "_", foo[2]));
                }
                j++;
            }
            count = i ;
            j = 0;
            for (i = count; i < CS.Count + count; i++)
            {
                st = CS[j].ToString();
                foo = st.Split(delims, StringSplitOptions.RemoveEmptyEntries);
                dataGridView1.Rows[i].Cells[0].Value = foo[0];
                if (!dgvCoordinates.ContainsKey(foo[0]))
                {
                    dgvCoordinates.Add(foo[0], String.Concat(foo[0], "_",foo[1], "_", foo[2]));
                    allDGVElements.Add(String.Concat(foo[0], "_", foo[1], "_", foo[2]));
                }
                j++;
            }
            count = i;
            j = 0;
            for (i = count; i < VS.Count + count; i++)
            {
                st = VS[j].ToString();
                foo = st.Split(delims, StringSplitOptions.RemoveEmptyEntries);
                dataGridView1.Rows[i].Cells[0].Value = foo[0];
                if (!dgvCoordinates.ContainsKey(foo[0]))
                {
                    dgvCoordinates.Add(foo[0], String.Concat(foo[0],"_", foo[1], "_", foo[2]));
                    allDGVElements.Add(String.Concat(foo[0], "_", foo[1], "_", foo[2]));
                }
                j++;

            }
            count = i;
            j = 0;
            for (i = count; i < Amplifier.Count + count; i++)
            {
                st = Amplifier[j].ToString();
                foo = st.Split(delims, StringSplitOptions.RemoveEmptyEntries);
                dataGridView1.Rows[i].Cells[0].Value = foo[0];
                if (!dgvCoordinates.ContainsKey(foo[0]))
                {
                    dgvCoordinates.Add(foo[0], String.Concat(foo[0], "_", foo[1], "_", foo[2]));
                    allDGVElements.Add(String.Concat(foo[0], "_", foo[1], "_", foo[2]));
                }
                j++;

            }
            count = i;
            j = 0;
            for (i = count; i < Attenuator.Count + count; i++)
            {
                st = Attenuator[j].ToString();
                foo = st.Split(delims, StringSplitOptions.RemoveEmptyEntries);
                dataGridView1.Rows[i].Cells[0].Value = foo[0];
                if (!dgvCoordinates.ContainsKey(foo[0]))
                {
                    dgvCoordinates.Add(foo[0], String.Concat(foo[0], "_", foo[1], "_", foo[2]));
                    allDGVElements.Add(String.Concat(foo[0], "_", foo[1], "_", foo[2]));
                }
                j++;

            }
            count = i;
            j = 0;
            for (i = count; i < Bias.Count + count; i++)
            {
                st = Bias[j].ToString();
                foo = st.Split(delims, StringSplitOptions.RemoveEmptyEntries);
                dataGridView1.Rows[i].Cells[0].Value = foo[0];
                if (!dgvCoordinates.ContainsKey(foo[0]))
                {
                    dgvCoordinates.Add(foo[0], String.Concat(foo[0], "_", foo[1], "_", foo[2]));
                    allDGVElements.Add(String.Concat(foo[0], "_", foo[1], "_", foo[2]));
                }
                j++;

            }
            count = i;
            j = 0;
            for (i = count; i < PDet.Count + count; i++)
            {
                st = PDet[j].ToString();
                foo = st.Split(delims, StringSplitOptions.RemoveEmptyEntries);
                dataGridView1.Rows[i].Cells[0].Value = foo[0];
                if (!dgvCoordinates.ContainsKey(foo[0]))
                {
                    dgvCoordinates.Add(foo[0], String.Concat(foo[0], "_", foo[1], "_", foo[2]));
                    allDGVElements.Add(String.Concat(foo[0], "_", foo[1], "_", foo[2]));
                }
                j++;

            }
            count = i;
            j = 0;
            for (i = count; i < Switch.Count + count; i++)
            {
                st = Switch[j].ToString();
                foo = st.Split(delims, StringSplitOptions.RemoveEmptyEntries);
                dataGridView1.Rows[i].Cells[0].Value = foo[0];
                if (!dgvCoordinates.ContainsKey(foo[0]))
                {
                    dgvCoordinates.Add(foo[0], String.Concat(foo[0], "_", foo[1], "_", foo[2]));
                    allDGVElements.Add(String.Concat(foo[0], "_", foo[1], "_", foo[2]));
                }
                j++;

            }
            count = i;
            j = 0;
            for (i = count; i < Other_c.Count + count; i++)
            {
                st = Other_c[j].ToString();
                foo = st.Split(delims, StringSplitOptions.RemoveEmptyEntries);
                dataGridView1.Rows[i].Cells[0].Value = foo[0];
                if (!dgvCoordinates.ContainsKey(foo[0]))
                {
                    dgvCoordinates.Add(foo[0], String.Concat(foo[0], "_", foo[1], "_", foo[2]));
                    allDGVElements.Add(String.Concat(foo[0], "_", foo[1], "_", foo[2]));
                }
                j++;

            }
            count = i;
            j = 0;
            for (i = count; i < ANDgate.Count + count; i++)
            {
                st = ANDgate[j].ToString();
                foo = st.Split(delims, StringSplitOptions.RemoveEmptyEntries);
                dataGridView1.Rows[i].Cells[0].Value = foo[0];
                if (!dgvCoordinates.ContainsKey(foo[0]))
                {
                    dgvCoordinates.Add(foo[0], String.Concat(foo[0], "_", foo[1], "_", foo[2]));
                    allDGVElements.Add(String.Concat(foo[0], "_", foo[1], "_", foo[2]));
                }
                j++;

            }
            count = i;
            j = 0;
            for (i = count; i < Tgate.Count + count; i++)
            {
                st = Tgate[j].ToString();
                foo = st.Split(delims, StringSplitOptions.RemoveEmptyEntries);
                dataGridView1.Rows[i].Cells[0].Value = foo[0];
                if (!dgvCoordinates.ContainsKey(foo[0]))
                {
                    dgvCoordinates.Add(foo[0], String.Concat(foo[0], "_", foo[1], "_", foo[2]));
                    allDGVElements.Add(String.Concat(foo[0], "_", foo[1], "_", foo[2]));
                }
                j++;

            }
            count = i;
            j = 0;
            for (i = count; i < ORgate.Count + count; i++)
            {
                st = ORgate[j].ToString();
                foo = st.Split(delims, StringSplitOptions.RemoveEmptyEntries);
                dataGridView1.Rows[i].Cells[0].Value = foo[0];
                if (!dgvCoordinates.ContainsKey(foo[0]))
                {
                    dgvCoordinates.Add(foo[0], String.Concat(foo[0], "_", foo[1], "_", foo[2]));
                    allDGVElements.Add(String.Concat(foo[0], "_", foo[1], "_", foo[2]));
                }
                j++;

            }
            count = i;
            j = 0;
            for (i = count; i < NORgate.Count + count; i++)
            {
                st = NORgate[j].ToString();
                foo = st.Split(delims, StringSplitOptions.RemoveEmptyEntries);
                dataGridView1.Rows[i].Cells[0].Value = foo[0];
                if (!dgvCoordinates.ContainsKey(foo[0]))
                {
                    dgvCoordinates.Add(foo[0], String.Concat(foo[0], "_", foo[1], "_", foo[2]));
                    allDGVElements.Add(String.Concat(foo[0], "_", foo[1], "_", foo[2]));
                }
                j++;

            }
            count = i;
            j = 0;
            for (i = count; i < Other_bb.Count + count; i++)
            {
                st = Other_bb[j].ToString();
                foo = st.Split(delims, StringSplitOptions.RemoveEmptyEntries);
                dataGridView1.Rows[i].Cells[0].Value = foo[0];
                if (!dgvCoordinates.ContainsKey(foo[0]))
                {
                    dgvCoordinates.Add(foo[0], String.Concat(foo[0], "_", foo[1], "_", foo[2]));
                    allDGVElements.Add(String.Concat(foo[0], "_", foo[1], "_", foo[2]));
                }
                j++;

            }
            count = i;
            j = 0;
            for (i = count; i < Buffer.Count + count; i++)
            {
                st = Buffer[j].ToString();
                foo = st.Split(delims, StringSplitOptions.RemoveEmptyEntries);
                dataGridView1.Rows[i].Cells[0].Value = foo[0];
                if (!dgvCoordinates.ContainsKey(foo[0]))
                {
                    dgvCoordinates.Add(foo[0], String.Concat(foo[0], "_", foo[1], "_", foo[2]));
                    allDGVElements.Add(String.Concat(foo[0], "_", foo[1], "_", foo[2]));
                }
                j++;

            }
            count = i;
            j = 0;
            for (i = count; i < Supply.Count + count; i++)
            {
                st = Supply[j].ToString();
                foo = st.Split(delims, StringSplitOptions.RemoveEmptyEntries);
                dataGridView1.Rows[i].Cells[0].Value = foo[0];
                if (!dgvCoordinates.ContainsKey(foo[0]))
                {
                    dgvCoordinates.Add(foo[0], String.Concat(foo[0], "_", foo[1], "_", foo[2]));
                    allDGVElements.Add(String.Concat(foo[0], "_", foo[1], "_", foo[2]));
                }
                j++;

            }
            count = i;
            j = 0;
            for (i = count; i < Ground.Count + count; i++)
            {
                st = Ground[j].ToString();
                foo = st.Split(delims, StringSplitOptions.RemoveEmptyEntries);
                dataGridView1.Rows[i].Cells[0].Value = foo[0];
                if (!dgvCoordinates.ContainsKey(foo[0]))
                {
                    dgvCoordinates.Add(foo[0], String.Concat(foo[0], "_", foo[1], "_", foo[2]));
                    allDGVElements.Add(String.Concat(foo[0], "_", foo[1], "_", foo[2]));
                }
                j++;

            }
            count = i;
            j = 0;
            for (i = count; i < newC.Count + count; i++)
            {
                st = newC[j].ToString();
                foo = st.Split(delims, StringSplitOptions.RemoveEmptyEntries);
                dataGridView1.Rows[i].Cells[0].Value = foo[0];
                if (!dgvCoordinates.ContainsKey(foo[0]))
                {
                    dgvCoordinates.Add(foo[0], String.Concat(foo[0], "_", foo[1], "_", foo[2]));
                    allDGVElements.Add(String.Concat(foo[0], "_", foo[1], "_", foo[2]));
                }
                j++;

            }
            count = i;
            j = 0;
            for (i = count; i < Nodes.Count + count; i++)
            {
                st = Nodes[j].ToString();
                foo = st.Split(delims, StringSplitOptions.RemoveEmptyEntries);
                dataGridView1.Rows[i].Cells[0].Value = foo[0];
                if (!dgvCoordinates.ContainsKey(foo[0]))
                {
                    dgvCoordinates.Add(foo[0], String.Concat(foo[0], "_", foo[1], "_", foo[2]));
                    allDGVElements.Add(String.Concat(foo[0], "_", foo[1], "_", foo[2]));
                }
                j++;

            }
           

        }


        private void dataGridView1_RowEnter(object sender, DataGridViewCellEventArgs e)
        {
            
            
        }

        private void dataGridView1_RowLeave(object sender, DataGridViewCellEventArgs e)
        {
           /* if(dataGridView1.RowCount > 0)
            hightlightElementBasedOnDGV(Color.White);*/
        }

        /// <summary>
        /// Highlight elements in the selected datagridview row - use color x
        /// </summary>
        /// <param name="x"></param>
        private void hightlightElementBasedOnDGV(Color x)
        {
            //preq is datagrid view is already selected.
            //function should be called only from datagridview events
            string name = dataGridView1.SelectedRows[0].Cells[0].Value.ToString();
            string[] foo = null;
            char[] delims = { '_' };
            string foob = null;
            if (dgvCoordinates.ContainsKey(name))
                foob = dgvCoordinates[name].ToString();
            else return;

            foo = foob.Split(delims, StringSplitOptions.RemoveEmptyEntries);
            mouse_x = int.Parse(foo[1]);
            mouse_y = int.Parse(foo[2]);
            drawOnPictureBox(x);
        }
        /// <summary>
        /// Highlights currently selected datagridview element in schematic
        /// </summary>
        /// <param name="index"></param>
        /// <param name="x"></param>
        private void hightlightElementBasedOnDGV(int index, Color x)
        {
            string name = dataGridView1.Rows[index].Cells[0].Value.ToString();
            string[] foo = null;
            char[] delims = { '_' };
            string foob = null;
            if (dgvCoordinates.ContainsKey(name))
                foob = dgvCoordinates[name].ToString();
            else return;

            foo = foob.Split(delims, StringSplitOptions.RemoveEmptyEntries);
            mouse_x = int.Parse(foo[1]);
            mouse_y = int.Parse(foo[2]);
            drawOnPictureBox(x);
        }

        

        /// <summary>
        /// Scratch function
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void runTestToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //scratch function
            //Scratch pad
            //for testing functions
            //1. testing database access using ADO.net
            DataRow m_rwPatent = m_dtPatents.Rows[0];
            if (m_dtPatents.Rows.Count == 0)
            {
                writeLog("Rows empty in database\n");
                return;
            }
            string st = m_dtPatents.Rows[m_rowPosition]["UID"].ToString();
            string st2 = m_dtPatents.Rows[m_rowPosition]["PID"].ToString();
            writeLog(String.Concat("Unique ID: ", st, "\n"));
            writeLog(String.Concat("Patent ID: ", st2, "\n"));
        }

        /// <summary>
        /// Clear all the elements and markings
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void clearAllToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //scratch function
            clearMarkings(true);

        }
        private void loadTestImage()
        {
            //scratch function
            pictureBox1.Image = System.Drawing.Image.FromFile("C:\\Workspace\\218268513445.jpg");
            ON_TRUTHING = true;
        }
        /// <summary>
        /// Scratch function - test
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void loadTestImageToolStripMenuItem_Click(object sender, EventArgs e)
        {
            pictureBox1.Focus();
            //Clear the array and marking if any
            clearAllArrays();
            resetAllArrays();
            dgvReset();
            ON_TRUTHING = true;
            startTruthingToolStripMenuItem.Text = "Done Truthing";
            writeLog("Truthing begin...\n");
            writeLog("Click on the image to identify components - Select Actions > Done Truthing when done");
            //scratch function
            loadTestImage();
        }

        /// <summary>
        /// enables option where the elements can be selected by double clicking the schematic
        /// this is valid during netlisting (after truthing is done)
        /// </summary>
        private void pickFromSchematic()
        {
            PICK_SCHEMATIC = true;
            
        }
        /// <summary>
        /// Tool strip click for picking from schematic during netlisting
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void pickFromSchematicToolStripMenuItem_Click(object sender, EventArgs e)
        {
            pickFromSchematic();
        }

        /// <summary>
        /// Function returns the mean square error between the x,y and x1,y1
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="x1"></param>
        /// <param name="y1"></param>
        /// <returns></returns>
        private double returnMSECoordinates(int x, int y, int x1, int y1)
        {
            //returns mean square error distance
            return Math.Pow((Math.Pow(x - x1, 2) + Math.Pow(y - y1, 2)), 0.5);
        }

        /// <summary>
        /// Function highlights the element that has been previously registered
        /// and which is closest to the point that was recently clicked
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        private string markClosestClick(int a, int b)
        {
            //Function takes two arguments a and b
            //a and b are mouse click coordinates.
            //the functionn highlights an element in the schematic that is closest to (a,b)
            //uses mean sq error as metric

            //returns name of the element

            int x = 0;
            int y = 0;
            char[] delims = { '_' };
            string[] foo = null;
            int closestIndex = 0;
            double preverror = 100000.0;
            double error = 0.0;
            for (int i = 0; i < allDGVElements.Count; i++)
            {
                foo = allDGVElements[i].ToString().Split(delims, StringSplitOptions.RemoveEmptyEntries);
                x = int.Parse(foo[1]);
                y = int.Parse(foo[2]);
                error = returnMSECoordinates(x, y, a, b);
                if (error < preverror)
                {
                    preverror = error;
                    closestIndex = i;
                }
            }
            //Draw a green circle there at closest index
            foo = allDGVElements[closestIndex].ToString().Split(delims, StringSplitOptions.RemoveEmptyEntries);
            //put it in stack
            //actionIndices.Push(closestIndex);
            drawOnPictureBox(Color.MediumOrchid, int.Parse(foo[1]), int.Parse(foo[2]));
            return foo[0];

        }

        /// <summary>
        /// Function marks the closest components around a mouse coordinate a,b
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="radius"></param>
        /// <param name="col"></param>
        private void markClosestComponents(int a, int b, int radius, Color col)
        {
            //function marks all the closest components defined by radius
            int x = 0;
            int y = 0;
            char[] delims = { '_' };
            string[] foo = null;
            double error = 0.0;
            double targetError = returnMSECoordinates(a, b, a + radius, b + radius);
            for (int i = 0; i < allDGVElements.Count; i++)
            {
                foo = allDGVElements[i].ToString().Split(delims, StringSplitOptions.RemoveEmptyEntries);
                x = int.Parse(foo[1]);
                y = int.Parse(foo[2]);
                error = returnMSECoordinates(x, y, a, b);
                if (error <= targetError)
                {
                    drawOnPictureBox(col, int.Parse(foo[1]), int.Parse(foo[2]));
                }
            }

        }

     
        /// <summary>
        /// Function returns the closest component name to the mouse coordinates (a,b)
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        private string closestComponent(int a, int b)
        {
            ///Same function as above
            ///without the drawing option
            int x = a;
            int y = b;
            char[] delims = { '_' };
            string[] foo = null;
            int closestIndex = 0;
            double preverror = 100000.0;
            double error = 0.0;
            for (int i = 0; i < allDGVElements.Count; i++)
            {
                foo = allDGVElements[i].ToString().Split(delims, StringSplitOptions.RemoveEmptyEntries);
                x = int.Parse(foo[1]);
                y = int.Parse(foo[2]);
                error = returnMSECoordinates(x, y, a, b);
                if (error < preverror)
                {
                    preverror = error;
                    closestIndex = i;
                }
            }
            
            foo = allDGVElements[closestIndex].ToString().Split(delims, StringSplitOptions.RemoveEmptyEntries);
            return foo[0];
        }

        /// <summary>
        /// Event on double click on picutrebox
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void pictureBox1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            //When the picture box is double clicked and we are in pick_schematic mode
            //the elements should be highlighted and the datagridview cell must be 
            //automatically filled.
            
            if (PICK_SCHEMATIC)
            {
                string st = markClosestClick(e.X, e.Y);
                dataGridView1.SelectedCells[0].Value = st;
                PICK_SCHEMATIC = false;
            }
        }

        /// <summary>
        /// Mouseclick on datagridview
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void dataGridView1_MouseUp(object sender, MouseEventArgs e)
        {
            //Makes right click as the selected cell
            if (e.Button == MouseButtons.Right)
            {
                DataGridView.HitTestInfo hti = dataGridView1.HitTest(e.X, e.Y);
                if (hti.Type == DataGridViewHitTestType.Cell)
                {
                    dataGridView1.ClearSelection();
                }
                if (hti.RowIndex >= 0 && hti.ColumnIndex >= 0)
                    dataGridView1.Rows[hti.RowIndex].Cells[hti.ColumnIndex].Selected = true;
            }
        }

        /// <summary>
        /// Generate netlist - When boolean forDB is true, generates
        /// netlist is a single string that can be stored in the database
        /// </summary>
        /// <param name="forDB"></param>
        /// <returns></returns>
        private string generateNL(Boolean forDB)
        {
            string st = null;
            if (!forDB)
            {
                for (int i = 0; i < dataGridView1.RowCount; i++)
                {
                    st = String.Concat(st, dataGridView1.Rows[i].Cells[0].Value.ToString(), "\t(");
                    if (dataGridView1.Rows[i].Cells[1].Value != null)
                        st = String.Concat(st, dataGridView1.Rows[i].Cells[1].Value.ToString(), "\t");
                    if (dataGridView1.Rows[i].Cells[2].Value != null)
                        st = String.Concat(st, dataGridView1.Rows[i].Cells[2].Value.ToString(), "\t");
                    if (dataGridView1.Rows[i].Cells[3].Value != null)
                        st = String.Concat(st, dataGridView1.Rows[i].Cells[3].Value.ToString(), "\t");
                    st = String.Concat(st, ")\n");

                }
            }
            else
            {
                for (int i = 0; i < dataGridView1.RowCount; i++)
                {
                    st = String.Concat(st, dataGridView1.Rows[i].Cells[0].Value.ToString(), "(");
                    if (dataGridView1.Rows[i].Cells[1].Value != null)
                        st = String.Concat(st, dataGridView1.Rows[i].Cells[1].Value.ToString(), ",");
                    if (dataGridView1.Rows[i].Cells[2].Value != null)
                        st = String.Concat(st, dataGridView1.Rows[i].Cells[2].Value.ToString(), ",");
                    if (dataGridView1.Rows[i].Cells[3].Value != null)
                        st = String.Concat(st, dataGridView1.Rows[i].Cells[3].Value.ToString(), ",");
                    st = String.Concat(st, ")-");

                }
            }
            return st;
        }

       /// <summary>
       /// View the netlist in the log window
       /// </summary>
       /// <param name="sender"></param>
       /// <param name="e"></param>
        private void viewNL_Click(object sender, EventArgs e)
        {
            if (viewNL.Text.Equals("View NL"))
            {
                viewNL.Text = "Go back";
                dataGridView1.Visible = false;
                richTextBox1.Visible = true;
                string st = generateNL(false);
                writeLog(String.Concat("Netlist is -->\n", st));
            }
            else
            {
                viewNL.Text = "View NL";
                dataGridView1.Visible = true;
                richTextBox1.Visible = false;
            }
        }

        /// <summary>
        /// Generates netlist for the database and updates the database
        /// </summary>
        private void GenerateNLandAddDB()
        {
            //Adds the netlist in database
            updateDatabase(txtUno.Text, generateNL(true));
            writeLog("Added netlist to database");
            writeLog("\n");
            dataGridView1.RowCount = 0;
            ON_NETLIST = false;
            ON_TRUTHING = false;
            viewNL.Visible = false;
            GenNL.Visible = false;
            remarkC.Visible = false;
            dataGridView1.Visible = false;
            richTextBox1.Visible = true;
        }

        /// <summary>
        /// Tool strip click to generate netlist and add to database
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void GenNL_Click(object sender, EventArgs e)
        {
            //Adds the netlist in database
           GenerateNLandAddDB();
           createNetlistToolStripMenuItem.Text = "Create Netlist";
           clearMarkings(true);
           cycleImagesFromDatabase();
        }
        /// <summary>
        /// Rotates the picture box image 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void rotateClockwiseToolStripMenuItem_Click(object sender, EventArgs e)
        {
            
            pictureBox1.Image.RotateFlip(RotateFlipType.Rotate90FlipNone);
            pictureBox1.Refresh();
        }

        /// <summary>
        /// Rotates the picture box image 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>

        private void rotateCClockwiseToolStripMenuItem_Click(object sender, EventArgs e)
        {
            pictureBox1.Image.RotateFlip(RotateFlipType.Rotate270FlipNone);
            pictureBox1.Refresh();
        }

        /// <summary>
        /// Rotates the picture box image 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void flipXToolStripMenuItem_Click(object sender, EventArgs e)
        {
            pictureBox1.Image.RotateFlip(RotateFlipType.RotateNoneFlipX);
            pictureBox1.Refresh();
        }

        /// <summary>
        /// Rotates the picture box image 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void filpYToolStripMenuItem_Click(object sender, EventArgs e)
        {
            pictureBox1.Image.RotateFlip(RotateFlipType.RotateNoneFlipY);
            pictureBox1.Refresh();
        }

        /// <summary>
        /// Toolstrip entry for skipping image 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void skipImageToolStripMenuItem_Click(object sender, EventArgs e)
        {
            skipImageInDatabase();

        }
        /// <summary>
        /// Toolstrip entry for going to previous image
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void prevImageToolStripMenuItem_Click(object sender, EventArgs e)
        {
            prevImageInDatabase();
        }

        /// <summary>
        /// Function redraws all the components that are in datagridview
        /// using color c
        /// </summary>
        /// <param name="c"></param>
        private void remarkComponents(Color c)
        {
            int x = 0;
            int y = 0;
            char[] delims = { '_' };
            string[] foo = null;
            
            for (int i = 0; i < allDGVElements.Count; i++)
            {
                foo = allDGVElements[i].ToString().Split(delims, StringSplitOptions.RemoveEmptyEntries);
                x = int.Parse(foo[1]);
                y = int.Parse(foo[2]);
                drawOnPictureBox(c, int.Parse(foo[1]), int.Parse(foo[2]));
            }
        }

        /// <summary>
        /// Clears all the drawings without deleting the elements from storage points
        /// </summary>
        private void clearAllArrays()
        {
            
            clearMarkings(Transistors);
            clearMarkings(Resistors);
            clearMarkings(Diodes);
            clearMarkings(Nodes);
            clearMarkings(Inductors);
            clearMarkings(Capacitors);
            clearMarkings(Switch);
            clearMarkings(Buffer);
            clearMarkings(Other_bb);
            clearMarkings(Other_c);
            clearMarkings(ANDgate);
            clearMarkings(ORgate);
            clearMarkings(NORgate);
            clearMarkings(Amplifier);
            clearMarkings(Attenuator);
            clearMarkings(CS);
            clearMarkings(VS);
            clearMarkings(Bias);
            clearMarkings(PDet);
            clearMarkings(Tgate);
            clearMarkings(Supply);
            clearMarkings(Ground);
            clearMarkings(newC);
        }
        /// <summary>
        /// reset all arrays. clear components
        /// </summary>
        private void resetAllArrays()
        {
            Transistors.Clear();
            Resistors.Clear();
            Diodes.Clear();
            Nodes.Clear();
            Inductors.Clear();
            Capacitors.Clear();
            Switch.Clear();
            Buffer.Clear();
            Other_bb.Clear();
            Other_c.Clear();
            ANDgate.Clear();
            ORgate.Clear();
            NORgate.Clear();
            Amplifier.Clear();
            Attenuator.Clear();
            CS.Clear();
            VS.Clear();
            Bias.Clear();
            PDet.Clear();
            Tgate.Clear();
            Supply.Clear();
            Ground.Clear();
            actionState.Clear();
            newC.Clear();
            //actionIndices.Clear();
            allDGVElements.Clear();
            dgvCoordinates.Clear();
        }
        /// <summary>
        /// Button click entry for remarking/unmarking components
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void remarkC_Click(object sender, EventArgs e)
        {
            if (remarkC.Text.Equals("Re-mark Components"))
            {
                ON_TRUTHING = true;
                pictureBox1.Focus();
                remarkC.Text = "Un-mark Components";
                remarkComponents(Color.Red);

            }
            else
            {
                ON_TRUTHING = false;
                remarkC.Text = "Re-mark Components";
                pictureBox1.Focus();
                clearAllArrays();
                populateDGVElements();
            }
        }

        private void pictureBox1_MouseMove(object sender, MouseEventArgs e)
        {
            
            LocalMousePosition = TranslateZoomMousePosition(pictureBox1.PointToClient(Cursor.Position), 
                                pictureBox1.Image, pictureBox1.Width, pictureBox1.Height);
            toolStripStatusLabel1.Text = String.Concat("(", LocalMousePosition.X.ToString(), ",",
                                        LocalMousePosition.Y.ToString(), ")");
            ///for storing mouse coordinates for drag
            if (DRAG)
            {
                pointArrayForMouseDreag.Add(LocalMousePosition);
                

            }
            ///end storing mouse coordinates for drag
            
            if (ON_NETLIST)
            {
                toolStripStatusLabel2.Text = String.Concat("Closest component = ", closestComponent(LocalMousePosition.X,
                                                            LocalMousePosition.Y));
            }

        }

        private void Form1_KeyPress(object sender, KeyPressEventArgs e)
        {
            
           
        }

        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            if (pictureBox1.Focused && ON_TRUTHING)
            {
                if (e.Control && e.KeyCode == Keys.A)
                {
                    mouse_x = LocalMousePosition.X;
                    mouse_y = LocalMousePosition.Y;
                    AddAmplifier();
                    toolStripStatusLabel2.Text = "Amplifier added";
                }
                if (e.Control && e.KeyCode == Keys.Z)
                {
                    mouse_x = LocalMousePosition.X;
                    mouse_y = LocalMousePosition.Y;
                    undoDrawing(true);
                    toolStripStatusLabel2.Text = "Undone";
                }
                if (e.Control && e.KeyCode == Keys.B)
                {
                    mouse_x = LocalMousePosition.X;
                    mouse_y = LocalMousePosition.Y;
                    AddBias();
                    toolStripStatusLabel2.Text = "Bias added";
                }
                if (e.Control && e.KeyCode == Keys.P)
                {
                    mouse_x = LocalMousePosition.X;
                    mouse_y = LocalMousePosition.Y;
                    AddPDET();
                    toolStripStatusLabel2.Text = "PDet added";
                }
                if (e.Control && e.KeyCode == Keys.T)
                {
                    mouse_x = LocalMousePosition.X;
                    mouse_y = LocalMousePosition.Y;
                    AddAttenuator();
                    toolStripStatusLabel2.Text = "Attenuator added";
                }
                if (e.Control && e.KeyCode == Keys.O)
                {
                    mouse_x = LocalMousePosition.X;
                    mouse_y = LocalMousePosition.Y;
                    AddOBB();
                    toolStripStatusLabel2.Text = "Misc Block added";
                }
                if (e.Control && e.KeyCode == Keys.D1)
                {
                    mouse_x = LocalMousePosition.X;
                    mouse_y = LocalMousePosition.Y;
                    AddAND();
                    toolStripStatusLabel2.Text = "AND gate added";
                }
                if (e.Control && e.KeyCode == Keys.D2)
                {
                    mouse_x = LocalMousePosition.X;
                    mouse_y = LocalMousePosition.Y;
                    AddOR();
                    toolStripStatusLabel2.Text = "OR gate added";
                }
                if (e.Control && e.KeyCode == Keys.D3)
                {
                    mouse_x = LocalMousePosition.X;
                    mouse_y = LocalMousePosition.Y;
                    AddNOR();
                    toolStripStatusLabel2.Text = "NOR gate added";
                }
                if (e.Control && e.KeyCode == Keys.D4)
                {
                    mouse_x = LocalMousePosition.X;
                    mouse_y = LocalMousePosition.Y;
                    AddTG();
                    toolStripStatusLabel2.Text = "T gate added";
                }
                if (e.Shift && e.Control && e.KeyCode == Keys.X)
                {
                    if (MessageBox.Show("Clear all and start over?", "Confirm", MessageBoxButtons.YesNo) == DialogResult.Yes)
                        clearMarkings(true);
                }
                if (!e.Control)
                {
                    switch (e.KeyCode)
                    {
                        case Keys.N:
                            {
                                mouse_x = LocalMousePosition.X;
                                mouse_y = LocalMousePosition.Y;
                                AddNode();
                                toolStripStatusLabel2.Text = "Node added";
                                break;
                            }
                        case Keys.T:
                            {
                                mouse_x = LocalMousePosition.X;
                                mouse_y = LocalMousePosition.Y;
                                AddTransistor();
                                toolStripStatusLabel2.Text = "Transistor added";
                                break;
                            }
                        case Keys.R:
                            {
                                mouse_x = LocalMousePosition.X;
                                mouse_y = LocalMousePosition.Y;
                                AddResistor();
                                toolStripStatusLabel2.Text = "Resistor added";
                                break;
                            }
                        case Keys.C:
                            {
                                mouse_x = LocalMousePosition.X;
                                mouse_y = LocalMousePosition.Y;
                                AddCapacitor();
                                toolStripStatusLabel2.Text = "Capacitor added";
                                break;
                            }
                        case Keys.D:
                            {
                                mouse_x = LocalMousePosition.X;
                                mouse_y = LocalMousePosition.Y;
                                AddDiode();
                                toolStripStatusLabel2.Text = "Diode added";
                                break;
                            }
                        case Keys.L:
                            {
                                mouse_x = LocalMousePosition.X;
                                mouse_y = LocalMousePosition.Y;
                                AddInductor();
                                toolStripStatusLabel2.Text = "Inductor added";
                                break;
                            }
                        case Keys.B:
                            {
                                mouse_x = LocalMousePosition.X;
                                mouse_y = LocalMousePosition.Y;
                                AddBuf();
                                toolStripStatusLabel2.Text = "Buffer added";
                                break;
                            }
                        case Keys.S:
                            {
                                mouse_x = LocalMousePosition.X;
                                mouse_y = LocalMousePosition.Y;
                                AddSupply();
                                toolStripStatusLabel2.Text = "Supply added";
                                break;
                            }
                        case Keys.G:
                            {
                                mouse_x = LocalMousePosition.X;
                                mouse_y = LocalMousePosition.Y;
                                AddGround();
                                toolStripStatusLabel2.Text = "Ground added";
                                break;
                            }

                        case Keys.W:
                            {

                                mouse_x = LocalMousePosition.X;
                                mouse_y = LocalMousePosition.Y;
                                AddSW();
                                toolStripStatusLabel2.Text = "Switch added";
                                break;
                            }
                        case Keys.I:
                            {

                                mouse_x = LocalMousePosition.X;
                                mouse_y = LocalMousePosition.Y;
                                AddCS();
                                toolStripStatusLabel2.Text = "Current source added";
                                break;
                            }
                        case Keys.V:
                            {

                                mouse_x = LocalMousePosition.X;
                                mouse_y = LocalMousePosition.Y;
                                AddVS();
                                toolStripStatusLabel2.Text = "Voltage source added";
                                break;
                            }
                        case Keys.O:
                            {

                                mouse_x = LocalMousePosition.X;
                                mouse_y = LocalMousePosition.Y;
                                AddOC();
                                toolStripStatusLabel2.Text = "Misc componenent added";
                                break;
                            }

                        default: break;
                    }
                }
            }
            if (dataGridView1.Visible && ON_NETLIST && !ON_TRUTHING)
            {
                if (!e.Control)
                {
                    switch (e.KeyCode)
                    {
                        case Keys.A:
                            {
                                string st = markClosestClick(LocalMousePosition.X, LocalMousePosition.Y);
                                dataGridView1.SelectedCells[0].Value = st;
                                if (TOGGLE_AUTO_CELL_MOVE)
                                {
                                    int index = dataGridView1.SelectedCells[0].ColumnIndex;
                                    if (index < DGV_COLS)
                                    {
                                        dataGridView1.Rows[dgvCurrentRowIndex].Cells[index + 1].Selected = true;
                                    }
                                    else
                                    {
                                        dgvCurrentRowIndex++;
                                        dataGridView1.Rows[dgvCurrentRowIndex].Cells[1].Selected = true;
                                    }
                                    dgvCellEnterCustom();
                                }
                                break;
                            }
                        case Keys.S:
                            {
                                int index = dataGridView1.SelectedCells[0].ColumnIndex;
                                if (index < DGV_COLS)
                                {
                                    dataGridView1.Rows[dgvCurrentRowIndex].Cells[index + 1].Selected = true;
                                    
                                }
                                else
                                {
                                    if(dgvCurrentRowIndex < (dataGridView1.RowCount - 1))
                                     dgvCurrentRowIndex++;
                                    dataGridView1.Rows[dgvCurrentRowIndex].Cells[1].Selected = true;
                                    
                                }
                                
                                dgvCellEnterCustom();
                                break;
                            }

                        case Keys.C:
                            {
                                if (HIGHLIGHT_ALL_CLOSE)
                                {
                                    markClosestComponents(LocalMousePosition.X, LocalMousePosition.Y, 50, Color.Aquamarine);
                                }
                                break;
                            }
                        case Keys.D:
                            {
                                if (HIGHLIGHT_ALL_CLOSE)
                                {
                                    HIGHLIGHT_ALL_CLOSE = false;
                                    toolStripStatusLabel2.Text = "Highlight close components option disabled";
                                }
                                else
                                {
                                    HIGHLIGHT_ALL_CLOSE = true;
                                    toolStripStatusLabel2.Text = "Highlight close components option enabled";
                                }

                                break;
                            }


                        default: break;
                    }
                }
                if (e.Control)
                {
                    switch (e.KeyCode)
                    {
                        case Keys.S:
                            {
                                if (TOGGLE_AUTO_CELL_MOVE)
                                {
                                    TOGGLE_AUTO_CELL_MOVE = false;
                                    toolStripStatusLabel2.Text = "Auto cell move turned off";
                                }
                                else
                                {
                                    TOGGLE_AUTO_CELL_MOVE = true;
                                    toolStripStatusLabel2.Text = "Auto cell move turned on";
                                }
                                break;
                            }
                        default: break;
                    }
                }


            }

        }

        private void commandsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            CommandHelp cs = new CommandHelp();
            cs.Show();

        }

        private void dataGridView1_MouseClick(object sender, MouseEventArgs e)
        {
            DataGridView.HitTestInfo hti = dataGridView1.HitTest(e.X, e.Y);
           if (hti.RowIndex >= 0)
            {
                if (hti.RowIndex != dgvCurrentRowIndex)
                {
                    //drawOnPictureBox(Color.White);
                    clearAllArrays();
                    setPictureBoxImage(rootImage);
                }
            }
            if (hti.RowIndex >= 0 && hti.ColumnIndex >= 0)
            {
                if (hti.Type == DataGridViewHitTestType.Cell)
                {
                    dataGridView1.ClearSelection();
                }
                dataGridView1.Rows[hti.RowIndex].Cells[hti.ColumnIndex].Selected = true;
                dgvCurrentRowIndex = hti.RowIndex;
                dgvCellEnterCustom();
               // hightlightElementBasedOnDGV(hti.RowIndex, Color.OrangeRed);
               // pictureBox1.Focus();
            }
        }

        private void moveImageToEndToolStripMenuItem_Click(object sender, EventArgs e)
        {
            moveImageDatabaseEnd();
            ON_TRUTHING = false;
            startTruthingToolStripMenuItem.Text = "Start Truthing";
            richTextBox1.Visible = true;
            dataGridView1.Visible = false;
            viewNL.Visible = false;
            GenNL.Visible = false;
            remarkC.Visible = false;
        }

        private void dgvCellEnterCustom()
        {
            //int rowIndex = dataGridView1.SelectedCells[0].RowIndex;
            int colIndex = dataGridView1.SelectedCells[0].ColumnIndex;
            if (dgvPrevRowIndex != dgvCurrentRowIndex)
            {
                clearAllArrays();
                setPictureBoxImage(rootImage);
                dgvPrevRowIndex = dgvCurrentRowIndex;
                //clearImagesinStackDGV(actionIndices, allDGVElements);
            }
            //dataGridView1.ClearSelection();
            //dataGridView1.Rows[rowIndex].Cells[colIndex].Selected = true;
            hightlightElementBasedOnDGV(dgvCurrentRowIndex, Color.OrangeRed);
            
            pictureBox1.Focus();
        }

        /// <summary>
        /// Function taken from CodeProject.
        /// for a zoom mode picturebox, this code translates, the corresponding mouse coordinates
        /// to actual mouse coordinates on the screen.
        /// </summary>
        /// <param name="coordinates"></param>
        /// <param name="img"></param>
        /// <param name="W"></param>
        /// <param name="H"></param>
        /// <returns></returns>
        protected Point TranslateZoomMousePosition(Point coordinates, Image img, int W, int H)
        {
            // test to make sure our image is not null
            if (img == null) return coordinates;
            // Make sure our control width and height are not 0 and our 
            // image width and height are not 0
            if (W == 0 || H == 0 || img.Width == 0 || img.Height == 0) return coordinates;
            // This is the one that gets a little tricky. Essentially, need to check 
            // the aspect ratio of the image to the aspect ratio of the control
            // to determine how it is being rendered
            float imageAspect = (float)img.Width / img.Height;
            float controlAspect = (float)W / H;
            float newX = coordinates.X;
            float newY = coordinates.Y;
            if (imageAspect > controlAspect)
            {
                // This means that we are limited by width, 
                // meaning the image fills up the entire control from left to right
                float ratioWidth = (float)img.Width / W;
                newX *= ratioWidth;
                float scale = (float)W / img.Width;
                float displayHeight = scale * img.Height;
                float diffHeight = H - displayHeight;
                diffHeight /= 2;
                newY -= diffHeight;
                newY /= scale;
            }
            else
            {
                // This means that we are limited by height, 
                // meaning the image fills up the entire control from top to bottom
                float ratioHeight = (float)img.Height / H;
                newY *= ratioHeight;
                float scale = (float)H / img.Height;
                float displayWidth = scale * img.Width;
                float diffWidth = W - displayWidth;
                diffWidth /= 2;
                newX -= diffWidth;
                newX /= scale;
            }
            return new Point((int)newX, (int)newY);
        }

        private void deleteRowToolStripMenuItem_Click(object sender, EventArgs e)
        {
            dataGridView1.Rows.RemoveAt(dataGridView1.SelectedCells[0].RowIndex);
        }

        private void dataTestToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DataDownloader dd = new DataDownloader();
            dd.Show();
        }

        /// <summary>
        /// Scratch function
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void identifyTransToolStripMenuItem_Click(object sender, EventArgs e)
        {
            makeImageBitmap();
        }

        /// <summary>
        /// Scratch function - makes image bitmap
        /// </summary>
        private void makeImageBitmap()
        {
            Graphics G;

            Image original = pictureBox1.Image;
            //check if the image is indexed or non indexed
            //if indexed, we need to create a bitmap version of the image before
            //starting to modify it.

            switch (original.PixelFormat)
            {
                case System.Drawing.Imaging.PixelFormat.Undefined:
                case System.Drawing.Imaging.PixelFormat.Format1bppIndexed:
                case System.Drawing.Imaging.PixelFormat.Format4bppIndexed:
                case System.Drawing.Imaging.PixelFormat.Format8bppIndexed:
                case System.Drawing.Imaging.PixelFormat.Format16bppGrayScale:
                case System.Drawing.Imaging.PixelFormat.Format16bppArgb1555:

                    // Create a new BitMap object using original Image instance
                    original = new Bitmap(original);
                    break;
            }

            //now form the graphics element from the non-indexed version of the
            //image
            G = Graphics.FromImage(original);

            
            pictureBox1.Image = original;

            G.Dispose();
        }

        private void createMask()
        {

        }
        private void createMaskToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!DRAG)
            {
                DRAG = true;
                pointArrayForMouseDreag.Clear();
            }

        }

        private void computeAndSetDragCoordinates(ArrayList a)
        {
            List<int> xvals = new List<int>();
            List<int> yvals = new List<int>();

            for (int i = 0; i < a.Count; i++)
            {
                xvals.Add(((Point)a[i]).X);
                yvals.Add(((Point)a[i]).Y);
            }
            xvals.Sort();
            yvals.Sort();
            drag_wid = xvals[xvals.Count - 1] - xvals[0];
            drag_len = yvals[yvals.Count - 1] - yvals[0];
            writeLog(String.Concat("Width = ", drag_wid.ToString(), " Len = ", drag_len.ToString(), "\n"));

        }

        private void logFunctionToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //computeAndSetDragCoordinates(pointArrayForMouseDreag);
            bool done = false;

            int width = pictureBox1.Image.Width;
            int height = pictureBox1.Image.Height;
            Bitmap bitmap = new Bitmap(pictureBox1.Image, new Size(width, height));
            double[,] bnew = new double[width, height];
            //Loop to read the data from the Bitmap image into the double array
            int i, j;
            for (i = 0; i < width; i++)
            {
                for (j = 0; j < height; j++)
                {
                    
                    Color pixelColor = bitmap.GetPixel(i, j);
                   // string b = pixelColor.ToKnownColor().ToString(); //the Brightness component
                    string b = bitmap.GetPixel(i, j).Name;
                    if (b.Equals("ffffffff"))
                    {
                        writeLog("0");
                    }
                    else
                    {
                        writeLog("1");
                    }
                    //writeLog(String.Concat(b, "\t[", i.ToString(), ",", j.ToString(), "]\n"));
                }
                writeLog("\n");
            }

            
            //int x_old = 0;
            //int y_old = 0;
            //for (int y = y_old; y < y_old - drag_len; y++)
            //{
            //    for (int x = x_old; x < x_old + drag_wid; x++)
            //    {

            //    }
            //}
        }



    }
}
