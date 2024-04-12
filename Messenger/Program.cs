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

        keyGen(1024).Wait();

        GetKey("toivcs@rit.edu").Wait();

        using (HttpClient client = new HttpClient()) {

            try {
                
                HttpResponseMessage response = await client.GetAsync("http://voyager.cs.rit.edu:5050/Key/toivcs@rit.edu");

                if (response.IsSuccessStatusCode) {
                    
                    string responseBody = await response.Content.ReadAsStringAsync();

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

    static async Task keyGen(int keysize) {
        Random random = new Random();
        int mult = 1;
        if(random.Next(0, 2) == 1)
            mult = -1;
        int pSize = keysize/2 + (random.Next(20, 30) * mult);
        int qSize = keysize - pSize;
        int ESize = 16;

        BigInteger p = PrimeNumbers.Prime(pSize);
        BigInteger q = PrimeNumbers.Prime(qSize);
        BigInteger NBig = BigInteger.Multiply(p, q);
        BigInteger T = BigInteger.Multiply(p-1, q-1);
        BigInteger EBig = PrimeNumbers.Prime(ESize);
        BigInteger DBig = modInverse(EBig, T);

        //Convert all numbers to properly formatted byte arrays (reversing endianness on length numbers to make them big endian)
        byte[] N = NBig.ToByteArray();
        byte[] n = BitConverter.GetBytes(BinaryPrimitives.ReverseEndianness(N.Length));
        byte[] E = EBig.ToByteArray();
        byte[] e = BitConverter.GetBytes(BinaryPrimitives.ReverseEndianness(ESize));
        byte[] D = DBig.ToByteArray();
        byte[] d = BitConverter.GetBytes(BinaryPrimitives.ReverseEndianness(D.Length));

        //Create public key byte array by block copying from other byte arrays in order
        byte[] publicBytes = new byte[e.Length + E.Length + n.Length + N.Length];
        System.Buffer.BlockCopy(e, 0, publicBytes, 0, e.Length);
        System.Buffer.BlockCopy(E, 0, publicBytes, e.Length, E.Length);
        System.Buffer.BlockCopy(n, 0, publicBytes, e.Length + E.Length, n.Length);
        System.Buffer.BlockCopy(N, 0, publicBytes, e.Length + E.Length + n.Length, N.Length);

        //Create private key byte array by block copying from other byte arrays in order
        byte[] privateBytes = new byte[d.Length + D.Length + n.Length + N.Length];
        System.Buffer.BlockCopy(d, 0, privateBytes, 0, d.Length);
        System.Buffer.BlockCopy(D, 0, privateBytes, d.Length, D.Length);
        System.Buffer.BlockCopy(n, 0, privateBytes, d.Length + D.Length, n.Length);
        System.Buffer.BlockCopy(N, 0, privateBytes, d.Length + D.Length + n.Length, N.Length);

        PublicKey publicKey = new PublicKey(Convert.ToBase64String(publicBytes));
        publicKey.WriteToFile();

        PrivateKey privateKey = new PrivateKey(Convert.ToBase64String(privateBytes));
        privateKey.WriteToFile();

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

                    int n = BinaryPrimitives.ReverseEndianness(BitConverter.ToInt32(keyBytes, 4 + e));

                    byte[] NArr = new byte[n];
                    Array.Copy(keyBytes, 4 + e + 4, NArr, 0, n);
                    BigInteger N = new BigInteger(NArr);       
                    
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

    static BigInteger modInverse(BigInteger a, BigInteger b)
    {
        BigInteger i = b, v = 0, d = 1;
        while (a>0) {
            BigInteger z = i/a, x = a;
            a = i % x;
            i = x;
            x = d;
            d = v - z*x;
            v = x;
        }
        v %= b;
        if (v<0) v = (v+b) % b;
        return v;
    }

}

class Message {
    public string email { get; set; }
    public string content { get; set; }

}

class PrivateKey {
    public string key { get; set; }
    public ArrayList emails;

    public void AddEmail(string email) {
        emails.Add(email);
    }

    public PrivateKey RetrieveKeyInfo() {
        string keyInfo = File.ReadAllText("private.key");
        return JsonSerializer.Deserialize<PrivateKey>(keyInfo);
    }

    public PrivateKey(string key) {
        this.key = key;
        emails = new ArrayList();
    }

    public void WriteToFile() {
        File.WriteAllText("private.key", JsonSerializer.Serialize(this));
    }

}

class PublicKey {
    public string email { get; set; }
    public string key { get; set; }

    public PublicKey(string key) {
        this.key = key;
        this.email = "";
    }

    public PublicKey RetrieveKeyInfo() {
        string keyInfo = File.ReadAllText("public.key");
        return JsonSerializer.Deserialize<PublicKey>(keyInfo);
    }

    public void WriteToFile() {
        File.WriteAllText("public.key", JsonSerializer.Serialize(this));
    }
}

class PrimeNumbers {

    static int primeBits = 0;
    static int primeMaxCount = 0;
    static int primeCount = 0;
    static BigInteger[] primeNumbers = new BigInteger[0];

    public static BigInteger Prime(int bits) {
        primeBits = bits;
        primeMaxCount = 1;
        primeNumbers = new BigInteger[primeMaxCount];
        for(int x = 0; x < Environment.ProcessorCount * 2; x++) {
            Thread thread = new Thread(new ThreadStart(GeneratePrimes));
            thread.Start();
        }

        foreach(BigInteger num in primeNumbers) {
            if(num.IsZero) {
                primeCount = 0;
                Prime(bits);
                return primeNumbers[0];
            }
        }

        return primeNumbers[0];
    }

    /// <summary>
    /// A Stack Overflow Aided version of the Miller-Rabin primality test
    /// </summary>
    /// <param name="bigInt">Candidate number</param>
    /// <returns>True if number is prime (to a reasonable degree of certainty), 
    /// false otherwise</returns>
    private static bool isPrime(BigInteger bigInt) {
        if(bigInt == 2 || bigInt == 3)
            return true;
        if(bigInt < 2 || bigInt % 2 == 0)
            return false;

        BigInteger d = bigInt - 1;
        int s = 0;

        while(d % 2 == 0)
        {
            d /= 2;
            s += 1;
        }

        RandomNumberGenerator rng = RandomNumberGenerator.Create();
        byte[] bytes = new byte[bigInt.ToByteArray().LongLength];
        
        BigInteger a;

        for(int i = 0; i < 10; i++)
        {
            do {
                rng.GetBytes(bytes);
                a = new BigInteger(bytes);
            } while(a < 2 || a >= bigInt - 2);

            BigInteger x = BigInteger.ModPow(a, d, bigInt);
            if(x == 1 || x == bigInt - 1)
                continue;

            for(int r = 1; r < s; r++)
            {
                x = BigInteger.ModPow(x, 2, bigInt);
                if(x == 1)
                    return false;
                if(x == bigInt - 1)
                    break;
            }

            if(x != bigInt - 1)
                return false;
        }

        return true;
    }


    private static void GeneratePrimes() {
        while(primeCount < primeMaxCount) {
            BigInteger candidate = GenerateNum(primeBits);
            if(isPrime(candidate)) {
                int index = primeCount;
                if(index < primeMaxCount) {
                    primeNumbers[index] = candidate;
                }
                primeCount++;
            }
        }
    }

    /// <summary>
    /// Generates a random BigInteger
    /// </summary>
    /// <param name="bits"></param>
    /// <returns></returns>
    private static BigInteger GenerateNum(int bits) {
        RandomNumberGenerator rng = RandomNumberGenerator.Create();
        byte[] bytes = new byte[bits/8];
        rng.GetBytes(bytes);
        return new BigInteger(bytes);
    }

}