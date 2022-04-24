using System.Security.Cryptography;

namespace LiveDetect.Service.Common
{
    public class Util
    {
        public static string EncryptString(string data, string secret)
        {
            byte[] byKey = System.Text.ASCIIEncoding.ASCII.GetBytes(secret);
            byte[] byIV = System.Text.ASCIIEncoding.ASCII.GetBytes(secret);

            DESCryptoServiceProvider cryptoProvider = new DESCryptoServiceProvider();
            cryptoProvider.Mode = CipherMode.ECB;
            int i = cryptoProvider.KeySize;
            MemoryStream ms = new MemoryStream();
            CryptoStream cst = new CryptoStream(ms, cryptoProvider.CreateEncryptor(byKey, byIV), CryptoStreamMode.Write);

            StreamWriter sw = new StreamWriter(cst);
            sw.Write(data);
            sw.Flush();
            cst.FlushFinalBlock();
            sw.Flush();
            return Convert.ToBase64String(ms.GetBuffer(), 0, (int)ms.Length);
        }

        public static string Decrypt(string cryptedString, string secret)
        {
            string result = string.Empty;
            try
            {
                byte[] byKey = System.Text.ASCIIEncoding.ASCII.GetBytes(secret);
                byte[] byIV = System.Text.ASCIIEncoding.ASCII.GetBytes(secret);

                if (String.IsNullOrEmpty(cryptedString))
                {
                    throw new ArgumentNullException
                       ("The string which needs to be decrypted can not be null.");
                }
                DESCryptoServiceProvider cryptoProvider = new DESCryptoServiceProvider();
                cryptoProvider.Mode = CipherMode.ECB;
                MemoryStream memoryStream = new MemoryStream
                        (Convert.FromBase64String(cryptedString));
                CryptoStream cryptoStream = new CryptoStream(memoryStream,
                    cryptoProvider.CreateDecryptor(byKey, byIV), CryptoStreamMode.Read);
                StreamReader reader = new StreamReader(cryptoStream);
                result = reader.ReadToEnd();
            }
            catch(Exception ex)
            {
                return String.Empty;
            }
            finally
            {

            }

            return result;
        }
    }

    //public static string GetBase64FromImage(string imagefile)
    //{
    //    string strbaser64 = "";
    //    try
    //    {
    //        Bitmap bmp = new Bitmap(imagefile);
    //        MemoryStream ms = new MemoryStream();
    //        bmp.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg);
    //        byte[] arr = new byte[ms.Length];
    //        ms.Position = 0;
    //        ms.Read(arr, 0, (int)ms.Length);
    //        ms.Close();
    //        strbaser64 = Convert.ToBase64String(arr);
    //    }
    //    catch (Exception)
    //    {
    //        throw new Exception("Something wrong during convert!");
    //    }
    //    return strbaser64;
    //}

}
