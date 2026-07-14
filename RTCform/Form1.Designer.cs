namespace RTCVideoChat
{
    partial class Form1
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            panelLocalVideo = new Panel();
            panelRemoteVideo = new Panel();
            btnConnect = new AeroButton();
            btnDisconnect = new AeroButton();
            btnCall = new AeroButton();
            lblStatus = new Label();
            cmbCameras = new ComboBox();
            lblCameraSelect = new Label();
            chatGroupBox = new GroupBox();
            rtbChatHistory = new RichTextBox();
            txtChatInput = new TextBox();
            btnSendMessage = new AeroButton();
            lblLocalTitle = new Label();
            lblRemoteTitle = new Label();
            panelControls = new Panel();
            panelVideoContainer = new Panel();
            chatGroupBox.SuspendLayout();
            panelControls.SuspendLayout();
            panelVideoContainer.SuspendLayout();
            SuspendLayout();
            
            // 
            // panelVideoContainer
            // 
            panelVideoContainer.Controls.Add(lblRemoteTitle);
            panelVideoContainer.Controls.Add(lblLocalTitle);
            panelVideoContainer.Controls.Add(panelRemoteVideo);
            panelVideoContainer.Controls.Add(panelLocalVideo);
            panelVideoContainer.Location = new Point(20, 20);
            panelVideoContainer.Name = "panelVideoContainer";
            panelVideoContainer.Size = new Size(820, 320);
            panelVideoContainer.TabIndex = 20;
            panelVideoContainer.BackColor = ColorTranslator.FromHtml("#222e35"); // WhatsApp ana panel rengi
            panelVideoContainer.BorderStyle = BorderStyle.FixedSingle;
            
            // 
            // lblLocalTitle
            // 
            lblLocalTitle.AutoSize = true;
            lblLocalTitle.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
            lblLocalTitle.ForeColor = ColorTranslator.FromHtml("#075e54");
            lblLocalTitle.Location = new Point(10, 10);
            lblLocalTitle.Name = "lblLocalTitle";
            lblLocalTitle.Size = new Size(120, 20);
            lblLocalTitle.Text = "Ben";
            
            // 
            // lblRemoteTitle
            // 
            lblRemoteTitle.AutoSize = true;
            lblRemoteTitle.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
            lblRemoteTitle.ForeColor = ColorTranslator.FromHtml("#075e54");
            lblRemoteTitle.Location = new Point(420, 10);
            lblRemoteTitle.Name = "lblRemoteTitle";
            lblRemoteTitle.Size = new Size(130, 20);
            lblRemoteTitle.Text = "Kişi";
            
            // 
            // panelLocalVideo
            // 
            panelLocalVideo.BackColor = ColorTranslator.FromHtml("#111b21"); // WhatsApp video kutusu rengi
            panelLocalVideo.BorderStyle = BorderStyle.FixedSingle;
            panelLocalVideo.Location = new Point(10, 35);
            panelLocalVideo.Name = "panelLocalVideo";
            panelLocalVideo.Size = new Size(390, 270);
            panelLocalVideo.TabIndex = 1;
            
            // 
            // panelRemoteVideo
            // 
            panelRemoteVideo.BackColor = ColorTranslator.FromHtml("#111b21");
            panelRemoteVideo.BorderStyle = BorderStyle.FixedSingle;
            panelRemoteVideo.Location = new Point(420, 35);
            panelRemoteVideo.Name = "panelRemoteVideo";
            panelRemoteVideo.Size = new Size(390, 270);
            panelRemoteVideo.TabIndex = 2;
            
            // 
            // panelControls
            // 
            panelControls.Controls.Add(lblCameraSelect);
            panelControls.Controls.Add(cmbCameras);
            panelControls.Controls.Add(btnConnect);
            panelControls.Controls.Add(btnDisconnect);
            panelControls.Controls.Add(btnCall);
            panelControls.Location = new Point(20, 360);
            panelControls.Name = "panelControls";
            panelControls.Size = new Size(820, 80);
            panelControls.TabIndex = 21;
            panelControls.BackColor = ColorTranslator.FromHtml("#222e35");
            panelControls.BorderStyle = BorderStyle.None;
            
            // 
            // lblCameraSelect
            // 
            lblCameraSelect.AutoSize = true;
            lblCameraSelect.Font = new Font("Segoe UI", 10F, FontStyle.Regular);
            lblCameraSelect.ForeColor = ColorTranslator.FromHtml("#25d366");
            lblCameraSelect.Location = new Point(10, 10);
            lblCameraSelect.Name = "lblCameraSelect";
            lblCameraSelect.Size = new Size(100, 19);
            lblCameraSelect.Text = "Kamera Seç";
            
            // 
            // cmbCameras
            // 
            cmbCameras.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbCameras.Font = new Font("Segoe UI", 10F);
            cmbCameras.FormattingEnabled = true;
            cmbCameras.Location = new Point(10, 35);
            cmbCameras.Name = "cmbCameras";
            cmbCameras.Size = new Size(400, 25);
            cmbCameras.TabIndex = 0;
            cmbCameras.BackColor = ColorTranslator.FromHtml("#202c33");
            cmbCameras.ForeColor = ColorTranslator.FromHtml("#e9edef");
            
            // 
            // btnConnect
            // 
            btnConnect.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            btnConnect.Location = new Point(430, 25);
            btnConnect.Name = "btnConnect";
            btnConnect.Size = new Size(120, 45);
            btnConnect.TabIndex = 3;
            btnConnect.Text = "Bağlan";
            btnConnect.UseVisualStyleBackColor = true;
            btnConnect.Enabled = false;
            
            // 
            // btnDisconnect
            // 
            btnDisconnect.Enabled = false;
            btnDisconnect.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            btnDisconnect.Location = new Point(560, 25);
            btnDisconnect.Name = "btnDisconnect";
            btnDisconnect.Size = new Size(120, 45);
            btnDisconnect.TabIndex = 4;
            btnDisconnect.Text = "Görüşmeyi Bitir";
            btnDisconnect.UseVisualStyleBackColor = true;
            
            // 
            // btnCall
            // 
            btnCall.Enabled = false;
            btnCall.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            btnCall.Location = new Point(690, 25);
            btnCall.Name = "btnCall";
            btnCall.Size = new Size(120, 45);
            btnCall.TabIndex = 5;
            btnCall.Text = "Görüntülü Ara";
            btnCall.UseVisualStyleBackColor = true;
            
            // 
            // lblStatus
            // 
            lblStatus.AutoSize = true;
            lblStatus.Font = new Font("Segoe UI", 9F, FontStyle.Italic);
            lblStatus.ForeColor = Color.FromArgb(120, 120, 120);
            lblStatus.Location = new Point(30, 460);
            lblStatus.Name = "lblStatus";
            lblStatus.Size = new Size(151, 20);
            lblStatus.Text = "Hazır";
            
            // 
            // chatGroupBox
            // 
            chatGroupBox.Controls.Add(btnSendMessage);
            chatGroupBox.Controls.Add(txtChatInput);
            chatGroupBox.Controls.Add(rtbChatHistory);
            chatGroupBox.Font = new Font("Segoe UI", 11F, FontStyle.Bold);
            chatGroupBox.ForeColor = ColorTranslator.FromHtml("#25d366");
            chatGroupBox.Location = new Point(20, 490);
            chatGroupBox.Name = "chatGroupBox";
            chatGroupBox.Padding = new Padding(15);
            chatGroupBox.Size = new Size(820, 250);
            chatGroupBox.TabIndex = 6;
            chatGroupBox.TabStop = false;
            chatGroupBox.Text = "Sohbet";
            chatGroupBox.BackColor = ColorTranslator.FromHtml("#222e35");
            
            // 
            // rtbChatHistory
            // 
            rtbChatHistory.BackColor = ColorTranslator.FromHtml("#202c33");
            rtbChatHistory.BorderStyle = BorderStyle.None;
            rtbChatHistory.Font = new Font("Segoe UI", 10F);
            rtbChatHistory.Location = new Point(15, 35);
            rtbChatHistory.Name = "rtbChatHistory";
            rtbChatHistory.Padding = new Padding(10);
            rtbChatHistory.ReadOnly = true;
            rtbChatHistory.Size = new Size(790, 170);
            rtbChatHistory.TabIndex = 0;
            rtbChatHistory.Text = "";
            rtbChatHistory.ForeColor = ColorTranslator.FromHtml("#222e35");
            
            // 
            // txtChatInput
            // 
            txtChatInput.BorderStyle = BorderStyle.FixedSingle;
            txtChatInput.Enabled = false;
            txtChatInput.Font = new Font("Segoe UI", 10F);
            txtChatInput.Location = new Point(15, 215);
            txtChatInput.Name = "txtChatInput";
            txtChatInput.PlaceholderText = "Mesaj yaz...";
            txtChatInput.Size = new Size(650, 25);
            txtChatInput.TabIndex = 1;
            txtChatInput.BackColor = ColorTranslator.FromHtml("#202c33");
            txtChatInput.ForeColor = ColorTranslator.FromHtml("#e9edef");
            
            // 
            // btnSendMessage
            // 
            btnSendMessage.Enabled = false;
            btnSendMessage.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            btnSendMessage.Location = new Point(675, 210);
            btnSendMessage.Name = "btnSendMessage";
            btnSendMessage.Size = new Size(130, 35);
            btnSendMessage.TabIndex = 2;
            btnSendMessage.Text = "Gönder";
            btnSendMessage.UseVisualStyleBackColor = true;
            btnSendMessage.BackColor = ColorTranslator.FromHtml("#25d366");
            btnSendMessage.ForeColor = ColorTranslator.FromHtml("#222e35");
            
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(8F, 19F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = ColorTranslator.FromHtml("#111b21");
            ClientSize = new Size(860, 760);
            Controls.Add(chatGroupBox);
            Controls.Add(lblStatus);
            Controls.Add(panelControls);
            Controls.Add(panelVideoContainer);
            Font = new Font("Segoe UI", 10F);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            Name = "Form1";
            Padding = new Padding(10);
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Sohbet";
            chatGroupBox.ResumeLayout(false);
            chatGroupBox.PerformLayout();
            panelControls.ResumeLayout(false);
            panelControls.PerformLayout();
            panelVideoContainer.ResumeLayout(false);
            panelVideoContainer.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        private Panel panelLocalVideo;
        private Panel panelRemoteVideo;
        private AeroButton btnConnect;
        private AeroButton btnDisconnect;
        private AeroButton btnCall;
        private Label lblStatus;
        private ComboBox cmbCameras;
        private Label lblCameraSelect;
        private GroupBox chatGroupBox;
        private RichTextBox rtbChatHistory;
        private TextBox txtChatInput;
        private AeroButton btnSendMessage;
        private Label lblLocalTitle;
        private Label lblRemoteTitle;
        private Panel panelControls;
        private Panel panelVideoContainer;
    }
}
