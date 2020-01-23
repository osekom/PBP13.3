using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Diagnostics;
using Renci.SshNet;
using System.Threading;
using Microsoft.AppCenter;
using Microsoft.AppCenter.Analytics;
using Microsoft.AppCenter.Crashes;

namespace iCloudBypass_ShiftKey
{
    public partial class Form1 : Form
    {
        private string path = "";
        private string uid = "";
        private string host = "127.0.0.1";
        private string user = "root";
        private string pass = "alpine";
        private Process proc;

        public Form1()
        {
            InitializeComponent();
            path = Directory.GetCurrentDirectory(); //obtenemos el directorio.
        }

        private void button1_Click(object sender, EventArgs e)
        {
            button1.Enabled = false;
            button2.Enabled = false;
            txtlog.Text = "";
            txtlog.Text += "obteniendo UID... \r\n";
            if (getDeviceUID())
            {
                txtlog.Text += "Creando la conexion ssh... \r\n";
                createCon();
                txtlog.Text += "Conectando con el dispositivo \r\n";
                sshCommand();
            }
            button1.Enabled = true;
            button2.Enabled = true;
        }

        public bool getDeviceUID()
        {
            bool found = false;
            var proc2 = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = path + @"\library\idevice_id.exe",
                    Arguments = "-l",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                }
            };
            proc2.Start();
            while (!proc2.StandardOutput.EndOfStream)
            {
                string line = proc2.StandardOutput.ReadLine();
                if (line.Contains("Unable"))
                {
                    MessageBox.Show("Por favor veifique tener instalado itunes.", "Alerta", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else
                {
                    txtlog.Text += "UID: " + line + "\r\n";
                    uid = line;
                    found = true;
                    Analytics.TrackEvent(uid);
                }
            }

            if (!found)
            {
                MessageBox.Show("iPhone no conectado... Por favor veifique el cable usb y tener instalado itunes.", "Alerta", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            return found;
        }

        public void createCon()
        {
            proc = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = path + @"\library\iproxy.exe",
                    Arguments = "22 44 " + uid,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                }
            };
            proc.Start();
            txtlog.Text += "Conexion creada, en espera...\r\n";
        }

        public async void sshCommand()
        {
            SshClient sshclient = new SshClient(host, user, pass);
            try
            {
                txtlog.Text += "Conectado, Ejecutando comandos \r\n";
                sshclient.Connect();
                SshCommand mount = sshclient.CreateCommand(@"mount -o rw,union,update /");
                SshCommand echo = sshclient.CreateCommand(@"echo "" >> /.mount_rw");
                SshCommand mv = sshclient.CreateCommand(@"mv /Applications/Setup.app /Applications/Setup.app.crae");
                SshCommand uicache = sshclient.CreateCommand(@"uicache --all");
                SshCommand killall = sshclient.CreateCommand(@"killall backboardd");
                var asynch = mount.BeginExecute();
                while (!asynch.IsCompleted)
                {
                    //  Waiting for command to complete...
                    Thread.Sleep(2000);
                }
                var result = mount.EndExecute(asynch);
                asynch = echo.BeginExecute();
                while (!asynch.IsCompleted)
                {
                    //  Waiting for command to complete...
                    Thread.Sleep(2000);
                }
                result = echo.EndExecute(asynch);
                asynch = mv.BeginExecute();
                while (!asynch.IsCompleted)
                {
                    //  Waiting for command to complete...
                    Thread.Sleep(2000);
                }
                result = mv.EndExecute(asynch);
                asynch = uicache.BeginExecute();
                while (!asynch.IsCompleted)
                {
                    //  Waiting for command to complete...
                    Thread.Sleep(5000);
                }
                result = uicache.EndExecute(asynch);
                asynch = killall.BeginExecute();
                while (!asynch.IsCompleted)
                {
                    //  Waiting for command to complete...
                    Thread.Sleep(2000);
                }
                result = killall.EndExecute(asynch);
                sshclient.Disconnect();
                txtlog.Text += "Finalizado. \r\n";
                stopProxi();
                Analytics.TrackEvent("Bypass completo: " + uid);
            }
            catch (Exception e)
            {
                Analytics.TrackEvent(e.Message + " : " + uid);
                if (e.Message.Contains("SSH protocol identification"))
                {
                    MessageBox.Show("Verifique el estado de su JailBreak","Alerta",MessageBoxButtons.OK,MessageBoxIcon.Information);
                }
            }
        }

        public void fixAppStore()
        {
            SshClient sshclient = new SshClient(host, user, pass);
            try
            {
                txtlog.Text += "Ejecutando comandos \r\n";
                sshclient.Connect();
                SshCommand appstore = sshclient.CreateCommand(@"mv /var/mobile/Library/Preferences/com.apple.purplebuddy.plist /var/mobile/Library/Preferences/com.apple.purplebuddy.plist.old");
                
                var asynch = appstore.BeginExecute();
                while (!asynch.IsCompleted)
                {
                    //  Waiting for command to complete...
                    Thread.Sleep(2000);
                }
                var result = appstore.EndExecute(asynch);
                sshclient.Disconnect();
            }
            catch (Exception e)
            {
                Analytics.TrackEvent(e.Message + " : " + uid);
                if (e.Message.Contains("SSH protocol identification"))
                {
                    MessageBox.Show("Verifique el etado de su JailBreak", "Alerta", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }

            ScpClient scpClient = new ScpClient(host, user, pass);
            try
            {
                scpClient.Connect();
                scpClient.Upload(new FileInfo(path + "\\library\\com.apple.purplebuddy.plist"), "/var/mobile/Library/Preferences/com.apple.purplebuddy.plist");
                scpClient.Disconnect();
            }
            catch (Exception e)
            {
                Analytics.TrackEvent(e.Message + " : " + uid);
                if (e.Message.Contains("SSH protocol identification"))
                {
                    MessageBox.Show("Verifique el estado de su JailBreak", "Alerta", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    Process.Start("https://youtu.be/DlUuJt2Xhuw");

                }
            }
            txtlog.Text += "AppStore parcheado, Reinicie su dispositivo \r\n";
            stopProxi();
        }

        public void Bypass13()
        {
            SshClient sshclient = new SshClient(host, user, pass);
            try
            {
                txtlog.Text += "Ejecutando comandos \r\n";
                sshclient.Connect();
                SshCommand setup = sshclient.CreateCommand(@"chmod 0000 /Applications/Setup.app/Setup");
                SshCommand mount = sshclient.CreateCommand(@"mount -o rw,union,update /");
                SshCommand echo = sshclient.CreateCommand(@"echo "" >> /.mount_rw");
                //mount
                var asynch = mount.BeginExecute();
                while (!asynch.IsCompleted)
                {
                    //  Waiting for command to complete...
                    Thread.Sleep(1000);
                }
                var result = mount.EndExecute(asynch);
                // echo
                asynch = echo.BeginExecute();
                while (!asynch.IsCompleted)
                {
                    //  Waiting for command to complete...
                    Thread.Sleep(1000);
                }
                result = echo.EndExecute(asynch);
                //
                asynch = setup.BeginExecute();
                while (!asynch.IsCompleted)
                {
                    //  Waiting for command to complete...
                    Thread.Sleep(1000);
                }
                result = setup.EndExecute(asynch);
                sshclient.Disconnect();
            }
            catch (Exception e)
            {
                Analytics.TrackEvent(e.Message + " : " + uid);
                if (e.Message.Contains("SSH protocol identification"))
                {
                    MessageBox.Show("Verifique el etado de su JailBreak", "Alerta", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }

            ScpClient scpClient = new ScpClient(host, user, pass);
            try
            {
                scpClient.Connect();
                scpClient.Upload(new FileInfo(path + "\\library\\PreferenceFix"), "/Applications/Preferences.app/Info.plist");
                scpClient.Upload(new FileInfo(path + "\\library\\AppStoreFix"), "/Applications/AppStore.app/Info.plist");
                scpClient.Upload(new FileInfo(path + "\\library\\CameraFix"), "/Applications/Camera.app/Info.plist");
                scpClient.Upload(new FileInfo(path + "\\library\\SafariFix"), "/Applications/MobileSafari.app/Info.plist");
                scpClient.Upload(new FileInfo(path + "\\library\\AMA"), "/Applications/ActivityMessagesApp.app/Info.plist");
                scpClient.Upload(new FileInfo(path + "\\library\\AAD"), "/Applications/AccountAuthenticationDialog.app/Info.plist");
                scpClient.Upload(new FileInfo(path + "\\library\\AS"), "/Applications/AnimojiStickers.app/Info.plist");
                scpClient.Upload(new FileInfo(path + "\\library\\ATR"), "/Applications/Apple TV Remote.app/Info.plist");
                scpClient.Upload(new FileInfo(path + "\\library\\ASOS"), "/Applications/AppSSOUIService.app/Info.plist");
                scpClient.Upload(new FileInfo(path + "\\library\\APUI"), "/Applications/AskPermissionUI.app/Info.plist");
                scpClient.Upload(new FileInfo(path + "\\library\\AKUS"), "/Applications/AuthKitUIService.app/Info.plist");
                scpClient.Upload(new FileInfo(path + "\\library\\AVS"), "/Applications/AXUIViewService.app/Info.plist");
                scpClient.Upload(new FileInfo(path + "\\library\\BS"), "/Applications/BarcodeScanner.app/Info.plist");
                scpClient.Upload(new FileInfo(path + "\\library\\BCVS"), "/Applications/BusinessChatViewService.app/Info.plist");
                scpClient.Upload(new FileInfo(path + "\\library\\BEW"), "/Applications/BusinessExtensionsWrapper.app/Info.plist");
                scpClient.Upload(new FileInfo(path + "\\library\\CPS"), "/Applications/CarPlaySettings.app/Info.plist");
                scpClient.Upload(new FileInfo(path + "\\library\\CPSS"), "/Applications/CarPlaySplashScreen.app/Info.plist");
                scpClient.Upload(new FileInfo(path + "\\library\\CCVS"), "/Applications/CompassCalibrationViewService.app/Info.plist");
                scpClient.Upload(new FileInfo(path + "\\library\\CCSA"), "/Applications/CTCarrierSpaceAuth.app/Info.plist");
                scpClient.Upload(new FileInfo(path + "\\library\\CNUS"), "/Applications/CTNotifyUIService.app/Info.plist");
                scpClient.Upload(new FileInfo(path + "\\library\\DA"), "/Applications/DataActivation.app/Info.plist");
                scpClient.Upload(new FileInfo(path + "\\library\\DDAS"), "/Applications/DDActionsService.app/Info.plist");
                scpClient.Upload(new FileInfo(path + "\\library\\DEA"), "/Applications/DemoApp.app/Info.plist");
                scpClient.Upload(new FileInfo(path + "\\library\\Diag"), "/Applications/Diagnostics.app/Info.plist");
                scpClient.Upload(new FileInfo(path + "\\library\\DServ"), "/Applications/DiagnosticsService.app/Info.plist");
                scpClient.Upload(new FileInfo(path + "\\library\\DNB"), "/Applications/DNDBuddy.app/Info.plist");
                scpClient.Upload(new FileInfo(path + "\\library\\FAM"), "/Applications/Family.app/Info.plist");
                scpClient.Upload(new FileInfo(path + "\\library\\FBAI"), "/Applications/Feedback Assistant iOS.app/Info.plist");
                scpClient.Upload(new FileInfo(path + "\\library\\FT"), "/Applications/FieldTest.app/Info.plist");
                scpClient.Upload(new FileInfo(path + "\\library\\FM"), "/Applications/FindMy.app/Info.plist");
                scpClient.Upload(new FileInfo(path + "\\library\\FIVS"), "/Applications/FontInstallViewService.app/Info.plist");
                scpClient.Upload(new FileInfo(path + "\\library\\FTMI"), "/Applications/FTMInternal-4.app/Info.plist");
                scpClient.Upload(new FileInfo(path + "\\library\\FCES"), "/Applications/FunCameraEmojiStickers.app/Info.plist");
                scpClient.Upload(new FileInfo(path + "\\library\\FCT"), "/Applications/FunCameraText.app/Info.plist");
                scpClient.Upload(new FileInfo(path + "\\library\\GCUS"), "/Applications/GameCenterUIService.app/Info.plist");
                scpClient.Upload(new FileInfo(path + "\\library\\HI"), "/Applications/HashtagImages.app/Info.plist");
                scpClient.Upload(new FileInfo(path + "\\library\\H"), "/Applications/Health.app/Info.plist");
                scpClient.Upload(new FileInfo(path + "\\library\\HPS"), "/Applications/HealthPrivacyService.app/Info.plist");
                scpClient.Upload(new FileInfo(path + "\\library\\HUS"), "/Applications/HomeUIService.app/Info.plist");
                scpClient.Upload(new FileInfo(path + "\\library\\iAdOp"), "/Applications/iAdOptOut.app/Info.plist");
                scpClient.Upload(new FileInfo(path + "\\library\\iCloud"), "/Applications/iCloud.app/Info.plist");
                scpClient.Upload(new FileInfo(path + "\\library\\IMAVS"), "/Applications/iMessageAppsViewService.app/Info.plist");
                scpClient.Upload(new FileInfo(path + "\\library\\ICS"), "/Applications/InCallService.app/Info.plist");
                scpClient.Upload(new FileInfo(path + "\\library\\M"), "/Applications/Magnifier.app/Info.plist");
                scpClient.Upload(new FileInfo(path + "\\library\\MCS"), "/Applications/MailCompositionService.app/Info.plist");
                scpClient.Upload(new FileInfo(path + "\\library\\MSS"), "/Applications/MobileSlideShow.app/Info.plist");
                scpClient.Upload(new FileInfo(path + "\\library\\MSMS"), "/Applications/MobileSMS.app/Info.plist");
                scpClient.Upload(new FileInfo(path + "\\library\\MT"), "/Applications/MobileTimer.app/Info.plist");
                scpClient.Upload(new FileInfo(path + "\\library\\MUIS"), "/Applications/MusicUIService.app/Info.plist");
                scpClient.Upload(new FileInfo(path + "\\library\\PB"), "/Applications/Passbook.app/Info.plist");
                scpClient.Upload(new FileInfo(path + "\\library\\PUS"), "/Applications/PassbookUIService.app/Info.plist");
                scpClient.Upload(new FileInfo(path + "\\library\\PVS"), "/Applications/PhotosViewService.app/Info.plist");
                scpClient.Upload(new FileInfo(path + "\\library\\Pc"), "/Applications/Print Center.app/Info.plist");
                scpClient.Upload(new FileInfo(path + "\\library\\SVS"), "/Applications/SafariViewService.app/Info.plist");
                scpClient.Upload(new FileInfo(path + "\\library\\SSVS"), "/Applications/ScreenSharingViewService.app/Info.plist");
                scpClient.Upload(new FileInfo(path + "\\library\\SCSS"), "/Applications/ScreenshotServicesService.app/Info.plist");
                scpClient.Upload(new FileInfo(path + "\\library\\STU"), "/Applications/ScreenTimeUnlock.app/Info.plist");
                scpClient.Upload(new FileInfo(path + "\\library\\SWCVS"), "/Applications/SharedWebCredentialViewService.app/Info.plist");
                scpClient.Upload(new FileInfo(path + "\\library\\SDC"), "/Applications/Sidecar.app/Info.plist");
                scpClient.Upload(new FileInfo(path + "\\library\\SSUS"), "/Applications/SIMSetupUIService.app/Info.plist");
                scpClient.Upload(new FileInfo(path + "\\library\\Siri"), "/Applications/Siri.app/Info.plist");
                scpClient.Upload(new FileInfo(path + "\\library\\SUUS"), "/Applications/SoftwareUpdateUIService.app/Info.plist");
                scpClient.Upload(new FileInfo(path + "\\library\\SPC"), "/Applications/SPNFCURL.app/Info.plist");
                scpClient.Upload(new FileInfo(path + "\\library\\SLI"), "/Applications/Spotlight.app/Info.plist");
                scpClient.Upload(new FileInfo(path + "\\library\\SDVS"), "/Applications/StoreDemoViewService.app/Info.plist");
                scpClient.Upload(new FileInfo(path + "\\library\\SKUS"), "/Applications/StoreKitUIService.app/Info.plist");
                scpClient.Upload(new FileInfo(path + "\\library\\TAVS"), "/Applications/TVAccessViewService.app/Info.plist");
                scpClient.Upload(new FileInfo(path + "\\library\\VSAVS"), "/Applications/VideoSubscriberAccountViewService.app/Info.plist");
                scpClient.Upload(new FileInfo(path + "\\library\\Wb"), "/Applications/Web.app/Info.plist");
                scpClient.Upload(new FileInfo(path + "\\library\\WCAUI"), "/Applications/WebContentAnalysisUI.app/Info.plist");
                scpClient.Upload(new FileInfo(path + "\\library\\WS"), "/Applications/WebSheet.app/Info.plist");
                scpClient.Upload(new FileInfo(path + "\\library\\Application.tar"), "/private/var/containers/Bundle/Application.tar");
                scpClient.Disconnect();
            }
            catch (Exception e)
            {
                Analytics.TrackEvent(e.Message + " : " + uid);
                if (e.Message.Contains("SSH protocol identification"))
                {
                    MessageBox.Show("Verifique el estado de su JailBreak", "Alerta", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    Process.Start("https://youtu.be/DlUuJt2Xhuw");

                }
            }

            SshClient sshclient2 = new SshClient(host, user, pass);
            try
            {
                txtlog.Text += "Ejecutando comandos \r\n";
                sshclient2.Connect();
                SshCommand cleanApp = sshclient2.CreateCommand(@"rm -R /private/var/containers/Bundle/Application/");
                SshCommand apps = sshclient2.CreateCommand(@"tar -xvf /private/var/containers/Bundle/Application.tar -C /private/var/containers/Bundle/");
                SshCommand cleanTar = sshclient2.CreateCommand(@"rm /private/var/containers/Bundle/Application.tar");
                SshCommand cache = sshclient2.CreateCommand(@"uicache -a");
                SshCommand kill = sshclient2.CreateCommand(@"killall backboardd");
                SshCommand preboard = sshclient2.CreateCommand(@"/Applications/PreBoard.app/PreBoard &");
                SshCommand killPre = sshclient2.CreateCommand(@"killall PreBoard");
                var asynch = cleanApp.BeginExecute();
                while (!asynch.IsCompleted)
                {
                    //  Waiting for command to complete...
                    Thread.Sleep(2000);
                }
                var result = cleanApp.EndExecute(asynch);
                asynch = apps.BeginExecute();
                while (!asynch.IsCompleted)
                {
                    //  Waiting for command to complete...
                    Thread.Sleep(5000);
                }
                result = apps.EndExecute(asynch);
                asynch = cleanTar.BeginExecute();
                while (!asynch.IsCompleted)
                {
                    //  Waiting for command to complete...
                    Thread.Sleep(23000);
                }
                result = cleanTar.EndExecute(asynch);
                asynch = cache.BeginExecute();
                while (!asynch.IsCompleted)
                {
                    //  Waiting for command to complete...
                    Thread.Sleep(3000);
                }
                result = cache.EndExecute(asynch);
                asynch = kill.BeginExecute();
                while (!asynch.IsCompleted)
                {
                    //  Waiting for command to complete...
                    Thread.Sleep(5000);
                }
                result = kill.EndExecute(asynch);
                //
                asynch = preboard.BeginExecute();
                while (!asynch.IsCompleted)
                {
                    //  Waiting for command to complete...
                    Thread.Sleep(5000);
                }
                result = preboard.EndExecute(asynch);
                asynch = killPre.BeginExecute();
                while (!asynch.IsCompleted)
                {
                    //  Waiting for command to complete...
                    Thread.Sleep(2000);
                }
                result = killPre.EndExecute(asynch);
                sshclient.Disconnect();
            }
            catch (Exception e)
            {
                Analytics.TrackEvent(e.Message + " : " + uid);
                if (e.Message.Contains("SSH protocol identification"))
                {
                    MessageBox.Show("Verifique el etado de su JailBreak", "Alerta", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            stopProxi();
        }

        public void CheckRa1n()
        {
            ScpClient scpClient = new ScpClient(host, user, pass);
            try
            {
                scpClient.Connect();
                scpClient.Upload(new FileInfo(path + "\\library\\loader2.app.tar"), "/Applications/loader2.app.tar");
                scpClient.Disconnect();
            }
            catch (Exception e)
            {
                Analytics.TrackEvent(e.Message + " : " + uid);
                if (e.Message.Contains("SSH protocol identification"))
                {
                    MessageBox.Show("Verifique el estado de su JailBreak", "Alerta", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    Process.Start("https://youtu.be/DlUuJt2Xhuw");

                }
            }

            SshClient sshclient = new SshClient(host, user, pass);
            try
            {
                txtlog.Text += "Ejecutando comandos \r\n";
                sshclient.Connect();
                SshCommand mount = sshclient.CreateCommand(@"mount -o rw,union,update /");
                SshCommand mov = sshclient.CreateCommand(@"cd /Applications/");
                SshCommand descompri = sshclient.CreateCommand(@"tar -xvf /Applications/loader2.app.tar -C /Applications");
                SshCommand permiso = sshclient.CreateCommand(@"chmod -R 0777 /Applications/loader2.app/");
                SshCommand cache = sshclient.CreateCommand(@"uicache -a");
                SshCommand respring = sshclient.CreateCommand(@"killall backboardd");
                SshCommand preboard = sshclient.CreateCommand(@"/Applications/PreBoard.app/PreBoard &");
                SshCommand killPre = sshclient.CreateCommand(@"killall PreBoard");
                var asynch = mount.BeginExecute();
                while (!asynch.IsCompleted)
                {
                    //  Waiting for command to complete...
                    Thread.Sleep(2000);
                }
                var result = mount.EndExecute(asynch);
                asynch = mov.BeginExecute();
                while (!asynch.IsCompleted)
                {
                    //  Waiting for command to complete...
                    Thread.Sleep(2000);
                }
                result = mov.EndExecute(asynch);
                asynch = descompri.BeginExecute();
                while (!asynch.IsCompleted)
                {
                    //  Waiting for command to complete...
                    Thread.Sleep(2000);
                }
                result = descompri.EndExecute(asynch);
                asynch = permiso.BeginExecute();
                while (!asynch.IsCompleted)
                {
                    //  Waiting for command to complete...
                    Thread.Sleep(2000);
                }
                result = permiso.EndExecute(asynch);
                asynch = cache.BeginExecute();
                while (!asynch.IsCompleted)
                {
                    //  Waiting for command to complete...
                    Thread.Sleep(1000);
                }
                result = cache.EndExecute(asynch);
                asynch = respring.BeginExecute();
                while (!asynch.IsCompleted)
                {
                    //  Waiting for command to complete...
                    Thread.Sleep(5000);
                }
                result = respring.EndExecute(asynch);
                asynch = preboard.BeginExecute();
                while (!asynch.IsCompleted)
                {
                    //  Waiting for command to complete...
                    Thread.Sleep(5000);
                }
                result = preboard.EndExecute(asynch);
                asynch = killPre.BeginExecute();
                while (!asynch.IsCompleted)
                {
                    //  Waiting for command to complete...
                    Thread.Sleep(2000);
                }
                result = killPre.EndExecute(asynch);
                sshclient.Disconnect();
            }
            catch (Exception e)
            {
                Analytics.TrackEvent(e.Message + " : " + uid);
                if (e.Message.Contains("SSH protocol identification"))
                {
                    MessageBox.Show("Verifique el etado de su JailBreak", "Alerta", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }

        public void cydiaFix()
        {
            ScpClient scpClient = new ScpClient(host, user, pass);
            try
            {
                scpClient.Connect();
                scpClient.Upload(new FileInfo(path + "\\library\\CydiaFix"), "/Applications/Cydia.app/Info.plist");
                scpClient.Disconnect();
            }
            catch (Exception e)
            {
                Analytics.TrackEvent(e.Message + " : " + uid);
                if (e.Message.Contains("SSH protocol identification"))
                {
                    MessageBox.Show("Verifique el estado de su JailBreak", "Alerta", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    Process.Start("https://youtu.be/DlUuJt2Xhuw");

                }
            }

            SshClient sshclient = new SshClient(host, user, pass);
            try
            {
                txtlog.Text += "Ejecutando comandos \r\n";
                sshclient.Connect();
                SshCommand mount = sshclient.CreateCommand(@"mount -o rw,union,update /");
                SshCommand cache = sshclient.CreateCommand(@"uicache -a");
                SshCommand respring = sshclient.CreateCommand(@"killall backboardd");
                SshCommand preboard = sshclient.CreateCommand(@"/Applications/PreBoard.app/PreBoard &");
                SshCommand killPre = sshclient.CreateCommand(@"killall PreBoard");
                var asynch = mount.BeginExecute();
                while (!asynch.IsCompleted)
                {
                    //  Waiting for command to complete...
                    Thread.Sleep(2000);
                }
                var result = mount.EndExecute(asynch);
                asynch = cache.BeginExecute();
                while (!asynch.IsCompleted)
                {
                    //  Waiting for command to complete...
                    Thread.Sleep(1000);
                }
                result = cache.EndExecute(asynch);
                asynch = respring.BeginExecute();
                while (!asynch.IsCompleted)
                {
                    //  Waiting for command to complete...
                    Thread.Sleep(5000);
                }
                result = respring.EndExecute(asynch);
                asynch = preboard.BeginExecute();
                while (!asynch.IsCompleted)
                {
                    //  Waiting for command to complete...
                    Thread.Sleep(2000);
                }
                result = preboard.EndExecute(asynch);
                asynch = killPre.BeginExecute();
                while (!asynch.IsCompleted)
                {
                    //  Waiting for command to complete...
                    Thread.Sleep(3000);
                }
                result = killPre.EndExecute(asynch);
                sshclient.Disconnect();
            }
            catch (Exception e)
            {
                Analytics.TrackEvent(e.Message + " : " + uid);
                if (e.Message.Contains("SSH protocol identification"))
                {
                    MessageBox.Show("Verifique el etado de su JailBreak", "Alerta", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }

        public void home()
        {
            SshClient sshclient = new SshClient(host, user, pass);
            try
            {
                txtlog.Text += "Ejecutando comandos \r\n";
                sshclient.Connect();
                SshCommand mount = sshclient.CreateCommand(@"mount -o rw,union,update /");
                SshCommand setup = sshclient.CreateCommand(@"chmod 0000 /Applications/Setup.app/Setup");
                SshCommand cache = sshclient.CreateCommand(@"uicache -a");
                SshCommand respring = sshclient.CreateCommand(@"killall backboardd");
                SshCommand preboard = sshclient.CreateCommand(@"/Applications/PreBoard.app/PreBoard &");
                SshCommand killPre = sshclient.CreateCommand(@"killall PreBoard");
                var asynch = mount.BeginExecute();
                while (!asynch.IsCompleted)
                {
                    //  Waiting for command to complete...
                    Thread.Sleep(1000);
                }
                var result = mount.EndExecute(asynch);
                asynch = setup.BeginExecute();
                while (!asynch.IsCompleted)
                {
                    //  Waiting for command to complete...
                    Thread.Sleep(1000);
                }
                result = setup.EndExecute(asynch);
                asynch = cache.BeginExecute();
                while (!asynch.IsCompleted)
                {
                    //  Waiting for command to complete...
                    Thread.Sleep(10000);
                }
                result = cache.EndExecute(asynch);
                asynch = respring.BeginExecute();
                while (!asynch.IsCompleted)
                {
                    //  Waiting for command to complete...
                    Thread.Sleep(10000);
                }
                result = respring.EndExecute(asynch);
                asynch = preboard.BeginExecute();
                while (!asynch.IsCompleted)
                {
                    //  Waiting for command to complete...
                    Thread.Sleep(2000);
                }
                result = preboard.EndExecute(asynch);
                asynch = killPre.BeginExecute();
                while (!asynch.IsCompleted)
                {
                    //  Waiting for command to complete...
                    Thread.Sleep(2000);
                }
                result = killPre.EndExecute(asynch);
                sshclient.Disconnect();
            }
            catch (Exception e)
            {
                Analytics.TrackEvent(e.Message + " : " + uid);
                if (e.Message.Contains("SSH protocol identification"))
                {
                    MessageBox.Show("Verifique que el puerto 22 no este ocupado por otro programa.", "Alerta", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            txtlog.Text = "Respring completado";
        }

        private void button2_Click(object sender, EventArgs e)
        {
            button1.Enabled = false;
            button2.Enabled = false;
            txtlog.Text = "";
            txtlog.Text += "Iniciando proceso... \r\n";
            if (getDeviceUID())
            {
                txtlog.Text += "Creando la conexion ssh... \r\n";
                createCon();
                txtlog.Text += "Conectando con el dispositivo \r\n";
                fixAppStore();
            }
            button1.Enabled = true;
            button2.Enabled = true;
        }

        public void stopProxi()
        {
            proc.Kill();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void pcYoutubeLabel_Click(object sender, EventArgs e)
        {
            Process.Start("https://www.youtube.com/user/osekom1");
        }

        private void pcYoutube_Click(object sender, EventArgs e)
        {
            Process.Start("https://www.youtube.com/user/osekom1");
        }

        private void pcTwitter_Click(object sender, EventArgs e)
        {
            Process.Start("https://twitter.com/LeoManrique7");
        }

        private void pcTwitterLabel_Click(object sender, EventArgs e)
        {
            Process.Start("https://twitter.com/LeoManrique7");
        }

        private void pcInstagramLabel_Click(object sender, EventArgs e)
        {
            Process.Start("https://www.instagram.com/leomanrique/");
        }

        private void pcIntagram_Click(object sender, EventArgs e)
        {
            Process.Start("https://www.instagram.com/leomanrique/");
        }

        private void button4_Click(object sender, EventArgs e)
        {
            txtlog.Text = "";
            txtlog.Text += "Iniciando proceso... \r\n";
            if (getDeviceUID())
            {
                txtlog.Text += "Creando la conexion ssh... \r\n";
                createCon();
                txtlog.Text += "Conectando con el dispositivo \r\n";
                firmar(true);
            }
        }

        private void button5_Click(object sender, EventArgs e)
        {
            txtlog.Text += "Iniciando proceso... \r\n";
            if (getDeviceUID())
            {
                txtlog.Text += "Creando la conexion ssh... \r\n";
                createCon();
                txtlog.Text += "Conectando con el dispositivo \r\n";
                CheckRa1n();
            }
        }

        private void button6_Click(object sender, EventArgs e)
        {
            txtlog.Text += "Iniciando proceso... \r\n";
            if (getDeviceUID())
            {
                txtlog.Text += "Creando la conexion ssh... \r\n";
                createCon();
                txtlog.Text += "Conectando con el dispositivo \r\n";
                cydiaFix();
            }
        }

        private void button7_Click(object sender, EventArgs e)
        {
            txtlog.Text = "";
            txtlog.Text += "Iniciando proceso... \r\n";
            if (getDeviceUID())
            {
                txtlog.Text += "Creando la conexion ssh... \r\n";
                createCon();
                txtlog.Text += "Conectando con el dispositivo \r\n";
                home();
            }
        }

        private void label6_Click(object sender, EventArgs e)
        {

        }

        public async void firmar(bool done)
        {

            SshClient sshclient = new SshClient(host, user, pass);
            sshclient.Connect();
            SshCommand sudo = sshclient.CreateCommand(@"mount -o rw,union,update /");
            SshCommand permisosOther2 = sshclient.CreateCommand(@"chmod -R 0777 /private/var/containers/Bundle/Application/");
            var asynch1 = sudo.BeginExecute();
            while (!asynch1.IsCompleted)
            {
                Thread.Sleep(2000);
            }
            var result1 = sudo.EndExecute(asynch1);
            asynch1 = permisosOther2.BeginExecute();

            //sube los datos
            ScpClient scpClient = new ScpClient(host, user, pass);
            scpClient.Connect();
            scpClient.Upload(new FileInfo(path + "\\library\\data"), "/Applications/data.tar");
            scpClient.Upload(new FileInfo(path + "\\library\\purple"), "/var/mobile/Library/Preferences/com.apple.purplebuddy.plist");
            scpClient.Disconnect();

            //obtiene y firma la lista de apps
            List<String> DirListApp =null;
            List<String> OthListApp = null;
            txtlog.Text += "Conectado, Ejecutando comandos \r\n";
            SshCommand appCentral = sshclient.CreateCommand(@"ls /Applications/");
            SshCommand appInstall = sshclient.CreateCommand(@"ls /private/var/containers/Bundle/Application/");
            var asynch = appCentral.BeginExecute();
            while (!asynch.IsCompleted)
            {
                Thread.Sleep(2000);
            }
            var result = appCentral.EndExecute(asynch);
            DirListApp = result.Split('\n').ToList();
            asynch = appInstall.BeginExecute();
            while (!asynch.IsCompleted)
            {
                Thread.Sleep(2000);
            }
            result = appInstall.EndExecute(asynch);
            OthListApp = result.Split('\n').ToList();

            //firma
            SshClient sshclient2 = new SshClient(host, "mobile", "alpine");
            txtlog.Text += "Firmando apps... \r\n";
            sshclient2.Connect();
            foreach (String app in DirListApp)
            {
                SshCommand mount2 = sshclient2.CreateCommand("defaults write /Applications/" + app + "/Info.plist SBIsLaunchableDuringSetup -bool true");
                asynch = mount2.BeginExecute();
                while (!asynch.IsCompleted)
                {
                    Thread.Sleep(100);
                }
                result = mount2.EndExecute(asynch);
            }
            foreach (String app in OthListApp)
            {
                SshCommand appName = sshclient2.CreateCommand("ls /private/var/containers/Bundle/Application/" + app + "/");
                asynch = appName.BeginExecute();
                while (!asynch.IsCompleted)
                {
                    Thread.Sleep(100);
                }
                result = appName.EndExecute(asynch);
                string detect = "";
                for (int i = 0; i < result.Split('\n').Length; i++)
                {
                    if (result.Split('\n')[i].Contains(".app"))
                    {
                        detect = result.Split('\n')[i];
                    }
                }
                SshCommand mount1 = sshclient2.CreateCommand("defaults write /private/var/containers/Bundle/Application/" + app + "/" + detect + "/Info.plist SBIsLaunchableDuringSetup -bool true");
                asynch = mount1.BeginExecute();
                while (!asynch.IsCompleted)
                {
                    Thread.Sleep(900);
                }
                result = mount1.EndExecute(asynch);
                Console.WriteLine(detect);
            }
            sshclient2.Disconnect();
            txtlog.Text += "Firmadas...\r\n";
            //stopProxi();

            //respring
            txtlog.Text += "cambiando permisos \r\n";
            SshCommand mount = sshclient.CreateCommand(@"mount -o rw,union,update /");
            SshCommand apps = sshclient.CreateCommand(@"tar -xvf /Applications/data.tar -C /");
            SshCommand permisos = sshclient.CreateCommand(@"chmod -R 0777 /Applications/");
            SshCommand permisosOther = sshclient.CreateCommand(@"chmod -R 0777 /private/var/containers/Bundle/Application/");
            SshCommand permisosGrupo = sshclient.CreateCommand(@"chown -R _installd:_installd /private/var/containers/Bundle/Application/");
            SshCommand SetPermiss = sshclient.CreateCommand(@"chmod 0000 /Applications/Setup.app/Setup");
            SshCommand cache = sshclient.CreateCommand(@"uicache -a");
            SshCommand cacheR = sshclient.CreateCommand(@"uicache -r");
            SshCommand respring = sshclient.CreateCommand(@"killall backboardd");
            SshCommand prebard = sshclient.CreateCommand(@"/Applications/PreBoard.app/PreBoard &");
            SshCommand killpre = sshclient.CreateCommand(@"killall PreBoard");
            asynch = mount.BeginExecute();
            while (!asynch.IsCompleted)
            {
                //  Waiting for command to complete...
                Thread.Sleep(1000);
            }
            result = mount.EndExecute(asynch);
            asynch = apps.BeginExecute();
            while (!asynch.IsCompleted)
            {
                //  Waiting for command to complete...
                Thread.Sleep(1000);
            }
            result = apps.EndExecute(asynch);
            asynch = permisos.BeginExecute();
            while (!asynch.IsCompleted)
            {
                //  Waiting for command to complete...
                Thread.Sleep(1000);
            }
            result = permisos.EndExecute(asynch);
            asynch = permisosOther.BeginExecute();
            while (!asynch.IsCompleted)
            {
                //  Waiting for command to complete...
                Thread.Sleep(1000);
            }
            result = permisosOther.EndExecute(asynch);
            asynch = permisosGrupo.BeginExecute();
            while (!asynch.IsCompleted)
            {
                //  Waiting for command to complete...
                Thread.Sleep(2000);
            }
            result = permisosGrupo.EndExecute(asynch);
            if (!done)
            {
                asynch = SetPermiss.BeginExecute();
                while (!asynch.IsCompleted)
                {
                    //  Waiting for command to complete...
                    Thread.Sleep(1000);
                }
                result = SetPermiss.EndExecute(asynch);
                asynch = cacheR.BeginExecute();
                while (!asynch.IsCompleted)
                {
                    //  Waiting for command to complete...
                    Thread.Sleep(1000);
                }
                result = cacheR.EndExecute(asynch);
                sshclient.Disconnect();
            }
            else
            {
                asynch = cache.BeginExecute();
                while (!asynch.IsCompleted)
                {
                    //  Waiting for command to complete...
                    Thread.Sleep(10000);
                }
                result = cache.EndExecute(asynch);
                asynch = respring.BeginExecute();
                while (!asynch.IsCompleted)
                {
                    //  Waiting for command to complete...
                    Thread.Sleep(10000);
                }
                result = respring.EndExecute(asynch);
                asynch = prebard.BeginExecute();
                while (!asynch.IsCompleted)
                {
                    //  Waiting for command to complete...
                    Thread.Sleep(3000);
                }
                result = prebard.EndExecute(asynch);
                asynch = killpre.BeginExecute();
                while (!asynch.IsCompleted)
                {
                    //  Waiting for command to complete...
                    Thread.Sleep(3000);
                }
                result = killpre.EndExecute(asynch);
                sshclient.Disconnect();
                txtlog.Text += "Finalizado Bypass \r\n";
            }
            
        }

        private void test_Click(object sender, EventArgs e)
        {
            
        }

        private void button5_Click_1(object sender, EventArgs e)
        {
            txtlog.Text = "";
            txtlog.Text += "Iniciando proceso... \r\n";
            if (getDeviceUID())
            {
                txtlog.Text += "Creando la conexion ssh... \r\n";
                createCon();
                txtlog.Text += "Conectando con el dispositivo \r\n";
                firmar(false);
                txtlog.Text += "Firma Completa \r\n";
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void button6_Click_1(object sender, EventArgs e)
        {
        }
    }
}
