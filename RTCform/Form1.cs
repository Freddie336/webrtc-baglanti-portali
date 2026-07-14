using Websocket.Client;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Drawing.Imaging;
using System.Drawing.Drawing2D;
using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using DirectShowLib;

namespace RTCVideoChat;

public partial class Form1 : Form
{
    private VideoCapture? videoCapture;
    private readonly PictureBox localPictureBox = new();
    private readonly PictureBox remotePictureBox = new();
    private readonly List<DsDevice> videoCaptureDevices = new();
    
    private WebsocketClient? websocketClient;
    private Uri serverUri = new Uri("ws://localhost:8080/ws");
    private readonly System.Windows.Forms.Timer videoFrameTimer;
    private const int VideoFrameInterval = 66; // ~15 FPS - Web ile uyumlu
    private bool isStreaming = false;
    private bool isConnectedToWeb = false;
    private string remoteClientType = "";
    private int framesSent = 0;
    private readonly ImageCodecInfo jpegEncoder;
    private readonly EncoderParameters encoderParams;

    public Form1()
    {
        InitializeComponent();
        ApplyModernTheme();
        LoadCameraList();
        SetupVideoComponents();
        
        // JPEG kalite ayarlarını yapılandır
        jpegEncoder = ImageCodecInfo.GetImageEncoders().First(c => c.FormatID == ImageFormat.Jpeg.Guid);
        encoderParams = new EncoderParameters(1);
        encoderParams.Param[0] = new EncoderParameter(Encoder.Quality, 80L); // %80 kalite
        
        videoFrameTimer = new System.Windows.Forms.Timer();
        videoFrameTimer.Interval = VideoFrameInterval;
        videoFrameTimer.Tick += VideoFrameTimer_Tick;
        
        this.FormClosing += Form1_FormClosing;
        btnConnect.Click += BtnConnect_Click;
        btnDisconnect.Click += BtnDisconnect_Click;
        btnCall.Click += BtnCall_Click;
        cmbCameras.SelectedIndexChanged += CmbCameras_SelectedIndexChanged;
        btnSendMessage.Click += BtnSendMessage_Click;
        txtChatInput.KeyDown += TxtChatInput_KeyDown;
        this.Load += Form1_Load;
    }

    private void Form1_Load(object? sender, EventArgs e)
    {
        lblStatus.Text = "Durum: Sunucuya bağlanılıyor...";
        InitializeWebSocket();
    }

    private void LoadCameraList()
    {
        videoCaptureDevices.Clear();
        cmbCameras.Items.Clear();

        videoCaptureDevices.AddRange(DsDevice.GetDevicesOfCat(FilterCategory.VideoInputDevice));

        if (videoCaptureDevices.Count == 0)
        {
            MessageBox.Show("Kamera bulunamadı!", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            btnConnect.Enabled = false;
            return;
        }

        foreach (var device in videoCaptureDevices)
        {
            string cameraInfo = $"{device.Name}";
            using (var cap = new VideoCapture(videoCaptureDevices.IndexOf(device)))
            {
                if (cap.IsOpened)
                {
                    double width = cap.Get(CapProp.FrameWidth);
                    double height = cap.Get(CapProp.FrameHeight);
                    double fps = cap.Get(CapProp.Fps);
                    cameraInfo += $" ({width}x{height} @ {fps:F1} FPS)";
                }
            }
            cmbCameras.Items.Add(cameraInfo);
        }

        if (cmbCameras.Items.Count > 0)
        {
            cmbCameras.SelectedIndex = 0;
        }
    }

    private void CmbCameras_SelectedIndexChanged(object sender, EventArgs e)
    {
        btnConnect.Enabled = cmbCameras.SelectedIndex >= 0;
        if (btnConnect.Enabled)
        {
            var selectedDevice = videoCaptureDevices[cmbCameras.SelectedIndex];
            lblStatus.Text = $"Durum: {selectedDevice.Name} seçildi";
        }
    }

    private void SetupVideoComponents()
    {
        localPictureBox.Dock = DockStyle.Fill;
        localPictureBox.SizeMode = PictureBoxSizeMode.Zoom;
        panelLocalVideo.Controls.Add(localPictureBox);

        remotePictureBox.Dock = DockStyle.Fill;
        remotePictureBox.SizeMode = PictureBoxSizeMode.Zoom;
        panelRemoteVideo.Controls.Add(remotePictureBox);
    }

    private void InitializeWebSocket()
    {
        websocketClient = new WebsocketClient(serverUri);
        
        websocketClient.MessageReceived.Subscribe(msg =>
        {
            try
            {
                var message = JObject.Parse(msg.Text);
                string type = message["type"].ToString();

                switch (type)
                {
                    case "client-type":
                        remoteClientType = message["clientType"].ToString();
                        this.Invoke(() =>
                        {
                            lblStatus.Text = $"Durum: {remoteClientType} bağlandı";
                            btnCall.Enabled = true;
                        });
                        break;

                    case "chat":
                        var content = message["content"]?.ToString() ?? "";
                        var sender = message["sender"]?.ToString() ?? "Bilinmeyen";
                        AppendChatMessage(sender, content);
                        break;

                    case "frame":
                        // Herhangi bir client'tan gelen frame'i işle
                        HandleWebFrame(message["data"].ToString());
                        Console.WriteLine("Frame alındı ve işlendi");
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Mesaj işleme hatası: {ex.Message}");
            }
        });

        websocketClient.ReconnectionHappened.Subscribe(info =>
        {
            this.Invoke(() =>
            {
                lblStatus.Text = $"Durum: Sunucuya bağlanıldı. Sohbete hazır.";
                txtChatInput.Enabled = true;
                btnSendMessage.Enabled = true;
                Console.WriteLine($"WebSocket bağlandı: {info.Type}");
            });
            
            // Bağlantı kurulduktan sonra client type gönder
            Task.Delay(500).ContinueWith(_ =>
            {
                try
                {
                    websocketClient?.Send(JsonConvert.SerializeObject(new
                    {
                        type = "client-type",
                        clientType = "winforms"
                    }));
                    Console.WriteLine("Client type gönderildi: winforms");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Client type gönderme hatası: {ex.Message}");
                }
            });
        });

        websocketClient.DisconnectionHappened.Subscribe(info =>
        {
            this.Invoke(() =>
            {
                if (info.Type == DisconnectionType.Error)
                {
                    lblStatus.Text = $"Durum: Bağlantı hatası! ({info.CloseStatusDescription})";
                }
                else
                {
                    lblStatus.Text = $"Durum: Bağlantı kesildi ({info.Type})";
                }
                btnCall.Enabled = false;
                isStreaming = false;
                txtChatInput.Enabled = false;
                btnSendMessage.Enabled = false;
                btnConnect.Enabled = false; // Yeniden bağlanma otomatik, butonu kapalı tut
                btnDisconnect.Enabled = false;
            });
        });

        Task.Run(() => websocketClient.Start());
    }

    private void HandleWebFrame(string base64Frame)
    {
        try
        {
            var frameData = Convert.FromBase64String(base64Frame);
            using var ms = new MemoryStream(frameData);
            var bitmap = new Bitmap(ms);
            
            this.Invoke(() =>
            {
                remotePictureBox.Image?.Dispose();
                remotePictureBox.Image = bitmap;
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Frame işleme hatası: {ex.Message}");
        }
    }

    private void BtnConnect_Click(object sender, EventArgs e)
    {
        try
        {
            if (cmbCameras.SelectedIndex < 0)
            {
                MessageBox.Show("Lütfen bir kamera seçin!", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            // WebSocket bağlantısı yoksa veya kapalıysa tekrar başlat
            if (websocketClient == null || !websocketClient.IsRunning)
            {
                InitializeWebSocket();
            }
            StartCamera();
            btnConnect.Enabled = false;
            btnDisconnect.Enabled = true;
            btnCall.Enabled = true; // Kamera başladığında ara butonunu aktif et
            cmbCameras.Enabled = false;
            lblStatus.Text = "Durum: Kamera başlatıldı. 'Ara' butonuna tıklayarak video paylaşımını başlatabilirsiniz.";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Kamera başlatılırken hata: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void StartCamera()
    {
        if (cmbCameras.SelectedIndex >= 0)
        {
            videoCapture?.Dispose();
            videoCapture = new VideoCapture(cmbCameras.SelectedIndex);
            
            if (videoCapture.IsOpened)
            {
                videoFrameTimer.Start();
            }
            else
            {
                throw new Exception("Kamera başlatılamadı!");
            }
        }
    }

    private void VideoFrameTimer_Tick(object? sender, EventArgs e)
    {
        if (videoCapture == null || !videoCapture.IsOpened) return;

        using var frame = videoCapture.QueryFrame();
        if (frame != null && !frame.IsEmpty)
        {
            try
            {
                var bitmap = frame.ToBitmap();
                
                // UI thread'de güncelle
                this.Invoke(() =>
                {
                    localPictureBox.Image?.Dispose();
                    localPictureBox.Image = new Bitmap(bitmap);
                });

                // Sadece streaming modunda ve bağlantı varsa gönder
                if (isStreaming && websocketClient?.IsRunning == true)
                {
                    try
                    {
                        using var ms = new MemoryStream();
                        bitmap.Save(ms, jpegEncoder, encoderParams);
                        var base64 = Convert.ToBase64String(ms.ToArray());
                        
                        var frameMessage = JsonConvert.SerializeObject(new
                        {
                            type = "frame",
                            data = base64
                        });
                        
                        websocketClient.Send(frameMessage);
                        framesSent++;
                        
                        if (framesSent % 30 == 0) // Her 30 frame'de bir log
                        {
                            Console.WriteLine($"Toplam frame gönderildi: {framesSent}");
                        }
                    }
                    catch (Exception streamEx)
                    {
                        Console.WriteLine($"Frame gönderme hatası: {streamEx.Message}");
                    }
                }
                
                bitmap.Dispose();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Frame işleme hatası: {ex.Message}");
            }
        }
    }

    private void BtnCall_Click(object sender, EventArgs e)
    {
        if (!isStreaming)
        {
            if (videoCapture?.IsOpened == true && websocketClient?.IsRunning == true)
            {
                isStreaming = true;
                btnCall.Text = "Paylaşımı Durdur";
                lblStatus.Text = "Durum: Video paylaşılıyor...";
                Console.WriteLine("Video streaming başlatıldı");
            }
            else
            {
                MessageBox.Show("Kamera veya WebSocket bağlantısı hazır değil!", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                Console.WriteLine($"Stream başlatılamadı - Kamera: {videoCapture?.IsOpened}, WebSocket: {websocketClient?.IsRunning}");
            }
        }
        else
        {
            isStreaming = false;
            btnCall.Text = "Ara";
            lblStatus.Text = "Durum: Video paylaşımı durduruldu";
            Console.WriteLine("Video streaming durduruldu");
        }
    }

    private void BtnDisconnect_Click(object sender, EventArgs e)
    {
        CloseConnections();
        btnConnect.Enabled = true;
        btnDisconnect.Enabled = false;
        btnCall.Enabled = false;
        btnCall.Text = "Ara";
        cmbCameras.Enabled = true;
        lblStatus.Text = "Durum: Bağlantı kesildi";
    }

    private void CloseConnections()
    {
        isStreaming = false;
        isConnectedToWeb = false;
        videoFrameTimer.Stop();
        remoteClientType = "";
        if (websocketClient != null)
        {
            try
            {
                websocketClient.Stop(System.Net.WebSockets.WebSocketCloseStatus.NormalClosure, "Bağlantı kapatıldı");
                websocketClient.Dispose();
            }
            catch { }
            websocketClient = null;
        }

        if (videoCapture != null)
        {
            videoCapture.Dispose();
            videoCapture = null;
        }

        if (localPictureBox.Image != null)
        {
            localPictureBox.Image.Dispose();
            localPictureBox.Image = null;
        }

        if (remotePictureBox.Image != null)
        {
            remotePictureBox.Image.Dispose();
            remotePictureBox.Image = null;
        }
    }

    private void Form1_FormClosing(object sender, FormClosingEventArgs e)
    {
        CloseConnections();
    }

    private void TxtChatInput_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Enter)
        {
            e.SuppressKeyPress = true; // Enter tuşunun ses çıkarmasını engelle
            BtnSendMessage_Click(sender, e);
        }
    }

    private void BtnSendMessage_Click(object? sender, EventArgs e)
    {
        var message = txtChatInput.Text.Trim();
        if (!string.IsNullOrEmpty(message) && websocketClient?.IsRunning == true)
        {
            AppendChatMessage("Siz", message);
            txtChatInput.Clear();

            var chatMessage = new
            {
                type = "chat",
                content = message,
                sender = "WinForms" 
            };
            
            websocketClient.Send(JsonConvert.SerializeObject(chatMessage));
        }
    }

    private void AppendChatMessage(string sender, string message)
    {
        if (this.InvokeRequired)
        {
            this.Invoke(() => AppendChatMessage(sender, message));
            return;
        }
        rtbChatHistory.SelectionStart = rtbChatHistory.TextLength;
        rtbChatHistory.SelectionLength = 0;
        // WhatsApp tarzı: Kullanıcı mesajı yeşil balon, karşı taraf gri balon
        if (sender == "Siz")
        {
            rtbChatHistory.SelectionBackColor = Color.FromArgb(220, 248, 198); // Açık yeşil
            rtbChatHistory.SelectionColor = Color.FromArgb(17, 27, 33); // Koyu metin
            rtbChatHistory.SelectionFont = new Font("Segoe UI", 10, FontStyle.Bold);
            rtbChatHistory.AppendText(sender + ": ");
            rtbChatHistory.SelectionFont = new Font("Segoe UI", 10, FontStyle.Regular);
            rtbChatHistory.AppendText(message + Environment.NewLine);
        }
        else
        {
            rtbChatHistory.SelectionBackColor = Color.FromArgb(255, 255, 255); // Beyaz/gri
            rtbChatHistory.SelectionColor = Color.FromArgb(17, 27, 33); // Koyu metin
            rtbChatHistory.SelectionFont = new Font("Segoe UI", 10, FontStyle.Bold);
            rtbChatHistory.AppendText(sender + ": ");
            rtbChatHistory.SelectionFont = new Font("Segoe UI", 10, FontStyle.Regular);
            rtbChatHistory.AppendText(message + Environment.NewLine);
        }
        rtbChatHistory.SelectionBackColor = rtbChatHistory.BackColor;
        rtbChatHistory.ScrollToCaret();
    }

    private void ApplyModernTheme()
    {
        // WhatsApp benzeri renk paleti
        var primaryColor = Color.FromArgb(37, 211, 102); // WhatsApp yeşili
        var primaryDarkColor = Color.FromArgb(7, 94, 84); // WhatsApp koyu yeşil
        var accentColor = Color.FromArgb(40, 47, 52); // WhatsApp koyu gri
        var backgroundColor = Color.FromArgb(17, 27, 33); // WhatsApp ana arka plan
        var cardColor = Color.FromArgb(34, 46, 53); // WhatsApp kart arka planı
        var borderColor = Color.FromArgb(42, 57, 66);
        var textPrimaryColor = Color.FromArgb(233, 237, 239); // Açık metin
        var textSecondaryColor = Color.FromArgb(134, 150, 160); // Açık gri
        var chatSentColor = Color.FromArgb(220, 248, 198); // WhatsApp gönderilen mesaj
        var chatReceivedColor = Color.FromArgb(255, 255, 255); // WhatsApp alınan mesaj

        // Ana form
        this.BackColor = backgroundColor;
        this.ForeColor = textPrimaryColor;
        this.Font = new Font("Segoe UI", 10F, FontStyle.Regular);
        this.Text = "Bağlantı Portalı - WhatsApp Stili";

        // Video container ve control panel'ler
        panelVideoContainer.BackColor = cardColor;
        panelControls.BackColor = cardColor;
        panelVideoContainer.BorderStyle = BorderStyle.None;
        panelControls.BorderStyle = BorderStyle.None;

        // Video panel'leri
        panelLocalVideo.BackColor = Color.FromArgb(17, 27, 33);
        panelRemoteVideo.BackColor = Color.FromArgb(17, 27, 33);
        panelLocalVideo.BorderStyle = BorderStyle.FixedSingle;
        panelRemoteVideo.BorderStyle = BorderStyle.FixedSingle;

        // Başlık etiketleri
        lblLocalTitle.ForeColor = primaryDarkColor;
        lblLocalTitle.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
        lblRemoteTitle.ForeColor = primaryDarkColor;
        lblRemoteTitle.Font = new Font("Segoe UI", 11F, FontStyle.Bold);

        // Diğer etiketler
        lblCameraSelect.ForeColor = textSecondaryColor;
        lblCameraSelect.Font = new Font("Segoe UI", 10F, FontStyle.Regular);
        lblStatus.ForeColor = textSecondaryColor;
        lblStatus.Font = new Font("Segoe UI", 9F, FontStyle.Italic);

        // ComboBox
        cmbCameras.BackColor = Color.FromArgb(42, 57, 66);
        cmbCameras.ForeColor = textPrimaryColor;
        cmbCameras.FlatStyle = FlatStyle.Flat;
        cmbCameras.Font = new Font("Segoe UI", 10F);

        // Modern düğmeler (AeroButton özel renkleri)
        btnConnect.Color1 = primaryColor;
        btnConnect.Color2 = primaryDarkColor;
        btnConnect.ForeColor = Color.White;

        btnDisconnect.Color1 = Color.FromArgb(229, 57, 53);
        btnDisconnect.Color2 = Color.FromArgb(179, 18, 23);
        btnDisconnect.ForeColor = Color.White;

        btnCall.Color1 = Color.FromArgb(37, 211, 102);
        btnCall.Color2 = Color.FromArgb(18, 140, 126);
        btnCall.ForeColor = Color.White;

        btnSendMessage.Color1 = primaryColor;
        btnSendMessage.Color2 = primaryDarkColor;
        btnSendMessage.ForeColor = Color.White;

        // Sohbet grubu
        chatGroupBox.BackColor = cardColor;
        chatGroupBox.ForeColor = primaryDarkColor;
        chatGroupBox.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
        chatGroupBox.Padding = new Padding(15);
        chatGroupBox.FlatStyle = FlatStyle.Flat;

        // Sohbet geçmişi
        rtbChatHistory.BackColor = Color.FromArgb(42, 57, 66);
        rtbChatHistory.ForeColor = textPrimaryColor;
        rtbChatHistory.BorderStyle = BorderStyle.None;
        rtbChatHistory.Font = new Font("Segoe UI", 10F);

        // Mesaj girişi
        txtChatInput.BackColor = Color.FromArgb(255,255,255);
        txtChatInput.ForeColor = Color.FromArgb(17, 27, 33);
        txtChatInput.Font = new Font("Segoe UI", 10F);
        txtChatInput.BorderStyle = BorderStyle.FixedSingle;
        txtChatInput.BorderStyle = BorderStyle.FixedSingle;
        txtChatInput.Padding = new Padding(8);
    }
}
