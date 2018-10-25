using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentFTP;
using Microsoft.AspNetCore.Mvc;

namespace GmailFtpTranferApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TransferController : ControllerBase
    {
        private static GmailApiService GmailService { get; set; } = new GmailApiService();
        private static FtpService FtpService { get; set; } = new FtpService();
        private static bool WatchRunning { get; set; } = false;
        private static List<string> TransferredFilenames = new List<string>();

        [HttpGet]
        public IActionResult Get()
        {
            var thread = new Thread(WatchForNewMessages);
            thread.Start();
            return Accepted();
        }

        public static void WatchForNewMessages(){
            if(!WatchRunning){
                WatchRunning = true;
                var stopWatch = new Stopwatch();
                stopWatch.Start();
                while(stopWatch.ElapsedMilliseconds < 600000){
                    TransferAttachmentToFTP();
                    Thread.Sleep(10000);
                }
                WatchRunning = false;
                TransferredFilenames.Clear();
            }
        }

        public static void TransferAttachmentToFTP(){
            bool wroteNewFile = false;
            var request = GmailService.service.Users.Messages.List("me");

            var messages = request.Execute().Messages;
            List<string> messageIds = new List<string>();

            foreach(var message in messages){
                messageIds.Add(message.Id);
            }

            foreach(var id in messageIds){
                var messageRequest = GmailService.service.Users.Messages.Get("me", id);
                var result = messageRequest.Execute().Payload;

                foreach(var part in result.Parts){
                    string attId = part.Body.AttachmentId;
                    if(!String.IsNullOrEmpty(part.Body.AttachmentId)){
                        if(!TransferredFilenames.Any(x => x == part.Filename)){
                            if(part.Filename.EndsWith(".xlsx") || part.Filename.EndsWith(".xls")){
                                var attachPart = GmailService.service.Users.Messages.Attachments.Get("me", id, attId).Execute();

                                // Converting from RFC 4648 base64 to base64url encoding
                                // see http://en.wikipedia.org/wiki/Base64#Implementations_and_history
                                string attachData = attachPart.Data.Replace('-', '+');
                                attachData = attachData.Replace('_', '/');

                                byte[] data = Convert.FromBase64String(attachData);
                                var ftpResult = FtpService.FtpClient.Upload(data, "/Output/" + part.Filename, FtpExists.Skip);
                                TransferredFilenames.Add(part.Filename);
                                wroteNewFile = true;
                            }
                        }
                    }
                }
            }

            if(wroteNewFile){
                Thread.Sleep(5000);
                var client = new HttpClient();
                var byteArray = Encoding.ASCII.GetBytes("roboticswaypointOracle@gmail.com:adminforwaypointO1");
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));

                var result = client.GetAsync("https://integrationexample-waypointoracle.integration.ocp.oraclecloud.com/ic/api/integration/v1/flows/rest/UIPATHORCHESTRATOR/1.0/startrobot").Result;
            }
        }
    }
}