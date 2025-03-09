using GMap.NET.MapProviders;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Hafız_IHO_Steam_Alcak_Irtif
{
    public partial class Form1 : Form
    {

        public Form1()
        {
            InitializeComponent();
        }

        #region Unnamed Variables
        string dataIn;
        string LogFilePath = "../../../Logs/Data-Log.txt";
        string errLogFilePath = "../../../Logs/Error-Log.txt";

        sbyte index_of_enlem, index_of_boylam, index_of_gazSensor, index_of_uvSensor, index_of_gps_error;
        string gazsensor, uvsensor;
        #endregion

        private void Form1_Load(object sender, EventArgs e)
        {
            string[] portNames = SerialPort.GetPortNames();
            ComPortBox.Items.AddRange(portNames);
            ComPortBox.Text = portNames[0];
            map.MapProvider = GMapProviders.GoogleMap;
            //map.DragButton = MouseButtons.Left;
            Control.CheckForIllegalCrossThreadCalls = false;
        }

        #region Com Port Open
        private void ComPortOpenButton_Click_1(object sender, EventArgs e)
        {
            ComPortOpen();
        }

        private void ComPortOpen()
        {
            try
            {
                seriP1.PortName = ComPortBox.Text;
                seriP1.Open();
                ComPortStatus.Value = 100;
            }
            catch (Exception err)
            {
                MessageBox.Show(err.Message, "Bir hatanız var!", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        #endregion

        #region Com Port Close
        private void ComPortCloseButton_Click(object sender, EventArgs e)
        {
            ComPortClose();
        }

        private void ComPortClose()
        {
            if (seriP1.IsOpen){
                seriP1.Close();
            }
            else
            {
                MessageBox.Show("Seri port zaten kapalı.", "Seri Port", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            ComPortStatus.Value = 0;
        }
        #endregion

        #region Data in, print and processing

        #region Veri alma
        private void seriP1_DataReceived(object sender, SerialDataReceivedEventArgs e) {
            dataIn = seriP1.ReadExisting(); //Örnek gelen veri - Q40,8401W31,1512G4213H8674L
            
            #region index of
            index_of_enlem      = Convert.ToSByte(dataIn.IndexOf('Q'));
            index_of_boylam     = Convert.ToSByte(dataIn.IndexOf('W'));
            index_of_gazSensor  = Convert.ToSByte(dataIn.IndexOf('G'));
            index_of_uvSensor   = Convert.ToSByte(dataIn.IndexOf('H'));
            index_of_gps_error  = Convert.ToSByte(dataIn.IndexOf('L'));
            #endregion

            DataProcessing();
            LogData();
            this.Invoke(new EventHandler(ShowData));
        }
        #endregion

        #region Veri kayıt etme
        private void LogData() {
            try {
                File.AppendAllText(LogFilePath, $"{dataIn}  |  {DateTime.Now} \n");
            }
            catch (Exception err)
            {
                File.AppendAllText(errLogFilePath, $"Log Error || {dataIn}  |  {DateTime.Now} \n");
            }
        }
        #endregion

        #region Veri ekrana yazdırma
        private void ShowData(object sender, EventArgs e) {

            #region Gaz Sensor
			try{
				if (index_of_gazSensor >= 0) { 
					gazsensor = dataIn.Substring(index_of_gazSensor + 1, (index_of_uvSensor - index_of_gazSensor) - 1);
					lblGaz.Text = $"{gazsensor}";
				}	
			}
			catch(Exception err){
                File.AppendAllText(errLogFilePath, $"Gaz Sensor - {err.Message}  |  {DateTime.Now} \n");
			}
            #endregion

            #region UV Sensor
			try{
				if (index_of_uvSensor >= 0 && index_of_gps_error <0) { 
					lblUV.Text = $"{dataIn.Substring(index_of_uvSensor + 1)}";
				}
                if (index_of_uvSensor >= 0 && index_of_gps_error >= 0)
                {
                    lblUV.Text = $"{dataIn.Substring(index_of_uvSensor + 1, (index_of_gps_error - index_of_uvSensor) - 1)}";
                }

            }
            catch (Exception err){
                File.AppendAllText(errLogFilePath, $"UV Sensor - {err.Message}  |  {DateTime.Now} \n");
			}
            #endregion

            #region
            //DataInFromComPort.Text += dataIn;
            //DataInFromComPort.Text += dataIn.Substring(index_of_irtifa + 2, (index_of_enlem - index_of_irtifa) - 2);
            //DataInFromComPort.SelectionStart = DataInFromComPort.Text.Length;
            //DataInFromComPort.ScrollToCaret();
            #endregion
        }
        #endregion

        #region Veri işleme
        private void DataProcessing()
        {
            #region GMap
			try{			
				if (index_of_enlem >= 0 && index_of_boylam >= 0)
				{
					lblenlem.Text = dataIn.Substring(index_of_enlem + 1, (index_of_boylam - index_of_enlem) - 1);
					lblboylam.Text = dataIn.Substring(index_of_boylam + 1, (index_of_boylam - index_of_enlem) - 1);
					lblKonum.Text = $"Konum: {lblenlem.Text}/{lblboylam.Text}";
				}
				if (lblenlem.Text != "Enlem" && lblboylam.Text != "Boylam")
				{
					map.MinZoom = 10;
					map.MaxZoom = 1000;
					map.Zoom = 15;
					map.Position = new GMap.NET.PointLatLng(Convert.ToDouble(lblenlem.Text.Replace('.', ',')), Convert.ToDouble(lblboylam.Text.Replace('.', ',')));
                    label5.Text = map.Position.ToString();
				}
			}
			catch(Exception err){
                File.AppendAllText(errLogFilePath, $"GMap.Net - {err.Message}  |  {DateTime.Now} \n");
			}

            #endregion

            #region Graph
            try
            {
                double oy1, oy2;
                oy1 = Convert.ToDouble(lblGaz.Text);
                oy2 = Convert.ToDouble(lblUV.Text);

                //Point leri temizleme.
                foreach (var series in chart1.Series)
                {
                    series.Points.Clear();
                }

                //Grafiğe değer ekleme
                chart1.Series["Faydalı Yük"].Points.Add(oy1);
                chart1.Series["Faydalı Yük"].Points.Add(oy2);

                //x ekseninde öğrenci isimlerini belirleme
                chart1.Series["Faydalı Yük"].Points[0].AxisLabel = "Gaz-Sensoru";
                chart1.Series["Faydalı Yük"].Points[1].AxisLabel = "UV-Sensor";

                //Sütun renklerini belirleme
                chart1.Series["Faydalı Yük"].Points[0].Color = Color.Aqua;
                chart1.Series["Faydalı Yük"].Points[1].Color = Color.Chocolate;

                chart1.ChartAreas[0].AxisX.LabelStyle.Angle = -70; // öğrenci isimleri dikey
                chart1.BackColor = Color.YellowGreen;//Arka Plan rengi
            }
            catch (Exception err)
            {
                File.AppendAllText(errLogFilePath, $"Graph Error - {err.Message}  |  {DateTime.Now} \n");
            }
            #endregion

            #region gps error
            if (index_of_gps_error >= 0 ) {
                btnGpsErr.BackColor = Color.DarkOrange;
                btnGpsErr.Text = "GPS Bağlanmadı";
            }
            #endregion
        }
        #endregion

        #endregion
    }
}