using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;



namespace EON_FIRMWARE_FLASH
{
    
    
    
    public partial class Form1 : Form
    {

        private byte[] Start = new byte [] {0x10, 0x13, 0x11 };
        private List<string> PortName = new List<string>();
        private bool err_device;
        private bool tx_end;
        private  byte[] ACK = new byte[] {0x79};
        private  byte[] NACK = new byte[] {0x1F};
        private  byte[]  SYNC = new byte[] {0x7F};
        private byte[] READ = new byte[] { 0x11, 0xEE };
        private byte[] READ_FLASH = new byte[256];
        private byte[] GET = new byte[] { 0x00,0xFF};
        private char[] GET_FLASH = new char[15];
        private bool Tick_Timer;
        private byte[] PAGE = new byte[0x400];
        private byte[] FLASH_MEMORY = new byte[0x20000];
        private const UInt32 Flash_Start_Address = 0x08000000;

        public Form1()
        {
            InitializeComponent();
            label2.Hide();
            label3.Hide();
            progressBar1.Hide();
            progressBar1.Minimum = 0;
            progressBar1.Maximum = 127;
            progressBar1.Style = ProgressBarStyle.Continuous;
            progressBar1.Step = 0;
            
            serialPort1.ReadTimeout = 1500;
            serialPort1.BaudRate = 9600;
            serialPort1.Handshake = System.IO.Ports.Handshake.None;
           
            comboBox1.Items.Clear();
            foreach (string s in System.IO.Ports.SerialPort.GetPortNames())
            {
                PortName.Add(s);
            }
            PortName.Sort();
            comboBox1.Items.AddRange(PortName.ToArray());
            if (PortName.Count > 0) comboBox1.SelectedIndex = 0;
            else serialPort1.PortName = " ";
            //serialPort1.PortName = PortName[comboBox1.SelectedIndex];
                    
           
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (serialPort1.PortName == " " )
            {
                MessageBox.Show("Не е избран порт или няма такъв!");
                return;
            }
            try
            {
                serialPort1.Open();
            }
            catch
            {
                MessageBox.Show("Портът е зает!",serialPort1 .PortName );
                return;
            }
             
             //Send Message to enter in flash mode 
            serialPort1.Write(Start,0,Start.Length);
             // Wait for ok from device 
            int read_byte;
            err_device = false;
            tx_end = false;
            do
            {

                try
                {
                    read_byte = serialPort1.ReadByte();
                }
                catch (TimeoutException timeout)
                {

                    MessageBox.Show("Не отговаря устройството");
                    err_device = true;
                    label2.Hide();
                    serialPort1.Close();
                    return;

                }

                if (read_byte == 'K') tx_end = true;

            } while (!tx_end);


               label2.Text="Device Ready !";
               label2.Show();

             
               serialPort1.Close();
               delay(1000);

               //Change speed 115200 for fast transfer
               // Change speed
               serialPort1.BaudRate = 115200;
               serialPort1.Parity = System.IO.Ports.Parity.Even;
               serialPort1.ReadBufferSize = 256;
               //Open port now 
               serialPort1.Open();

                //Send SYNC Byte 0x7F
              serialPort1.Write(SYNC,0,1);
               if (!Wait_Answert())  return;
               // If ACK Bootloader is ready....  

               Get_Flash_Command();
                 
                
              
            // serialPort1.Close ();

        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {

            if (serialPort1.IsOpen) serialPort1.Close();

          
              if(PortName.Count>0) serialPort1.PortName = PortName[comboBox1.SelectedIndex];
             
             // serialPort1.PortName = comboBox1.Text;
            //MessageBox.Show("Eto ME");
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            Tick_Timer = true;
        }


        private void delay(int time)
        {
            timer1.Interval = time;
            Tick_Timer = false;
            timer1.Enabled = true;
            timer1.Start();
            while (!Tick_Timer)
            {
                Application.DoEvents();
            }
        }


        private bool Wait_Answert ()
        {
            int read;
            if (serialPort1.IsOpen)
            {
                try
                {
                    read = serialPort1.ReadByte();
                }
                catch (TimeoutException)
                {
                    MessageBox.Show("Няма комуникация с боот лодера");
                    err_device = true;
                    label2.Hide();
                    serialPort1.Close();
                    return false;
                }

                if ((byte)read != ACK[0])
                {
                    MessageBox.Show("Няма комуникация с боот лодера");
                    err_device = true;
                    label2.Hide();
                    serialPort1.Close();
                    return false;
                }
                else return true;





            }



            return false;
        }


      private void Get_Flash_Command()
       {

          if(!err_device)
          {
        // Boot loader Send ACk Next send Get command 0x00 plus XOR 
           serialPort1.Write(GET, 0, 2);              
         //Wait for Answert ACK or NACK or Nothigh
           if (!Wait_Answert())
           {
               MessageBox.Show("Err start GET Command");
               err_device=false;
               return;
           }
           bool ACK_OK = false;
           char read_data;
           byte index=0;
           do
           {
               try
               {
                   read_data = (char)serialPort1.ReadByte();
                   GET_FLASH[index++] = read_data;
                   if (read_data == ACK[0]) ACK_OK = true;
               }
               catch (TimeoutException)
               {
                   MessageBox.Show("No Ack from GET command");
                   err_device = true;
                   return;
               }

           } while (!ACK_OK);    
 

                 
          }
       }

      private void Read_Flash_Command( UInt32 address,byte numbers )
       {
         if (!err_device)
           {
          // Boot loader Send READ Command 
            serialPort1.Write(READ, 0, 2);  
          // Wait for Answert
           if (!Wait_Answert())
           {
               MessageBox.Show("Err start READ command");
               err_device = false;
               return;
           }

      /*After the transmission of the ACK byte,
       the bootloader waits for an address (4 bytes, byte 1 is the address MSB and byte 4 is the LSB)
       and a checksum byte
      */  
          // Send Address + Check summa

           byte[] adr = new byte [5];
           UInt32 test= address >> 24;
           adr[0] = (byte)(address >> 24 );
           adr[1] = (byte)(address >> 16);
           adr[2] = (byte)(address >> 8);
           adr[3] = (byte)(address);
           adr[4] = (byte)( adr[0] ^ adr[1] ^ adr[2] ^ adr[3]);
           
           serialPort1.Write(adr, 0, 5);

          //Wait for ACK
           if (!Wait_Answert())
           {
               MessageBox.Show("Err  READ ADDRESS command");
               err_device = false;
               return;
           }  
             
          //Send  the numbers to transmit + Check summa
           adr[0] = numbers;
           adr[1] = (byte)(numbers ^ 0xFF);
           serialPort1.Write(adr, 0,2);

           //Wait for ACK
           if (!Wait_Answert())
           {
               MessageBox.Show("Err  READ Numbers command");
               err_device = false;
               return;
           }  
             
         //Wait for ACK and go Read bytes if Check summa wrong send NACK
           int k = 0;
           do
           {
               try
               {
                   //serialPort1.Read(READ_FLASH, 0, numbers);
                   READ_FLASH[k++] = (byte)serialPort1.ReadByte();
               }

               catch (TimeoutException)
               {
                   MessageBox.Show("Err on reading ...", Convert.ToString(numbers));
                   break;
               }
           } while (k <= numbers);

            // Read susccess
            // MessageBox.Show("Read success");
          }
       }


        private void Read_Flash_Page( byte page)
        {
            UInt32 Start_Address;

            Start_Address = (uint)page * 1024 + Flash_Start_Address ;
            int page_counter = 0, buffer = 0;

            for (int i = 0; i < 4; i++)
            {
                Read_Flash_Command(Start_Address, 255);
                // Copy  READ_FLASH to PAGE             
                for (buffer = 0; buffer < 256; buffer++)
                {
                    PAGE[page_counter++] = READ_FLASH[buffer];
                }

                Start_Address += 0x100;
            }

            //MessageBox.Show("ok");
            
        }



        private void Read_Flash()
        {
            byte page ;
            uint adr = 0;
            progressBar1.Value = progressBar1.Minimum;
            label3.Text = "Read flash";
            label3.Show();
            //progressBar1.t
            progressBar1.Show();

            for (page = 0; page < 128; page++)
            {
                Read_Flash_Page(page);
                //Copy PAGE to FLASH_MEMORY
                for (uint page_index = 0; page_index < 0x400; page_index++)
                {
                    FLASH_MEMORY[adr++] = PAGE[page_index];
                }
                progressBar1.Value =  page;
            
            }

            return;

        }



        private void button2_Click(object sender, EventArgs e)
      {

          Read_Flash();
      }

      
    }
}
