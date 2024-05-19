using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;
using RestSharp;
using RestSharp.Authenticators;
using HtmlAgilityPack;

namespace WindowsFormsApp1
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        public Form1 (Form2 form2)
        {
            InitializeComponent();
        }
        
        public string generatedCode 
        { 
            get 
            {
                return generatedCode;
            } 

            private set 
            {
                generatedCode = value; 
            } 
        }

        public string inputCode
        {
            get
            {
                return inputCode;
            }

            private set
            {
                inputCode = value;
            }
        }

        private async Task<bool> SendSMSAsync(string ip, string phone, string msg)
        {
            try
            {
                var firstCookie = await GetInitialCookieAsync(ip);
                if (firstCookie != null)
                {
                    var (secondCookie, token) = await GetTokenAsync(ip, firstCookie);
                    if (!string.IsNullOrEmpty(token))
                    {
                        return await SendSMSRequestAsync(ip, phone, msg, secondCookie, token);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}"); // Вывод ошибки
            }

            return false;
        }
        
        private string GenerateCode()
        {
            Random random = new Random();

            var randomNumber = random.Next(1000, 10000);

            return randomNumber.ToString();
        }

        private bool ApproveCode()
        {

            if (generatedCode == inputCode)
            {
                return true;
            }
            else
            {
                return false;
            }
        }


        private async Task<Cookie> GetInitialCookieAsync(string ip)
        {
            var cookieContainer = new CookieContainer();
            var uri = new Uri($"http://{ip}/html/index.html");
            using (var handler = new HttpClientHandler { CookieContainer = cookieContainer })
            using (var client = new HttpClient(handler))
            {
                await client.GetAsync(uri);
                var cookies = cookieContainer.GetCookies(uri);
                return cookies.Count > 0 ? cookies[0] : null;
            }
        }

        private async Task<(Cookie, string)> GetTokenAsync(string ip, Cookie initialCookie)
        {
            var cookieContainer = new CookieContainer();
            cookieContainer.Add(initialCookie);
            var uri = new Uri($"http://{ip}/html/smsinbox.html");
            using (var handler = new HttpClientHandler { CookieContainer = cookieContainer })
            using (var client = new HttpClient(handler))
            {
                var html = await client.GetStringAsync(uri);
                var cookies = cookieContainer.GetCookies(uri);
                var secondCookie = cookies.Count > 0 ? cookies[0] : null;

                var doc = new HtmlAgilityPack.HtmlDocument();
                doc.LoadHtml(html);
                var metaTags = doc.DocumentNode.SelectNodes("//meta");
                var token = metaTags?.Count >= 2 ? metaTags[1].GetAttributeValue("content", "") : string.Empty;
                return (secondCookie, token);
            }
        }
        private async Task<bool> SendSMSRequestAsync(string ip, string phone, string msg, Cookie secondCookie, string token)
        {
            var msgLength = msg.Length;
            var time = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            var sms = $"<?xml version='1.0' encoding='UTF-8'?><request><Index>-1</Index><Phones><Phone>{phone}</Phone></Phones><Sca/><Content>{msg}</Content><Length>{msgLength}</Length><Reserved>1</Reserved><Date>{time}</Date></request>";
            var uri = new Uri($"http://{ip}/api/sms/send-sms");
            var client = new RestClient(uri);

            var request = new RestRequest(uri, Method.Post);
            request.AddHeader("__RequestVerificationToken", token);
            request.AddCookie(secondCookie.Name, secondCookie.Value, "/", uri.Host);
            request.AddHeader("Content-Type", "text/xml");
            request.AddHeader("X-Requested-With", "XMLHttpRequest");
            request.AddParameter("text/xml", sms, ParameterType.RequestBody);

            var response = await client.ExecuteAsync(request);

            if (response.IsSuccessful)
            {
                var xmlDoc = new XmlDocument();
                xmlDoc.LoadXml(response.Content);
                var responseElement = xmlDoc.GetElementsByTagName("response");
                if (responseElement.Count > 0)
                {
                    var resultOK = responseElement[0].InnerText.ToLower();
                    return resultOK == "ok";
                }
            }

            return false;
        }

        
        private async void button1_Click_1(object sender, EventArgs e)
        {
            var ip = "192.168.8.1"; // IP 
            var phone = "+79113172105"; // Номер телефона
            var msg = GenerateCode(); // Сообщение 
            generatedCode = msg;

            var result = await SendSMSAsync(ip, phone, msg);
            if (result)
            {
                MessageBox.Show("СМС усшепно отпралена!");
                Form2 newForm = new Form2();
                newForm.Show();
            }
            else
            {
                MessageBox.Show("Ошибка при отправлении.");
            }
        }
    }
}