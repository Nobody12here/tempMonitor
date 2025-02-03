using CPUTempMonitor;
using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Threading;
using System.Windows.Forms;
using static ApiHelper;
using HidSharp;
using Newtonsoft.Json;
using System.Net.Http;
using System.Text;
using LibreHardwareMonitor.Hardware;
using Newtonsoft.Json.Linq;

namespace SystemShieldClientApp
{
    partial class Form1 : Form
    {
        private CancellationTokenSource monitoringTokenSource;
 

        private void btnStartMonitoring_Click(object sender, EventArgs e)
        {
            if (DeviceId == 0)
            {
                MessageBox.Show("Please verify the device first.");
                return;
            }

            // Start monitoring in a background task
            monitoringTokenSource = new CancellationTokenSource();
            UpdateOnlineStatus(true);
            Task.Run(() => MonitorTemp(DeviceId, txtEmail.Text, monitoringTokenSource.Token));
        }

private void Form1_Load(object sender, EventArgs e)
        {
            Console.WriteLine("Working");
            // Post that the device is online when the form is opened
            
        }

        private void Form1_FormClosing(Object sender, FormClosingEventArgs e)
        {
            Console.WriteLine("Close working ");
            monitoringTokenSource?.Cancel();
            UpdateOnlineStatus(false);

        }

        private async void UpdateOnlineStatus(bool isOnline)
        {
            try
            {
                var payload = new { status = isOnline, deviceId = DeviceId ,email= txtEmail.Text };
                var jsonPayload = JsonConvert.SerializeObject(payload);
                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
                Console.WriteLine("Calling....");
                // Post to the online status API endpoint
                await ApiHelper.PostData("/monitoring/updateOnlineStatus", content);
            }
            catch (Exception ex)
            {
                // Log or handle exceptions as needed
                Console.WriteLine($"Failed to update online status: {ex.Message}");
            }
        }

        private void MonitorTemp(int deviceId, string email, CancellationToken token)
        {
            var computer = new Computer
            {
                IsCpuEnabled = true,
                IsMemoryEnabled = true,
                IsGpuEnabled = true
            };
            computer.Open();
            var updateVisitor = new UpdateVisitor();

            while (!token.IsCancellationRequested)
            {
                computer.Accept(updateVisitor);
                
                // Update the form with the monitoring values (Invoke required to access UI from background thread)
                Invoke((MethodInvoker)(() =>
                {
                    foreach (var hardware in computer.Hardware)
                    {
                        if (hardware.HardwareType == HardwareType.Cpu)
                        {
                            lblCpuLoadValue.Text = GetSensorValue(hardware, SensorType.Load, "CPU Total") + " %";
                            lblCpuTempValue.Text = GetSensorValue(hardware, SensorType.Temperature, "CPU Package") + " °C";
                        }
                        if (hardware.HardwareType == HardwareType.Memory)
                        {
                            lblMemoryUsageValue.Text = GetSensorValue(hardware, SensorType.Load, "Memory") + " %";
                        }
                        if (hardware.HardwareType == HardwareType.GpuNvidia || hardware.HardwareType.ToString() == "GpuIntel"|| hardware.HardwareType == HardwareType.GpuAmd)
                        {
                            lblGpuLoadValue.Text = GetSensorValue(hardware, SensorType.Load, "D3D 3D") + " %";
                            lblGpuTempValue.Text = GetSensorValue(hardware, SensorType.Temperature, "CPU Package") + " °C";
                        }
                    }
                }));

                // Post data to the API periodically
                SendMonitoringData(email, deviceId);

                Thread.Sleep(1000); // Adjust this interval as needed
            }

            computer.Close();
        }

        private string GetSensorValue(IHardware hardware, SensorType type, string sensorName)
        {
            foreach (var sensor in hardware.Sensors)
            {
                if (sensor.SensorType == type && sensor.Name == sensorName && sensor.Value.HasValue)
                    return sensor.Value.Value.ToString("0.0");
            }
            return "N/A";
        }

        private void SendMonitoringData(string email, int deviceId)
        {
            
            // Example of sending CPU load to API
            if (float.TryParse(lblCpuLoadValue.Text.Replace(" %", ""), out float cpuLoad))
            {
                var payload = new { usage = new[] { cpuLoad }, email, deviceId };
                var jsonPayload = JsonConvert.SerializeObject(payload);
                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
                ApiHelper.PostData("/monitoring/updateCpuUsage", content).GetAwaiter().GetResult();
            }

            // Example of sending CPU temperature
            if (float.TryParse(lblCpuTempValue.Text.Replace(" °C", ""), out float cpuTemp))
            {
                var payload = new { temps = new[] { cpuTemp }, email, deviceId };
                var jsonPayload = JsonConvert.SerializeObject(payload);
                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
                ApiHelper.PostData("/monitoring/updateCpuTemp", content).GetAwaiter().GetResult();
            }
            // Sending GPU load to API
            if (float.TryParse(lblGpuLoadValue.Text.Replace(" %", ""), out float gpuLoad))
            {
                var payload = new { usage = new[] { gpuLoad }, email, deviceId }; // Adjust key name based on your API
                var jsonPayload = JsonConvert.SerializeObject(payload);
                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
                ApiHelper.PostData("/monitoring/updateGpuUsage", content).GetAwaiter().GetResult();
            }

            // Sending GPU temperature to API
            if (float.TryParse(lblGpuTempValue.Text.Replace(" °C", ""), out float gpuTemp))
            {
                var payload = new { temp = new[] { gpuTemp }, email, deviceId }; // Adjust key name based on your API
                var jsonPayload = JsonConvert.SerializeObject(payload);
                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
                ApiHelper.PostData("/monitoring/updateGpuTemp", content).GetAwaiter().GetResult();
            }

            // Sending Memory usage to API
            if (float.TryParse(lblMemoryUsageValue.Text.Replace(" %", ""), out float memoryUsage))
            {
                var payload = new { usage = new[] { memoryUsage }, email, deviceId }; // Adjust key name based on your API
                var jsonPayload = JsonConvert.SerializeObject(payload);
                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
                ApiHelper.PostData("/monitoring/updateRamUsage", content).GetAwaiter().GetResult();
            }
        }

        private async void btnVerifyDevice_Click(object sender, EventArgs e)
        {
            // Code for verifying device goes here
            string email = (string)txtEmail.Text;
            string otp = (string)txtOTP.Text;
            dynamic response = await ApiHelper.VerifyDeviceAsync(email, otp);
            isVerified = response?.status == 200;
            if (isVerified)
            {
                DeviceId = response?.device ;
            }
            
            lblVerificationStatus.Text = isVerified ? "Device verified successfully!" : "Device verification failed.";
            lblVerificationStatus.ForeColor = isVerified ? System.Drawing.Color.Green : System.Drawing.Color.Red;
        }
       

        
     
        private bool isVerified=false;
        private int DeviceId;
        private Label lblTitle;
        private TextBox txtEmail;
        private TextBox txtOTP;
        private Button btnVerifyDevice;
        private Button btnStartMonitoring;
        private Label lblVerificationStatus;
        private TableLayoutPanel statsPanel;
        private Label lblCpuLoad, lblCpuTemp, lblMemoryUsage, lblGpuLoad, lblGpuTemp;
        private Label lblCpuLoadValue, lblCpuTempValue, lblMemoryUsageValue, lblGpuLoadValue, lblGpuTempValue;
        
        private void InitializeComponent()
        {
            // Set up form
            this.Text = "System Monitoring Tool";
            this.Size = new Size(400, 600);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.BackColor = Color.FromArgb(245, 245, 245);
            this.Font = new Font("Segoe UI", 10);
            this.Load += new EventHandler(Form1_Load);
            this.FormClosing += new FormClosingEventHandler(Form1_FormClosing); ;
            // Title Label
            lblTitle = new Label()
            {
                Text = "System Monitoring Tool",
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                ForeColor = Color.FromArgb(45, 62, 80),
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Top,
                Padding = new Padding(0, 20, 0, 20)
            };
            this.Controls.Add(lblTitle);

            // Email TextBox
            txtEmail = new TextBox()
            {
                Text = "Enter Email",
                Width = 250,
                Location = new Point(70, 70),
                BorderStyle = BorderStyle.FixedSingle
            };
            this.Controls.Add(txtEmail);

            // OTP TextBox
            txtOTP = new TextBox()
            {
                Text = "Enter OTP",
                Width = 250,
                Location = new Point(70, 110),
                BorderStyle = BorderStyle.FixedSingle
            };
            this.Controls.Add(txtOTP);

            // Verify Device Button
            btnVerifyDevice = new Button()
            {
                Text = "Verify Device",
                Location = new Point(70, 150),
                Width = 250,
                Height = 35,
                BackColor = Color.FromArgb(52, 152, 219),
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.White
            };
            btnVerifyDevice.FlatAppearance.BorderSize = 0;
            btnVerifyDevice.Click += new System.EventHandler(this.btnVerifyDevice_Click);
            this.Controls.Add(btnVerifyDevice);

            // Verification Status Label
            lblVerificationStatus = new Label()
            {
                Text = "Status: Not Verified",
                Font = new Font("Segoe UI", 9, FontStyle.Regular),
                ForeColor = Color.Gray,
                Location = new Point(70, 190),
                AutoSize = true
            };
            this.Controls.Add(lblVerificationStatus);

            // Start Monitoring Button
            btnStartMonitoring = new Button()
            {
                Text = "Start Monitoring",
                Location = new Point(70, 220),
                Width = 250,
                Height = 35,
                BackColor = Color.FromArgb(46, 204, 113),
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.White
            };
            btnStartMonitoring.FlatAppearance.BorderSize = 0;
            btnStartMonitoring.Click += new System.EventHandler(this.btnStartMonitoring_Click);
            this.Controls.Add(btnStartMonitoring);

            // Stats Panel (Grid)
            statsPanel = new TableLayoutPanel()
            {
                ColumnCount = 2,
                RowCount = 5,
                Location = new Point(70, 270),
                Size = new Size(250, 200),
                CellBorderStyle = TableLayoutPanelCellBorderStyle.Single,
                BackColor = Color.White
            };
            this.Controls.Add(statsPanel);

            // Labels for stats
            AddStatRow("CPU Load:", out lblCpuLoad, out lblCpuLoadValue);
            AddStatRow("CPU Temperature:", out lblCpuTemp, out lblCpuTempValue);
            AddStatRow("Memory Usage:", out lblMemoryUsage, out lblMemoryUsageValue);
            AddStatRow("GPU Load:", out lblGpuLoad, out lblGpuLoadValue);
            AddStatRow("GPU Temperature:", out lblGpuTemp, out lblGpuTempValue);
        }

        private void AddStatRow(string labelText, out Label label, out Label valueLabel)
        {
            label = new Label()
            {
                Text = labelText,
                TextAlign = ContentAlignment.MiddleLeft,
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 10),
                ForeColor = Color.FromArgb(52, 73, 94)
            };
            valueLabel = new Label()
            {
                Text = "N/A",
                TextAlign = ContentAlignment.MiddleRight,
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = Color.FromArgb(44, 62, 80)
            };
            statsPanel.Controls.Add(label);
            statsPanel.Controls.Add(valueLabel);
        }
    }
}
