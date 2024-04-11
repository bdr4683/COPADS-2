using System;
using System.Numerics;
using System.IO;
using System.Threading;
using System.Security.Cryptography;
using System.Data.SqlTypes;
using System.Collections;
using System.Reflection.Metadata;
using System.Dynamic;
using System.Diagnostics;
using System.Net.Http;
using System.Text.Json;
using System.Buffers.Binary;

class Program {

    static void Main(string[] args) {
        AsyncMain().Wait();
    }

    static async Task AsyncMain() {

        GetKey("toivcs@rit.edu").Wait();

        using (HttpClient client = new HttpClient()) {

            try {
                
                HttpResponseMessage response = await client.GetAsync("http://voyager.cs.rit.edu:5050/Key/toivcs@rit.edu");

                if (response.IsSuccessStatusCode) {
                    
                    string responseBody = await response.Content.ReadAsStringAsync();

                    Console.WriteLine(responseBody);

                }
                else{
                    Console.WriteLine("Failed");
                }

            }
            catch (HttpRequestException e) {
                //TODO
                Console.WriteLine("Error: " + e.Message);
            }
        }

    }

    static async Task GetKey(string user) {
        using (HttpClient client = new HttpClient()) {

            try {
                
                HttpResponseMessage response = await client.GetAsync("http://voyager.cs.rit.edu:5050/Key/" + user);

                if (response.IsSuccessStatusCode) {
                    
                    string responseBody = await response.Content.ReadAsStringAsync();

                    JsonDocument jsonDocument = JsonDocument.Parse(responseBody);

                    // Get the root element
                    JsonElement root = jsonDocument.RootElement;

                    string email = "";
                    // Access the values
                    if (root.TryGetProperty("email", out JsonElement emailProp)) {
                        email = emailProp.GetString();
                    }
                    else {
                        Console.WriteLine("Err: Email not found");
                        return;
                    }

                    string key = "";
                    // Access the values
                    if (root.TryGetProperty("key", out JsonElement keyProp)) {
                        key = keyProp.GetString();
                    }
                    else {
                        Console.WriteLine("Err: Key not found");
                        return;
                    }
                    
                    byte[] keyBytes = Convert.FromBase64String(key);

                    int e = BinaryPrimitives.ReverseEndianness(BitConverter.ToInt32(keyBytes, 0));

                    byte[] EArr = new byte[e];
                    Array.Copy(keyBytes, 4, EArr, 0, e);
                    BigInteger E = new BigInteger(EArr);
                    Console.WriteLine(E);

                    int n = BinaryPrimitives.ReverseEndianness(BitConverter.ToInt32(keyBytes, 4 + e));

                    byte[] NArr = new byte[n];
                    Array.Copy(keyBytes, 4 + e, NArr, 0, n);
                    BigInteger N = new BigInteger(NArr);
                    Console.WriteLine(N);

                }
                else{
                    Console.WriteLine("Failed");
                }

            }
            catch (HttpRequestException e) {
                //TODO
                Console.WriteLine("Error: " + e.Message);
                return;
            }
        }
    }

}