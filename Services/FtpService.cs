using System.Net;
using FluentFTP;

namespace GmailFtpTranferApi
{
    public class FtpService
    {
        public FtpClient FtpClient;
        public FtpService()
        {
            FtpClient = new FtpClient("52.6.0.127");
            FtpClient.Credentials = new NetworkCredential("justinsbestbusiness/justin.blau8@gmail.com", "Welcome1!");
        }
    }
}