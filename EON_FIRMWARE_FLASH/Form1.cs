using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;



namespace EON_FIRMWARE_FLASH
{
    
    
    
    public partial class Form1 : Form
    {

        private byte[] Start = new byte [] {0x10, 0x13, 0x11 };
        private List<string> PortName = new List<string>();
        private bool device;
        private bool tx_end;
        private  byte[] ACK = new byte[] {0x79};
        private  byte[] NACK = new byte[] {0x1F};
        private  byte[]  SYNC = new byte[] {0x7F};
        private byte[] READ = new byte[] { 0x11, 0xEE };
        private byte[] READ_FLASH = new byte[258];
        private byte[] GET = new byte[] { 0x00,0xFF};
        private char[] GET_FLASH = new char[15];
        private byte[] ERASE = new byte[] { 0x43, 0xBC };
        private byte[] WRITE = new byte[] { 0x31, 0xCE };
        private byte[] WRITE_FLASH = new byte[258];
        private bool Tick_Timer;
        private byte[] PAGE = new byte[0x400];
        private byte[] FLASH_MEMORY = new byte[0x20000];
        private byte[] READ_FILE =   new byte[0x20000];
        private UInt32 READ_FILE_LENGTH;
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
            READ_FILE_LENGTH = 0;
            
            serialPort1.ReadTimeout = 1500;
            serialPort1.BaudRate = 9600;
            serialPort1.Handshake = System.IO.Ports.Handshake.None;
            device = false;
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
            device = false;
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
                    device = false;
                    label2.Hide();
                    serialPort1.Close();
                    return;

                }

                if (read_byte == 'K') tx_end = true;

            } while (!tx_end);

            device = true;
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
            byte read=0;
            if (serialPort1.IsOpen)
            {
                try
                {
                    read = (byte) serialPort1.ReadByte();
                }
                catch (TimeoutException)
                {
                    //Console.Write(read.ToString("X"));
                    MessageBox.Show("Няма връзка с боот-лодера :"+(read.ToString ("X"))); 
                    device = false;
                    label2.Hide();
                    serialPort1.Close();
                    return false;
                }

                if (read != ACK[0])
                {
                    MessageBox.Show("Няма ACK от боот-лодера :" + (read.ToString ("X")));
                   // label2.Hide();
                   // serialPort1.Close();
                    return false;
                }
                else return true;





            }



            return false;
        }


      private void Get_Flash_Command()
       {

          if(device)
          {
        // Boot loader Send ACk Next send Get command 0x00 plus XOR 
           serialPort1.Write(GET, 0, 2);              
         //Wait for Answert ACK or NACK or Nothigh
           if (!Wait_Answert())
           {
               MessageBox.Show("Err start GET Command");
               device=false;
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
                   device = false;
                   return;
               }

           } while (!ACK_OK);    
 

                 
          }
       }

      private void Read_Flash_Command( UInt32 address,byte numbers )
       {
         if (device)
           {
          // Boot loader Send READ Command 
            serialPort1.Write(READ, 0, 2);  
          // Wait for Answert
           if (!Wait_Answert())
           {
               MessageBox.Show("Err start READ command");
               device = false;
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
               device = false;
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
               device = false;
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
      private void Write_Flash_Command(UInt32 address, byte number)
      {
          if (device)
          {

              // Boot loader Send WRITE Command 
              serialPort1.Write(WRITE, 0, 2);
              // Wait for Answert
              if (!Wait_Answert())
              {
                  MessageBox.Show("Err start WRITE command");
                  //device = false;
                  return;
              }
          
              byte[] adr = new byte[5];
              UInt32 test = address >> 24;
              adr[0] = (byte)(address >> 24);
              adr[1] = (byte)(address >> 16);
              adr[2] = (byte)(address >> 8);
              adr[3] = (byte)(address);
              adr[4] = (byte)(adr[0] ^ adr[1] ^ adr[2] ^ adr[3]);

              serialPort1.Write(adr, 0, 5);
              //Wait for ACK
              if (!Wait_Answert())
              {
                  MessageBox.Show("Err WRITE ADDRESS command");
                  //device = false;
                  return;
              }  

              //Send number + bytes to write + xor(number byte to write)
               //Full page 
              number = 0xFF;
              byte[] number1 = new byte[1] { 0xff};
              byte xor=WRITE_FLASH  [0];
              for (uint i = 1; i <= number; i++)
              {
                  xor ^= WRITE_FLASH [i];
              }
              WRITE_FLASH[number + 1] =(byte)(number ^ xor);
              serialPort1.Write(number1,0,1);
              serialPort1.Write(WRITE_FLASH,0, number +2);


              if (!Wait_Answert())
              {
                  MessageBox.Show("Err WRITE_FLASH command");
                 // device = false;
                  return;
              }  

          }

      }
      private bool Erase_Flash_Page(byte page , byte numbers)
      {
         // if (device)
          {
              // Boot loader Send Erase Command 
               
              serialPort1.Write(ERASE, 0, 2);
              // Wait for Answert
              if (!Wait_Answert())
              {
                  MessageBox.Show("Err start Erase command");
                  device = false;
                  return false;
              }
         
              byte[] ERASE_FLASH = new byte[numbers+3];
               ERASE_FLASH[0] = numbers;
               byte j=1;
               byte xor=numbers;
               for (byte i = 0; i < numbers + 1; i++)
               {
                   ERASE_FLASH[j++] = i;
                   xor ^=i;
               }

               ERASE_FLASH[j++] = xor;
                         
              // Send Erase page + nimners command
              serialPort1.Write(ERASE_FLASH ,0,j);
              serialPort1.ReadTimeout = 10000;
                if (!Wait_Answert())
              {
                  MessageBox.Show("Err Erase page,numbers command");
                  device = false;
                  return false;
              }

          }
          return true;
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
            if (device)
            {
                byte page;
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
                    progressBar1.Value = page;

                }
            }
            return;

        }
        private void Write_Flash()
        {
           device = true;
             
        if (device && (READ_FILE_LENGTH>0) )
            {
                label3.Text = "Erase flash";
                label3.Show();
                byte page =(byte) (READ_FILE_LENGTH / 1024);
                if ((READ_FILE_LENGTH - 1024 * page) != 0) page++; 
               if (Erase_Flash_Page(0, page)) 
                {
               UInt32 address=0 ;
              

         for (UInt32 flash_address =Flash_Start_Address  ; flash_address <= (Flash_Start_Address + 1024 * page); flash_address += 0x100)
                 {
                   // Console.Write(flash_address.ToString("X"));
                   // Console.Write("\n");
                    //FLASH_
                    for (uint  index_address = 0; index_address < 256; index_address++)
                    {
                        if (address < READ_FILE_LENGTH) WRITE_FLASH[index_address] = READ_FILE[address++];
                        else WRITE_FLASH[index_address] = 0xFF;                                           
                    }

                    Console.Write(flash_address.ToString("X"));
                    Console.Write("\n");
                    Write_Flash_Command(flash_address, 255);

                    //string Stop = Console.ReadLine();
                   
                }


         //string Stop = Console.ReadLine();

                }





            }
        }



        private void button2_Click(object sender, EventArgs e)
      {

          Read_Flash();
      }




        private bool Save_File()
        {
            //Stream myStream;
            SaveFileDialog saveFileDialog1 = new SaveFileDialog();
            saveFileDialog1.Filter = "bin files (*.bin)|*.bin|All files (*.*)|*.*";
            saveFileDialog1.FilterIndex = 2;
            saveFileDialog1.RestoreDirectory = true;
            if (saveFileDialog1.ShowDialog() == DialogResult.OK)
            {
             try
                {
                    System.IO.FileStream _FileStream = new System.IO.FileStream(saveFileDialog1.FileName, System.IO.FileMode.Create,
                                      System.IO.FileAccess.Write);
                  _FileStream.Write(FLASH_MEMORY, 0, FLASH_MEMORY.Length);
                  _FileStream.Close();
                   return true;
                }

                catch (Exception _Ex)
                {
                    Console.Write(_Ex.ToString());

                }

                

            }


            return false;  

        }


        private bool  Read_File()
        {
            READ_FILE_LENGTH = 0;
            OpenFileDialog openFileDialog1 = new OpenFileDialog();
           openFileDialog1.Filter = "bin files (*.bin)|*.bin|All files (*.*)|*.*";
            openFileDialog1.FilterIndex = 2;
            openFileDialog1.RestoreDirectory = true;
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    System.IO.FileStream _FileStream = new System.IO.FileStream(openFileDialog1.FileName, System.IO.FileMode.Open,
                                     System.IO.FileAccess.Read);
                    READ_FILE_LENGTH = (UInt32)(_FileStream.Length);
                    if(READ_FILE_LENGTH  > 0x20000) 
                    {
                        MessageBox.Show("File is too big....");
                         return false;
                    }

                    _FileStream.Read(READ_FILE, 0, (int)READ_FILE_LENGTH);
                    _FileStream.Close();
                    return true;
                }

                catch (Exception _Ex)
                {
                    MessageBox.Show(_Ex.ToString());
                    Console.Write(_Ex.ToString());

                }

             }


            return false;  






        }
        private void button3_Click(object sender, EventArgs e)
        {
            Save_File();
        }

        private void button4_Click(object sender, EventArgs e)
        {
            Write_Flash();

        }

        private void button5_Click(object sender, EventArgs e)
        {
            Read_File();
        }

        private void button6_Click(object sender, EventArgs e)
        {
            Get_Flash_Command();
        }

        private void button7_Click(object sender, EventArgs e)
        {
              Write_Flash_Command(0x08000000, 255);
        }
      
    }
}
