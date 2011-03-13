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
    public partial class DataDownloader : Form
    {
        OleDbConnection m_cnADONETConnection = new OleDbConnection();
        OleDbDataAdapter m_daDataAdapter = new OleDbDataAdapter();
        DataTable m_dtPatents = new DataTable();
        int m_rowPosition = 0;

        string MasterLink = "http://www.google.com/patents?lr=&q=uspclass:%22$CLASS$%22&num=100&sa=N&start=$STARTINDEX$&scoring=1";
        ArrayList urls = new ArrayList(); //search url
        ArrayList subUrls = new ArrayList();
        string CURRENT_TXT = null;

        /// <summary>
        /// Open the ADO.net database connection
        /// Define dataAdpater and COmmand builder
        /// </summary>
        private void openConnection()
        {
            m_dtPatents.Clear();
            m_cnADONETConnection.ConnectionString = String.Concat(@"Provider=Microsoft.Jet.OLEDB.4.0;Data Source=", textBox3.Text);
            String cmd = String.Concat("Select * From ", textBox2.Text); ;
            m_daDataAdapter = new OleDbDataAdapter(cmd, m_cnADONETConnection);

            OleDbCommandBuilder m_cbCommandBuilder = new OleDbCommandBuilder(m_daDataAdapter);
            m_daDataAdapter.Fill(m_dtPatents);
        }

        public DataDownloader()
        {
            InitializeComponent();
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


        /// <summary>
        /// Simple function to print string to the log window (richtextbox1)
        /// </summary>
        /// <param name="message"></param>
        private void writeLog(string message)
        {
            richTextBox1.AppendText(message);
            richTextBox1.ScrollToCaret();

        }
       

        private void loadCLASSSUBCLASSFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //Open the text file that contains patent class subclass
            //this function goes through that text file and
            //generates search links for all the patents belonging to class subclass
            string foo;
            ArrayList cid = new ArrayList();
            ArrayList initLinks = new ArrayList();
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                System.IO.StreamReader sr = new System.IO.StreamReader(openFileDialog1.FileName);
                while (sr.Peek() >= 0)
                {
                    string str = sr.ReadLine();
                    foo = string.Join(null,System.Text.RegularExpressions.Regex.Split(str,"[^\\d+(\\d{3})*]"));
                    //writeLog(string.Join(null,System.Text.RegularExpressions.Regex.Split(str,"[^\\d+(,\\d{3})*]")));
                    if (!foo.Equals(txtClass.Text))
                        foo = foo.Replace(txtClass.Text, String.Concat(txtClass.Text, "/"));
                    if (foo.Contains("+"))
                        foo = foo.Remove(foo.IndexOf('+'), 1);
                    
                    if ((foo.Length >= txtClass.Text.Length) && foo.Contains(txtClass.Text) && !cid.Contains(foo) && foo.Substring(0,txtClass.Text.Length).Equals(txtClass.Text))
                    {
                        writeLog(foo);
                        writeLog("\n");
                        cid.Add(foo);
                    }
                }

            }
            int STARTINDEX = 0;
            for (int i = 0; i < cid.Count; i++)
            {
                foo = MasterLink.Replace("$STARTINDEX$", STARTINDEX.ToString());
                foo = foo.Replace("$CLASS$", cid[i].ToString());
                urls.Add(foo); //urls contains search strings or address
                writeLog(foo);
                writeLog("\n");
            }
            for (int i = 0; i < urls.Count; i++)
            {
                LoadSubURLS(urls, i);
            }
            URLarrayToFile(subUrls);

        }
        /// <summary>
        /// Function loads all the subURLs for the ArrayList s[index]
        /// If there is a search string http://somesearchhere.. which is a google search result
        /// this function stores the entire search result returned urls
        /// For instance http://www.google.com/patents?lr=&scoring=1&q=uspclass:%22330%22&num=100&sa=N&start=100
        /// For the above url, all the search results are stored/appended in the subURL array
        /// </summary>
        /// <param name="s"></param>
        /// <param name="index"></param>
        private void LoadSubURLS(ArrayList s, int index)
        {
            
            
            //return the sub url for searching the addresss s[index]
            //get the html of the search string and count the links
            string localURL = s[index].ToString(); ///this is the url with which we start with
            bool done = false; ///Loop variable. done only when all search results are over
            int loop = 1; ///Loop variable to increment page count on google
            int count = 0;
            while (!done)
            {
                System.Threading.Thread.Sleep(20000); ///15 second force sleep
                downloadData(localURL); ///Download the search->localURL html
                Regex r = new Regex("http://([\\w+?\\.\\w+])+([a-zA-Z0-9\\~\\!\\@\\#\\$\\%\\^\\&amp;\\*\\(\\)_\\-\\=\\+\\\\\\/\\?\\.\\:\\;\\'\\,]*)?", RegexOptions.IgnoreCase);
                string str;
                System.Text.ASCIIEncoding enc = new System.Text.ASCIIEncoding();
                str = enc.GetString(downloadedData);

                // get all the matches depending upon the regular expression
                MatchCollection mcl = r.Matches(str);
                ArrayList a = new ArrayList();
                

                foreach (Match ml in mcl)
                {
                    foreach (Group g in ml.Groups)
                    {
                        string b = g.Value + "";
                        // Add the extracted urls to the array list
                        ///Lot of irrelevant links exist in the html
                        ///we need only the patent info. Hence filter the links to make
                        ///sure they correspond to patent url
                        if (b.Contains("http://") && b.Contains("google.com/patents/") && b.Contains("uspclass"))
                        {
                            writeLog(b);
                            subUrls.Add(b);
                            writeLog(String.Concat("\n--", (count + 1).ToString(), "--\n"));
                            count++; ///counter keeps track of the patent urls found here. if
                                     ///this moves to 100, it means there is a next page in
                                     ///most probablity
                        }

                    }
                }


                //step 1 - replace the localURL link with the next page
                ///the start=0 is replaced by start=100 or 200 or so forth
                int index1 = localURL.IndexOf("&start=");
                int index2 = localURL.IndexOf("&scor");
                string temp = localURL.Substring(index1, index2 - index1);
                string temp2 = String.Concat(temp.Substring(0, temp.IndexOf('=') + 1), (100 * loop).ToString());
                localURL = localURL.Replace(temp, temp2);
                writeLog(String.Concat("\n\nStart Index Changed ---Loop =",loop.ToString(),"\n"));
                writeLog(localURL);
                writeLog("\n");
                ///If the count reached maximum, keep going.
                ///Else we have hit all we can for that patent
                if (count == 100)
                {
                    done = false;
                    count = 0;
                    loop ++;
                }
                else
                {
                    done = true;
                   
                }
            }

        }
        /// <summary>
        /// Take all the urls in the file and store it into array
        /// </summary>
        /// <param name="al"></param>
        private void URLarrayToFile(ArrayList al)
        {
            Stream myStream;
            saveFileDialog1 = new SaveFileDialog();
            saveFileDialog1.Filter = "Text file|*.txt|All file (*.*)|*.*";
            saveFileDialog1.Title = "Save all the sub URLs";
            saveFileDialog1.FilterIndex = 2;
            saveFileDialog1.RestoreDirectory = true;
            
            // If the file name is not an empty string open it for saving.
            if (saveFileDialog1.ShowDialog() == DialogResult.OK)
            {
                if ((myStream = saveFileDialog1.OpenFile()) != null)
                {
                    StreamWriter wText = new StreamWriter(myStream);
                    for (int i = 0; i < al.Count; i++)
                    {
                        wText.WriteLine(al[i].ToString());         
                    }
                    
                    myStream.Close();
                } 

            }
        }

        private void extractImageLinksFromURLFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            subUrls.Clear();
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                System.IO.StreamReader sr = new System.IO.StreamReader(openFileDialog1.FileName);
                while (sr.Peek() >= 0)
                {
                    string str = sr.ReadLine();
                    if (str.Contains("http://") && str.Contains("google.com/patents"))
                        subUrls.Add(str);

                }
                writeLog("File load complete...\n");
            }
            axWebBrowser1.Navigate(subUrls[0].ToString());
        }

        private void button1_Click(object sender, EventArgs e)
        {
            extractImages();
        }


        private void extractImages()
        {
            ArrayList subUrlImageAbstract = new ArrayList();
            ArrayList subUrlImageDrawing = new ArrayList();
            Random random = new Random();
            //int count = 0;
            ///From file - populate the suburl array
           
            ///For each suburl, extract html and find the first instance of books.google.com
            ///
            Regex r = new Regex("http://bks([0-9].books.google.com)+([a-zA-Z0-9\\~\\!\\@\\#\\$\\%\\^\\&amp;\\*\\(\\)_\\-\\=\\+\\\\\\/\\?\\.\\:\\;\\'\\,]*)?", RegexOptions.IgnoreCase);
            Regex r2 = new Regex("&printsec=drawing&zoom=([0-9])");
            string foo;
            string LocalURL = null;
            int maxzoom = 1;
            System.Text.ASCIIEncoding enc = new System.Text.ASCIIEncoding();
            //length control loop 
            //0-2 over
            //2-500
            //300-500
            for (int i = 300; i < 500; i++)
            {

                axWebBrowser1.Navigate(subUrls[i].ToString());
                downloadData(subUrls[i].ToString());

                //this suburl can also be used to generate patent field info

                foo = enc.GetString(downloadedData);
                //writeLog("---------------\n");
                //writeLog(foo);
                //writeLog("---------------\n");
                // get all the matches depending upon the regular expression
                MatchCollection mcl = r.Matches(foo);
                foreach (Match ml in mcl)
                {
                    foreach (Group g in ml.Groups)
                    {
                        string b = g.Value + "";
                        // Add the extracted urls to the array list
                        ///Lot of irrelevant links exist in the html
                        ///we need only the patent info. Hence filter the links to make
                        ///sure they correspond to patent url
                        if (b.Contains("http://bks") && b.Contains("books.google.com/patents") && b.Contains("&printsec=abstract"))
                        {
                            LocalURL = b;
                            writeLog(String.Concat("abst = ", b, "\n"));
                        }


                    }
                }
                mcl = r2.Matches(foo);
                foreach (Match ml in mcl)
                {
                    foreach (Group g in ml.Groups)
                    {
                        string b = g.Value + "";
                        b = b.Substring(b.LastIndexOf('=') + 1);
                        if (int.Parse(b) > maxzoom)
                            maxzoom = int.Parse(b);
                    }
                }
                writeLog(String.Concat("Maximum zoom = ", maxzoom.ToString(), "\n"));
                string temp = LocalURL.Replace("&zoom=1&", String.Concat("&zoom=", maxzoom.ToString(), "&"));
                subUrlImageAbstract.Add(temp);
                writeLog("---------------\n");
                string iHtml = infoFromHtml();
                string path = textBox1.Text;
                if (iHtml != null)
                {
                    path = string.Concat(path, iHtml, "_abstract.jpg");
                    //save it to file
                    System.Threading.Thread.Sleep(random.Next(30000, 40000));
                    downloadData(temp);
                    BinaryWriter bw = new BinaryWriter(File.Open(path, FileMode.Create));
                    bw.Write(downloadedData);

                }
                writeLog(path);
                writeLog("\n");


                writeLog(String.Concat(temp, "\n"));
                temp = temp.Replace("&printsec=abstract", "&printsec=drawing");
                writeLog(String.Concat(temp, "\n"));
                writeLog("---------------\n");

                path = textBox1.Text;
                if (iHtml != null)
                {
                    path = string.Concat(path, iHtml, "_drawing.jpg");
                    //save it to file
                    System.Threading.Thread.Sleep(random.Next(30000, 40000));
                    downloadData(temp);
                    BinaryWriter bw = new BinaryWriter(File.Open(path, FileMode.Create));
                    bw.Write(downloadedData);
                    string tempbar = iHtml.Replace('x', '/');
                    tempbar = tempbar.Replace('_', ',');
                    updateDatabase(uID(temp), tempbar, path);
                }
                writeLog(path);
                writeLog("\n");
                path = textBox1.Text;
                TextWriter tw = new StreamWriter(string.Concat(path, iHtml, ".txt"));
                tw.WriteLine(CURRENT_TXT);
                tw.Close();

                subUrlImageDrawing.Add(temp);
                int rdm = random.Next(35000, 40000);
                writeLog(String.Concat("Waiting for ", (rdm / 1000).ToString(), " sec\n"));
                System.Threading.Thread.Sleep(rdm);


            }
        }

        private string infoFromHtml()
        {
            const string HTML_TAG_PATTERN = "<.*?>"; //for regex
            int foo, foob;
            string ap = "Patent number : ";
            if (downloadedData != null && downloadedData.Length != 0)
            {
                string s = Encoding.ASCII.GetString(downloadedData);
                //Strip the HTML tags <, *, ? etc
                s = Regex.Replace(s, HTML_TAG_PATTERN, " ");
                writeLog(s);
                CURRENT_TXT = s;
                //Extract the patent number which is between Patent number field and 
                //Filing date field
                foo = s.IndexOf("Patent number");
                if (foo == -1)
                {
                    foo = s.IndexOf("Application number");
                    ap = "Application number : ";
                }
                foob = s.IndexOf("Publication number");
                if (foob == -1)
                {
                    foob = s.IndexOf("Filing date");
                    
                }
                string s2 = s.Substring(foo+ap.Length, foob-foo-ap.Length-1);
                s2 = s2.Replace('/', 'x');
                s2 = s2.Replace(',', '_');

                //foo = s.IndexOf("Patent number : ")+15;
                //foob = s.IndexOf("Filing date");
                //return (String.Concat("Patent number: ",s.Substring(foo,foob-foo)));

                return s2;

                //MessageBox.Show(s);
            }
            return null;
        }

        private string uID(string urlname)
        {
            //patent ID is given by &ci=()&
            //& begin and & end
            //This patent ID is the unique ID that google generates
            //This is not the patent number
            //this is used for the file name
            int startIndex = urlname.IndexOf("?id=");
            int endIndex = urlname.IndexOf("&printsec=");
            return (urlname.Substring(startIndex + 4, endIndex - (startIndex + 4)));
        }

        

        /// <summary>
        /// Close the database connection
        /// </summary>
        private void closeConnection()
        {
            m_cnADONETConnection.Close();
        }


        private void DataDownloader_Load(object sender, EventArgs e)
        {
            openConnection();
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
            writeLog(String.Concat("Updated database entry\n", "Added Entry = ", UID, "(UID)\t", PID, "(PID)\n"));
            ///If you don't reset the datatable, there will be concurrency
            ///exceptions
            m_dtPatents.Reset();
            closeConnection();
        }

       
        /*
        private string findPatentNumber(string urlname)
        {

            //Patent number will be obtained from the document
            //the image url is just for the link
            //From the image url, we can get the patent home page when we
            //chop the link to until the &pg= field

            //string patentUrl = urlname.Substring(0, urlname.IndexOf("&dq="));
            const string HTML_TAG_PATTERN = "<.*?>"; //for regex
            int foo, foob;

            //clear the byte array
            //we want to fetch the HTML now and not the image
            //downloadedData.Initialize();
            //MessageBox.Show(patentUrl);
            //the patent URL points to teh patent homepage, meaning data will be html
            //downloadData(patentUrl);
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
        */
    }
}
