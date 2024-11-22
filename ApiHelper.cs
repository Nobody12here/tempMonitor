using Newtonsoft.Json;
using System.Net;
using System.Text;
using System.Net.Http;
using System.Threading.Tasks;
using System;


class ApiHelper
{
    static HttpClient client = new HttpClient();
    public static string URL = "http://localhost:3000/api";
    public static async Task<HttpStatusCode> PostData(string url, StringContent data)
    {
        Console.WriteLine("Calling");
        try
        {
            HttpResponseMessage message = await client.PostAsync((URL + url), data);
            string response = await message.Content.ReadAsStringAsync();
            return message.StatusCode;
        }
        catch (HttpRequestException e)
        {
            Console.WriteLine("Error : ", e.Message);
            return HttpStatusCode.InternalServerError;
        }
    }
    public static async Task<dynamic> VerifyDeviceAsync(string email, string otp)
    {
        using (HttpClient client = new HttpClient())
        {
            var apiUrl = URL + "/verifyDevice";
            var requestData = new
            {
                Email = email,
                OTP = otp
            };

            string jsonData = JsonConvert.SerializeObject(requestData);
            var content = new StringContent(jsonData, Encoding.UTF8, "application/json");

            try
            {
                HttpResponseMessage response = await client.PostAsync(apiUrl, content);
                if (response.IsSuccessStatusCode)
                {
                    // Assuming the API returns a JSON object with a success property
                    string responseContent = await response.Content.ReadAsStringAsync();
                    dynamic jsonResponse = JsonConvert.DeserializeObject(responseContent);
                    Console.WriteLine(jsonResponse);
                    return jsonResponse;
                }
                else
                {
                    Console.WriteLine($"Error: {response.StatusCode} - {response.ReasonPhrase}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
                return false;
            }
        }
    }

}
