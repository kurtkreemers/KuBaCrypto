using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Xml.Linq;
using System.Xml.XPath;

namespace KuBaCrypto
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        DispatcherTimer timer = new DispatcherTimer();
        string dirPath = KuBaCrypto.Properties.Resources.MainDir;
        Int64 fileLength = 0;
        private string publicKey;
        private string privateKey;
        private string FilePath;
        private string encryptedFilePath;
        private string AESFilePath;
        private string HASHFilePath;
        private string steganoFileName;
        private string steganoOfdTitle;
        private Bitmap bmpImage;
        private Bitmap bmpHidden;
        byte[] encryptionKey;
        byte[] encryptionIV;
        XElement aesKeyElement;
        XElement aesIvElement;
        XElement hashCodeElement;

        public MainWindow()
        {
            InitializeComponent();
            timer.Interval = TimeSpan.FromMilliseconds(500);
            timer.Tick += timer_Tick;
            timer.IsEnabled = true;
            //Generate encryptionkey at startup
            encryptionKey = Encipher.GenerateRandom(32);
            
        }

        void timer_Tick(object sender, EventArgs e)
        {
            if (!Layout.CheckFile(btCreateKeysA.Tag.ToString(), dirPath))
            {
                btCreateKeysA.IsEnabled = true;
                btCreateKeysA.ToolTip = null;
            }
            else
            {
                btCreateKeysA.IsEnabled = false;
                btCreateKeysA.ToolTip = "Keys already exist at " + dirPath;
            }
            if (!Layout.CheckFile(btCreateKeysB.Tag.ToString(), dirPath))
            {
                btCreateKeysB.IsEnabled = true;
                btCreateKeysB.ToolTip = null;
            }
            else
            {
                btCreateKeysB.IsEnabled = false;
                btCreateKeysB.ToolTip = "Keys already exist at " + dirPath;
            }

            if (privateKey != null && publicKey != null && (cb_encrypt.IsChecked == true || (cb_decrypt.IsChecked == true && AESFilePath != null && HASHFilePath != null)))
            {
                if ((cb_stegano.IsChecked == false && FilePath != null) || (cb_stegano.IsChecked == true && bmpImage != null && cb_encrypt.IsChecked == true) ||
                    (cb_stegano.IsChecked == true && bmpImage != null && FilePath == null && cb_decrypt.IsChecked == true))
                    btActivate.IsEnabled = true;
                else
                    btActivate.IsEnabled = false;
            }
            else
                btActivate.IsEnabled = false;

            if (cb_encrypt.IsChecked == true || cb_decrypt.IsChecked == true)
            {
                Layout.ButtonEnable(WindowGrid, true);
                if (cb_stegano.IsChecked == true)
                    tb_SteganoFilePath.Visibility = Visibility.Visible;
                else
                    tb_SteganoFilePath.Visibility = Visibility.Hidden;
            }
            else
            {
                Layout.ButtonEnable(WindowGrid, false);
                Layout.LabelClear(WindowGrid);
                tb_SteganoFilePath.Visibility = Visibility.Hidden;
                ResetAll();
            }
            if (cb_decrypt.IsChecked == true && cb_stegano.IsChecked == true)
                Layout.ControlsHidden(WindowGrid, true, "Crypto");
            else
                Layout.ControlsHidden(WindowGrid, false, "Crypto");

        }
        private void cb_encrypt_Checked(object sender, RoutedEventArgs e)
        {
            Lbl_CryptoFileSelect.Content = "Select a file for encryption :";
            Lbl_PriKeySelect.Content = "Select the PrivateKey for signing HashCode :";
            Lbl_PubKeySelect.Content = "Select the PublicKey for encrypting AESKey :";
            btActivate.Content = "Encrypt";
            Layout.ControlsHidden(WindowGrid, true, "AES");
            Layout.ControlsHidden(WindowGrid, true, "HASH");
            cb_decrypt.IsChecked = false;
            steganoOfdTitle = "Select a picture for embedding the encrypted file";
            Status.Content = String.Empty;
            ResetAll();
        }

        private void cb_decrypt_Checked(object sender, RoutedEventArgs e)
        {
            Lbl_CryptoFileSelect.Content = "Select a file for decryption :";
            Lbl_PriKeySelect.Content = "Select the PrivateKey for decrypting AESKey :";
            Lbl_PubKeySelect.Content = "Select the PublicKey for decrypting HashCode :";
            Lbl_AESFileSelect.Content = "Select the Encrypted AESfile for decryption :";
            Lbl_HASHFileSelect.Content = "Select the Signed HASHfile :";
            btActivate.Content = "Decrypt";
            Layout.ControlsHidden(WindowGrid, false, "AES");
            Layout.ControlsHidden(WindowGrid, false, "HASH");
            cb_encrypt.IsChecked = false;
            steganoOfdTitle = "Select a picture for extracting the encrypted file";
            Status.Content = String.Empty;
            ResetAll();
        }

        private void btCreateKeys_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!Directory.Exists(dirPath))
                {
                    Directory.CreateDirectory(dirPath);
                }
                System.Windows.Controls.Button bt = (System.Windows.Controls.Button)sender;
                string fileNamePrivate = String.Empty;
                string fileNamePublic = String.Empty;
                if (bt.Tag.ToString() == "A")
                {
                    fileNamePrivate = "private_A.xml";
                    fileNamePublic = "public_A.xml";
                }
                else if (bt.Tag.ToString() == "B")
                {
                    fileNamePrivate = "private_B.xml";
                    fileNamePublic = "public_B.xml";
                }
                string publicKeyPath = System.IO.Path.Combine(dirPath, fileNamePublic);
                string privateKeyPath = System.IO.Path.Combine(dirPath, fileNamePrivate);

                string publicRSAKey = null;
                string privateRSAKey = null;
                //Create and write the keys
                Encipher.GenerateRSAKeyPair(out publicRSAKey, out privateRSAKey);
                using (StreamWriter sw = File.CreateText(publicKeyPath))
                {
                    sw.Write(publicRSAKey);
                }

                using (StreamWriter sw = File.CreateText(privateKeyPath))
                {
                    sw.Write(privateRSAKey);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            Status.Content= "Keys created at " + KuBaCrypto.Properties.Resources.MainDir;

        }

        private static void CreateAESXml(byte[] encryptionKey, byte[] encryptionIv, string rsaKey, string aesXmlFilePath, string filePath)
        {
            string template = "<DataInfo>" +
                "<Encrypted>True</Encrypted>" +
                "<FileInfo>" +
                "<Name/>" +
                "<Extension/>" +
                "</FileInfo>" +
                "<KeyEncryption algorithm='RSA2048'>" +
                "</KeyEncryption>" +
                "<DataEncryption algorithm='AES256'>" +
                "<AESEncryptedKeyValue>" +
                "<Key/>" +
                "<IV/>" +
                "</AESEncryptedKeyValue>" +
                "</DataEncryption>" +
                "</DataInfo>";

            XDocument doc = XDocument.Parse(template);
            //Write filename and extension in XML
            doc.Descendants("FileInfo").Single().Descendants("Name").Single().Value = System.IO.Path.GetFileNameWithoutExtension(filePath);
            doc.Descendants("FileInfo").Single().Descendants("Extension").Single().Value = System.IO.Path.GetExtension(filePath);
            //Encrypt the keys and write in XML
            doc.Descendants("DataEncryption").Single().Descendants("AESEncryptedKeyValue").Single().Descendants("Key").Single().Value = System.Convert.ToBase64String(Encipher.RSAEncryptBytes(encryptionKey, rsaKey));
            doc.Descendants("DataEncryption").Single().Descendants("AESEncryptedKeyValue").Single().Descendants("IV").Single().Value = System.Convert.ToBase64String(Encipher.RSAEncryptBytes(encryptionIv, rsaKey));

            doc.Save(aesXmlFilePath);
        }

        private static void CreateHASHXml(string hashCodeXMLPath, string rsaKey, string hashXmlFilePath)
        {
            string template = "<DataInfo>" +
                "<Encrypted>True</Encrypted>" +
                "<KeyEncryption algorithm='RSA2048'>" +
                "</KeyEncryption>" +
                "<DataEncryption>" +
                "<HASHEncryptedKeyValue>" +
                "<SHA256/>" +
                "</HASHEncryptedKeyValue>" +
                "</DataEncryption>" +
                "</DataInfo>";

            XDocument doc = XDocument.Parse(template);
            //Calculate the hash, sign it and write in XML
            doc.Descendants("DataEncryption").Single().Descendants("HASHEncryptedKeyValue").Single().Descendants("SHA256").Single().Value = System.Convert.ToBase64String(Encipher.GenerateDigitalSignature(hashCodeXMLPath, rsaKey));

            doc.Save(hashXmlFilePath);
        }

        private void bt_PubFileSelect_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                OpenFileDialog ofd = Layout.OpenXMLFile();
                ofd.Title = "Select a xml file containing a public key";
                ofd.InitialDirectory = KuBaCrypto.Properties.Resources.MainDir.Replace("/",@"\");
                Nullable<bool> result = ofd.ShowDialog(this);
                if (result == true)
                {
                    using (StreamReader sr = File.OpenText(ofd.FileName))
                    {
                        publicKey = sr.ReadToEnd();
                        Encipher.RSAKeyControl(publicKey);
                        tb_PubKeyFilePath.Text = ofd.FileName;
                    }
                }
            }
            catch (Exception ex)
            {
                tb_PubKeyFilePath.Text = null;
                publicKey = null;
                MessageBox.Show(ex.Message);
            }
        }

        private void bt_CryptoFileSelect_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                OpenFileDialog ofd = new OpenFileDialog();
                ofd.Title = "Select a file";
                Nullable<bool> result = ofd.ShowDialog(this);
                if (result == true)
                {
                    tb_CryptoFilePath.Text = ofd.FileName;
                    FilePath = ofd.FileName;
                    fileLength = new FileInfo(FilePath).Length;
                }
                if (bmpImage != null)
                    SteganoSizeCheck();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void bt_PriFileSelect_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                OpenFileDialog ofd = Layout.OpenXMLFile();
                ofd.Title = "Select a xml file containing a private key";
                ofd.InitialDirectory = KuBaCrypto.Properties.Resources.MainDir.Replace("/", @"\");
                Nullable<bool> result = ofd.ShowDialog(this);
                if (result == true)
                {
                    using (StreamReader sr = File.OpenText(ofd.FileName))
                    {
                        privateKey = sr.ReadToEnd();
                        Encipher.RSAKeyControl(privateKey);
                        tb_PriKeyFilePath.Text = ofd.FileName;
                    }
                }
            }
            catch (Exception ex)
            {
                tb_PriKeyFilePath.Text = null;
                privateKey = null;
                MessageBox.Show(ex.Message);
            }
        }
        private void bt_AESFileSelect_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                OpenFileDialog ofd = Layout.OpenXMLFile();
                ofd.Title = "Select a xml file containing the encrypted AESkey";
                Nullable<bool> result = ofd.ShowDialog(this);
                if (result == true)
                {
                    AESFilePath = ofd.FileName;
                    XDocument doc = XDocument.Load(AESFilePath);
                    aesKeyElement = doc.Root.XPathSelectElement("./DataEncryption/AESEncryptedKeyValue/Key");
                    aesIvElement = doc.Root.XPathSelectElement("./DataEncryption/AESEncryptedKeyValue/IV");
                    if (aesKeyElement != null && aesIvElement != null)
                    {
                        tb_AESFilePath.Text = ofd.FileName;
                    }
                    else
                    {
                        aesKeyElement = null;
                        aesIvElement = null;
                        throw new Exception("AESKey or AESIV missing, chosen file not correct!!!");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        private void bt_HASHFileSelect_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                OpenFileDialog ofd = Layout.OpenXMLFile();
                ofd.Title = "Select a xml file containing the HASHCode";
                Nullable<bool> result = ofd.ShowDialog(this);
                if (result == true)
                {
                    HASHFilePath = ofd.FileName;
                    XDocument hashDoc = XDocument.Load(HASHFilePath);
                    hashCodeElement = hashDoc.Root.XPathSelectElement("./DataEncryption/HASHEncryptedKeyValue/SHA256");
                    if (hashCodeElement != null)
                    {
                        tb_HASHFilePath.Text = ofd.FileName;
                    }
                    else
                    {
                        throw new Exception("HashCode missing, chosen file not correct!!!");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        private void btActivate_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (cb_encrypt.IsChecked == true)
                {
                    //Create new Initialisation Vector
                    encryptionIV = Encipher.GenerateRandom(16);
                    var filename = System.IO.Path.GetFileNameWithoutExtension(FilePath);
                    dirPath = dirPath + filename + '_' + DateTime.Now.ToShortTimeString().Replace(':', '_') + '/';
                    if (!Directory.Exists(dirPath))
                    {
                        Directory.CreateDirectory(dirPath);
                    }
                    //Create xml containing encrypted AESkey
                    CreateAESXml(encryptionKey, encryptionIV, publicKey, dirPath + "AESEncrypted.xml", FilePath);
                    //Create xml containing signed Hash
                    CreateHASHXml(FilePath, privateKey, dirPath + "HASHSigned.xml");
                    encryptedFilePath = PathChange(FilePath);
                    Status.Content = "File Encrypting busy...";
                    Dispatcher.Invoke(() => { }, DispatcherPriority.Background); 
                    //Encrypt file
                    Encipher.EncryptFile(FilePath, encryptedFilePath, encryptionKey, encryptionIV);
                    //Put encrypted data in picture
                    if (cb_stegano.IsChecked == true)
                    {
                        Status.Content = "Merging file busy...";
                        Dispatcher.Invoke(() => { }, DispatcherPriority.Background);                       
                        bmpHidden = SteganoGraphy.embedData(encryptedFilePath, bmpImage);
                        //Save file as .png
                        bmpHidden.Save(dirPath + steganoFileName + ".png", System.Drawing.Imaging.ImageFormat.Png);
                    }
                    Status.Content = "Files created at " + dirPath;
                }
                if (cb_decrypt.IsChecked == true)
                {
                    if (cb_stegano.IsChecked == true)
                    {
                        Status.Content = "Extracting file busy...";
                        Dispatcher.Invoke(() => { }, DispatcherPriority.Background);
                        SteganoPathChange();
                        SteganoGraphy.extractData(bmpImage, FilePath);
                        var filename = System.IO.Path.GetFileNameWithoutExtension(FilePath);
                        dirPath = dirPath + filename.Replace("_encrypted", "") + '/';
                    }                    
                    byte[] aesKey = Encipher.RSADecryptBytes(Convert.FromBase64String(aesKeyElement.Value), privateKey);
                    byte[] aesIv = Encipher.RSADecryptBytes(Convert.FromBase64String(aesIvElement.Value), privateKey);
                    encryptedFilePath = PathChange(FilePath);
                    //Decrypting file
                    Encipher.DecryptFile(FilePath, encryptedFilePath, aesKey, aesIv);
                    Status.Content = "Decrypting file busy...";
                    Dispatcher.Invoke(() => { }, DispatcherPriority.Background); 
                    //Create hash
                    byte[] hashCodeSign = Convert.FromBase64String(hashCodeElement.Value);
                    Status.Content = encryptedFilePath + " created";
                    //Compare hash
                    MessageBox.Show(Encipher.VerifyHash(encryptedFilePath, publicKey, hashCodeSign) ? "HashCode correct!!!" : "HashCode not correct!!!");
                   
                }
                ResetAll();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private string PathChange(string uri)
        {
            var fileName = System.IO.Path.GetFileNameWithoutExtension(uri);
            var extension = System.IO.Path.GetExtension(uri);
            var path = String.Empty;
            if (cb_encrypt.IsChecked == true)
                path = dirPath + fileName + "_encrypted" + extension;
            else if (cb_decrypt.IsChecked == true && cb_stegano.IsChecked == false)
                path = dirPath + fileName.Replace("_encrypted", "_decrypted") + extension;
            else if (cb_decrypt.IsChecked == true && cb_stegano.IsChecked == true)
                path = FilePath.Replace("_encrypted", "_decrypted");
            return path;
        }
        private void SteganoPathChange()
        {
            FilePath = dirPath + "stegano/";
            XDocument stegDoc = XDocument.Load(AESFilePath);
            XElement NameElement = stegDoc.Root.XPathSelectElement("./FileInfo/Name");
            XElement ExtensionElement = stegDoc.Root.XPathSelectElement("./FileInfo/Extension");
            FilePath = FilePath + NameElement.Value  +"_" + DateTime.Now.ToShortTimeString().Replace(':', '_') + '/';
            Directory.CreateDirectory(FilePath);
            FilePath += NameElement.Value + "_encrypted" + ExtensionElement.Value;
        }

        private void cb_stegano_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (bmpImage == null && cb_encrypt.IsChecked == true || cb_decrypt.IsChecked == true)
                {
                    OpenFileDialog ofd = new OpenFileDialog();
                    ofd.Filter = "Image Files (*.png; *.bmp)| *.png; *.bmp";
                    ofd.Title = steganoOfdTitle;
                    Nullable<bool> result = ofd.ShowDialog(this);
                    if (result == true)
                    {
                        using (Stream BmpStream = System.IO.File.Open(ofd.FileName, System.IO.FileMode.Open))
                        {
                            bmpImage = new Bitmap(Image.FromStream(BmpStream));
                            tb_SteganoFilePath.Text = ofd.FileName;
                            steganoFileName = System.IO.Path.GetFileNameWithoutExtension(ofd.FileName);
                        }
                    }
                    else
                        cb_stegano.IsChecked = false;
                }
                else
                {
                    bmpImage = null;
                    steganoFileName = null;
                    cb_stegano.IsChecked = false;
                    tb_SteganoFilePath.Text = String.Empty;
                }
                if (bmpImage != null && cb_encrypt.IsChecked == true)
                {
                    if (fileLength != 0)
                        SteganoSizeCheck();
                }
                if(bmpImage != null && cb_decrypt.IsChecked == true)
                {
                    FilePath = null;
                }
                if (cb_decrypt.IsChecked == false && cb_encrypt.IsChecked == false)
                {
                    MessageBox.Show("First select en- or decryption to activate", "", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
        private void SteganoSizeCheck()
        {
            if (!((SteganoGraphy.CountPixels(bmpImage) * 3) > (fileLength + (16-(fileLength%16)))*8))
            {
                bmpImage = null;
                cb_stegano.IsChecked = false;
                tb_SteganoFilePath.Text = String.Empty;
                MessageBox.Show("Picture to small for embedding file", "", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void ResetAll()
        {
            Layout.ClearTextbox(WindowGrid);
            privateKey = null;
            publicKey = null;
            FilePath = null;
            encryptedFilePath = null;
            AESFilePath = null;
            HASHFilePath = null;
            bmpImage = null;
            bmpHidden = null;
            steganoFileName = null;
            fileLength = 0;
            cb_stegano.IsChecked = false;
            dirPath = KuBaCrypto.Properties.Resources.MainDir;
        }

    }
}
