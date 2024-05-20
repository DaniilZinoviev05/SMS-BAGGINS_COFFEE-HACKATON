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
using System.Text.RegularExpressions;

namespace WindowsFormsApp1
{
    public partial class Form1 : Form
    {
        private Timer countdownTimer;
        private int countdownTime;
        private string msg;
        public Form1()
        {
            InitializeComponent();

            countdownTimer = new Timer();
            countdownTimer.Interval = 1000;
            countdownTimer.Tick += CountdownTimer_Tick;
            maskedTextBox2.Visible = false;
        }

        private async void button1_Click(object sender, EventArgs e)
        {
            var ip = "192.168.8.1"; // IP 
            var phone = maskedTextBox1.Text.Trim().Replace(" ", "").Replace("-", "").Replace("(", "").Replace(")", "");

            var msg = GenerateCode();

            var result = await SendSMSAsync(ip, phone, msg);

            if (result)
            {
                MessageBox.Show("Сообщение успешно отправлено");
                maskedTextBox1.Visible = false;
                maskedTextBox2.Visible = true;
                button1.Enabled = false;
                countdownTime = 60;
                countdownTimer.Start();
            }
            else
            {
                MessageBox.Show("Ошибка при отправлении.");
            }
        }

        private string GenerateCode()
        {
            Random random = new Random();

            var randomNumber = random.Next(1000, 10000);

            return randomNumber.ToString();
        }

        private bool Mask()
        {
            return maskedTextBox2.MaskFull && maskedTextBox2.Text.Trim().Replace(" ", "") == msg;
        }

        private void CountdownTimer_Tick(object sender, EventArgs e)
        {
            countdownTime--;

            button1.Text = $"Пожалуйста, подождите... {countdownTime}s";

            if (countdownTime <= 0)
            {
                countdownTimer.Stop();
                button1.Enabled = true;
                button1.Text = "Отправить SMS";
            }
            // Проверка maskedTextBox2
            if (Mask())
            {
                countdownTimer.Stop();
                MessageBox.Show("Добро пожаловать!");
                button1.Enabled = true;
                button1.Text = "Получить код по SMS";
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

        private void label1_Click(object sender, EventArgs e)
        {
            throw new System.NotImplementedException();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            //throw new System.NotImplementedException();
        }

        private void maskedTextBox1_MaskInputRejected(object sender, MaskInputRejectedEventArgs e)
        {

        }

        private void maskedTextBox2_MaskInputRejected(object sender, MaskInputRejectedEventArgs e)
        {

        }
    }
}