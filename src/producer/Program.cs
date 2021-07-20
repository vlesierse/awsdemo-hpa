using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

// get enviroment variables
var apiUrl = args.Length > 0 ? args[0] : "http://a1f789f1266f645418a9d999d59b31d0-35973386.eu-west-1.elb.amazonaws.com";
var orderRate = args.Length > 1 ? Int32.Parse(args[1]) : 1;

// create new http client
var client = new HttpClient();
client.BaseAddress = new Uri(apiUrl);

while (!Console.KeyAvailable)
{
    try {
        var response = await client.PostAsync("order", new StringContent("{\"orderId\":\"" + Guid.NewGuid() + "\"}", Encoding.UTF8, "application/json"));
        Console.WriteLine("Created order: " + response.StatusCode);
    }
    catch(Exception ex) {
        Console.WriteLine("Failed: " + ex.Message);
    }
    await Task.Delay(1000 / orderRate);
}
