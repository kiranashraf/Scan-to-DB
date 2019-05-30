using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using MathWorks.MATLAB.NET.Utility;
using MathWorks.MATLAB.NET.Arrays;
using PlotDemoComp;
using System.Drawing.Drawing2D;
using abc;
using MySql.Data.MySqlClient;
//using testocr;
using test;
using System.Drawing.Imaging;
using System.Threading;
using System.Text.RegularExpressions;
[assembly: NativeGC(true, GCBlockSize = 25)]  // Set native memory management block size to 25 MB.

namespace testocr
{
    public partial class Form1 : Form
    {
        Image image;
        public bool Makeselection = false;
        int count = 0;
        int cropX;
        int cropY;
        int cropWidth;
        private Size OriginalImageSize;
        int cropHeight;
        string[] data = new string[12];
        //int oCropX;
        //int oCropY;
        public Pen cropPen;
        string address;
        string[] words;
        string temp;
        Bitmap _img;
        public DashStyle cropDashStyle = DashStyle.DashDot;
        #region MAIN
        public Form1()
        {
            InitializeComponent();
        }
        //[STAThread]
    
       

        private void tabControl1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (tabControl1.SelectedTab == tabPage2)
            {
                List<string> devices = WIAScanner.GetDevices();

                foreach (string device in devices)
                {
                    lbDevices.Items.Add(device);
                }

                if (lbDevices.Items.Count == 0)
                {
                    MessageBox.Show("You do not have any WIA devices.");
                    tabControl1.SelectedTab = tabPage1;
                }
                else
                {
                    lbDevices.SelectedIndex = 0;
                }

            }
            else if (tabControl1.SelectedTab == tabPage4)
            {
                if (image == null)
                {
                    MessageBox.Show("Please Choose Image First");
                    tabControl1.SelectedTab = tabPage1;
                }
                else
                {
                    reset();
                    LoadImage();
                }
            }
            else if (tabControl1.SelectedTab == tabPage5)
            {

                address = data[7]+ " " + data[8];
                string[] words = address.Split(',');
                //MessageBox.Show(address + " " + words.Length);
                madd1.Text = "";
                madd2.Text = "";
                madd3.Text = "";
                madd4.Text = "";
                for (int i = 0; i < words.Length; i++)
                {
                    if (i < 4)
                        madd1.Text = madd1.Text + " " + words[i];

                    else if (i < 7)
                    {
                        madd2.Text = madd2.Text + " " + words[i];
                    }
                    else if (i < 10)
                    {
                        madd3.Text = madd3.Text + " " + words[i];
                    }
                    else
                        madd4.Text = madd4.Text + " " + words[i];
                }
            
                
                member.Text = data[0];
                memname.Text = data[1];
                fname.Text = data[2];
                nationality.Text = data[4];
                qualificationm.Text = data[5];
                professionm.Text = data[6];
                madd1.Text = data[7];
                madd2.Text = data[8];
                mailadd1.Text = data[9];
                mailadd2.Text = data[10];
            }

        }

        private void button2_Click(object sender, EventArgs e)
        {
            try
            {
                List<Image> images = WIAScanner.Scan((string)lbDevices.SelectedItem);
                foreach (Image img in images)
                {
                    img.Save(@"D:\" + DateTime.Now.ToString("yyyy-MM-dd HHmmss") + ".jpeg", ImageFormat.Jpeg);
                    pictureBox2.Image = img;
                    
                }
                image = pictureBox2.Image;
                
            }
            catch (Exception exc)
            {


                MessageBox.Show(exc.Message,"ERROR");
            }
        }
      
        private void button4_Click(object sender, EventArgs e)
        {
            Thread newThread = new Thread(new ThreadStart(ThreadMethod));
            newThread.SetApartmentState(ApartmentState.STA);
            newThread.Start();
        }

         public void ThreadMethod()
        {
            if (InvokeRequired)
            {

                OpenFileDialog ofd = new OpenFileDialog();

                ofd.Filter = "";
                DialogResult dr = ofd.ShowDialog();
                ofd.Title = "Select image";
                if (dr == DialogResult.Cancel)
                    return;
                pictureBox4.Image = Image.FromFile(ofd.FileName);
                this.Invoke(new MethodInvoker(delegate
                {
                    string test = ofd.FileName;
                    img.Text = test;
                    image = pictureBox4.Image;
                }));
                return;
                
            }

        }

         

         private void LoadImage()
         {
             //we set the picturebox size according to image, we can get image width and height with the help of Image.Width and Image.height properties.
             int imgWidth = image.Width;
             int imghieght = image.Height;
             pictureBox5.Width = imgWidth;
             pictureBox5.Height = imghieght;
             pictureBox5.Image = image;
             PictureBoxLocation();
             OriginalImageSize = new Size(imgWidth, imghieght);

             //SetResizeInfo();
         }
         private void PictureBoxLocation()
         {
             int _x = 0;
             int _y = 0;
             if (splitContainer1.Panel1.Width > pictureBox5.Width)
             {
                 _x = (splitContainer1.Panel1.Width - pictureBox5.Width) / 2;
             }
             if (splitContainer1.Panel1.Height > pictureBox5.Height)
             {
                 _y = (splitContainer1.Panel1.Height - pictureBox5.Height) / 2;
             }
             pictureBox5.Location = new Point(_x, _y);
         }

         private void splitContainer1_Panel1_Resize(object sender, EventArgs e)
         {
             PictureBoxLocation();
         }

         private void button5_Click_1(object sender, EventArgs e)
         {
             Makeselection = true;
             button6.Enabled = true;
         }

         private void button6_Click_1(object sender, EventArgs e)
         {
             button6.Enabled = false;
             Cursor = Cursors.Default;

             try
             {
                 if (cropWidth < 1)
                 {
                     return;
                 }
                 Rectangle rect = new Rectangle(cropX, cropY, cropWidth, cropHeight);
                 //First we define a rectangle with the help of already calculated points
                 Bitmap OriginalImage = new Bitmap(pictureBox5.Image, pictureBox5.Width, pictureBox5.Height);
                 //Original image
                 _img = new Bitmap(cropWidth, cropHeight);
                 // for cropinf image
                 Graphics g = Graphics.FromImage(_img);
                 // create graphics
                 g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                 g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
                 g.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
                 //set image attributes
                 g.DrawImage(OriginalImage, 0, 0, rect, GraphicsUnit.Pixel);
                 
                 crop.Image = _img;
                 ans.Visible = true;
                 testclass test = new testclass();
                 if (count < 11)
                 {
                     ///////////////////////////////////////////////////////////////////////////////////////////////////
                     //Bitmap bitmap = new Bitmap(_img, new Size(wid, hei));
                     int wid = _img.Width;
                     int hei = _img.Height;

                     //Declare the double array of grayscale values to be read from "bitmap"
                     double[,] bnew = new double[wid, hei];

                     //Loop to read the data from the Bitmap image into the double array
                     int i, j;
                     for (i = 0; i < wid; i++)
                     {
                         for (j = 0; j < hei; j++)
                         {
                             Color pixelColor = _img.GetPixel(i, j);
                             double b = pixelColor.GetBrightness(); //the Brightness component

                             //Note that rows in C# correspond to columns in MWarray
                             bnew.SetValue(b, i, j);
                         }
                     }


                     MWArray cellout = null;

                     //pictureBox5.Width = _img.Width;
                     //pictureBox5.Height = _img.Height;
                     cellout = test.test((MWNumericArray)bnew);
                     MWCharArray item3 = (MWCharArray)cellout;

                     //mwString c = item3.ToString();
                     //item3 = item3.ToString();
                     //char[,] native3 = (char[,])item3.ToArray();
                     ans.Text = string.Format("{0}", item3.ToString());
                     //MessageBox.Show(string.Format("Sum={0}",item3.ToString()));
                     data[count] = string.Format("{0}", item3.ToString());
                     scase(1);
                     //test.cc();
                     count++;
                 }
             }
             catch (Exception ex)
             {
                 MessageBox.Show(ex.Message,"Error!");
             }
         }

         private void pictureBox5_MouseDown_1(object sender, MouseEventArgs e)
         {
             Cursor = Cursors.Default;
             if (Makeselection)
             {

                 try
                 {
                     if (e.Button == System.Windows.Forms.MouseButtons.Left)
                     {
                         Cursor = Cursors.Cross;
                         cropX = e.X;
                         cropY = e.Y;

                         cropPen = new Pen(Color.Black, 1);
                         cropPen.DashStyle = DashStyle.DashDotDot;


                     }
                     pictureBox5.Refresh();

                 }
                 catch (Exception ex)
                 {
                     MessageBox.Show(ex.Message, "Error!");
                 }
             }
         }

         private void pictureBox5_MouseMove_1(object sender, MouseEventArgs e)
         {
             Cursor = Cursors.Default;
             if (Makeselection)
             {

                 try
                 {
                     if (pictureBox5.Image == null)
                         return;


                     if (e.Button == System.Windows.Forms.MouseButtons.Left)
                     {
                         pictureBox5.Refresh();
                         cropWidth = e.X - cropX;
                         cropHeight = e.Y - cropY;
                         pictureBox5.CreateGraphics().DrawRectangle(cropPen, cropX, cropY, cropWidth, cropHeight);
                     }



                 }
                 catch (Exception ex)
                 {
                     //if (ex.Number == 5)
                     //    return;
                     MessageBox.Show(ex.Message, "Error!");
                 }
             }
         }

         private void pictureBox5_MouseUp_1(object sender, MouseEventArgs e)
         {
             if (Makeselection)
             {
                 Cursor = Cursors.Default;
             }
         }

         private void button7_Click(object sender, EventArgs e)
         {
             scase(2);
             count = count + 1;
             button6.Enabled = false;
             if (count > 12)
             {
                 button7.Enabled = false;
                 button5.Enabled = false;
                 button();
             }
         }

         public void scase(int num)
         {
             Color color; 
             if (num == 1)
                 color = System.Drawing.Color.MediumTurquoise;
             else
                 color = System.Drawing.Color.LightPink;

             switch (count)
             {
                 case 0:
                     mem.ForeColor = color;
                     break;
                 case 3:
                     nationality.ForeColor = color;
                     break;
                 case 1:
                     name.ForeColor = color;
                     break;
                 case 2:
                     father.ForeColor = color;
                     break;
                 case 10:
                     mailing2.ForeColor = color;
                     break;
                 case 4:
                     qualification.ForeColor = color;
                     break;
                 case 5:
                     profession.ForeColor = color;
                     break;
                 case 6:
                     add1.ForeColor = color;
                     break;
                 case 7:
                     add2.ForeColor = color;
                     break;
                 case 8:
                     married.ForeColor = color;
                     break;
                 case 9:
                     Mailing.ForeColor = color;
                     break;
                 case 11:
                     pic.ForeColor = color;
                     break;
             }
              
         }

        public static string Wordify(string pascalCaseString)
        {            
        Regex r = new Regex("(?<=[a-z])(?<x>[A-Z])|(?<=.)(?<x>[A-Z])(?=[a-z])");
        return r.Replace(pascalCaseString, " ${x}");
        }

        public void button()
        {
            if (count > 12)
                next.Visible = true;
        }

        private void next_Click(object sender, EventArgs e)
        {
            tabControl1.SelectedTab = tabPage5;
        }

        private void button8_Click(object sender, EventArgs e)
        {
            
            

            string conString = "Server=localhost;Port=3306;Database=acopak;Uid=root;password='root'";
            MySqlConnection conn = new MySqlConnection(conString);
            MySqlCommand command = conn.CreateCommand();
            command.CommandText = @"INSERT INTO  all_members (Memno ,Name ,Father_Husband ,Add1 ,Add2 ,Add3 ,Add4 ,Profession ,Qualification )"
                + "VALUES"
                + "(@Memno ,@Name ,@Father_Husband ,@Add1 ,@Add2 ,@Add3 ,@Add4 ,@Profession ,@Qualification);Select LAST_INSERT_ID();";

            
            command.Parameters.AddWithValue("@Memno", member.Text);
            command.Parameters.AddWithValue("@Name", memname.Text);
            command.Parameters.AddWithValue("@Father_Husband", fname.Text);
            command.Parameters.AddWithValue("@Add1", madd1.Text);
            command.Parameters.AddWithValue("@Add2", madd2.Text);
            command.Parameters.AddWithValue("@Add3", madd3.Text);
            command.Parameters.AddWithValue("@Add4", madd4.Text);
           command.Parameters.AddWithValue("@Profession", professionm.Text.ToString());
            command.Parameters.AddWithValue("@Qualification", qualificationm.Text.ToString());
                       try
            {
                conn.Open();
                int id = Convert.ToInt32(command.ExecuteScalar());
                conn.Close();
                command.Parameters.Clear();
                if (count >= 11)
                {
                    Bitmap temp = _img;
                    temp.Save("D:\\memberpics/mem" + id + ".jpeg", System.Drawing.Imaging.ImageFormat.Jpeg);
                }
                MessageBox.Show("Your record has been Added Successfully", "Congratulations");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Oopss");
            }
        }

        private void reset()
        {
            count = 0;
            Color color;
            color = System.Drawing.Color.Snow;
                mem.ForeColor = color;
               
                     nationality.ForeColor = color;

                     name.ForeColor = color;
                
                     father.ForeColor = color;
                    
                     mailing2.ForeColor = color;
                 
                     qualification.ForeColor = color;
                 
                     profession.ForeColor = color;
                
                     add1.ForeColor = color;
                
                     add2.ForeColor = color;
                  
                     married.ForeColor = color;
                 
                     Mailing.ForeColor = color;
                     mailing2.ForeColor = color;
               
                     pic.ForeColor = color;
              
        }

      

       

        
    

      


    }
   
        #endregion

}